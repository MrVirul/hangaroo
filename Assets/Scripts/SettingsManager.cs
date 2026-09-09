using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SettingsManager : MonoBehaviour
{

    [Header("Settings")]
    public Toggle hintsToggle;
    public Toggle showCategoryToggle;
    public TMP_Dropdown difficultyDropdown;

    [Header("Audio")]
    public Scrollbar volumeSlider;
    public Button clickSoundButton;
    public Button gameSoundsButton;
    public Button bgMusicButton;

    [Header("Audio Visuals")]
    [SerializeField] private Color enabledColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);


    void Start()
    {
        LoadSettings();
        RefreshAudioSettings();
    }


    public void OpenSettings()
    {
        gameObject.SetActive(true);
    }


    public void ResetToDefault()
    {
        if (hintsToggle != null)
            hintsToggle.isOn = true;

        if (showCategoryToggle != null)
            showCategoryToggle.isOn = true;

        if (difficultyDropdown != null)
            difficultyDropdown.value = 0;

        ApplyVolume(1f);
        ApplyClickSound(true);
        ApplyGameSounds(true);
        ApplyBgMusic(true);

        RefreshAudioSettings();
    }


    public void Save()
    {
        SaveSettings();

        gameObject.SetActive(false);
    }


    public void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);
    }


    public void ToggleClickSound()
    {
        if (AudioManager.Instance == null)
            return;

        bool enabled = !AudioManager.Instance.ClickEnabled;

        ApplyClickSound(enabled);

        if (enabled)
            AudioManager.Instance.PlayClick();
    }


    public void ToggleGameSounds()
    {
        if (AudioManager.Instance == null)
            return;

        bool enabled = !AudioManager.Instance.GameSoundsEnabled;

        ApplyGameSounds(enabled);
    }


    public void ToggleBgMusic()
    {
        if (AudioManager.Instance == null)
            return;

        bool enabled = !AudioManager.Instance.MusicEnabled;

        ApplyBgMusic(enabled);
    }


    void ApplyVolume(float value)
    {
        if (volumeSlider != null)
            volumeSlider.value = value;
        else if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);
    }


    void ApplyClickSound(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetClickEnabled(enabled);

        UpdateButtonVisual(clickSoundButton, enabled);
    }


    void ApplyGameSounds(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGameSoundsEnabled(enabled);

        UpdateButtonVisual(gameSoundsButton, enabled);
    }


    void ApplyBgMusic(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicEnabled(enabled);

        UpdateButtonVisual(bgMusicButton, enabled);
    }


    void RefreshAudioSettings()
    {
        if (AudioManager.Instance == null)
            return;

        if (volumeSlider != null)
            volumeSlider.value = AudioManager.Instance.SfxVolume;

        UpdateButtonVisual(clickSoundButton, AudioManager.Instance.ClickEnabled);
        UpdateButtonVisual(gameSoundsButton, AudioManager.Instance.GameSoundsEnabled);
        UpdateButtonVisual(bgMusicButton, AudioManager.Instance.MusicEnabled);
    }


    void UpdateButtonVisual(Button button, bool enabled)
    {
        if (button == null)
            return;

        Graphic graphic = button.targetGraphic != null
            ? button.targetGraphic as Graphic
            : null;

        if (graphic == null)
            graphic = button.image;

        if (graphic != null)
            graphic.color = enabled ? enabledColor : disabledColor;
    }


    void SaveSettings()
    {
        PlayerPrefs.SetInt("Hints", hintsToggle != null && hintsToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("ShowCategory", showCategoryToggle != null && showCategoryToggle.isOn ? 1 : 0);

        if (difficultyDropdown != null)
            PlayerPrefs.SetInt("Difficulty", difficultyDropdown.value);

        if (AudioManager.Instance != null)
        {
            PlayerPrefs.SetFloat("SfxVolume", AudioManager.Instance.SfxVolume);
            PlayerPrefs.SetInt("ClickSound", AudioManager.Instance.ClickEnabled ? 1 : 0);
            PlayerPrefs.SetInt("GameSounds", AudioManager.Instance.GameSoundsEnabled ? 1 : 0);
            PlayerPrefs.SetInt("MusicEnabled", AudioManager.Instance.MusicEnabled ? 1 : 0);
        }

        PlayerPrefs.Save();
    }


    void LoadSettings()
    {
        if (hintsToggle != null)
            hintsToggle.isOn = PlayerPrefs.GetInt("Hints", 1) == 1;

        if (showCategoryToggle != null)
            showCategoryToggle.isOn = PlayerPrefs.GetInt("ShowCategory", 1) == 1;

        if (difficultyDropdown != null)
            difficultyDropdown.value = PlayerPrefs.GetInt("Difficulty", 0);
    }

}