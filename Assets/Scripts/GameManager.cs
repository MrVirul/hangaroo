using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public TMP_Text hintText;


    private int score;
    public int CurrentScore => score;
    private int revealedCount;


    private readonly UsedLetters usedLetters = new UsedLetters();


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
            words.Add(new WordData(word.clue, word.answer, word.hint));
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

        usedLetters.Clear();

        clueText.text = currentWord.clue;

        RefreshHint();

        CreateLetterSlots();

        RefreshKeyboard();
    }


    void RefreshKeyboard()
    {
        if (KeyboardManager.Instance != null)
            KeyboardManager.Instance.RefreshForNewWord();
    }


    public bool IsLetterUsed(char letter)
    {
        return usedLetters.Contains(letter);
    }


    public void RefreshHint()
    {
        if (hintText == null)
            return;

        hintText.gameObject.SetActive(true);

        if (currentWord == null)
            return;

        string hint = string.IsNullOrEmpty(currentWord.hint)
            ? currentWord.clue
            : currentWord.hint;

        hintText.text = hint;
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


        if (currentWord.answer.Length > 0)
            FitSlotsToWidth();

    }


    void FitSlotsToWidth()
    {

        if (slots.Count == 0 || letterSlotParent == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)letterSlotParent);

        if (letterSlotParent.parent is RectTransform parentRt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        }

        float availableWidth =
        ((RectTransform)letterSlotParent).rect.width;

        if (availableWidth <= 0f)
            return;

        float spacing = 10f;
        var hlg = ((RectTransform)letterSlotParent).GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) spacing = hlg.spacing;

        float total = 0f;

        foreach (LetterSlot slot in slots)
        {
            total += ((RectTransform)slot.transform).rect.width;
        }

        total += spacing * (slots.Count - 1);

        if (total <= availableWidth)
        {
            foreach (LetterSlot slot in slots)
            {
                slot.transform.localScale = Vector3.one;
            }
            return;
        }

        float scale = availableWidth / total;

        foreach (LetterSlot slot in slots)
        {
            slot.transform.localScale =
            Vector3.one * scale;
        }

    }



    public void CheckLetter(char guess)
    {

        if (!usedLetters.TryAdd(guess))
            return;

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