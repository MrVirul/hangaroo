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

    public WordListResult(IReadOnlyList<WordData> words, WordContentSource source, int contentVersion)
    {
        this.words = words;
        this.source = source;
        this.contentVersion = contentVersion;
    }

    /// <summary>
    /// True unless the words came straight from the remote source - cached and
    /// bundled content both reflect an earlier fetch (or none at all).
    /// </summary>
    public bool isStale => source != WordContentSource.Firestore;

    /// <summary>True when the game is running purely on content that ships in the build.</summary>
    public bool isOffline => source == WordContentSource.Bundled;
}
