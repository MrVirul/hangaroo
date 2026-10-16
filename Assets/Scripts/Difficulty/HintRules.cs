/// <summary>
/// Pure decision logic for whether the per-word hint is visible.
///
/// The hint is gated by two independent switches: the player's <c>Hints</c>
/// preference (set in the Settings panel) and the active difficulty's
/// <see cref="HintPolicy"/>. Keeping the combination here - rather than inline in
/// <c>GameManager</c> - makes every policy/guess-count combination testable
/// without a scene (Story 1.3).
/// </summary>
public static class HintRules
{
    /// <summary>
    /// Whether the hint should be shown for the current word.
    /// </summary>
    /// <param name="config">The run's difficulty, or null when none resolved.</param>
    /// <param name="playerHintsEnabled">The <c>Hints</c> setting from the settings panel.</param>
    /// <param name="wrongGuessesThisWord">Misses so far on the current word.</param>
    public static bool ShouldShow(DifficultyConfig config, bool playerHintsEnabled, int wrongGuessesThisWord)
    {
        // The player's preference is the outer gate: with Hints off the hint is
        // hidden on every difficulty, including Easy's FreeImmediate policy.
        if (!playerHintsEnabled)
        {
            return false;
        }

        // No config resolved (missing asset): fall back to the player's choice
        // rather than hiding a feature they asked for.
        if (config == null)
        {
            return true;
        }

        return config.ShouldShowHint(wrongGuessesThisWord);
    }
}
