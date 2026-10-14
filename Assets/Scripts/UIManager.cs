using TMPro;
using UnityEngine;


public class UIManager : MonoBehaviour
{

    public static UIManager Instance;


    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject runCompletePanel;
    public GameObject pausePanel;
    public GameObject hud;

    public TMP_Text victoryAnswerText;
    public TMP_Text victoryScoreText;
    public TMP_Text gameOverScoreText;

    [Tooltip("Optional title shown on the run-complete panel (e.g. 'RUN COMPLETE').")]
    public TMP_Text runCompleteTitleText;
    [Tooltip("End-of-run summary lines shown on the run-complete panel.")]
    public TMP_Text runCompleteSummaryText;


    void Awake()
    {
        Instance = this;
        InitializePanels();
    }

    private void InitializePanels()
    {
        if (gameOverPanel != null)   gameOverPanel.SetActive(false);
        if (victoryPanel != null)    victoryPanel.SetActive(false);
        if (runCompletePanel != null) runCompletePanel.SetActive(false);
        if (pausePanel != null)      pausePanel.SetActive(false);
        if (hud != null)             hud.SetActive(true);

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


    /// <summary>
    /// Completing every word in a run: shows the distinct run-complete summary
    /// instead of looping straight into another word.
    /// </summary>
    public void ShowRunComplete(RunSummary summary, PersonalBest best)
    {
        if (runCompleteTitleText != null && summary != null)
        {
            runCompleteTitleText.text = summary.isNewBestScore ? "NEW BEST!" : "RUN COMPLETE";
        }

        if (runCompleteSummaryText != null && summary != null)
        {
            int accuracy = Mathf.RoundToInt(summary.Accuracy * 100f);
            int personalBest = best != null ? best.bestScore : summary.score;

            runCompleteSummaryText.text =
                "Score: " + summary.score + "\n" +
                "Words: " + summary.wordsSolved + "/" + summary.totalWords + "\n" +
                "Accuracy: " + accuracy + "%\n" +
                "Hints: " + summary.hintsUsed + "\n" +
                "Best streak: " + summary.bestStreak + "\n" +
                "Personal best: " + personalBest;
            runCompleteSummaryText.ForceMeshUpdate();
        }

        if (hud != null)
            hud.SetActive(false);

        if (runCompletePanel != null)
            runCompletePanel.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayVictory();
    }


    /// <summary>Hides every end-of-run panel and restores the HUD.</summary>
    public void HideEndPanels()
    {
        if (victoryPanel != null)    victoryPanel.SetActive(false);
        if (gameOverPanel != null)   gameOverPanel.SetActive(false);
        if (runCompletePanel != null) runCompletePanel.SetActive(false);

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