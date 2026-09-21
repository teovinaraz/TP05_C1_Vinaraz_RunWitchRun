using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private GameObject buttonsRoot;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private string gameplayScene = "Gameplay";
    [SerializeField] private string creditsScene = "Credits";

    private void Start()
    {
        Time.timeScale = 1f;
        settingsMenu.gameObject.SetActive(false);

        playButton.onClick.AddListener(() => SceneManager.LoadScene(gameplayScene));
        creditsButton.onClick.AddListener(() => SceneManager.LoadScene(creditsScene));
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(Quit);
        settingsMenu.Closed += () => buttonsRoot.SetActive(true);

        int best = PlayerPrefs.GetInt(GameManager.HighScoreKey, 0);
        bestScoreText.text = "BEST SCORE: " + best.ToString("000000");
    }

    private void Update()
    {
        if (GameInput.CancelPressed && settingsMenu.gameObject.activeSelf)
        {
            settingsMenu.Close();
        }
    }

    private void OpenSettings()
    {
        buttonsRoot.SetActive(false);
        settingsMenu.Open();
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
