using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
        return SkipRequested(Keyboard.current, Pointer.current);
    }

    /// <summary>
    /// True when the given devices report a skip gesture this frame.
    ///
    /// The project runs with Active Input Handling = "Input System Package
    /// (New)" (ProjectSettings.asset, activeInputHandler: 1), where the legacy
    /// UnityEngine.Input API throws InvalidOperationException on first use, so
    /// the gesture is read through the package instead (issue #60). A Pointer is
    /// any mouse, touch or pen, covering the old Input.anyKeyDown (which
    /// included mouse buttons) and Input.touchCount > 0.
    ///
    /// Taking the devices as parameters keeps the query testable without a live
    /// splash scene; the parameterless overload supplies the current devices.
    /// </summary>
    public static bool SkipRequested(Keyboard keyboard, Pointer pointer)
    {
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return true;

        if (pointer != null && pointer.press.wasPressedThisFrame)
            return true;

        return false;
    }
}
