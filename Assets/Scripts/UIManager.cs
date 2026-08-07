using UnityEngine;


public class UIManager : MonoBehaviour
{

    public static UIManager Instance;


    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject pausePanel;



    void Awake()
    {
        Instance = this;
    }



    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }



    public void ShowVictory()
    {
        victoryPanel.SetActive(true);
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