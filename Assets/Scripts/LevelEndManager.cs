using UnityEngine;

/// <summary>
/// Handles the Game Over and Victory sequences: activates the matching panel,
/// grounds the panel's "Result" board on the bottom-centre of the visible
/// screen (above device cutouts), and swaps BGM/SFX audio.
///
/// Attach to any full-screen RectTransform (e.g. the Canvas or HUD) so the
/// safe-area to canvas-unit conversion uses the correct reference rect.
/// Assign every serialized field from the Inspector.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LevelEndManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Result")]
    [SerializeField, Tooltip("The 'Result' child GameObject inside the Game Over panel.")]
    private RectTransform gameOverResult;
    [SerializeField, Tooltip("The 'Result' child GameObject inside the Victory panel.")]
    private RectTransform victoryResult;
    [SerializeField, Tooltip("Extra distance kept above the safe-area bottom edge, in canvas units.")]
    private float groundMargin = 8f;

    [Header("Audio")]
    [SerializeField, Tooltip("Background music AudioSource, stopped while a result panel is shown.")]
    private AudioSource bgmSource;
    [SerializeField, Tooltip("Sound effects AudioSource used to play the result clips.")]
    private AudioSource sfxSource;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    private RectTransform reference;

    private void Awake()
    {
        reference = transform as RectTransform;
    }

    public void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
        StopBgm();
        PlayClip(victorySound);
        GroundResult(victoryResult);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        StopBgm();
        PlayClip(defeatSound);
        GroundResult(gameOverResult);
    }

    public void ReturnToMainMenu()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        RestartBgm();
    }

    /// <summary>
    /// Pins the Result board to (0.5, 0) bottom-centre with pivot (0.5, 0) and
    /// offsets its Y so it sits just above the safe-area bottom edge, keeping it
    /// clear of the iPad home indicator and grounded on the background art.
    /// </summary>
    private void GroundResult(RectTransform result)
    {
        if (result == null)
        {
            return;
        }
        if (reference == null)
        {
            reference = transform as RectTransform;
        }

        float safeY = Screen.safeArea.yMin / Screen.height;
        float groundY = safeY * reference.rect.height + groundMargin;

        result.anchorMin = new Vector2(0.5f, 0f);
        result.anchorMax = new Vector2(0.5f, 0f);
        result.pivot = new Vector2(0.5f, 0f);
        result.anchoredPosition = new Vector2(0f, groundY);
    }

    private void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    private void RestartBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.Play();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}