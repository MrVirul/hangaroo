using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditPanel;

    [Header("Scene Names")]
    [SerializeField] private string gameplayScene = "GamePlay";

    private void Start()
    {
        howToPlayPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditPanel.SetActive(false);
    }

    //=========================
    // Play Button
    //=========================
    public void PlayGame()
    {
        SceneManager.LoadScene(gameplayScene);
    }

    //=========================
    // How To Play
    //=========================
    public void OpenHowToPlay()
    {
        howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel.SetActive(false);
    }

    //=========================
    // Settings
    //=========================
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    //=========================
    // Credits
    //=========================
    public void OpenCredits()
    {
        creditPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        creditPanel.SetActive(false);
    }

    //=========================
    // Quit
    //=========================
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}