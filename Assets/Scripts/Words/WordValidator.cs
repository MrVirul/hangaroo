using System.Collections.Generic;

/// <summary>
/// Pure, allocation-light validation for authored word content.
///
/// The game can only ever render <c>A</c>-<c>Z</c> letter slots (there are exactly
/// 26 keyboard buttons and the scoring path treats a slot as its alphabetical
/// index), so an answer containing anything else would be an uncompletable,
/// negatively-scoring word. This type is the single source of
/// truth for "is this entry safe to ship?" and is shared by the runtime loader
/// (<see cref="WordPool"/>) and the editor content validator.
///
/// Content policy (option (a) in issue #18): answers are single words of
/// <c>A</c>-<c>Z</c> only. Spaces, apostrophes and accented characters are
/// rejected by design. See <c>docs/WORD_CONTENT_POLICY.md</c>.
/// </summary>
public static class WordValidator
{
    /// <summary>
    /// Inclusive answer-length band, equal to the union of the Easy/Medium/Hard
    /// difficulty bands (4-6, 6-8, 8-12). The shared word list must be usable by
    /// at least one difficulty, so it is validated against the union.
    /// Per-bank validation can pass its own band to <see cref="Validate"/>.
    /// </summary>
    public const int MinAnswerLength = 4;
    public const int MaxAnswerLength = 12;

    /// <summary>Trimmed, upper-cased copy of <paramref name="value"/>; never null.</summary>
    public static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }

    /// <summary>True when <paramref name="value"/> is non-empty and contains only A-Z.</summary>
    public static bool IsLetterOnly(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (char c in value)
        {
            if (c < 'A' || c > 'Z')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates one entry against the given inclusive length band. Checks, in
    /// order: empty clue, empty answer, non-A-Z answer, answer too short, answer
    /// too long, empty hint, hint duplicating the clue.
    /// </summary>
    public static WordValidationResult Validate(string clue, string answer, string hint, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(clue))
            return WordValidationResult.Invalid("clue is empty");

        string normalizedAnswer = Normalize(answer);

        if (normalizedAnswer.Length == 0)
            return WordValidationResult.Invalid("answer is empty");

        if (!IsLetterOnly(normalizedAnswer))
            return WordValidationResult.Invalid("answer '" + answer + "' contains characters outside A-Z");

        if (normalizedAnswer.Length < minLength)
            return WordValidationResult.Invalid("answer '" + normalizedAnswer + "' is shorter than the minimum of " + minLength);

        if (normalizedAnswer.Length > maxLength)
            return WordValidationResult.Invalid("answer '" + normalizedAnswer + "' is longer than the maximum of " + maxLength);

        if (string.IsNullOrWhiteSpace(hint))
            return WordValidationResult.Invalid("hint is empty");

        if (Normalize(hint) == Normalize(clue))
            return WordValidationResult.Invalid("hint duplicates the clue");

        return WordValidationResult.Valid();
    }

    /// <summary>
    /// Returns each normalized answer that appears more than once, in first-seen
    /// order. Blank answers are ignored (they are reported as invalid elsewhere).
    /// </summary>
    public static List<string> FindDuplicateAnswers(IEnumerable<string> answers)
    {
        var seen = new HashSet<string>();
        var duplicates = new List<string>();

        if (answers == null)
            return duplicates;

        foreach (string answer in answers)
        {
            string normalized = Normalize(answer);

            if (normalized.Length == 0)
                continue;

            if (!seen.Add(normalized) && !duplicates.Contains(normalized))
                duplicates.Add(normalized);
        }

        return duplicates;
    }
}
