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

        // A run that ended left the music stopped for its summary panel, and
        // AudioManager survives scene loads, so returning to the menu would
        // otherwise arrive silent. The menu always has music (issue #21).
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMusic();
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
    // The matching closes are handled by the shared PanelToggle.Close() on each
    // panel's own exit control, so this component only opens.
    public void OpenHowToPlay()
    {
        howToPlayPanel.SetActive(true);
    }

    //=========================
    // Settings
    //=========================
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    //=========================
    // Credits
    //=========================
    public void OpenCredits()
    {
        creditPanel.SetActive(true);
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