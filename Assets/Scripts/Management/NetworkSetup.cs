using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

using System.Linq;
using TMPro;
using Unity.Services.Core;

using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using UnityEngine.UnityConsent;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;





#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
using System.Diagnostics;
#endif

using Debug = UnityEngine.Debug;

public class NetworkSetup : MonoBehaviour
{
    [SerializeField] private int                maxPlayers = 2;
    [SerializeField] private string             joinCode = "";
    [SerializeField] private string             sceneName = "Battle";
    //[SerializeField] private bool               enableAnalytics;
    public GameObject[] Players { get; private set; }
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
    private int playerIndex = 0;
    private UnityTransport transport;
    private ShowLobbyCode lobbyCode;
    private bool isRelay = false;

    void Start()
    {
        DontDestroyOnLoad(this);
        SceneManager.LoadScene(sceneName);

        Players = new GameObject[maxPlayers];

        lobbyCode = GetComponent<ShowLobbyCode>();
    
        if (SelectionData.isServer)
        {
            isServer = SelectionData.isServer;
        }
        else
        {
            joinCode = SelectionData.code;
        }

        transport = GetComponent<UnityTransport>();

        if (transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
        {
            isRelay = true;
        }

        if (isServer)
        {
            string charName = SelectionData.prefabName;
            List<int> deckList = SelectionData.deck;
            StartCoroutine(StartAsServerCR(charName, deckList));
        }
        else
        {
            string charName = SelectionData.prefabName;
            List<int> deckList = SelectionData.deck;
            StartCoroutine(StartAsClientCR(charName,deckList));
        }
    }

    IEnumerator StartAsServerCR(string character, List<int> deckIds)
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        SetWindowTitle("Starting as server...");

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

                    transport.SetRelayServerData(relayData.IPv4Address, relayData.Port, relayData.AllocationIDBytes, relayData.Key, relayData.ConnectionData);
                }
            }
        }

        //InitAnalytics();

        networkManager.NetworkConfig.ConnectionApproval = true;

        if (networkManager.StartServer())
        {
            SetWindowTitle("ArcanisDuels - Server");
            Debug.Log($"Serving on port {transport.ConnectionData.Port}...");

            networkManager.ConnectionApprovalCallback += ApprovalCheck;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            SetWindowTitle("Fail to start as server");
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
        }
    }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Player {clientId} disconnected!");
    }

    IEnumerator StartAsClientCR(string characterName, List<int> deckIds)
    {
        var networkManager = GetComponent<NetworkManager>();
        networkManager.enabled = true;
        var transport = GetComponent<UnityTransport>();
        transport.enabled = true;
        SetWindowTitle("Starting as client...");

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
            SetWindowTitle("ArcanisDuels - Client...");
            Debug.Log($"Connecting on port {transport.ConnectionData.Port}...");
        }
        else
        {
            SetWindowTitle("Fail to start as client");
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

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowText(IntPtr hWnd, string lpString);
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    static extern IntPtr EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    // Delegate to filter windows
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    private static IntPtr FindWindowByProcessId(uint processId)
    {
        IntPtr windowHandle = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            uint windowProcessId;
            GetWindowThreadProcessId(hWnd, out windowProcessId);
            if (windowProcessId == processId)
            {
                windowHandle = hWnd;
                return false; // Found the window, stop enumerating
            }
            return true; // Continue enumerating
        }, IntPtr.Zero);
        return windowHandle;
    }

    static void SetWindowTitle(string title)
    {
#if !UNITY_EDITOR
        uint processId = (uint)Process.GetCurrentProcess().Id;
        IntPtr hWnd = FindWindowByProcessId(processId);
        if (hWnd != IntPtr.Zero)
        {
            SetWindowText(hWnd, title);
        }
#endif
    }
#else
    static void SetWindowTitle(string title)
    {
    }
#endif


#if UNITY_EDITOR
    [MenuItem("Tools/Build Windows (x64)", priority = 0)]
    public static bool BuildGame()
    {
        // Specify build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        buildPlayerOptions.locationPathName = Path.Combine("Builds", "ArcanisDuels.exe");
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        // Perform the build
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        // Output the result of the build
        Debug.Log($"Build ended with status: {report.summary.result}");
        // Additional log on the build, looking at report.summary
        return report.summary.result == BuildResult.Succeeded;
    }
#endif


#if UNITY_EDITOR
    private static void Run(string path, string args)
    {
        // Start a new process
        Process process = new Process();
        // Configure the process using the StartInfo properties
        process.StartInfo.FileName = path;
        process.StartInfo.Arguments = args;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Normal; // Choose the window style: Hidden, Minimized, Maximized, Normal
        process.StartInfo.RedirectStandardOutput = false; // Set to true to redirect the output (so you can read it in Unity)
        process.StartInfo.UseShellExecute = true; // Set to false if you want to redirect the output
                                                  // Run the process
        process.Start();
    }

    [MenuItem("Tools/Build and Launch (Server)", priority = 10)]
    public static void BuildAndLaunch1()
    {
        CloseAll();
        if (BuildGame())
        {
            LaunchServer();
        }
    }
    [MenuItem("Tools/Build and Launch (Client)", priority = 15)]
    public static void BuildAndLaunchClient()
    {
        CloseAll();
        if (BuildGame())
        {
            LaunchClient();
        }
    }

    [MenuItem("Tools/Build and Launch (Server + Client)", priority = 20)]
    public static void BuildAndLaunchServerAndClient()
    {
        CloseAll();
        if (BuildGame())
        {
            LaunchClientAndServer();
        }
    }
    [MenuItem("Tools/Launch (Server) _F11", priority = 30)]
    public static void LaunchServer()
    {
        Run("C:\\Users\\imlis\\Desktop\\ArcanisDuels - Build\\ArcanisDuels.exe", "--server");
    }
    [MenuItem("Tools/Launch (Server + Client)", priority = 40)]
    public static void LaunchClientAndServer()
    {
        LaunchServer();
        LaunchClient();
    }
    [MenuItem("Tools/Launch (Client)", priority = 45)]
    public static void LaunchClient()
    {
        Run("C:\\Users\\imlis\\Desktop\\ArcanisDuels - Build\\ArcanisDuels.exe", "");
    }

    [MenuItem("Tools/Close All", priority = 100)]
    public static void CloseAll()
    {
        // Get all processes with the specified name
        Process[] processes = Process.GetProcessesByName("ArcanisDuels");
        foreach (var process in processes)
        {
            try
            {
                // Close the process
                process.Kill();
                // Wait for the process to exit
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                // Handle exceptions, if any
                // This could occur if the process has already exited or you don't have permission to kill it
                Debug.LogWarning($"Error trying to kill process {process.ProcessName}: {ex.Message}");
            }
        }
    }
#endif
}
