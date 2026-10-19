using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// On-disk shape of a cached word list (issue #32's "Cache format"): one file
/// per difficulty band under <c>Application.persistentDataPath</c>, versioned
/// by <c>contentVersion</c> and timestamped so a long-offline player is
/// eventually served the bundled list instead of ancient content.
/// </summary>
[Serializable]
public sealed class WordCacheEntry
{
    /// <summary>Content version the words were fetched under; 0/absent means unversioned (invalid).</summary>
    public int contentVersion;

    /// <summary>UTC fetch time, ISO-8601 round-trip ("O"); empty/absent means unreadable (invalid).</summary>
    public string fetchedAt = "";

    /// <summary>Band this entry serves; read back as a guard against a mis-keyed file.</summary>
    public string difficultyId = "";

    /// <summary>The words themselves, in the same wire shape <c>words.json</c> uses.</summary>
    public List<WordData> words = new List<WordData>();
}

/// <summary>
/// The local-cache tier of the word read path (Story 2.3, issue #32): a
/// versioned JSON file per difficulty written after a successful remote fetch
/// and read when the fetch fails, so the game degrades from Firestore to a
/// known-good snapshot before it falls back to the bundled list.
///
/// Every read is defensive: a missing, corrupt, unversioned, mis-keyed or
/// stale file is a cache miss, never an exception. An empty word list is
/// never written - an empty remote result must not overwrite good content
/// (issue #32, acceptance criterion 3).
/// </summary>
public sealed class WordCache
{
    /// <summary>
    /// How long a cached list stays usable before the cache is invalidated and
    /// the bundled list takes over (the issue's staleness window). Refetching
    /// is attempted before every read regardless; the window only decides
    /// whether a failed fetch may still serve the snapshot. 30 days keeps a
    /// briefly-offline player on the freshest content they actually saw while
    /// guaranteeing a long-abandoned snapshot is never served indefinitely.
    /// </summary>
    public static readonly TimeSpan DefaultMaxAge = TimeSpan.FromDays(30);

    private readonly string directory;

    public WordCache(string directory)
    {
        this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    /// <summary>File used for one band; difficulty keys are normalised so casing cannot fork the cache.</summary>
    public string PathFor(string difficultyId)
    {
        return Path.Combine(directory, "words-" + WordDifficulty.Normalize(difficultyId) + ".json");
    }

    /// <summary>
    /// Reads the cache for one band if the file exists and is valid: parseable,
    /// versioned, timestamped, keyed to the band, and non-empty. Any other
    /// shape is a miss. The staleness window is not applied here.
    /// </summary>
    public bool TryRead(string difficultyId, out WordCacheEntry entry)
    {
        entry = null;

        string path = PathFor(difficultyId);
        if (!File.Exists(path))
            return false;

        WordCacheEntry parsed;
        try
        {
            parsed = JsonUtility.FromJson<WordCacheEntry>(File.ReadAllText(path));
        }
        catch (Exception)
        {
            return false; // corrupt file - treated as a miss
        }

        if (parsed == null)
            return false;

        if (parsed.contentVersion < 1)
            return false; // absent contentVersion reads as 0: an unversioned entry is invalid

        if (!IsValidTimestamp(parsed.fetchedAt))
            return false;

        if (string.IsNullOrEmpty(parsed.difficultyId) ||
            WordDifficulty.Normalize(parsed.difficultyId) != WordDifficulty.Normalize(difficultyId))
            return false;

        if (parsed.words == null || parsed.words.Count == 0)
            return false;

        entry = parsed;
        return true;
    }

    /// <summary>
    /// Reads the cache only if it is also within <paramref name="maxAge"/>.
    /// A stale entry is invalidated on the spot - deleted, then reported as a
    /// miss - per the issue's <c>cache miss / stale -&gt; bundled</c> rule.
    /// </summary>
    public bool TryReadFresh(string difficultyId, TimeSpan maxAge, DateTimeOffset now, out WordCacheEntry entry)
    {
        entry = null;

        if (!TryRead(difficultyId, out WordCacheEntry parsed))
            return false;

        DateTimeOffset fetchedAt;
        if (!DateTimeOffset.TryParse(parsed.fetchedAt, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out fetchedAt))
            return false;

        if (now - fetchedAt > maxAge)
        {
            Delete(PathFor(difficultyId));
            return false;
        }

        entry = parsed;
        return true;
    }

    /// <summary>
    /// Writes one band's cache file. Refuses (returns false, leaving any
    /// existing file untouched) when the word list is empty or the content
    /// version is missing - an empty or unversioned fetch must never overwrite
    /// good cached content (issue #32, acceptance criteria 3 and 4).
    /// </summary>
    public bool Write(string difficultyId, int contentVersion, IReadOnlyList<WordData> words, DateTimeOffset now)
    {
        if (words == null || words.Count == 0)
            return false;

        if (contentVersion < 1)
            return false;

        var entry = new WordCacheEntry
        {
            contentVersion = contentVersion,
            fetchedAt = now.ToUniversalTime().ToString("O"),
            difficultyId = WordDifficulty.Normalize(difficultyId),
            words = new List<WordData>(words),
        };

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(PathFor(difficultyId), JsonUtility.ToJson(entry, true));
            return true;
        }
        catch (Exception e)
        {
            // A full disk or missing permission must not break the read path.
            Debug.LogWarning("WordCache: could not write " + PathFor(difficultyId) + ": " + e.Message);
            return false;
        }
    }

    /// <summary>Deletes one band's cache file if present; never throws.</summary>
    public void Invalidate(string difficultyId)
    {
        Delete(PathFor(difficultyId));
    }

    private void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception)
        {
            // Best effort: a file we cannot delete is still a valid entry or a
            // clean miss on the next read; failing quietly matches the tier's
            // never-throw contract.
        }
    }

    private static bool IsValidTimestamp(string fetchedAt)
    {
        DateTimeOffset parsed;
        return !string.IsNullOrEmpty(fetchedAt) &&
               DateTimeOffset.TryParse(fetchedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed);
    }
}
