using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The remote tier of the word read path (Story 2.3, issue #32): a Firestore
/// query once the Firebase SDK lands (issue #30), a test double before then.
///
/// The seam returns raw document field dictionaries - exactly what
/// <c>DocumentSnapshot.ToDictionary()</c> hands over - so the Firebase type
/// stays outside this assembly and <see cref="WordRepository"/> can map and
/// validate the documents itself with <see cref="FirestoreWordMapper"/>,
/// without the SDK (the same split issue #33 set up).
/// </summary>
public interface IWordSource
{
    /// <summary>
    /// Fetches the word documents for one difficulty band plus the current
    /// <c>contentVersion</c>. Implementations report transport failure as
    /// <see cref="WordSourceResult.Failed"/> rather than throwing, but the
    /// repository also tolerates a thrown exception.
    /// </summary>
    Task<WordSourceResult> FetchAsync(string difficultyId);
}

/// <summary>Outcome of one remote fetch: success carries the documents and the content version.</summary>
public sealed class WordSourceResult
{
    /// <summary>False when the fetch failed (network, permission, timeout).</summary>
    public readonly bool succeeded;

    /// <summary>
    /// <c>contentVersion</c> from <c>content_meta</c> - the change marker the
    /// cache is versioned by. Only meaningful when <see cref="succeeded"/> is true.
    /// </summary>
    public readonly int contentVersion;

    /// <summary>Raw document fields, in query order; empty on failure or when nothing matched.</summary>
    public readonly IReadOnlyList<IReadOnlyDictionary<string, object>> documents;

    private static readonly IReadOnlyList<IReadOnlyDictionary<string, object>> NoDocuments =
        new List<IReadOnlyDictionary<string, object>>();

    private WordSourceResult(bool succeeded, int contentVersion,
        IReadOnlyList<IReadOnlyDictionary<string, object>> documents)
    {
        this.succeeded = succeeded;
        this.contentVersion = contentVersion;
        this.documents = documents ?? NoDocuments;
    }

    /// <summary>The fetch failed; the repository falls through to the cache.</summary>
    public static WordSourceResult Failed()
    {
        return new WordSourceResult(false, 0, null);
    }

    /// <summary>The fetch completed (possibly with zero documents).</summary>
    public static WordSourceResult From(int contentVersion,
        IReadOnlyList<IReadOnlyDictionary<string, object>> documents)
    {
        return new WordSourceResult(true, contentVersion, documents);
    }
}
