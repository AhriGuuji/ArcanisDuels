using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using UnityEditor;

using System.Linq;
using Unity.Services.Core;

using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;

using Debug = UnityEngine.Debug;

public class NetworkSetup : MonoBehaviour
{
    [SerializeField] private int                maxPlayers = 2;
    [SerializeField] private string             joinCode = "";
    [SerializeField] private string             sceneName = "Battle";
    //[SerializeField] private bool               enableAnalytics;
    public List<PlayerInfo> Players { get; private set; }
    private Dictionary<ulong, string> pendingPlayerCharacters = new Dictionary<ulong, string>();
    public ulong ClientID => NetworkManager.Singleton.LocalClientId;


    public class RelayHostData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationID;
        public byte[] AllocationIDBytes;
        public byte[] ConnectionData;
        public byte[] HostConnectionData;
        public byte[] Key;
    }
    private RelayHostData relayData;

    private bool isServer = false;
    private UnityTransport transport;
    private ShowLobbyCode lobbyCode;
    private bool isRelay = false;

    public bool CanStart {get; private set;}
    private string characterName;
    private List<int> deckIds;
    private bool isAuthenticated = false;
    
    async void Start()
    {
        // Initialize Unity Services FIRST
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized!");
            
            // Check if already signed in
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log($"Already signed in as: {AuthenticationService.Instance.PlayerId}");
                isAuthenticated = true;
                ProceedWithNetworkSetup();
            }
            else
            {
                // Try auto-sign in with saved credentials
                await AutoSignIn();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize: {e.Message}");
        }
    }
    
    private async Task AutoSignIn()
    {
        // Check for saved credentials (from your LocalLogin)
        if (PlayerPrefs.HasKey("PlayerName") && PlayerPrefs.HasKey("Password"))
        {
            string username = PlayerPrefs.GetString("PlayerName");
            string password = PlayerPrefs.GetString("Password");
            
            await SignInWithUsernamePasswordAsync(username, password);
        }
        
        if (!isAuthenticated)
        {
            // Fallback to anonymous sign in
            await SignInAnonymously();
        }
        
        ProceedWithNetworkSetup();
    }
    
    private async Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Signed in anonymously as: {AuthenticationService.Instance.PlayerId}");
            isAuthenticated = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Anonymous sign in failed: {e.Message}");
        }
    }
    
    public async Task SignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("SignUp successful!");
            
            // Save credentials after successful signup
            PlayerPrefs.SetString("PlayerName", username);
            PlayerPrefs.SetString("Password", password);
            PlayerPrefs.Save();
            
            isAuthenticated = true;
        }
        catch (AuthenticationException ex)
        {
            HandleAuthError(ex);
        }
        catch (RequestFailedException ex)
        {
            HandleAuthError(ex);
        }
    }
    
    public async Task SignInWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("SignIn successful!");
            
            // Save credentials on successful login
            PlayerPrefs.SetString("PlayerName", username);
            PlayerPrefs.SetString("Password", password);
            PlayerPrefs.Save();
            
            isAuthenticated = true;
        }
        catch (AuthenticationException ex)
        {
            HandleAuthError(ex);
        }
        catch (RequestFailedException ex)
        {
            HandleAuthError(ex);
        }
    }
    
    private void HandleAuthError(Exception ex)
    {
        Debug.LogError($"Authentication failed: {ex.Message}");
        isAuthenticated = false;
        
        // You can show UI message here
        // UIManager.Instance.ShowError($"Login failed: {ex.Message}");
    }
    
    private void ProceedWithNetworkSetup()
    {
        if (!isAuthenticated)
        {
            Debug.LogError("Cannot proceed - not authenticated!");
            return;
        }
        
        // Your existing network setup code here
        // (loading scene, setting up relay, etc.)
        Debug.Log("Authentication complete! Proceeding with network setup...");
        
        DontDestroyOnLoad(this);
        Players = new List<PlayerInfo>();
        lobbyCode = GetComponent<ShowLobbyCode>();
        
        CanStart = false;

        DontDestroyOnLoad(this);

        Players = new ();

        lobbyCode = GetComponent<ShowLobbyCode>();
        
        characterName = SelectionData.prefabName;
        deckIds = SelectionData.deck;
    
        if (SelectionData.isServer)
        {
            isServer = SelectionData.isServer;
        }
        else
        {
            isServer = false;
            joinCode = SelectionData.code;
        }

        transport = GetComponent<UnityTransport>();

        if (transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
        {
            isRelay = true;
        }

        if (isServer)
            StartCoroutine(StartAsServerCR(characterName, deckIds));
        else
            StartCoroutine(StartAsClientCR(characterName, deckIds));
    }

    IEnumerator StartAsServerCR(string character, List<int> deckIds)
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;

        // Wait a frame for setups to be done
        yield return null;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        if (isRelay)
        {
            var allocationTask = CreateAllocationAsync(maxPlayers);
            yield return new WaitUntil(() => allocationTask.IsCompleted);
            if (allocationTask.Exception != null)
            {
                Debug.LogError("Allocation failed: " + allocationTask.Exception);
                yield break;
            }
            else
            {
                Debug.Log("Allocation successfull!");
                // Fetch result of the task
                Allocation allocation = allocationTask.Result;

                relayData = new RelayHostData();
                // Find the appropriate endpoint, just select the first one and use it
                foreach (var endpoint in allocation.ServerEndpoints)
                {
                    relayData.IPv4Address = endpoint.Host;
                    relayData.Port = (ushort)endpoint.Port;
                    break;
                }
                relayData.AllocationID = allocation.AllocationId;
                relayData.AllocationIDBytes = allocation.AllocationIdBytes;
                relayData.ConnectionData = allocation.ConnectionData;
                relayData.Key = allocation.Key;

                var joinCodeTask = GetJoinCodeAsync(relayData.AllocationID);
                yield return new WaitUntil(() => joinCodeTask.IsCompleted);
                if (joinCodeTask.Exception != null)
                {
                    Debug.LogError("Join code failed: " + joinCodeTask.Exception);
                    yield break;
                }
                else
                {
                    Debug.Log("Code retrieved!");
                    relayData.JoinCode = joinCodeTask.Result;

                    lobbyCode.SetCode(relayData.JoinCode);

                    transport.SetRelayServerData(relayData.IPv4Address, relayData.Port, relayData.AllocationIDBytes, relayData.Key, relayData.ConnectionData);
                }
            }
        }

        //InitAnalytics();

        networkManager.NetworkConfig.ConnectionApproval = true;

        if (networkManager.StartServer())
        {
            Debug.Log($"Serving on port {transport.ConnectionData.Port}...");

            string hostData = $"{character}|{string.Join(",", deckIds)}";
            pendingPlayerCharacters[NetworkManager.ServerClientId] = hostData;

            networkManager.ConnectionApprovalCallback += ApprovalCheck;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;

            OnClientConnected(NetworkManager.ServerClientId);
        }
        else
        {
            Debug.LogError($"Failed to serve on port {transport.ConnectionData.Port}...");
        }
    }

    private async Task<Allocation> CreateAllocationAsync(int maxPlayers)
    {
        try
        {
            // This requests space for maxPlayers + 1 connections (the +1 is for the server itself)
            Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxPlayers);
            return allocation;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error creating allocation: " + e);
            throw;
        }
    }

    private async Task<string> GetJoinCodeAsync(Guid allocationID)
    {
        try
        {
            string code = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocationID);
            return code;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error retrieving join code: " + e);
            throw;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        {
        // You need to retrieve from pendingPlayerCharacters, not relayData
            if (pendingPlayerCharacters.TryGetValue(clientId, out string characterData))
            {
                string[] parts = characterData.Split('|');
                string characterName = parts[0];
                List<int> deckIds = parts[1].Split(',').Select(int.Parse).ToList();
                
                Debug.Log($"Player {clientId} connected as {characterName} with deck: {string.Join(",", deckIds)}");

                PlayerInfo player = new (clientId, characterName, deckIds);
                

                if (clientId == NetworkManager.ServerClientId)
                {
                    // Host is always player 0
                    Players.Insert(0, player);  // If using List
                }
                else
                {
                    Players.Add(player);  // Clients go to next available slot
                }
            }

            if (Players.Count == maxPlayers)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                CanStart = true;
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} disconnected!");

        PlayerInfo disconnectedPlayer = Players.Find(p => p.ClientID == clientId);
        if (disconnectedPlayer != null)
        {
            Players.Remove(disconnectedPlayer);
            Debug.Log($"Removed {disconnectedPlayer.CharacterName} from players list");
        }
        
        // Reset CanStart if needed
        if (Players.Count < maxPlayers)
            CanStart = false;
        
        // Clean up pending data
        pendingPlayerCharacters.Remove(clientId);
    }

    IEnumerator StartAsClientCR(string characterName, List<int> deckIds)
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;

        string combinedData = $"{characterName}|{string.Join(",", deckIds)}";
        byte[] connectionData = System.Text.Encoding.UTF8.GetBytes(combinedData);
        networkManager.NetworkConfig.ConnectionData = connectionData;

        // Wait a frame for setups to be done
        yield return null;

        if (isRelay)
        {
            //Ask Unity Services for allocation data based on a join code
            var joinAllocationTask = JoinAllocationAsync(joinCode);
            yield return new WaitUntil(() => joinAllocationTask.IsCompleted);
            if (joinAllocationTask.Exception != null)
            {
                Debug.LogError("Join allocation failed: " + joinAllocationTask.Exception);
                yield break;
            }
            else
            {
                Debug.Log("Allocation joined!");

                relayData = new RelayHostData();
                var allocation = joinAllocationTask.Result;
                // Find the appropriate endpoint, just select the first one and use it
                foreach (var endpoint in allocation.ServerEndpoints)
                {
                    relayData.IPv4Address = endpoint.Host;
                    relayData.Port = (ushort)endpoint.Port;
                    break;
                }
                relayData.AllocationID = allocation.AllocationId;
                relayData.AllocationIDBytes = allocation.AllocationIdBytes;
                relayData.ConnectionData = allocation.ConnectionData;
                relayData.HostConnectionData = allocation.HostConnectionData;
                relayData.Key = allocation.Key;
                transport.SetRelayServerData(relayData.IPv4Address, relayData.Port,
                                                relayData.AllocationIDBytes, relayData.Key, relayData.ConnectionData,
                                                relayData.HostConnectionData);
            }
        }

        //InitAnalytics();

        if (networkManager.StartClient())
        {
            Debug.Log($"Connecting on port {transport.ConnectionData.Port}...");
        }
        else
        {
            Debug.LogError($"Failed to connect on port {transport.ConnectionData.Port}...");
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        byte[] connectionData = request.Payload;
        ulong clientId = request.ClientNetworkId;
        
        if (connectionData != null && connectionData.Length > 0)
        {
            string fullData = System.Text.Encoding.UTF8.GetString(connectionData);
            // Store the full data for parsing in OnClientConnected
            pendingPlayerCharacters[clientId] = fullData;
            
            // Optional: Parse immediately for approval logic
            string[] parts = fullData.Split('|');
            string characterName = parts[0];
            List<int> deckIds = parts[1].Split(',').Select(int.Parse).ToList();
            
            Debug.Log($"Client {clientId} connecting as {characterName} with {deckIds.Count} cards");
            
            // Example approval condition
            if (deckIds.Count != 20)
            {
                response.Approved = false;
                response.Reason = "Invalid deck size!";
                return;
            }
        }
        
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }

    /*void InitAnalytics()
    {
        if (enableAnalytics)
        {
            ConsentState consentState = EndUserConsent.GetConsentState();
            consentState.AnalyticsIntent = ConsentStatus.Granted;
            EndUserConsent.SetConsentState(consentState);
        }
    }*/

    private async Task<JoinAllocation> JoinAllocationAsync(string joinCode)
    {
        try
        {
            var allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);

            return allocation;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error joining allocation: " + e);
            throw;
        }
    }
}