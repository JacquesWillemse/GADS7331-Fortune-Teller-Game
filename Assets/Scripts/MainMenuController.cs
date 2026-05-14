using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: wire <see cref="startButton"/> to load <see cref="gameSceneName"/> (must be in Build Settings).</summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MainScene";
    [SerializeField] private Button startButton;

    void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(LoadGameScene);
    }

    void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(LoadGameScene);
    }

    public void LoadGameScene()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[MainMenuController] No game scene name set.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
