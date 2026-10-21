using System.Collections.Generic;

/// <summary>
/// Which tier served a resolved word list (issue #32's three tiers).
/// </summary>
public enum WordContentSource
{
    /// <summary>The bundled <c>words.json</c> that ships in the build - the last resort.</summary>
    Bundled,

    /// <summary>The versioned local cache under <c>Application.persistentDataPath</c>.</summary>
    Cache,

    /// <summary>The remote source: the Firestore query, once the SDK lands (issue #30).</summary>
    Firestore,
}

/// <summary>
/// Why the remote tier did not serve a run's words (issue #40's
/// <c>content_fallback</c> reasons: a Firestore outage, an empty collection
/// and a bad deploy must be distinguishable). <see cref="None"/> covers both
/// cases where nothing degraded: the remote tier served the list, or it was
/// never consulted (no source configured).
/// </summary>
public enum WordFallbackReason
{
    /// <summary>The remote tier served the list, or was never consulted.</summary>
    None,

    /// <summary>Transport failure: network, permission or timeout.</summary>
    Network,

    /// <summary>The query succeeded but returned no documents.</summary>
    Empty,

    /// <summary>Documents existed but none passed validation - a bad deploy.</summary>
    Invalid,
}

/// <summary>
/// A resolved word list plus where it came from (issue #32's load-path
/// annotations: <c>isStale</c> / <c>isOffline</c>). <see cref="words"/> is
/// never empty - <see cref="WordRepository"/> degrades through the tiers to
/// the built-in fallback pool rather than handing back an empty list.
/// </summary>
public sealed class WordListResult
{
    /// <summary>The resolved list for one difficulty band; never empty.</summary>
    public readonly IReadOnlyList<WordData> words;

    /// <summary>The tier that produced this list.</summary>
    public readonly WordContentSource source;

    /// <summary>
    /// The <c>contentVersion</c> the words carry; 0 for bundled content,
    /// which has no remote version.
    /// </summary>
    public readonly int contentVersion;

    /// <summary>
    /// Why the remote tier did not serve this list (issue #40).
    /// <see cref="WordFallbackReason.None"/> when nothing degraded.
    /// </summary>
    public readonly WordFallbackReason fallbackReason;

    public WordListResult(IReadOnlyList<WordData> words, WordContentSource source, int contentVersion,
        WordFallbackReason fallbackReason = WordFallbackReason.None)
    {
        this.words = words;
        this.source = source;
        this.contentVersion = contentVersion;
        this.fallbackReason = fallbackReason;
    }

    /// <summary>
    /// True unless the words came straight from the remote source - cached and
    /// bundled content both reflect an earlier fetch (or none at all).
    /// </summary>
    public bool isStale => source != WordContentSource.Firestore;

    /// <summary>True when the game is running purely on content that ships in the build.</summary>
    public bool isOffline => source == WordContentSource.Bundled;
}
