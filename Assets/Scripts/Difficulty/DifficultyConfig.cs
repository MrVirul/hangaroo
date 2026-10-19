using UnityEngine;

/// <summary>
/// Authoring data for a single difficulty. One asset per <see cref="DifficultyId"/>,
/// owned and resolved exclusively through <see cref="DifficultyLibrary"/>.
///
/// This type deliberately holds only data and pure predicates: no scene lookups,
/// no UI, no PlayerPrefs. That keeps it trivially testable and lets Story 1.3 wire
/// the values into gameplay without reworking the contract.
/// </summary>
[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Hangaroo/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Identity")]
    public DifficultyId id = DifficultyId.Medium;
    public string displayName = "Medium";
    [TextArea]
    public string description;

    [Header("Run rules")]
    [Min(1)]
    public int livesPerWord = 3;

    [Header("Word length band")]
    [Min(1)]
    public int minWordLength = 6;
    [Min(1)]
    public int maxWordLength = 8;

    [Header("Hint policy")]
    public HintPolicy hintPolicy = HintPolicy.FreeAfterWrongGuesses;
    [Min(0)]
    public int hintThresholdWrongGuesses = 2;
    [Range(0f, 1f)]
    public float hintScorePenalty = 0f;

    [Header("Scoring")]
    [Min(0.01f)]
    public float scoreMultiplier = 1f;

    /// <summary>True when the answer length falls inside this difficulty's band.</summary>
    public bool IsWordLengthValid(int length)
    {
        return length >= minWordLength && length <= maxWordLength;
    }

    /// <summary>
    /// Whether the hint should be visible given the current number of wrong guesses.
    /// A penalised hint is still shown - its cost is applied by the scoring path.
    ///
    /// A wrong guess that uses the last life fails the word, and the gameplay
    /// loop stops evaluating the hint at that point. A threshold at or above
    /// <see cref="livesPerWord"/> would therefore never be observable (Hard has
    /// two lives and a two-miss threshold, so it could never reveal its hint).
    /// The threshold is capped one miss below the life count so the hint always
    /// arrives while a life is still left.
    /// </summary>
    public bool ShouldShowHint(int wrongGuesses)
    {
        switch (hintPolicy)
        {
            case HintPolicy.FreeImmediate:
                return true;
            case HintPolicy.FreeAfterWrongGuesses:
            case HintPolicy.Penalised:
                int threshold = Mathf.Min(
                    hintThresholdWrongGuesses,
                    Mathf.Max(0, livesPerWord - 1));
                return wrongGuesses >= threshold;
            case HintPolicy.Disabled:
            default:
                return false;
        }
    }

    /// <summary>
    /// Development-time consistency check. Logs a descriptive error for an inverted
    /// word-length range, a non-positive lives/score value or an out-of-range penalty.
    /// </summary>
    public void Validate()
    {
        if (livesPerWord <= 0)
        {
            Debug.LogError($"[DifficultyConfig:{name}] livesPerWord must be positive (got {livesPerWord}).", this);
        }

        if (scoreMultiplier <= 0f)
        {
            Debug.LogError($"[DifficultyConfig:{name}] scoreMultiplier must be greater than zero (got {scoreMultiplier}).", this);
        }

        if (minWordLength > maxWordLength)
        {
            Debug.LogError($"[DifficultyConfig:{name}] word-length range is inverted (min {minWordLength} > max {maxWordLength}).", this);
        }

        if (hintScorePenalty < 0f || hintScorePenalty > 1f)
        {
            Debug.LogError($"[DifficultyConfig:{name}] hintScorePenalty must be within [0, 1] (got {hintScorePenalty}).", this);
        }
    }

    void OnValidate()
    {
        Validate();
    }
}
