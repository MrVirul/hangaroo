/// <summary>
/// The finite, documented set of word categories (Story 1.4).
///
/// Categories are player-visible: the <c>ShowCategory</c> setting reveals a
/// word's category as an extra nudge, so the set is deliberately small and
/// stable. New categories must be added here, to
/// <c>docs/WORD_CONTENT_POLICY.md</c>, and to the CI mirror in
/// <c>scripts/validate-content.py</c>.
/// </summary>
public static class WordCategory
{
    /// <summary>Every known category key, lower-case.</summary>
    public static readonly string[] All =
    {
        "animals",
        "food",
        "geography",
        "history",
        "movies",
        "music",
        "nature",
        "science",
        "space",
        "sports",
        "technology",
        "general",
    };

    /// <summary>Trimmed, lower-cased category key; never null.</summary>
    public static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    /// <summary>True when <paramref name="value"/> is one of the documented categories.</summary>
    public static bool IsKnown(string value)
    {
        string normalized = Normalize(value);

        foreach (string category in All)
        {
            if (category == normalized)
                return true;
        }

        return false;
    }
}
