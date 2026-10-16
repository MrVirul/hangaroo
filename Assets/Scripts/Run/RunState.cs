using System;

/// <summary>
/// Mutable state for a single run - the model the Difficulty and Cloud Save
/// epics both build on. A plain, serialisable object: no scene lookups, no UI,
/// no PlayerPrefs. Constructed at run start and carried across words.
/// </summary>
[Serializable]
public class RunState
{
    public string runId;
    public DifficultyId difficulty;
    public int totalWords;
    public int wordsSolved;
    public int score;
    public int hintsUsed;
    public int wrongGuesses;
    public int correctGuesses;
    public int bestStreak;
    public long startedAtUnixMs;

    /// <summary>Words still to solve before the run completes.</summary>
    public int WordsRemaining
    {
        get { return Math.Max(0, totalWords - wordsSolved); }
    }

    /// <summary>True once every word in the run has been solved.</summary>
    public bool IsComplete
    {
        get { return totalWords > 0 && wordsSolved >= totalWords; }
    }

    /// <summary>Correct guesses as a fraction of all letter guesses (0 when none).</summary>
    public float Accuracy
    {
        get
        {
            int attempts = correctGuesses + wrongGuesses;
            return attempts == 0 ? 0f : (float)correctGuesses / attempts;
        }
    }

    public void AddScore(int points)
    {
        if (points > 0)
        {
            score += points;
        }
    }

    /// <summary>Removes points (e.g. a revealed hint's penalty), never below zero.</summary>
    public void DeductScore(int points)
    {
        if (points <= 0)
        {
            return;
        }

        score = System.Math.Max(0, score - points);
    }

    public void RecordCorrectGuess()
    {
        correctGuesses++;
    }

    public void RecordWrongGuess()
    {
        wrongGuesses++;
    }

    public void RecordHint()
    {
        hintsUsed++;
    }

    public void RecordWordSolved()
    {
        wordsSolved++;
    }
}
