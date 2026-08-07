using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;


    [Header("Word Data")]
    public TextAsset wordsFile;

    private List<WordData> words = new List<WordData>();


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

        LoadWords();
    }


    void Start()
    {
        LoadWord();
    }


    void LoadWords()
    {
        if (wordsFile == null)
        {
            Debug.LogWarning("Words file not assigned in the Inspector.");
            return;
        }

        WordList data = JsonUtility.FromJson<WordList>(wordsFile.text);

        if (data == null || data.words == null)
        {
            Debug.LogWarning("Words file is empty or malformed.");
            return;
        }

        foreach (WordData word in data.words)
        {
            words.Add(new WordData(word.clue, word.answer));
        }
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