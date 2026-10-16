using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashScreenManager : MonoBehaviour
{
    [SerializeField] private float splashDuration = 2f;
    [SerializeField] private string nextScene = "MainMenu";

    private bool advanced;

    private IEnumerator Start()
    {
        // Unscaled time: the splash must never hang if Time.timeScale is left at
        // 0 by anything upstream (issue #20, part 4).
        float elapsed = 0f;
        while (elapsed < splashDuration && !SkipRequested())
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Advance();
    }

    private void Update()
    {
        // Any tap / key skips the splash once it is on screen.
        if (SkipRequested())
            Advance();
    }

    private void Advance()
    {
        if (advanced)
            return;

        advanced = true;
        SceneManager.LoadScene(nextScene);
    }

    private static bool SkipRequested()
    {
        return Input.anyKeyDown || Input.touchCount > 0;
    }
}