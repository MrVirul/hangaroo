/// <summary>
/// How a difficulty exposes the per-word hint. Consumed by
/// <see cref="DifficultyConfig.ShouldShowHint"/>.
/// </summary>
public enum HintPolicy
{
    /// <summary>Hint is visible as soon as the word loads. (Easy)</summary>
    FreeImmediate = 0,

    /// <summary>Hint is revealed after a number of wrong guesses. (Medium)</summary>
    FreeAfterWrongGuesses = 1,

    /// <summary>Hint is available but reduces the score awarded for the word. (Hard)</summary>
    Penalised = 2,

    /// <summary>Hint is never shown. Reserved for a future no-assist mode.</summary>
    Disabled = 3
}
