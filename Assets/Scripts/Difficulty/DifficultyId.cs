/// <summary>
/// Stable, persisted difficulty identifiers.
///
/// The numeric values are written to PlayerPrefs (and, once the cloud-save epic
/// lands, to Firestore), so they must never be reordered or renumbered. New
/// difficulties must be appended with a fresh value.
/// </summary>
public enum DifficultyId
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

/// <summary>
/// Conversion helpers for <see cref="DifficultyId"/>. Kept separate because C#
/// enums cannot declare their own methods.
/// </summary>
public static class DifficultyIdExtensions
{
    /// <summary>The difficulty used when no valid preference is stored.</summary>
    public const DifficultyId Default = DifficultyId.Medium;

    /// <summary>Value to persist for this difficulty.</summary>
    public static int ToPlayerPrefs(this DifficultyId id)
    {
        return (int)id;
    }

    /// <summary>
    /// Resolves a persisted value, falling back to <see cref="Default"/> (Medium)
    /// for anything that is not a known id rather than throwing or defaulting to Easy.
    /// </summary>
    public static DifficultyId FromPlayerPrefs(int value)
    {
        switch (value)
        {
            case (int)DifficultyId.Easy:
                return DifficultyId.Easy;
            case (int)DifficultyId.Hard:
                return DifficultyId.Hard;
            case (int)DifficultyId.Medium:
            default:
                return Default;
        }
    }
}
