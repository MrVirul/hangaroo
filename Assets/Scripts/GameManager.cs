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

    [Tooltip("Inclusive answer-length band applied to the authored word list. Defaults to the union of the difficulty bands.")]
    [Min(1)] public int minAnswerLength = WordValidator.MinAnswerLength;
    [Min(1)] public int maxAnswerLength = WordValidator.MaxAnswerLength;

    private List<WordEntry> words = new List<WordEntry>();


    private WordEntry currentWord;


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

    [Tooltip("Smallest width the fit-to-width pass will shrink a letter slot to (legibility floor).")]
    [Min(1f)] public float minSlotWidth = SlotStripLayout.MinSlotWidth;


    private List<LetterSlot> slots = new List<LetterSlot>();

    // Authored slot width, captured once from the prefab's LayoutElement so the
    // fit pass can restore it when a word is short enough to fit at full size.
    private float authoredSlotWidth = -1f;

    private List<int> wordOrder = new List<int>();
    private int wordPosition;

    private readonly System.Random rng = new System.Random();


    void Awake()
    {
        Time.timeScale = 1f;
        Instance = this;

        // Auto-heal PlayerPrefs: ensure ShowCategory and Hints default to enabled (1).
        // Older versions of SettingsManager saved 0 when toggle references were unassigned.
        if (PlayerPrefs.GetInt("ShowCategory", 1) == 0)
        {
            PlayerPrefs.SetInt("ShowCategory", 1);
        }

        if (PlayerPrefs.GetInt("Hints", 1) == 0)
        {
            PlayerPrefs.SetInt("Hints", 1);
        }
        PlayerPrefs.Save();

        LoadWords();
        BuildWordOrder();
    }


    void Start()
    {
        LoadWord();
    }


    void LoadWords()
    {
        words.Clear();

        if (wordsFile == null)
        {
            Debug.LogError("GameManager: wordsFile is not assigned. Using the built-in fallback word pool.");
            UseFallbackWords();
            return;
        }

        WordPool.ParseResult parsed =
            WordPool.Parse(wordsFile.text, minAnswerLength, maxAnswerLength);

        if (parsed.rejected.Count > 0)
        {
            string noun = parsed.rejected.Count == 1 ? "entry" : "entries";
            Debug.LogError(
                "GameManager: rejected " + parsed.rejected.Count + " malformed word " + noun +
                " from " + wordsFile.name + ":\n - " + string.Join("\n - ", parsed.rejected));
        }

        if (!parsed.HasWords)
        {
            Debug.LogError("GameManager: no valid words remained in " + wordsFile.name + ". Using the built-in fallback word pool.");
            UseFallbackWords();
            return;
        }

        words.AddRange(parsed.words);
    }


    void UseFallbackWords()
    {
        words.Clear();
        words.AddRange(WordPool.CreateFallback());
    }


    void BuildWordOrder()
    {
        // Re-shuffle on every wrap-around so each playthrough of the list is a
        // fresh order, and never the exact order of the previous playthrough.
        wordOrder = WordOrderBuilder.ShuffleDifferentFrom(words.Count, wordOrder, rng);
        wordPosition = 0;
    }


    void LoadWord()
    {
        if (words.Count == 0)
            return;

        // A full cycle just completed (or the pool changed): start a new run.
        if (wordPosition >= words.Count || wordOrder.Count != words.Count)
            BuildWordOrder();

        currentWord = words[wordOrder[wordPosition]];
        wordPosition++;

        revealedCount = 0;

        usedLetters.Clear();

        RefreshClue();

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


    public void RefreshClue()
    {
        if (clueText == null)
            return;

        bool showCategory = PlayerPrefs.GetInt("ShowCategory", 1) == 1;
        clueText.gameObject.SetActive(showCategory);

        if (!showCategory || currentWord == null)
            return;

        clueText.text = "CLUE: " + currentWord.clue.ToUpper();
        clueText.ForceMeshUpdate();
    }


    public void RefreshHint()
    {
        if (hintText == null)
            return;

        bool showHint = PlayerPrefs.GetInt("Hints", 1) == 1;
        hintText.gameObject.SetActive(showHint);

        if (!showHint || currentWord == null)
            return;

        string hint = string.IsNullOrEmpty(currentWord.hint)
            ? currentWord.clue
            : currentWord.hint;

        hintText.text = "HINT: " + hint.ToUpper();
        hintText.ForceMeshUpdate();
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

        RectTransform container = (RectTransform)letterSlotParent;

        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        if (container.parent is RectTransform parentRt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        }

        float availableWidth = container.rect.width;

        if (availableWidth <= 0f)
            return;

        float spacing = 10f;
        var hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) spacing = hlg.spacing;

        float fittedWidth = SlotStripLayout.FitSlotWidth(
            slots.Count,
            availableWidth,
            spacing,
            ResolveAuthoredSlotWidth(),
            minSlotWidth);

        foreach (LetterSlot slot in slots)
        {
            SetSlotWidth(slot, fittedWidth);
        }

        // A layout group ignores localScale, so the strip is resized by writing
        // each slot's RectTransform width and LayoutElement, then rebuilding.
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

    }


    float ResolveAuthoredSlotWidth()
    {

        if (authoredSlotWidth > 0f)
            return authoredSlotWidth;

        if (letterSlotPrefab != null)
        {
            var prefabLayout = letterSlotPrefab.GetComponent<LayoutElement>();

            if (prefabLayout != null && prefabLayout.preferredWidth > 0f)
                authoredSlotWidth = prefabLayout.preferredWidth;
        }

        if (authoredSlotWidth <= 0f)
            authoredSlotWidth = 100f;

        return authoredSlotWidth;

    }


    static void SetSlotWidth(LetterSlot slot, float width)
    {

        if (slot == null)
            return;

        // A HorizontalLayoutGroup only drives child widths when childControlWidth
        // is on; otherwise it lays children out at their RectTransform width.
        // Keep both in sync so the fit applies either way (issue #16, part 2).
        var rt = slot.transform as RectTransform;

        if (rt != null)
        {
            var size = rt.sizeDelta;
            size.x = width;
            rt.sizeDelta = size;
        }

        var layout = slot.GetComponent<LayoutElement>();

        if (layout == null)
            return;

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;

    }



    public void CheckLetter(char guess)
    {

        if (!usedLetters.TryAdd(guess))
            return;

        int revealedThisGuess = 0;


        foreach (LetterSlot slot in slots)
        {

            if (slot.GetLetter() == guess)
            {
                slot.ShowLetter();
                revealedCount++;
                revealedThisGuess++;
            }

        }


        if (revealedThisGuess > 0)
        {
            score += ScoreCalculator.BasePoints(guess, revealedThisGuess);
            UpdateScore();
        }
        else
        {
            HeartManager.Instance.LoseHeart();
        }



        CheckWin();

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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    public void ExitToMainMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }


    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}