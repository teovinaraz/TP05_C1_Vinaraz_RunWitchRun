using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Text popupText;
    [SerializeField] private GameObject broomIndicator;
    [SerializeField] private RectTransform broomFill;
    [SerializeField] private GameObject invincibleIndicator;
    [SerializeField] private RectTransform invincibleFill;
    [SerializeField] private GameObject livesDisplay;
    [SerializeField] private Text livesText;

    [Header("Paneles")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private SettingsMenu settingsMenu;
    [SerializeField] private PauseManager pauseManager;

    [Header("Menu de pausa")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button pauseMainMenuButton;

    [Header("Game Over")]
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private GameObject newRecordLabel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button gameOverMenuButton;

    [Header("Tiempos")]
    [SerializeField] private float gameOverDelay = 0.9f;
    [SerializeField] private float popupDuration = 1.1f;
    [SerializeField] private float popupRise = 60f;

    private GameManager gm;
    private Coroutine popupRoutine;
    private Vector2 popupBasePosition;

    public bool IsSettingsOpen => settingsMenu != null && settingsMenu.gameObject.activeSelf;

    private void Start()
    {
        gm = GameManager.Instance;

        popupBasePosition = popupText.rectTransform.anchoredPosition;
        popupText.text = string.Empty;
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        settingsMenu.gameObject.SetActive(false);
        broomIndicator.SetActive(false);
        invincibleIndicator.SetActive(false);
        livesDisplay.SetActive(false);

        pauseButton.onClick.AddListener(pauseManager.Pause);
        resumeButton.onClick.AddListener(pauseManager.Resume);
        pauseSettingsButton.onClick.AddListener(OpenSettings);
        pauseMainMenuButton.onClick.AddListener(pauseManager.GoToMainMenu);
        retryButton.onClick.AddListener(gm.RestartGame);
        gameOverMenuButton.onClick.AddListener(gm.LoadMainMenu);
        settingsMenu.Closed += OnSettingsClosed;

        gm.ScoreChanged += OnScoreChanged;
        gm.ScoreAdded += OnScoreAdded;
        gm.StateChanged += OnStateChanged;
        gm.GameEnded += OnGameEnded;
        if (gm.Player != null)
        {
            gm.Player.BroomStarted += OnBroomStarted;
            gm.Player.InvincibilityStarted += OnInvincibilityStarted;
            gm.Player.ExtraLifeUsed += OnExtraLifeUsed;
            gm.Player.ExtraLivesChanged += OnExtraLivesChanged;
        }

        OnScoreChanged(gm.Score);
        highScoreText.text = "BEST: " + FormatScore(gm.HighScore);
    }

    private void OnDestroy()
    {
        if (gm == null)
        {
            return;
        }
        gm.ScoreChanged -= OnScoreChanged;
        gm.ScoreAdded -= OnScoreAdded;
        gm.StateChanged -= OnStateChanged;
        gm.GameEnded -= OnGameEnded;
        if (gm.Player != null)
        {
            gm.Player.BroomStarted -= OnBroomStarted;
            gm.Player.InvincibilityStarted -= OnInvincibilityStarted;
            gm.Player.ExtraLifeUsed -= OnExtraLifeUsed;
            gm.Player.ExtraLivesChanged -= OnExtraLivesChanged;
        }
    }

    private void Update()
    {
        if (gm == null)
        {
            return;
        }

        PlayerController player = gm.Player;
        bool broom = player != null && player.IsBroomActive && gm.State != GameState.GameOver;
        if (broomIndicator.activeSelf != broom)
        {
            broomIndicator.SetActive(broom);
        }
        if (broom)
        {
            broomFill.localScale = new Vector3(player.BroomTimeNormalized, 1f, 1f);
        }

        bool invincible = player != null && player.IsInvincible && gm.State != GameState.GameOver;
        if (invincibleIndicator.activeSelf != invincible)
        {
            invincibleIndicator.SetActive(invincible);
        }
        if (invincible)
        {
            invincibleFill.localScale = new Vector3(player.InvincibleNormalized, 1f, 1f);
        }

        if (gm.State == GameState.GameOver && gameOverPanel.activeSelf && GameInput.RetryPressed)
        {
            gm.RestartGame();
        }
    }

    private void OnScoreChanged(int score)
    {
        scoreText.text = "SCORE: " + FormatScore(score);
    }

    private void OnScoreAdded(int amount)
    {
        ShowPopup("+" + amount);
    }

    private void OnBroomStarted()
    {
        ShowPopup("MAGIC BROOM!  x" + gm.Player.Data.broomSpeedMultiplier.ToString("0.#") + " SPEED");
    }

    private void OnInvincibilityStarted()
    {
        ShowPopup("INVINCIBLE!");
    }

    private void OnExtraLifeUsed()
    {
        ShowPopup("SAVED! -1 LIFE");
    }

    private void OnExtraLivesChanged(int count)
    {
        livesText.text = "LIVES: " + count;
        livesDisplay.SetActive(count > 0);
    }

    private void OnStateChanged(GameState state)
    {
        pausePanel.SetActive(state == GameState.Paused && !IsSettingsOpen);
        if (state != GameState.Paused)
        {
            settingsMenu.gameObject.SetActive(false);
        }
        pauseButton.gameObject.SetActive(state == GameState.Playing);
    }

    private void OnGameEnded()
    {
        StartCoroutine(ShowGameOver());
    }

    private IEnumerator ShowGameOver()
    {
        pauseButton.gameObject.SetActive(false);
        yield return new WaitForSecondsRealtime(gameOverDelay);

        finalScoreText.text = "SCORE: " + FormatScore(gm.Score);
        bestScoreText.text = "BEST: " + FormatScore(gm.HighScore);
        newRecordLabel.SetActive(gm.IsNewHighScore);
        highScoreText.text = "BEST: " + FormatScore(gm.HighScore);
        gameOverPanel.SetActive(true);
    }

    private void OpenSettings()
    {
        pausePanel.SetActive(false);
        settingsMenu.Open();
    }

    public void CloseSettings()
    {
        settingsMenu.Close();
    }

    private void OnSettingsClosed()
    {
        pausePanel.SetActive(gm != null && gm.State == GameState.Paused);
    }

    private void ShowPopup(string message)
    {
        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }
        popupRoutine = StartCoroutine(PopupRoutine(message));
    }

    private IEnumerator PopupRoutine(string message)
    {
        popupText.text = message;
        Color baseColor = popupText.color;
        float t = 0f;
        while (t < popupDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popupDuration);
            popupText.rectTransform.anchoredPosition = popupBasePosition + Vector2.up * (k * popupRise);
            popupText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
            yield return null;
        }
        popupText.text = string.Empty;
        popupText.color = baseColor;
        popupText.rectTransform.anchoredPosition = popupBasePosition;
    }

    private static string FormatScore(int score)
    {
        return score.ToString("000000");
    }
}
