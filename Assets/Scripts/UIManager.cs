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


    void Awake()
    {
        Instance = this;
    }



    public void ShowGameOver()
    {
        if (hud != null)
            hud.SetActive(false);

        gameOverPanel.SetActive(true);
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