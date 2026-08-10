using UnityEngine;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    private void Start()
    {
        Button[] buttons = Object.FindObjectsOfType<Button>(true);

        foreach (Button button in buttons)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();
    }
}
