using System;

/// <summary>
/// Drives a run's per-word loop: refills lives for every new word, tracks the
/// current streak, and reports when a word is failed or the run is complete.
///
/// Pure C# so the lifecycle is unit-testable without a scene. The scene layer
/// (GameManager/HeartManager) mirrors the numbers this exposes; it never owns
/// them. Lives are per word, so exhausting them ends the run.
/// </summary>
public class RunLifecycle
{
    public RunState State { get; private set; }

    /// <summary>Lives granted at the start of each word (from the difficulty config).</summary>
    public int MaxLivesPerWord { get; private set; }

    public int LivesRemaining { get; private set; }
    public int CurrentStreak { get; private set; }
    public bool IsWordFailed { get; private set; }

    /// <summary>Wrong guesses on the current word only; drives the hint threshold.</summary>
    public int WrongGuessesThisWord { get; private set; }

    public RunLifecycle(RunState state, int maxLivesPerWord)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        MaxLivesPerWord = Math.Max(1, maxLivesPerWord);
    }

    public bool IsRunComplete
    {
        get { return State.IsComplete; }
    }

    /// <summary>Starts (or restarts) a word with a full set of lives.</summary>
    public void BeginWord()
    {
        LivesRemaining = MaxLivesPerWord;
        WrongGuessesThisWord = 0;
        IsWordFailed = false;
    }

    /// <summary>
    /// Records a wrong guess. Returns true when that guess used the last life,
    /// which fails the word - and, because lives are per word, the run.
    /// </summary>
    public bool RegisterWrongGuess()
    {
        State.RecordWrongGuess();
        WrongGuessesThisWord++;
        LivesRemaining = Math.Max(0, LivesRemaining - 1);
        IsWordFailed = LivesRemaining <= 0;
        return IsWordFailed;
    }

    /// <summary>Records a correct guess and adds its points to the run score.</summary>
    public void RegisterCorrectGuess(int points)
    {
        State.RecordCorrectGuess();
        State.AddScore(points);
    }

    public void RegisterHint()
    {
        State.RecordHint();
    }

    /// <summary>Marks the current word solved and advances the streak.</summary>
    public void CompleteWord()
    {
        CurrentStreak++;
        State.RecordWordSolved();

        if (CurrentStreak > State.bestStreak)
        {
            State.bestStreak = CurrentStreak;
        }
    }

    /// <summary>Marks the current word failed and clears the streak.</summary>
    public void FailWord()
    {
        CurrentStreak = 0;
    }
}
