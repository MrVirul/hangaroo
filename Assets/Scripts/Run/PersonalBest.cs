using System;

/// <summary>
/// The local personal best, persisted before any cloud save exists. Kept as a
/// plain object so the merge rule is unit-testable independent of storage.
/// </summary>
[Serializable]
public class PersonalBest
{
    public int bestScore;
    public int bestStreak;
    public long lastPlayedAtUnixMs;

    /// <summary>
    /// Folds a finished run into the best. Returns true when the run set a new
    /// best score. Never lowers an existing best.
    /// </summary>
    public bool Record(RunSummary summary)
    {
        if (summary == null)
        {
            return false;
        }

        bool newBest = summary.score > bestScore;

        if (newBest)
        {
            bestScore = summary.score;
        }

        if (summary.bestStreak > bestStreak)
        {
            bestStreak = summary.bestStreak;
        }

        if (summary.finishedAtUnixMs > lastPlayedAtUnixMs)
        {
            lastPlayedAtUnixMs = summary.finishedAtUnixMs;
        }

        return newBest;
    }
}
