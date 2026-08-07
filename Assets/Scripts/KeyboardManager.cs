using UnityEngine;
using UnityEngine.UI;


public class KeyboardManager : MonoBehaviour
{


    public void PressLetter(string letter)
    {

        char selectedLetter =
        letter.ToUpper()[0];


        GameManager.Instance
        .CheckLetter(selectedLetter);


        GetComponent<Button>()
        .interactable = false;

    }


}