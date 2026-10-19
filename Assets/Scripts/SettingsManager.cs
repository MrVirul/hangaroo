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
        else
            PlayerPrefs.SetInt("Hints", 1);

        if (showCategoryToggle != null)
            showCategoryToggle.isOn = true;
        else
            PlayerPrefs.SetInt("ShowCategory", 1);

        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify(DifficultyIdExtensions.Default.ToPlayerPrefs());
            difficultyDropdown.RefreshShownValue();
        }

        ApplyVolume(1f);
        ApplyClickSound(true);
        ApplyGameSounds(true);
        ApplyBgMusic(true);

        // "Reset To Default" reads as a committed action, so write the values
        // through and close the panel exactly like Save does.
        SaveSettings();
        RefreshAudioSettings();
        RefreshGameplay();
        gameObject.SetActive(false);
    }


    public void Save()
    {
        SaveSettings();
        RefreshGameplay();
        gameObject.SetActive(false);
    }


    void RefreshGameplay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshClue();
            GameManager.Instance.RefreshHint();
        }
    }


    public void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);

        PersistVolume(value);
    }


    /// <summary>
    /// Applied the moment the dropdown changes (not only on Save), so the
    /// difficulty the player selects is live immediately.
    /// </summary>
    public void OnDifficultyChanged(int index)
    {
        DifficultyLibrary.SetCurrent(DifficultyIdExtensions.FromPlayerPrefs(index));
    }


    public void OnHintsChanged(bool enabled)
    {
        PersistToggle("Hints", enabled);
        RefreshGameplay();
    }


    public void OnShowCategoryChanged(bool enabled)
    {
        PersistToggle("ShowCategory", enabled);
        RefreshGameplay();
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

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSfxVolume(value);

        PersistVolume(value);
    }


    void ApplyClickSound(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetClickEnabled(enabled);

        PersistToggle("ClickSound", enabled);
        UpdateButtonVisual(clickSoundButton, enabled);
    }


    void ApplyGameSounds(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetGameSoundsEnabled(enabled);

        PersistToggle("GameSounds", enabled);
        UpdateButtonVisual(gameSoundsButton, enabled);
    }


    void ApplyBgMusic(bool enabled)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicEnabled(enabled);

        PersistToggle("MusicEnabled", enabled);
        UpdateButtonVisual(bgMusicButton, enabled);
    }


    // Audio changes are written through on every interaction so the panel's exit
    // path is no longer load-bearing: closing it by any route keeps the choice.
    static void PersistToggle(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }


    static void PersistVolume(float value)
    {
        PlayerPrefs.SetFloat("SfxVolume", value);
        PlayerPrefs.Save();
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

        // The wide Game Sounds pill carries its own label and spells the state
        // out; the two small square toggles have no label and stay colour-coded.
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = enabled ? "ON" : "OFF";
    }


    void SaveSettings()
    {
        if (hintsToggle != null)
            PlayerPrefs.SetInt("Hints", hintsToggle.isOn ? 1 : 0);
        else if (!PlayerPrefs.HasKey("Hints") || PlayerPrefs.GetInt("Hints", 1) == 0)
            PlayerPrefs.SetInt("Hints", 1);

        if (showCategoryToggle != null)
            PlayerPrefs.SetInt("ShowCategory", showCategoryToggle.isOn ? 1 : 0);
        else if (!PlayerPrefs.HasKey("ShowCategory") || PlayerPrefs.GetInt("ShowCategory", 1) == 0)
            PlayerPrefs.SetInt("ShowCategory", 1);

        if (difficultyDropdown != null)
            DifficultyLibrary.SetCurrent(DifficultyIdExtensions.FromPlayerPrefs(difficultyDropdown.value));

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
        // SetValueWithoutNotify / SetIsOnWithoutNotify: loading the persisted
        // values must not fire the change handlers (which would re-write the very
        // keys we are reading).
        if (hintsToggle != null)
            hintsToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Hints", 1) == 1);

        if (showCategoryToggle != null)
            showCategoryToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("ShowCategory", 1) == 1);

        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify(DifficultyLibrary.Current.ToPlayerPrefs());
            difficultyDropdown.RefreshShownValue();
        }
    }

}