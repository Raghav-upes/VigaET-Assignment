/*
 * PlayerSpawner.cs
 * ----------------
 * SUMMARY:
 * This script manages spawning, tracking, and UI notifications for all players in a multiplayer Fusion session.
 * - Handles avatar creation and removal when players join or leave.
 * - Keeps the player UI and notifications up to date.
 * - Ensures all player avatars are shown in consistent slots and names are displayed in notifications.
 * - Provides feedback on user connection events (join/leave) using smooth notification UI.
 * 
 * Attach this script to any persistent GameObject in your scene.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    // Singleton pattern: there can only be one PlayerSpawner in the scene
    public static PlayerSpawner Instance { get; private set; }

    // NetworkPrefabRef is a Fusion type for referencing networked prefabs in the inspector
    [SerializeField] private NetworkPrefabRef prefab;

    // VisualTreeAsset holds the notification panel UI template
    [SerializeField] private VisualTreeAsset notificationPanel;

    // Keeps track of each player's avatar on this client
    private Dictionary<PlayerRef, MyPlayerAvatar> playerAvatars = new Dictionary<PlayerRef, MyPlayerAvatar>();

    // Stores player names for use in notifications and UI
    private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();

    // Fusion network runner instance
    private NetworkRunner runner;

    // The notifications UI container in the UI Toolkit hierarchy
    private VisualElement notificationsContainer;

    // Basic singleton implementation; if another exists, destroy this one
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // At start, register network callbacks so this script can handle player join/leave etc.
    private void Start()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.Runner != null)
        {
            runner = NetworkManager.Instance.Runner;
            runner.AddCallbacks(this);
        }
        else
        {
            Debug.LogError("NetworkManager or Runner not found!");
        }
    }

    // Remove callbacks on destroy to prevent memory leaks or null errors
    private void OnDestroy()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
    }

    // Find or create the UI element that holds notifications (popups)
    public void SetupNotificationsContainer()
    {
        if (UIManager.Instance?.UIDocument?.rootVisualElement != null)
        {
            var root = UIManager.Instance.UIDocument.rootVisualElement;
            notificationsContainer = root.Q<VisualElement>("NotificationsContainer");

            // If not found, create it dynamically and style it
            if (notificationsContainer == null)
            {
                notificationsContainer = new VisualElement();
                notificationsContainer.name = "NotificationsContainer";
                notificationsContainer.AddToClassList("notifications-container");
                notificationsContainer.style.position = Position.Absolute;
                notificationsContainer.style.top = 20;
                notificationsContainer.style.left = Length.Percent(50);
                root.Add(notificationsContainer);
            }
        }
    }

    
    // Called by each MyPlayerAvatar after spawning to register itself for tracking and notifications.
    public void RegisterAvatar(PlayerRef owner, MyPlayerAvatar avatar)
    {
        playerAvatars[owner] = avatar;

        // Save player name for future notifications if available
        if (avatar != null && !string.IsNullOrEmpty(avatar.PlayerName.ToString()))
        {
            playerNames[owner] = avatar.PlayerName.ToString();
        }

        // Show notification that a player joined (after PlayerName is available)
        string playerName = GetPlayerName(owner);
        ShowNotification(playerName, true);

        ShiftOtherPlayersLeft();
        UpdateUsersCountLabel();
    }

    //Called by MyPlayerAvatar on despawn for cleanup and notification.
    public void UnregisterAvatar(PlayerRef owner, MyPlayerAvatar avatar)
    {
        if (playerAvatars.TryGetValue(owner, out var a) && a == avatar)
        {
            // Show leave notification before removing avatar
            string playerName = GetPlayerName(owner);
            ShowNotification(playerName, false);

            playerAvatars.Remove(owner);
        }
        ShiftOtherPlayersLeft();
        UpdateUsersCountLabel();
    }

  
    // Update the users count label in the UI.
    private void UpdateUsersCountLabel()
    {
        int count = playerAvatars.Count;
        if (UIManager.Instance.UsersCountLabel != null)
            UIManager.Instance.UsersCountLabel.text = $"{count} out of 4 users connected";
    }


    // Shifts all remote (non-local) player avatars left, so there are no gaps in UI slots.
    private void ShiftOtherPlayersLeft()
    {
        // Collect all non-local avatars
        var otherAvatars = new List<MyPlayerAvatar>();
        foreach (var kvp in playerAvatars)
        {
            if (!kvp.Value.IsLocalPlayerAvatar)
                otherAvatars.Add(kvp.Value);
        }

        // Sort for consistent order
        otherAvatars.Sort((a, b) => a.Owner.PlayerId.CompareTo(b.Owner.PlayerId));

        // Move each avatar to a specific UI slot
        for (int i = 0; i < otherAvatars.Count; i++)
        {
            otherAvatars[i].MoveToOtherPlayerSlot(i + 1); // 1-based for OtherPlayer1, 2, 3
        }

        // Clear any extra (unused) slots
        var root = UIManager.Instance.UIDocument.rootVisualElement;
        for (int i = otherAvatars.Count + 1; i <= 3; i++)
        {
            var slot = root.Q<VisualElement>($"OtherPlayer{i}");
            if (slot != null)
                slot.Clear();
        }
    }


    // Show a notification in the UI that a player joined or left.
    private void ShowNotification(string playerName, bool isJoining)
    {
        if (notificationPanel == null || notificationsContainer == null)
        {
            Debug.LogWarning("Notification panel or container not set up!");
            return;
        }

        // Create notification UI from template
        var notification = notificationPanel.Instantiate();
        var notificationElement = notification.Q<VisualElement>("Notification");
        var messageLabel = notification.Q<Label>("NotificationMessage");

        if (notificationElement != null && messageLabel != null)
        {
            // Set message text
            messageLabel.text = isJoining ? $"{playerName} joined" : $"{playerName} left";

            // Apply animation classes
            notificationElement.RemoveFromClassList("notification-visible");
            notificationElement.AddToClassList("notification-slide-in");

            if (isJoining)
            {
                notificationElement.AddToClassList("notification-join");
            }
            else
            {
                notificationElement.AddToClassList("notification-leave");
            }

            // Add notification to container
            notificationsContainer.Add(notification);

            // Animate notification: slide-in, hold, then fade out and remove
            StartCoroutine(AnimateNotification(notification, notificationElement, isJoining));
        }
    }

    // Coroutine to handle notification animation timing.
    private IEnumerator AnimateNotification(VisualElement notification, VisualElement notificationElement, bool isJoining)
    {
        yield return new WaitForEndOfFrame();

        // Slide-in animation
        notificationElement.AddToClassList("notification-slide-in-active");
        yield return new WaitForSeconds(0.4f);

        // Keep visible for a short duration
        float displayDuration = isJoining ? 3f : 2f;
        yield return new WaitForSeconds(displayDuration);

        // Start fade-out
        notificationElement.RemoveFromClassList("notification-slide-in-active");
        notificationElement.AddToClassList("notification-fade-out");
        notificationElement.AddToClassList("notification-fade-out-active");
        yield return new WaitForSeconds(0.3f);

        // Remove notification from hierarchy
        if (notification.parent != null)
        {
            notification.RemoveFromHierarchy();
        }
    }


    // Returns the display name for a player, preferring their custom name, else falls back to "Player #"
    private string GetPlayerName(PlayerRef player)
    {
        // Try stored name first
        if (playerNames.TryGetValue(player, out string storedName))
        {
            return storedName;
        }

        // Try from avatar if possible
        if (playerAvatars.TryGetValue(player, out var avatar) && avatar != null)
        {
            if (!string.IsNullOrEmpty(avatar.PlayerName.ToString()))
            {
                playerNames[player] = avatar.PlayerName.ToString();
                return avatar.PlayerName.ToString();
            }
        }

        // Default fallback: Player ID
        return $"Player {player.PlayerId}";
    }

    // ---------------------------
    // FUSION NETWORK CALLBACKS
    // ---------------------------

    // Called when a new player joins the session. Spawns an avatar for the local player, updates UI.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        SetupNotificationsContainer();
        Debug.Log($"Player {player.PlayerId} joined the game");

        // Spawn only for the local player, others will auto-register on all clients
        if (runner.LocalPlayer == player)
        {
            var spawnedObject = runner.Spawn(prefab, Vector3.zero, Quaternion.identity, player);
            if (spawnedObject != null)
            {
                var avatar = spawnedObject.GetComponent<MyPlayerAvatar>();
                if (avatar != null)
                {
                    Debug.Log($"Avatar spawned for player {player.PlayerId}");
                }
            }
        }

        // Show room code and update user count
        if (UIManager.Instance.RoomCodeLabel != null)
            UIManager.Instance.RoomCodeLabel.text = $"Room: #{runner.SessionInfo.Name}";
        UpdateUsersCountLabel();
    }


    // Handles when a player leaves: despawns their avatar, shows notification, and updates UI.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player.PlayerId} left the game");

        // Show leave notification to all clients
        string playerName = GetPlayerName(player);
        ShowNotification(playerName, false);

        // Despawn the avatar if we have it
        if (playerAvatars.ContainsKey(player))
        {
            var avatar = playerAvatars[player];
            if (avatar != null && avatar.Object != null)
            {
                runner.Despawn(avatar.Object);
            }
            // UnregisterAvatar will be called from MyPlayerAvatar.Despawned()
        }

        // Remove stored player name for cleanliness
        playerNames.Remove(player);

        // UI shift and update
        ShiftOtherPlayersLeft();
        UpdateUsersCountLabel();
    }

 
    // On shutdown, clears all data and updates the UI.
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner Shutdown: {shutdownReason}");
        playerAvatars.Clear();
        playerNames.Clear();
        UpdateUsersCountLabel();
    }

    // ------------- UNUSED CALLBACKS BELOW (NO EXPLANATION) ---------------

    #region INetworkCallbacks (Unused)

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    #endregion
}
