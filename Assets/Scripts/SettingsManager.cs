using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class SettingsManager : MonoBehaviour
{

    [Header("Settings")]
    public Toggle hintsToggle;
    public Toggle showCategoryToggle;
    public TMP_Dropdown difficultyDropdown;


    void Start()
    {
        LoadSettings();
    }


    public void ResetToDefault()
    {
        if (hintsToggle != null)
            hintsToggle.isOn = true;

        if (showCategoryToggle != null)
            showCategoryToggle.isOn = true;

        if (difficultyDropdown != null)
            difficultyDropdown.value = 0;
    }


    public void Save()
    {
        SaveSettings();

        gameObject.SetActive(false);
    }


    void SaveSettings()
    {
        PlayerPrefs.SetInt("Hints", hintsToggle != null && hintsToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("ShowCategory", showCategoryToggle != null && showCategoryToggle.isOn ? 1 : 0);

        if (difficultyDropdown != null)
            PlayerPrefs.SetInt("Difficulty", difficultyDropdown.value);

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
