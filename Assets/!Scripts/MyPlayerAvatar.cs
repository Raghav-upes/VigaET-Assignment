/*
 * MyPlayerAvatar.cs
 * -----------------
 * SUMMARY:
 * Represents a networked player avatar in the game.
 * Handles player UI card creation, color, name display, voice chat indicator, mic status, and container management.
 * Syncs state across network and updates local UI accordingly.
 */

using Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.UIElements;

public class MyPlayerAvatar : NetworkBehaviour
{
    [Header("UI Toolkit")]
    public VisualTreeAsset playerCardTemplate; // UI template for player card

    private VisualElement playerCardElement;
    private VisualElement containerElement;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked, OnChangedRender(nameof(OnPlayerDataChanged))]
    public NetworkString<_16> PlayerName { get; set; }

    private bool isLocalPlayer;

    private VisualElement indicatorBig;
    private VisualElement indicator;

    [Networked, OnChangedRender(nameof(OnSpeakingChanged))]
    public bool IsSpeakingNet { get; set; }

    [SerializeField] private Recorder voiceRecorder;
    [SerializeField] private Speaker voiceSpeaker;
    private float silenceThreshold = 0.01f;

    public bool IsLocalPlayerAvatar => Owner == Runner.LocalPlayer;

    public Texture2D OnMicIcon;
    public Texture2D OffMicIcon;

    [Networked, OnChangedRender(nameof(OnMicStatusChanged))]
    public bool MicEnabled { get; set; } = true;

    // Called when the avatar is spawned in the network
    public override void Spawned()
    {
        isLocalPlayer = Object.HasInputAuthority;
        Owner = Object.InputAuthority;

        if (isLocalPlayer)
        {
            PlayerName = NetworkManager.Instance.playerName;

            // Setup voice recorder for local player
            if (voiceRecorder == null)
                voiceRecorder = FindFirstObjectByType<Recorder>();
            if (voiceRecorder != null)
            {
                voiceRecorder.TransmitEnabled = true;
                voiceRecorder.RecordingEnabled = true;
            }

            // Link mic controller if available
            var micController = GetComponent<MicController>();
            if (micController != null)
            {
                micController.voiceRecorder = voiceRecorder;
                micController.InitFromAvatar();
            }
        }

        PlayerSpawner.Instance.RegisterAvatar(Owner, this);

        // UI slot selection (local or remote)
        var root = UIManager.Instance.UIDocument.rootVisualElement;
        if (isLocalPlayer)
            containerElement = root.Q<VisualElement>("LocalPlayer");
        else
        {
            for (int i = 1; i <= 3; i++)
            {
                var slot = root.Q<VisualElement>($"OtherPlayer{i}");
                if (slot.childCount == 0)
                {
                    containerElement = slot;
                    break;
                }
            }
        }

        if (containerElement == null)
        {
            Debug.LogError("No suitable container found!");
            return;
        }

        // Setup player card UI
        playerCardElement = playerCardTemplate.CloneTree();
        containerElement.Clear();
        playerCardElement.style.width = Length.Percent(100);
        playerCardElement.style.minWidth = Length.Percent(100);
        playerCardElement.style.maxWidth = Length.Percent(100);

        playerCardElement.style.height = Length.Percent(100);
        playerCardElement.style.minHeight = Length.Percent(100);
        playerCardElement.style.maxHeight = Length.Percent(100);

        containerElement.Add(playerCardElement);

        indicatorBig = playerCardElement.Q<VisualElement>("IndicatorBig");
        indicator = playerCardElement.Q<VisualElement>("Indicator");
        SetIndicatorOpacity(false);

        InitializeUI();
        UpdateMicStatusIcon();
    }

    // Initializes player card UI color and name
    private void InitializeUI()
    {
        var avatar = playerCardElement.Q<VisualElement>("Avatar");
        if (avatar != null)
        {
            int playerSeed = Owner.PlayerId;
            Color uniqueColor = GenerateDarkPaleColor(playerSeed);
            avatar.style.backgroundColor = uniqueColor;
        }
        UpdatePlayerNameDisplay();
    }

    // Generates a consistent but unique color per player
    public static Color GenerateDarkPaleColor(int uniqueSeed = 0)
    {
        System.Random rand = new System.Random(uniqueSeed);
        float hue = (float)rand.NextDouble();
        float saturation = 0.4f + 0.3f * (float)rand.NextDouble();
        float value = 0.45f + 0.25f * (float)rand.NextDouble();
        return Color.HSVToRGB(hue, saturation, value);
    }

    // Called when player data changes (like name)
    private void OnPlayerDataChanged()
    {
        UpdatePlayerNameDisplay();
    }

    // Updates name and initial in the player card UI
    private void UpdatePlayerNameDisplay()
    {
        if (playerCardElement != null)
        {
            var label = playerCardElement.Q<Label>("NameLabel");
            if (label != null)
            {
                string displayName = PlayerName.ToString();
                if (isLocalPlayer) displayName += " (You)";
                label.text = displayName;
                playerCardElement.Q<Label>("NameInitial").text = displayName[0].ToString().ToUpper();
            }
        }
    }

    // Called when the avatar is despawned
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (containerElement != null && playerCardElement != null)
        {
            containerElement.Remove(playerCardElement);
        }
        PlayerSpawner.Instance.UnregisterAvatar(Owner, this);
    }

    // Called every network tick for the local player, to detect speaking state
    public override void FixedUpdateNetwork()
    {
        if (!isLocalPlayer) return;

        bool isSpeaking = voiceRecorder != null && voiceRecorder.IsCurrentlyTransmitting &&
            voiceRecorder.LevelMeter.CurrentAvgAmp > silenceThreshold;
        IsSpeakingNet = isSpeaking;
    }

    // UI change when speaking state updates
    private void OnSpeakingChanged()
    {
        SetIndicatorOpacity(IsSpeakingNet);
    }

    // Helper to change UI indicator opacity
    private void SetIndicatorOpacity(bool speaking)
    {
        float value = speaking ? 100f : 0f;
        if (indicatorBig != null) indicatorBig.style.opacity = value;
        if (indicator != null) indicator.style.opacity = value;
    }

    // Moves this player card to a different "OtherPlayer" slot
    public void MoveToOtherPlayerSlot(int slotIndex)
    {
        if (playerCardElement == null)
            return;

        var root = UIManager.Instance.UIDocument.rootVisualElement;
        var newContainer = root.Q<VisualElement>($"OtherPlayer{slotIndex}");
        if (newContainer == null)
            return;

        if (containerElement != null)
            containerElement.Remove(playerCardElement);

        newContainer.Clear();
        newContainer.Add(playerCardElement);
        containerElement = newContainer;
    }

    // Enables/disables mic for this avatar
    public void SetMicEnabled(bool enabled)
    {
        if (IsLocalPlayerAvatar)
            MicEnabled = enabled;
        UpdateMicStatusIcon();
    }

    // Triggered on mic status change
    private void OnMicStatusChanged() => UpdateMicStatusIcon();

    // Updates mic icon and color in UI
    public void UpdateMicStatusIcon()
    {
        var micStatusElement = playerCardElement?.Q<VisualElement>("MyMicStatus");
        if (micStatusElement != null)
        {
            micStatusElement.style.backgroundImage = new StyleBackground(
                MicEnabled ? OnMicIcon : OffMicIcon
            );
            micStatusElement.style.unityBackgroundImageTintColor = MicEnabled
                ? new Color(0.290f, 0.871f, 0.502f)
                : new Color(0.973f, 0.443f, 0.443f);
            micStatusElement.tooltip = MicEnabled ? "Mic On" : "Mic Off";
        }
    }
}
