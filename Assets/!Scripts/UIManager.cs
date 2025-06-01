/*
 * UIManager.cs
 * ------------
 * SUMMARY:
 * Caches and manages access to important UI Toolkit elements in the scene.
 * Uses singleton pattern for easy global access.
 */

using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    // Singleton instance (global access)
    public static UIManager Instance { get; private set; }

    // Cached UI references
    public UIDocument UIDocument { get; private set; }
    public Label RoomCodeLabel { get; private set; }
    public Label UsersCountLabel { get; private set; }
    public Button VideoButton { get; private set; }
    // Add more UI elements here if needed

    private void Awake()
    {
        // Singleton pattern: only one allowed
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Persist across scenes if needed

        // Cache the main UIDocument and relevant UI controls
        UIDocument = FindFirstObjectByType<UIDocument>();
        if (UIDocument != null)
        {
            var root = UIDocument.rootVisualElement;
            RoomCodeLabel = root.Q<Label>("RoomCode");
            UsersCountLabel = root.Q<Label>("UsersCount");
            VideoButton = root.Q<Button>("Video");
            // Add more queries for other UI elements as required
        }
        else
        {
            Debug.LogWarning("UIDocument not found in scene!");
        }
    }
}
