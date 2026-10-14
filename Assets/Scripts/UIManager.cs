using TMPro;
using UnityEngine;


public class UIManager : MonoBehaviour
{

    public static UIManager Instance;


    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject pausePanel;
    public GameObject hud;

    public TMP_Text victoryAnswerText;
    public TMP_Text victoryScoreText;
    public TMP_Text gameOverScoreText;


    void Awake()
    {
        Instance = this;
        InitializePanels();
    }

    private void InitializePanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null)  victoryPanel.SetActive(false);
        if (pausePanel != null)    pausePanel.SetActive(false);
        if (hud != null)           hud.SetActive(true);

        Transform settings = transform.Find("Settings_Panel");
        if (settings != null) settings.gameObject.SetActive(false);

        Transform howToPlay = transform.Find("How to Play Panel");
        if (howToPlay != null) howToPlay.gameObject.SetActive(false);
    }



    public void ShowGameOver(string score = null)
    {
        if (string.IsNullOrEmpty(score) && GameManager.Instance != null)
        {
            score = GameManager.Instance.CurrentScore.ToString();
        }

        if (gameOverScoreText == null && gameOverPanel != null)
        {
            var t = gameOverPanel.transform.Find("Board/score");
            if (t != null)
                gameOverScoreText = t.GetComponent<TMP_Text>();
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Score: " + (score ?? "0");
            gameOverScoreText.ForceMeshUpdate();
        }

        if (hud != null)
            hud.SetActive(false);

        gameOverPanel.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayDefeat();
    }



    public void ShowVictory(string answer, string score)
    {
        if (victoryAnswerText != null)
            victoryAnswerText.text = answer;

        if (victoryScoreText != null)
            victoryScoreText.text = "Score: " + score;

        if (hud != null)
            hud.SetActive(false);

        victoryPanel.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVictory();
    }


    public void HideVictory()
    {
        victoryPanel.SetActive(false);

        if (hud != null)
            hud.SetActive(true);
    }



    public void Pause()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0;
    }



    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1;
    }

}