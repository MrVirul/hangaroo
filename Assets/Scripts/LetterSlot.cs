using TMPro;
using UnityEngine;


public class LetterSlot : MonoBehaviour
{

    public TMP_Text letterText;


    private char letter;


    public bool isRevealed;



    public void SetLetter(char value)
    {
        letter = value;
        letterText.text = "";
        isRevealed = false;
    }



    public void ShowLetter()
    {
        letterText.text = letter.ToString();
        isRevealed = true;
    }



    public char GetLetter()
    {
        return letter;
    }

}