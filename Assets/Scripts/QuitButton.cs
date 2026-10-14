using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hides a "Quit" button on platforms where <see cref="Application.Quit"/> does
/// nothing, so the button never promises something it cannot do (issue #16,
/// part 1). On the Editor the pause-menu quit path exits play mode instead, so
/// the button stays usable.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuitButton : MonoBehaviour
{
    void Awake()
    {
        if (IsQuitUnsupported())
            gameObject.SetActive(false);
    }

    /// <summary>
    /// True on platforms where quitting the application is not supported.
    /// WebGL has no way to close the browser tab from script, so the button is
    /// hidden there.
    /// </summary>
    public static bool IsQuitUnsupported()
    {
        return Application.platform == RuntimePlatform.WebGLPlayer;
    }
}
