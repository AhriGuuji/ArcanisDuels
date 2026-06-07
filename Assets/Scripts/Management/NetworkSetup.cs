using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class NetworkSetup : MonoBehaviour
{
    [SerializeField] private int    _maxPlayers = 2;
    [SerializeField] private string _joinCode   = "";
    [SerializeField] private string _sceneName  = "Battle";

    public List<PlayerInfo> Players    { get; private set; }
    public bool             CanStart   { get; private set; }
    public ulong            ClientID   => NetworkManager.Singleton.LocalClientId;

    private Dictionary<ulong, string> _pendingPlayerData = new();
    private RelayHostData              _relayData;
    private UnityTransport             _transport;
    private ShowLobbyCode              _lobbyCode;
    private bool                       _isServer;
    private bool                       _isRelay;
    private bool                       _isAuthenticated;
    private string                     _characterName;
    private List<int>                  _deckIds;

    //Lifecycle

    private async void Start()
    {
        try
        {
            //Inícia os serviços do Unity, para fins de login
            await UnityServices.InitializeAsync();

            //Se tiver já logado, procede. Se não, tenta outra forma
            if (AuthenticationService.Instance.IsSignedIn)
            {
                _isAuthenticated = true;
                ProceedWithNetworkSetup();
            }
            else
            {
                await AutoSignIn();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    //Authentication

    private async Task AutoSignIn()
    {
        // Se tiver os dados nos PlayerPrefs(Tenho noção da falta de encriptação 
        // dos PlayerPrefs, mas serviu mais como algo experimental que ficou 
        // implementado sem tempo de modificar), tenta fazer login
        if (PlayerPrefs.HasKey("PlayerName") && PlayerPrefs.HasKey("Password"))
        {
            string username = PlayerPrefs.GetString("PlayerName");
            string password = PlayerPrefs.GetString("Password");
            await SignInWithUsernamePasswordAsync(username, password);
        }

        // Se não tiver autenticado até este momento, faz anonimamente.
        if (!_isAuthenticated)
            await SignInAnonymously();

        ProceedWithNetworkSetup();
    }

    private async Task SignInAnonymously()
    {
        try
        {
            //Login anónimo
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _isAuthenticated = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Anonymous sign-in failed: {e.Message}");
        }
    }

    public async Task SignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            //Registar usuário
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            SaveCredentials(username, password);
            _isAuthenticated = true;
        }
        catch (Exception ex)
        {
            HandleAuthError(ex);
        }
    }

    public async Task SignInWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            //Faz log do jogador
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            SaveCredentials(username, password);
            _isAuthenticated = true;
        }
        catch (AuthenticationException)
        {
            //Caso jogador não exista, faz signUp
            await SignUpWithUsernamePasswordAsync(username,password);
        }
        catch (Exception ex)
        {
            HandleAuthError(ex);
        }
    }

    // Salva as credenciais no PlayerPrefs. Repito, eu sei que é errado.
    private void SaveCredentials(string username, string password)
    {
        PlayerPrefs.SetString("PlayerName", username);
        PlayerPrefs.SetString("Password", password);
        PlayerPrefs.Save();
    }

    private void HandleAuthError(Exception ex)
    {
        Debug.LogError($"Authentication failed: {ex.Message}");
        _isAuthenticated = false;
        RetrieveScene("MainMenu");
    }

    //Network Setup

    private void ProceedWithNetworkSetup()
    {
        //Caso voltar atrás da cena não funcione.
        if (!_isAuthenticated)
        {
            Debug.LogError("Cannot proceed — not authenticated!");
            return;
        }

        DontDestroyOnLoad(this);
        
        Players        = new List<PlayerInfo>();            // PlayerData
        CanStart       = false;                             // Booleano que vai avisar o Battle Manager 
                                                            // que pode começar só para ter a certeza
        _lobbyCode     = GetComponent<ShowLobbyCode>();     // Contem o código da sala
        _transport     = GetComponent<UnityTransport>();    // Transportador do Unity
        _characterName = SelectionData.prefabName;          // Informação local do personagem escolhido
        _deckIds       = SelectionData.deck;                // Informação local do deck escolhido
        _isServer      = SelectionData.isServer;            // Informação local se o jogador decidiu começar como Host
        _isRelay       = _transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport; // Para Relay

        if (!_isServer)
            _joinCode = SelectionData.code;                 // Código escolhido localmente

        if (_isServer)
            StartCoroutine(StartAsHostCR(_characterName, _deckIds)); // Começa como Host
        else
            StartCoroutine(StartAsClientCR(_characterName, _deckIds)); // Começa como Client
    }

    // Server

    private IEnumerator StartAsHostCR(string character, List<int> deckIds)
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        _transport.enabled     = true;

        yield return null;

        if (_isRelay)
        {
            yield return SetupRelayAsServer();
            if (_relayData == null) yield break;
        }

        string hostData = $"{character}|{string.Join(",", deckIds)}";
        _pendingPlayerData[NetworkManager.ServerClientId] = hostData;

        networkManager.NetworkConfig.ConnectionApproval   =  true;
        networkManager.ConnectionApprovalCallback         += ApprovalCheck;

        if (networkManager.StartHost())
        {
            networkManager.OnClientConnectedCallback  += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            OnClientConnected(NetworkManager.ServerClientId);
        }
        else
        {
            Debug.LogError($"Failed to start server on port {_transport.ConnectionData.Port}");
        }
    }

    private IEnumerator SetupRelayAsServer()
    {
        var allocationTask = CreateAllocationAsync(_maxPlayers);
        yield return new WaitUntil(() => allocationTask.IsCompleted);

        if (allocationTask.Exception != null)
        {
            Debug.LogError("Relay allocation failed: " + allocationTask.Exception);
            yield break;
        }

        Allocation allocation = allocationTask.Result;
        _relayData = new RelayHostData();

        foreach (var endpoint in allocation.ServerEndpoints)
        {
            _relayData.IPv4Address = endpoint.Host;
            _relayData.Port        = (ushort)endpoint.Port;
            break;
        }

        _relayData.AllocationID      = allocation.AllocationId;
        _relayData.AllocationIDBytes = allocation.AllocationIdBytes;
        _relayData.ConnectionData    = allocation.ConnectionData;
        _relayData.Key               = allocation.Key;

        var joinCodeTask = GetJoinCodeAsync(_relayData.AllocationID);
        yield return new WaitUntil(() => joinCodeTask.IsCompleted);

        if (joinCodeTask.Exception != null)
        {
            Debug.LogError("Failed to get join code: " + joinCodeTask.Exception);
            _relayData = null;
            yield break;
        }

        _relayData.JoinCode = joinCodeTask.Result;
        _lobbyCode.SetCode(_relayData.JoinCode);
        _transport.SetRelayServerData(_relayData.IPv4Address, _relayData.Port, _relayData.AllocationIDBytes, _relayData.Key, _relayData.ConnectionData);
    }

    private async Task<Allocation> CreateAllocationAsync(int maxPlayers)
    {
        try
        {
            return await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxPlayers);
        }
        catch (Exception e)
        {
            Debug.LogError("Error creating allocation: " + e);
            throw;
        }
    }

    private async Task<string> GetJoinCodeAsync(Guid allocationID)
    {
        try
        {
            return await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocationID);
        }
        catch (Exception e)
        {
            Debug.LogError("Error getting join code: " + e);
            throw;
        }
    }

    //Client

    private IEnumerator StartAsClientCR(string characterName, List<int> deckIds)
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        _transport.enabled     = true;

        string combinedData = $"{characterName}|{string.Join(",", deckIds)}";
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(combinedData);

        yield return null;

        if (_isRelay)
        {
            yield return SetupRelayAsClient();
            if (_relayData == null) yield break;
        }

        if (networkManager.StartClient())
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private IEnumerator SetupRelayAsClient()
    {
        var joinTask = JoinAllocationAsync(_joinCode);
        yield return new WaitUntil(() => joinTask.IsCompleted);

        if (joinTask.Exception != null)
        {
            Debug.LogError("Failed to join relay: " + joinTask.Exception);
            yield break;
        }

        var allocation = joinTask.Result;
        _relayData = new RelayHostData();

        foreach (var endpoint in allocation.ServerEndpoints)
        {
            _relayData.IPv4Address = endpoint.Host;
            _relayData.Port        = (ushort)endpoint.Port;
            break;
        }

        _relayData.AllocationID          = allocation.AllocationId;
        _relayData.AllocationIDBytes     = allocation.AllocationIdBytes;
        _relayData.ConnectionData        = allocation.ConnectionData;
        _relayData.HostConnectionData    = allocation.HostConnectionData;
        _relayData.Key                   = allocation.Key;

        _transport.SetRelayServerData(_relayData.IPv4Address, _relayData.Port, _relayData.AllocationIDBytes, _relayData.Key, _relayData.ConnectionData, _relayData.HostConnectionData);
    }

    private async Task<JoinAllocation> JoinAllocationAsync(string joinCode)
    {
        try
        {
            return await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError("Error joining allocation: " + e);
            throw;
        }
    }

    //Callbacks

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        ulong clientId = request.ClientNetworkId;

        if (request.Payload == null || request.Payload.Length == 0)
        {
            response.Approved = false;
            return;
        }

        string   fullData      = System.Text.Encoding.UTF8.GetString(request.Payload);
        string[] parts         = fullData.Split('|');
        List<int> deckIds      = parts[1].Split(',').Select(int.Parse).ToList();

        _pendingPlayerData[clientId] = fullData;

        if (deckIds.Count != 20)
        {
            response.Approved = false;
            response.Reason   = "Invalid deck size!";
            return;
        }

        response.Approved            = true;
        response.CreatePlayerObject  = false;
        response.Pending             = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!_pendingPlayerData.TryGetValue(clientId, out string characterData)) return;

        string[]  parts         = characterData.Split('|');
        string    characterName = parts[0];
        List<int> deckIds       = parts[1].Split(',').Select(int.Parse).ToList();

        PlayerInfo player = new(clientId, characterName, deckIds);

        if (clientId == NetworkManager.ServerClientId)
            Players.Insert(0, player);
        else
            Players.Add(player);

        if (Players.Count == _maxPlayers)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.LoadScene(_sceneName, LoadSceneMode.Single);
        }
    }

    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        
        if (clientsTimedOut.Count > 0)
        {
            Debug.LogError("Some clients timed out loading the scene!");
            return;
        }
        
        CanStart = true;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} disconnected.");
        RetrieveScene("MainMenu");
    }

    public void RetrieveScene(string scene)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);
        Destroy(gameObject);
    }
}