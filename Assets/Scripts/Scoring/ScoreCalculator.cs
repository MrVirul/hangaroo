/// <summary>
/// Pure scoring rules, kept free of scene state so they can be unit tested and,
/// later, extended with difficulty multipliers and bonuses (EPIC 1 / Story 1.3).
///
/// The formula is order-independent: a correct guess is worth its letter value
/// multiplied only by the number of slots that guess itself revealed - never by
/// the running total of letters revealed in the word.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>Alphabetical value of a letter: A = 1 ... Z = 26. Case-insensitive.</summary>
    public static int LetterValue(char letter)
    {
        return char.ToUpperInvariant(letter) - 'A' + 1;
    }

    /// <summary>
    /// Base points for a correct guess. Returns 0 when the guess revealed nothing
    /// (an incorrect guess), so callers can branch on the result rather than track
    /// a separate "correct" flag.
    /// </summary>
    public static int BasePoints(char guess, int lettersRevealedByGuess)
    {
        if (lettersRevealedByGuess <= 0)
        {
            return 0;
        }

        return LetterValue(guess) * lettersRevealedByGuess;
    }

    /// <summary>
    /// Points for a correct guess after the difficulty's multiplier is applied
    /// (Story 1.3): <c>base x scoreMultiplier</c>. Rounding is per guess, so the
    /// total for a word stays independent of the order its letters are guessed in.
    /// A multiplier of zero or less is treated as 1 so a bad config cannot zero
    /// the scoreboard.
    /// </summary>
    public static int GuessPoints(int basePoints, float scoreMultiplier)
    {
        if (basePoints <= 0)
        {
            return 0;
        }

        float multiplier = scoreMultiplier <= 0f ? 1f : scoreMultiplier;

        return (int)System.Math.Round(basePoints * multiplier, System.MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Points removed from a word's running total when a penalised hint is
    /// revealed: a fraction of the points earned on that word so far, clamped to
    /// <c>[0, 1]</c>. Returns 0 when there is nothing to penalise.
    /// </summary>
    public static int HintPenalty(int wordPointsSoFar, float hintScorePenalty)
    {
        if (wordPointsSoFar <= 0 || hintScorePenalty <= 0f)
        {
            return 0;
        }

        float fraction = hintScorePenalty > 1f ? 1f : hintScorePenalty;

        return (int)System.Math.Round(wordPointsSoFar * fraction, System.MidpointRounding.AwayFromZero);
    }
}
