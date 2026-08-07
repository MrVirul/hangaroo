using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;


    [Header("Word Data")]
    public TextAsset wordsFile;

    private List<WordData> words = new List<WordData>();


    private WordData currentWord;


    [Header("UI")]
    public TMP_Text clueText;
    public TMP_Text scoreText;


    private int score;
    private int revealedCount;


    [Header("Letter Slots")]
    public GameObject letterSlotPrefab;
    public Transform letterSlotParent;


    private List<LetterSlot> slots = new List<LetterSlot>();

    private List<int> wordOrder = new List<int>();
    private int wordPosition;


    void Awake()
    {
        Instance = this;

        LoadWords();
        BuildWordOrder();
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


    void BuildWordOrder()
    {
        wordOrder.Clear();

        for (int i = 0; i < words.Count; i++)
            wordOrder.Add(i);

        for (int i = 0; i < wordOrder.Count; i++)
        {
            int swap = Random.Range(i, wordOrder.Count);
            int temp = wordOrder[i];
            wordOrder[i] = wordOrder[swap];
            wordOrder[swap] = temp;
        }

        wordPosition = 0;
    }


    void LoadWord()
    {
        if (words.Count == 0 || wordPosition >= words.Count)
        {
            wordPosition = 0;

            if (words.Count == 0)
                return;
        }

        currentWord = words[wordOrder[wordPosition]];
        wordPosition++;

        revealedCount = 0;

        clueText.text = currentWord.clue;

        CreateLetterSlots();
    }


    public void ContinueGame()
    {
        ClearSlots();

        if (HeartManager.Instance.HasLives())
        {
            UIManager.Instance.HideVictory();
            LoadWord();
        }
    }


    void ClearSlots()
    {
        foreach (LetterSlot slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slots.Clear();
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

        int points = LetterValue(guess);


        foreach (LetterSlot slot in slots)
        {

            if (slot.GetLetter() == guess)
            {
                slot.ShowLetter();
                correct = true;
                revealedCount++;
            }

        }


        if (correct)
        {
            score += points * revealedCount;
            UpdateScore();
        }
        else
        {
            HeartManager.Instance.LoseHeart();
        }



        CheckWin();

    }


    int LetterValue(char letter)
    {
        return char.ToUpper(letter) - 'A' + 1;
    }


    void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }



    void CheckWin()
    {

        foreach (LetterSlot slot in slots)
        {
            if (!slot.isRevealed)
                return;
        }


        Debug.Log("Victory");

        UIManager.Instance.ShowVictory(currentWord.answer, score.ToString());

    }


    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    public void ExitToMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }

}