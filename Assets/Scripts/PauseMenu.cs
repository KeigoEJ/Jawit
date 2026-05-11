using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")] 
    [Tooltip("Optional. If set, this GameObject will be enabled/disabled when pausing.")]
    [SerializeField] private GameObject pausePanel;

    [Header("Settings")]
    [Tooltip("If true, Time.timeScale will be set to 0 when paused.")]
    [SerializeField] private bool pauseTime = true;

    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        // ESC toggles pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (pauseTime)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // Allow UI interaction with mouse
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    // Called by Continue button
    public void Continue()
    {
        ResumeGame();
    }

    // Called by Cancel button (Back)
    public void Cancel()
    {
        ResumeGame();
    }

    private void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseTime)
            Time.timeScale = previousTimeScale;

        // Keep cursor as-is to avoid interfering with player/controller setup.
    }
}
