using System;

/// <summary>
/// Snapshot of a finished run, used by the game-over/run-complete screens and
/// by the personal-best store. Plain and serialisable so it can later be handed
/// to the cloud-save epic without reworking the model.
/// </summary>
[Serializable]
public class RunSummary
{
    public string runId;
    public DifficultyId difficulty;
    public int wordsSolved;
    public int totalWords;
    public int score;
    public int hintsUsed;
    public int wrongGuesses;
    public int correctGuesses;
    public int bestStreak;
    public long startedAtUnixMs;
    public long finishedAtUnixMs;
    public bool completedAllWords;
    public bool isNewBestScore;

    /// <summary>Correct guesses as a fraction of all letter guesses (0 when none).</summary>
    public float Accuracy
    {
        get
        {
            int attempts = correctGuesses + wrongGuesses;
            return attempts == 0 ? 0f : (float)correctGuesses / attempts;
        }
    }

    public static RunSummary From(RunState state, long finishedAtUnixMs, bool completedAllWords)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        return new RunSummary
        {
            runId = state.runId,
            difficulty = state.difficulty,
            wordsSolved = state.wordsSolved,
            totalWords = state.totalWords,
            score = state.score,
            hintsUsed = state.hintsUsed,
            wrongGuesses = state.wrongGuesses,
            correctGuesses = state.correctGuesses,
            bestStreak = state.bestStreak,
            startedAtUnixMs = state.startedAtUnixMs,
            finishedAtUnixMs = finishedAtUnixMs,
            completedAllWords = completedAllWords,
        };
    }
}
