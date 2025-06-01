/*
 * VideoSyncController.cs
 * ----------------------
 * SUMMARY:
 * Keeps a Unity VideoPlayer in sync across all networked clients (play/pause and time).
 * Handles play/pause UI and sends changes over the network using Fusion.
 */

using Fusion;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class VideoSyncController : NetworkBehaviour
{
    [Header("Video Components")]
    public VideoPlayer videoPlayer;
    public RenderTexture renderTexture;

    [Header("UI Toolkit")]
    private Button videoButton;

    [Networked, OnChangedRender(nameof(OnVideoPlayStateChanged))]
    public bool IsPlaying { get; set; }

    [Networked, OnChangedRender(nameof(OnVideoTimeChanged))]
    public float VideoTime { get; set; }

    public Texture2D playButton;

    // Called when this component is spawned on the network
    public override void Spawned()
    {
        videoButton = UIManager.Instance.VideoButton;
        if (videoButton == null)
        {
            Debug.LogError("VideoSyncController: 'video' Button not found in UXML!");
            return;
        }
        videoButton.clicked += OnVideoButtonPressed;

        // Assign target render texture if set
        if (videoPlayer != null && renderTexture != null)
            videoPlayer.targetTexture = renderTexture;

        UpdateVideoUI();
        UpdateVideoPlayer();
    }

    // Called when video button is pressed
    private void OnVideoButtonPressed()
    {
        RpcTogglePlayPause((float)videoPlayer.time);
    }

    // Called on the server to toggle play/pause and sync time
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcTogglePlayPause(float requestTime)
    {
        IsPlaying = !IsPlaying;
        VideoTime = requestTime;
    }

    // Runs when play/pause state changes
    private void OnVideoPlayStateChanged()
    {
        UpdateVideoUI();
        UpdateVideoPlayer();
    }

    // Updates the play/pause icon
    private void UpdateVideoUI()
    {
        if (videoButton != null)
            videoButton.iconImage = IsPlaying ? null : playButton;
    }

    // Plays or pauses the video
    private void UpdateVideoPlayer()
    {
        if (videoPlayer == null) return;

        if (IsPlaying)
        {
            if (!videoPlayer.isPlaying)
                videoPlayer.Play();
        }
        else
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Pause();
        }
    }

    // Runs when video time changes (syncs all players)
    private void OnVideoTimeChanged()
    {
        if (videoPlayer == null) return;

        // Only jump if difference is significant
        if (Mathf.Abs((float)videoPlayer.time - VideoTime) > 0.1f)
        {
            videoPlayer.time = VideoTime;
        }
    }

    // Called every network tick on authority
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Continuously update VideoTime to keep in sync
        if (IsPlaying && videoPlayer.isPlaying)
        {
            VideoTime = (float)videoPlayer.time;
        }
    }
}
