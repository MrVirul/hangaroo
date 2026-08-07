using UnityEngine;


public class UIManager : MonoBehaviour
{

    public static UIManager Instance;


    [Header("UI")]
    public GameObject victoryPanel;
    public GameObject gameOverPanel;


    void Awake()
    {
        Instance = this;
    }


    public void ShowVictory()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }


    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

}