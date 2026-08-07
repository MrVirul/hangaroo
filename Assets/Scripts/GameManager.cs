using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;


    [Header("Word Data")]
    public List<WordData> words = new List<WordData>()
    {
        new WordData("A planet we live on", "EARTH"),
        new WordData("A programming language", "JAVA"),
        new WordData("Largest land animal", "ELEPHANT"),
    };


    private WordData currentWord;


    [Header("UI")]
    public TMP_Text clueText;


    [Header("Letter Slots")]
    public GameObject letterSlotPrefab;
    public Transform letterSlotParent;


    private List<LetterSlot> slots = new List<LetterSlot>();


    void Awake()
    {
        Instance = this;
    }


    void Start()
    {
        LoadWord();
    }


    void LoadWord()
    {

        currentWord = words[
            Random.Range(0, words.Count)
        ];


        clueText.text = currentWord.clue;


        CreateLetterSlots();

    }



    void CreateLetterSlots()
    {

        foreach (char letter in currentWord.answer)
        {

            GameObject obj =
            Instantiate(
                letterSlotPrefab,
                letterSlotParent
            );


            LetterSlot slot =
            obj.GetComponent<LetterSlot>();


            slot.SetLetter(letter);


            slots.Add(slot);

        }

    }



    public void CheckLetter(char guess)
    {

        bool correct = false;


        foreach (LetterSlot slot in slots)
        {

            if (slot.GetLetter() == guess)
            {
                slot.ShowLetter();
                correct = true;
            }

        }



        if (correct == false)
        {
            HeartManager.Instance.LoseHeart();
        }



        CheckWin();

    }



    void CheckWin()
    {

        foreach (LetterSlot slot in slots)
        {
            if (!slot.isRevealed)
                return;
        }


        Debug.Log("Victory");

        UIManager.Instance.ShowVictory();

    }

}