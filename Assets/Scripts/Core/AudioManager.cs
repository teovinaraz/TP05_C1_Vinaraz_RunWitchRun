using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";
    private const string MusicPrefKey = "RWR_MusicVolume";
    private const string SfxPrefKey = "RWR_SfxVolume";
    private const float MinDb = -80f;
    private const float MinLinear = 0.0001f;

    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Musica")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("SFX")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip gemClip;
    [SerializeField] private AudioClip powerUpClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip buttonClip;

    [Header("Volumenes por defecto (0-1)")]
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.9f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private float musicVolume;
    private float sfxVolume;

    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.outputAudioMixerGroup = musicGroup;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.outputAudioMixerGroup = sfxGroup;

        musicVolume = PlayerPrefs.GetFloat(MusicPrefKey, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxPrefKey, defaultSfxVolume);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }
        // el mixer no toma bien SetFloat en Awake, por eso va en Start
        ApplyVolumes();
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVolumes();
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        PlayMusic(sceneName == gameplaySceneName ? gameplayMusic : menuMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying))
        {
            return;
        }
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void SetMusicVolume(float linear01)
    {
        musicVolume = Mathf.Clamp01(linear01);
        PlayerPrefs.SetFloat(MusicPrefKey, musicVolume);
        SetMixer(MusicParam, musicVolume);
    }

    public void SetSfxVolume(float linear01)
    {
        sfxVolume = Mathf.Clamp01(linear01);
        PlayerPrefs.SetFloat(SfxPrefKey, sfxVolume);
        SetMixer(SfxParam, sfxVolume);
    }

    private void ApplyVolumes()
    {
        SetMixer(MusicParam, musicVolume);
        SetMixer(SfxParam, sfxVolume);
    }

    private void SetMixer(string parameter, float linear)
    {
        if (mixer == null)
        {
            return;
        }
        // slider lineal a decibeles
        float db = linear <= MinLinear ? MinDb : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(parameter, db);
    }

    public void PlayJump() { PlaySfx(jumpClip); }
    public void PlayLand() { PlaySfx(landClip); }
    public void PlayGem() { PlaySfx(gemClip); }
    public void PlayPowerUp() { PlaySfx(powerUpClip); }
    public void PlayGameOver() { PlaySfx(gameOverClip); }
    public void PlayButton() { PlaySfx(buttonClip); }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
