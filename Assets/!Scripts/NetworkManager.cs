/*
 * NetworkManager.cs
 * -----------------
 * SUMMARY:
 * This script manages multiplayer network sessions in a Unity game using Photon Fusion.
 * - Ensures there is only one NetworkManager instance (singleton pattern).
 * - Handles session creation and joining (host/join logic).
 * - Provides a user interface connection status indicator.
 * - Manages scene transitions and shows connection feedback.
 * 
 * Usage: Attach this script to a persistent GameObject in your Unity scene.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Singleton instance for global access
    public static NetworkManager Instance { get; private set; }

    // Prefab to create the NetworkRunner (main Fusion networking object)
    [SerializeField] private GameObject runnerPrefab;
    public string playerName;

    // Reference to the current network runner
    public NetworkRunner Runner { get; private set; }

    // UI Elements for connection feedback (Unity UI Toolkit)
    private VisualElement connectionDot;
    private Label connectionStatusLabel;

    // Ensure only one instance exists across all scenes
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // Find and store connection status UI elements at start
    private void Start()
    {
        var root = FindFirstObjectByType<UIDocument>().rootVisualElement;
        connectionDot = root.Q<VisualElement>("ConnectionDot");
        connectionStatusLabel = root.Q<Label>("ConnectionStatusLabel");
    }

    // Instantiates a new NetworkRunner and sets up callbacks
    public void createRunner()
    {
        Runner = Instantiate(runnerPrefab, transform).GetComponent<NetworkRunner>();
        Runner.AddCallbacks(this); // Register this script for network events
    }

    // Start a new multiplayer session with a unique room code
    public async void createSession(string roomCode)
    {
        createRunner(); // Make sure we have a network runner ready
        await Connect(roomCode); // Connect to network session
        await LoadScene();       // Load the game scene after connecting
    }

    // Join an existing multiplayer session using a room code
    public async void joinSession(string roomCode)
    {
        createRunner();
        await Connect(roomCode);
        await LoadScene();
    }

    // Loads the game scene asynchronously and updates the UI on completion
    public async Task LoadScene()
    {
        // Begin loading scene index 1 (change if your gameplay scene index is different)
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(1);
        // Wait until the scene is fully loaded
        while (!asyncOperation.isDone)
        {
            await Task.Yield();
        }

        // Update connection status UI to show success (green)
        connectionStatusLabel.text = "Connected ...";
        connectionStatusLabel.style.color = new Color(0.133f, 0.773f, 0.369f);
        connectionDot.style.backgroundColor = new Color(0.133f, 0.773f, 0.369f);
    }

    // Connects to a Fusion multiplayer session (either as host or client)
    public async Task Connect(string sessionName)
    {
        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared, // Shared mode for co-op or versus play
            SessionName = sessionName,  // Unique session/room name
            SceneManager = GetComponent<NetworkSceneManagerDefault>(), // Scene management for syncing
            Scene = SceneRef.FromIndex(1) // Scene index to sync (same as LoadScene)
        };

        // Show connecting status (yellow)
        connectionStatusLabel.text = "Connecting ...";
        connectionStatusLabel.style.color = new Color(0.820f, 0.769f, 0.090f);
        connectionDot.style.backgroundColor = new Color(0.820f, 0.769f, 0.090f);

        // Start the game/network session
        await Runner.StartGame(args);
    }

    // ---------------------------
    // FUSION NETWORK CALLBACKS
    // ---------------------------

    // Called when a new player joins the session
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("Player has joined");
        // You could add logic here to update a player list, spawn the player, etc.
    }

    // Called when the network runner shuts down
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log("Runner Shutdown");
        // You could add logic here to clean up or reset UI.
    }

    // Called when the client disconnects from the server
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // Show UI as "Ready to connect" (greyed out)
        connectionStatusLabel.text = "Ready to connect";
        connectionStatusLabel.style.color = new Color(0.627f, 0.627f, 0.627f);
        connectionDot.style.backgroundColor = new Color(0.627f, 0.627f, 0.627f);
    }

    // Called when a connection attempt fails
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        connectionStatusLabel.text = "Ready to connect";
        connectionStatusLabel.style.color = new Color(0.627f, 0.627f, 0.627f);
        connectionDot.style.backgroundColor = new Color(0.627f, 0.627f, 0.627f);
    }


    #region
    // ---------------------------------------------------
    // (Other INetworkRunnerCallbacks methods intentionally left empty)
    // ---------------------------------------------------
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
       
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
 
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
      
    }

    #endregion
}
