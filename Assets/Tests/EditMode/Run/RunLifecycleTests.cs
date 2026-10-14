using NUnit.Framework;

/// <summary>
/// EditMode coverage for the run lifecycle (issue #19): start, win a word,
/// refill, lose a word, game over, restart.
/// </summary>
public class RunLifecycleTests
{
    static RunState NewState(int totalWords)
    {
        return new RunState
        {
            runId = "test-run",
            difficulty = DifficultyId.Medium,
            totalWords = totalWords,
            startedAtUnixMs = 1000L,
        };
    }

    [Test]
    public void BeginWord_RefillsLivesToTheConfiguredMaximum()
    {
        var run = new RunLifecycle(NewState(3), 3);

        run.BeginWord();
        Assert.AreEqual(3, run.LivesRemaining);

        run.RegisterWrongGuess();
        Assert.AreEqual(2, run.LivesRemaining);

        // A new word starts with a full set again.
        run.BeginWord();
        Assert.AreEqual(3, run.LivesRemaining);
    }

    [Test]
    public void CompleteWord_TracksSolvedWordsScoreAndStreak()
    {
        var run = new RunLifecycle(NewState(2), 3);

        run.BeginWord();
        run.RegisterCorrectGuess(50);
        run.CompleteWord();

        Assert.AreEqual(1, run.State.wordsSolved);
        Assert.AreEqual(50, run.State.score);
        Assert.AreEqual(1, run.CurrentStreak);
        Assert.AreEqual(1, run.State.bestStreak);
    }

    [Test]
    public void WrongGuess_ReducesLivesAndFailsTheWordAtZero()
    {
        var run = new RunLifecycle(NewState(2), 2);
        run.BeginWord();

        Assert.IsFalse(run.RegisterWrongGuess());
        Assert.AreEqual(1, run.LivesRemaining);

        Assert.IsTrue(run.RegisterWrongGuess());
        Assert.AreEqual(0, run.LivesRemaining);
        Assert.IsTrue(run.IsWordFailed);
    }

    [Test]
    public void Accuracy_IsCorrectGuessesOverAllGuesses()
    {
        var run = new RunLifecycle(NewState(1), 5);
        run.BeginWord();

        run.RegisterCorrectGuess(10);
        run.RegisterCorrectGuess(10);
        run.RegisterWrongGuess();
        run.RegisterWrongGuess();
        run.RegisterWrongGuess();

        Assert.AreEqual(2, run.State.correctGuesses);
        Assert.AreEqual(3, run.State.wrongGuesses);
        Assert.AreEqual(0.4f, run.State.Accuracy, 0.0001f);
    }

    [Test]
    public void RegisterHint_CountsHints()
    {
        var run = new RunLifecycle(NewState(1), 3);

        run.RegisterHint();
        run.RegisterHint();

        Assert.AreEqual(2, run.State.hintsUsed);
    }

    [Test]
    public void Lifecycle_Start_WinWord_Refill_LoseWord_GameOver_Restart()
    {
        // start
        var run = new RunLifecycle(NewState(2), 2);
        Assert.AreEqual(0, run.State.wordsSolved);

        // win a word, then refill for the next
        run.BeginWord();
        run.RegisterCorrectGuess(30);
        run.CompleteWord();
        Assert.AreEqual(1, run.State.wordsSolved);
        Assert.IsFalse(run.IsRunComplete);

        run.BeginWord();
        Assert.AreEqual(2, run.LivesRemaining);

        // lose the word - and the run - by exhausting its lives
        Assert.IsFalse(run.RegisterWrongGuess());
        Assert.IsTrue(run.RegisterWrongGuess());
        run.FailWord();
        Assert.AreEqual(0, run.CurrentStreak);

        // restart is a fresh run
        var restarted = new RunLifecycle(NewState(2), 2);
        restarted.BeginWord();
        Assert.AreEqual(0, restarted.State.wordsSolved);
        Assert.AreEqual(2, restarted.LivesRemaining);
    }

    [Test]
    public void Lifecycle_SolvingEveryWord_CompletesTheRun()
    {
        var run = new RunLifecycle(NewState(2), 3);

        run.BeginWord();
        run.CompleteWord();
        Assert.IsFalse(run.IsRunComplete);

        run.BeginWord();
        run.CompleteWord();

        Assert.IsTrue(run.IsRunComplete);
    }

    [Test]
    public void Summary_CapturesTheEndOfRunNumbers()
    {
        var run = new RunLifecycle(NewState(3), 3);
        run.BeginWord();
        run.RegisterCorrectGuess(40);
        run.RegisterWrongGuess();
        run.RegisterHint();
        run.CompleteWord();

        RunSummary summary = RunSummary.From(run.State, 9999L, completedAllWords: false);

        Assert.AreEqual(1, summary.wordsSolved);
        Assert.AreEqual(3, summary.totalWords);
        Assert.AreEqual(40, summary.score);
        Assert.AreEqual(1, summary.wrongGuesses);
        Assert.AreEqual(1, summary.hintsUsed);
        Assert.AreEqual(1, summary.bestStreak);
        Assert.AreEqual(9999L, summary.finishedAtUnixMs);
        Assert.IsFalse(summary.completedAllWords);
    }
}
