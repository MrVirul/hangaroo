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

    // Every parsed word, all bands. The per-run `words` list is a difficulty
    // slice of this, resolved once in StartRun and never touched mid-run.
    private readonly List<WordEntry> allWords = new List<WordEntry>();


    private WordEntry currentWord;


    [Header("UI")]
    public TMP_Text clueText;
    public TMP_Text scoreText;
    public TMP_Text hintText;
    public TMP_Text difficultyText;


    private int score;
    public int CurrentScore => score;
    private int revealedCount;

    // Difficulty rules resolved once per run (Story 1.3): the config never
    // changes mid-run, and neither does its multiplier.
    private DifficultyConfig config;
    private float scoreMultiplier = 1f;
    private int wordScore;
    private bool hintCounted;


    private readonly UsedLetters usedLetters = new UsedLetters();


    // Run lifecycle: the scene mirrors it, it is never owned by the UI.
    private RunLifecycle run;
    private readonly IBestScoreStore bestScoreStore = new PlayerPrefsBestScoreStore();
    private bool runEnded;


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
    }


    void Start()
    {
        StartRun();
        LoadWord();
    }


    /// <summary>
    /// Begins a fresh run for the active difficulty. Lives come from the
    /// difficulty config (per word), so they are not derived from the scene.
    /// </summary>
    void StartRun()
    {
        config = DifficultyLibrary.CurrentConfig;
        int livesPerWord = config != null ? config.livesPerWord : 4;
        scoreMultiplier = config != null ? config.scoreMultiplier : 1f;

        score = 0;
        runEnded = false;

        // Resolve the run's word list once, for the active difficulty. Nothing
        // below this line re-reads difficulty, so a mid-run change can never
        // swap the words out from under the player (Story 1.4).
        ResolveWordPoolForRun();

        run = new RunLifecycle(
            new RunState
            {
                runId = System.Guid.NewGuid().ToString("N"),
                difficulty = DifficultyLibrary.Current,
                totalWords = words.Count,
                startedAtUnixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            },
            livesPerWord);

        RefreshDifficultyLabel();
    }


    /// <summary>
    /// Builds the per-run <see cref="words"/> list from the parser's full pool,
    /// keeping only the entries that declare the active difficulty. Falls back to
    /// every parsed word (then the built-in pool) if a band is empty, so a thin
    /// bank degrades to a playable board rather than a blank one.
    /// </summary>
    void ResolveWordPoolForRun()
    {
        words.Clear();

        string difficulty = WordDifficulty.Normalize(DifficultyLibrary.Current.ToString());

        foreach (WordEntry entry in allWords)
        {
            if (WordDifficulty.Normalize(entry.difficulty) == difficulty)
                words.Add(entry);
        }

        if (words.Count == 0 && allWords.Count > 0)
        {
            Debug.LogWarning(
                "GameManager: no words authored for difficulty '" + difficulty +
                "'. Falling back to the full pool.");
            words.AddRange(allWords);
        }

        if (words.Count == 0)
            words.AddRange(WordPool.CreateFallback());

        BuildWordOrder();
    }


    void RefreshDifficultyLabel()
    {
        if (difficultyText == null)
            return;

        string label = config != null ? config.displayName : DifficultyLibrary.Current.ToString();
        difficultyText.text = "Difficulty: " + label.ToUpperInvariant();
    }


    void LoadWords()
    {
        allWords.Clear();

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

        allWords.AddRange(parsed.words);
    }


    void UseFallbackWords()
    {
        allWords.Clear();
        allWords.AddRange(WordPool.CreateFallback());
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
        wordScore = 0;
        hintCounted = false;

        usedLetters.Clear();

        // Lives are per word: every word starts with a full set (issue #19).
        if (run != null)
            run.BeginWord();

        if (HeartManager.Instance != null && run != null)
            HeartManager.Instance.ResetLives(run.MaxLivesPerWord);

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


    /// <summary>
    /// Refreshes the hint line from the active difficulty's <see cref="HintPolicy"/>
    /// and the player's Hints preference. Returns true when a hint is visible.
    ///
    /// Safe to call at any point (word load, a wrong guess, or the Hints setting
    /// changing mid-run): the decision is re-derived from the current wrong-guess
    /// count every time. The hint is counted and, on a penalised policy, its
    /// score cost is applied exactly once per word.
    /// </summary>
    public bool RefreshHint()
    {
        if (hintText == null)
            return false;

        bool hintsEnabled = PlayerPrefs.GetInt("Hints", 1) == 1;
        int wrongGuesses = run != null ? run.WrongGuessesThisWord : 0;
        bool showHint = HintRules.ShouldShow(config, hintsEnabled, wrongGuesses);

        hintText.gameObject.SetActive(showHint);

        if (!showHint || currentWord == null)
            return false;

        string hint = string.IsNullOrEmpty(currentWord.hint)
            ? currentWord.clue
            : currentWord.hint;

        hintText.text = "HINT: " + hint.ToUpper();
        hintText.ForceMeshUpdate();

        // Count the reveal once per word, even though RefreshHint is called again
        // on every wrong guess after the threshold is met. Easy's FreeImmediate
        // hint is not a "use" - it is simply part of the board - so only hints
        // that are earned (Medium) or paid for (Hard) are counted.
        if (!hintCounted)
        {
            hintCounted = true;

            if (config != null && config.hintPolicy != HintPolicy.FreeImmediate)
            {
                if (run != null)
                    run.RegisterHint();

                ApplyHintPenalty();
            }
        }

        return true;
    }


    void ApplyHintPenalty()
    {
        if (run == null || config == null || config.hintPolicy != HintPolicy.Penalised)
            return;

        int penalty = ScoreCalculator.HintPenalty(wordScore, config.hintScorePenalty);

        if (penalty <= 0)
            return;

        run.State.DeductScore(penalty);
        wordScore -= penalty;
        score = run.State.score;
        UpdateScore();
    }


    public void ContinueGame()
    {
        if (runEnded)
            return;

        ClearSlots();

        if (UIManager.Instance != null)
            UIManager.Instance.HideEndPanels();

        LoadWord();
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

        if (runEnded || run == null)
            return;

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
            int points = ScoreCalculator.GuessPoints(
                ScoreCalculator.BasePoints(guess, revealedThisGuess),
                scoreMultiplier);

            run.RegisterCorrectGuess(points);
            wordScore += points;
            score = run.State.score;
            UpdateScore();
        }
        else
        {
            bool wordFailed = run.RegisterWrongGuess();

            if (HeartManager.Instance != null)
                HeartManager.Instance.SetLives(run.LivesRemaining);

            if (wordFailed)
            {
                run.FailWord();
                EndRun(false);
                return;
            }

            // A wrong guess can satisfy the difficulty's hint threshold, so the
            // hint (and its penalty) is re-evaluated here, not only on load.
            RefreshHint();
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

        run.CompleteWord();

        // Every word solved: show the run-complete summary instead of looping.
        if (run.IsRunComplete)
        {
            EndRun(true);
            return;
        }

        UIManager.Instance.ShowVictory(currentWord.answer, score.ToString());

    }


    /// <summary>
    /// Ends the current run, folds it into the local personal best, and shows the
    /// matching panel. Restarting later builds a fresh run in <see cref="StartRun"/>.
    /// </summary>
    void EndRun(bool completedAllWords)
    {
        runEnded = true;

        long finishedAt = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RunSummary summary = RunSummary.From(run.State, finishedAt, completedAllWords);

        PersonalBest best = bestScoreStore.Load();
        summary.isNewBestScore = best.Record(summary);
        bestScoreStore.Save(best);

        if (UIManager.Instance == null)
            return;

        if (completedAllWords)
            UIManager.Instance.ShowRunComplete(summary, best);
        else
            UIManager.Instance.ShowGameOver(summary.score.ToString());
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