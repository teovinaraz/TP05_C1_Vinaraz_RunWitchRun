using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    public bool IsPaused => GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;

    private void Update()
    {
        if (GameInput.PausePressed)
        {
            if (uiManager != null && uiManager.IsSettingsOpen)
            {
                uiManager.CloseSettings();
                return;
            }
            TogglePause();
        }
    }

#if !UNITY_EDITOR
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Pause();
        }
    }
#endif

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Pause();
        }
    }

    public void Resume()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Resume();
        }
    }

    public void GoToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
    }
}
