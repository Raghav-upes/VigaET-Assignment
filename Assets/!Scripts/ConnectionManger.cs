/*
 * ConnectionManager.cs
 * --------------------
 * SUMMARY:
 * Handles the UI for entering player name and room code, and triggers creating or joining a multiplayer room.
 * Uses Unity UI Toolkit (UIDocument).
 */

using UnityEngine;
using UnityEngine.UIElements;

public class ConnectionManager : MonoBehaviour
{
    // UI fields for name, room code, and action buttons
    private TextField playerNameField;
    private TextField roomCodeField;
    private Button createRoomButton;
    private Button joinRoomButton;

    private void Start()
    {
        // Find UI elements by their UXML names
        var root = FindFirstObjectByType<UIDocument>().rootVisualElement;

        playerNameField = root.Q<TextField>("NameInput");
        roomCodeField = root.Q<TextField>("RoomCodeInput");
        createRoomButton = root.Q<Button>("CreateRoomButton");
        joinRoomButton = root.Q<Button>("JoinRoomButton");

        // Error if UI elements are missing
        if (playerNameField == null || roomCodeField == null || createRoomButton == null || joinRoomButton == null)
        {
            Debug.LogError("UI Elements not found! Please check UXML names.");
            return;
        }

        // Set initial state of the buttons
        UpdateButtonState();

        // React to changes in input fields
        playerNameField.RegisterValueChangedCallback(_ => UpdateButtonState());
        roomCodeField.RegisterValueChangedCallback(_ => UpdateButtonState());

        // Assign what happens when buttons are clicked
        createRoomButton.clicked += CreateRoom;
        joinRoomButton.clicked += JoinRoom;
    }

    // Enable or disable buttons based on valid input
    private void UpdateButtonState()
    {
        bool canProceed = !string.IsNullOrWhiteSpace(playerNameField.value) && !string.IsNullOrWhiteSpace(roomCodeField.value);
        createRoomButton.SetEnabled(canProceed);
        joinRoomButton.SetEnabled(canProceed);
    }

    // Called when Create Room is clicked
    private void CreateRoom()
    {
        NetworkManager.Instance.playerName = playerNameField.value.Trim();
        NetworkManager.Instance.createSession(roomCodeField.value.Trim());
    }

    // Called when Join Room is clicked
    private void JoinRoom()
    {
        NetworkManager.Instance.playerName = playerNameField.value.Trim();
        NetworkManager.Instance.joinSession(roomCodeField.value.Trim());
    }
}
