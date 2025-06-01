/*
 * MicController.cs
 * ----------------
 * SUMMARY:
 * Lets the user toggle their microphone on/off and updates the UI + Photon Voice recorder accordingly.
 * Designed for use with Unity UI Toolkit and Photon Voice.
 */

using UnityEngine;
using UnityEngine.UIElements;
using Photon.Voice.Unity;

public class MicController : MonoBehaviour
{
    // Mic icon textures for UI (assign in Inspector)
    [Header("Mic Icon Assets")]
    public Texture2D OnMicIcon;   // Green mic icon
    public Texture2D OffMicIcon;  // Red/Muted mic icon

    [Header("Voice")]
    public Recorder voiceRecorder;

    // UI references
    private Button micButton;
    private VisualElement micIcon;

    // Current mic state
    private bool micOn = true;

    // Called by MyPlayerAvatar after setup, finds UI controls
    public void InitFromAvatar()
    {
        var root = UIManager.Instance.UIDocument.rootVisualElement;

        micButton = root.Q<Button>("Mic");
        micIcon = root.Q<VisualElement>("MicIcon");

        if (micButton == null || micIcon == null)
        {
            Debug.LogError("MicController: 'Mic' Button or 'MicIcon' not found in UXML!");
            return;
        }

        // Listen for button press
        micButton.clicked += ToggleMic;
    }

    // Called when mic button is pressed
    private void ToggleMic()
    {
        micOn = !micOn;
        SetMicState(micOn, false);
    }

    // Applies new mic state to UI and voice system
    private void SetMicState(bool enabled, bool firstSetup)
    {
        // Let MyPlayerAvatar sync this state over network
        GetComponent<MyPlayerAvatar>()?.SetMicEnabled(enabled);

        // Enable/disable voice transmission
        if (voiceRecorder != null)
        {
            voiceRecorder.TransmitEnabled = enabled;
            voiceRecorder.RecordingEnabled = enabled;
        }

        // Button color reflects mic state
        micButton.style.backgroundColor = enabled ? new Color(0.133f, 0.773f, 0.369f) : new Color(0.937f, 0.267f, 0.267f);

        // Show correct icon and tooltip
        if (micIcon != null)
        {
            micIcon.style.backgroundImage = new StyleBackground(enabled ? OnMicIcon : OffMicIcon);
            micIcon.tooltip = enabled ? "Mic On" : "Mic Off";
        }

        if (!firstSetup)
            Debug.Log("MicController: Mic is now " + (enabled ? "ON" : "OFF"));
    }
}
