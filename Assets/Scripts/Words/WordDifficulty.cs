/// <summary>
/// The three authored difficulty bands and the answer-length range each accepts
/// (Story 1.4).
///
/// Bands are inclusive and deliberately overlap: a 6-letter word is valid in
/// both Easy and Medium, an 8-letter word in both Medium and Hard. A word's band
/// is therefore the one it <em>declares</em>, never one inferred from its length.
///
/// These values mirror the <c>minWordLength</c>/<c>maxWordLength</c> authored on
/// the <c>DifficultyConfig</c> assets and are the single source of truth for the
/// word-content pipeline.
/// </summary>
public static class WordDifficulty
{
    public const string Easy = "easy";
    public const string Medium = "medium";
    public const string Hard = "hard";

    /// <summary>Every known difficulty, easiest first.</summary>
    public static readonly string[] All = { Easy, Medium, Hard };

    /// <summary>Trimmed, lower-cased difficulty key; never null.</summary>
    public static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    /// <summary>True when <paramref name="value"/> names a known difficulty.</summary>
    public static bool IsKnown(string value)
    {
        string normalized = Normalize(value);
        return normalized == Easy || normalized == Medium || normalized == Hard;
    }

    /// <summary>Inclusive minimum answer length for a difficulty (unknown → Medium's band).</summary>
    public static int MinLength(string difficulty)
    {
        switch (Normalize(difficulty))
        {
            case Easy: return 4;
            case Hard: return 8;
            default: return 6;
        }
    }

    /// <summary>Inclusive maximum answer length for a difficulty (unknown → Medium's band).</summary>
    public static int MaxLength(string difficulty)
    {
        switch (Normalize(difficulty))
        {
            case Easy: return 6;
            case Hard: return 12;
            default: return 8;
        }
    }

    /// <summary>True when <paramref name="length"/> falls inside the named difficulty's band.</summary>
    public static bool IsInBand(string difficulty, int length)
    {
        return IsKnown(difficulty)
            && length >= MinLength(difficulty)
            && length <= MaxLength(difficulty);
    }

    /// <summary>
    /// Infers a band from an answer length alone, for entries that predate
    /// Story 1.4 and declare no <c>difficulty</c> (issue #33, backwards
    /// compatibility). The first band that fits wins, easiest first: up to 6
    /// letters → easy, 7-8 → medium, 9+ → hard. Lengths outside the 4-12 union
    /// still resolve to a band here; the validator's band check rejects them
    /// afterwards rather than burdening inference with failure cases.
    /// </summary>
    public static string InferFromLength(int answerLength)
    {
        if (answerLength <= MaxLength(Easy))
            return Easy;

        if (answerLength <= MaxLength(Medium))
            return Medium;

        return Hard;
    }
}
