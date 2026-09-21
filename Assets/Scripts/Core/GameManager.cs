using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public const string HighScoreKey = "RWR_HighScore";
    public const string MainMenuScene = "MainMenu";

    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerController player;

    private float scoreAccumulator;

    public PlayerController Player => player;
    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public bool IsNewHighScore { get; private set; }
    public float PlayTime { get; private set; }

    public float WorldSpeed => (IsPlaying && player != null) ? player.CurrentSpeed : 0f;

    public event Action<int> ScoreChanged;
    public event Action<int> ScoreAdded;
    public event Action<GameState> StateChanged;
    public event Action GameEnded;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        State = GameState.Playing;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!IsPlaying)
        {
            return;
        }

        PlayTime += Time.deltaTime;
        scoreAccumulator += player.Data.survivalPointsPerSecond * Time.deltaTime;
        int whole = Mathf.FloorToInt(scoreAccumulator);
        if (whole > 0)
        {
            scoreAccumulator -= whole;
            Score += whole;
            ScoreChanged?.Invoke(Score);
        }
    }

    public void AddScore(int amount)
    {
        if (!IsPlaying)
        {
            return;
        }
        Score += amount;
        ScoreChanged?.Invoke(Score);
        ScoreAdded?.Invoke(amount);
    }

    public void Pause()
    {
        if (State == GameState.Playing)
        {
            SetState(GameState.Paused);
        }
    }

    public void Resume()
    {
        if (State == GameState.Paused)
        {
            SetState(GameState.Playing);
        }
    }

    public void EndGame()
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        SetState(GameState.GameOver);

        IsNewHighScore = Score > HighScore;
        if (IsNewHighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }
        GameEnded?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    private void SetState(GameState newState)
    {
        State = newState;
        // la UI sigue andando porque no depende del timeScale
        Time.timeScale = newState == GameState.Paused ? 0f : 1f;
        StateChanged?.Invoke(newState);
    }
}
