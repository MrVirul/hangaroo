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
}
