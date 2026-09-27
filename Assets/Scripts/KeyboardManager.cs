using UnityEngine;
using UnityEngine.UI;


public class KeyboardManager : MonoBehaviour
{

    public static KeyboardManager Instance;


    void Awake()
    {
        Instance = this;
    }


    public void PressLetter(string letter)
    {
        if (string.IsNullOrEmpty(letter) || GameManager.Instance == null)
            return;

        GameManager.Instance.CheckLetter(letter.ToUpperInvariant()[0]);

        RefreshKeys();
    }


    public void RefreshForNewWord()
    {
        RefreshKeys();
    }


    void RefreshKeys()
    {
        GameManager game = GameManager.Instance;

        if (game == null)
            return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            char letter = FirstLetter(button.name);

            button.interactable = letter == '\0' || !game.IsLetterUsed(letter);
        }
    }


    static char FirstLetter(string buttonName)
    {
        if (string.IsNullOrEmpty(buttonName))
            return '\0';

        char first = buttonName.ToUpperInvariant()[0];

        return char.IsLetter(first) ? first : '\0';
    }
}
