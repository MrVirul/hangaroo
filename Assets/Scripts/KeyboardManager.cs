using UnityEngine;
using UnityEngine.UI;


public class KeyboardManager : MonoBehaviour
{
    public void PressLetter(string letter)
    {
        char selectedLetter = letter.ToUpper()[0];

        GameManager.Instance.CheckLetter(selectedLetter);

        DisableKey(letter);
    }

    private void DisableKey(string letter)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.name.Equals(letter, System.StringComparison.OrdinalIgnoreCase))
            {
                button.interactable = false;
                return;
            }
        }
    }
}