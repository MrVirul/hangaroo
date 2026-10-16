using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hides the control it is attached to on platforms where <c>Application.Quit()</c>
/// is a no-op (WebGL), so the player is never offered a button that cannot do
/// anything (issue #20, part 1).
///
/// Attach to a Quit button; the button keeps working normally everywhere else.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlatformQuitButton : MonoBehaviour
{
    private void Awake()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            gameObject.SetActive(false);
    }
}
