using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text musicValueText;
    [SerializeField] private Text sfxValueText;
    [SerializeField] private Button backButton;
    [SerializeField] private float sfxPreviewCooldown = 0.15f;

    private float lastPreviewTime = -10f;

    public event Action Closed;

    private void Awake()
    {
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        backButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        AudioManager audio = AudioManager.Instance;
        float music = audio != null ? audio.MusicVolume : 0.8f;
        float sfx = audio != null ? audio.SfxVolume : 0.9f;
        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
        RefreshLabels();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    private void OnMusicChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
        RefreshLabels();
    }

    private void OnSfxChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(value);
            if (Time.unscaledTime - lastPreviewTime > sfxPreviewCooldown)
            {
                lastPreviewTime = Time.unscaledTime;
                AudioManager.Instance.PlayGem();
            }
        }
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100f) + "%";
        sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
    }
}
