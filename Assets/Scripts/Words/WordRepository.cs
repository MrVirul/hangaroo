using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Three-tier word read path (Story 2.3, issue #32): the remote source first,
/// then the versioned local cache, then the bundled <c>words.json</c>, then
/// the built-in pool. <see cref="GetWordsAsync"/> never returns an empty list
/// and never throws - every failure mode degrades to something playable
/// (acceptance criterion 1).
///
/// Remote documents arrive untrusted and are mapped through
/// <see cref="FirestoreWordMapper"/> - the same <see cref="WordValidator"/>
/// rules the editor tooling applies - then band-filtered and de-duplicated
/// with the bundled loader's semantics (acceptance criterion 7). An empty
/// fetch (zero documents, or none that survive validation) falls through to
/// the cache without overwriting it (acceptance criterion 3), and a successful
/// fetch replaces the cache wholesale, which is how a differing
/// <c>contentVersion</c> invalidates the previous entry (acceptance
/// criterion 4).
///
/// The remote source is null until the Firebase SDK lands (issue #30); with
/// no source configured the cache and bundled tiers are the normal path and
/// no degradation warning is logged. Once a fetch has been attempted, falling
/// through to the bundled list logs exactly one aggregated warning - counts,
/// never one message per word (acceptance criterion 9).
/// </summary>
public sealed class WordRepository : IWordRepository
{
    public const string LogPrefix = "WordRepository:";

    private readonly IWordSource source;
    private readonly WordCache cache;
    private readonly BundledWordSource bundled;
    private readonly int minLength;
    private readonly int maxLength;
    private readonly TimeSpan maxAge;

    public event Action OnContentChanged;

    /// <param name="source">The remote tier; null until issue #30 wires the Firestore source in.</param>
    /// <param name="cache">The local cache tier; may be null to disable caching (tests).</param>
    /// <param name="bundled">The bundled tier; may be null when no <c>wordsFile</c> is assigned.</param>
    /// <param name="minLength">Inclusive answer-length band (validator bounds for both bundled and remote words).</param>
    /// <param name="maxLength">Inclusive answer-length band (validator bounds for both bundled and remote words).</param>
    /// <param name="maxAge">Staleness window for cached content; defaults to <see cref="WordCache.DefaultMaxAge"/>.</param>
    public WordRepository(IWordSource source, WordCache cache, BundledWordSource bundled,
        int minLength, int maxLength, TimeSpan? maxAge = null)
    {
        this.source = source;
        this.cache = cache;
        this.bundled = bundled;
        this.minLength = minLength;
        this.maxLength = maxLength;
        this.maxAge = maxAge ?? WordCache.DefaultMaxAge;
    }

    /// <inheritdoc/>
    public async Task<WordListResult> GetWordsAsync(string difficultyId)
    {
        try
        {
            return await ResolveAsync(difficultyId);
        }
        catch (Exception e)
        {
            // Never throws: even an unexpected failure degrades to the built-in pool.
            Debug.LogError(
                LogPrefix + " unexpected failure while resolving words for difficulty '" +
                difficultyId + "' (" + e.Message + "). Using the built-in fallback word pool.");
            return new WordListResult(WordPool.CreateFallback(), WordContentSource.Bundled, 0);
        }
    }

    private async Task<WordListResult> ResolveAsync(string difficultyId)
    {
        bool fetchAttempted = false;
        string fetchFailure = "no reason recorded";

        // Why the remote tier did not serve (issue #40's content_fallback);
        // stays None when the remote tier serves, or is never consulted.
        WordFallbackReason fallbackReason = WordFallbackReason.None;

        // Tier 1: the remote source. A thrown exception, a failed result, an
        // empty result and an all-invalid result all fall through - none of
        // them may overwrite good cached content - and each records why, so
        // the degradation below can report it in one aggregated warning.
        if (source != null)
        {
            fetchAttempted = true;

            WordSourceResult fetched = null;
            bool transportFailed = false;
            try
            {
                fetched = await source.FetchAsync(difficultyId);
            }
            catch (Exception e)
            {
                transportFailed = true;
                fetchFailure = e.Message;
                fallbackReason = WordFallbackReason.Network;
            }

            if (!transportFailed && fetched == null)
            {
                fetchFailure = "the source returned no result";
                fallbackReason = WordFallbackReason.Network;
            }
            else if (!transportFailed && !fetched.succeeded)
            {
                fetchFailure = "the source reported a failure";
                fallbackReason = WordFallbackReason.Network;
            }
            else if (!transportFailed)
            {
                List<WordData> mapped = MapAndValidate(fetched.documents, difficultyId);

                if (mapped.Count > 0)
                {
                    int previousVersion = -1;
                    if (cache != null && cache.TryRead(difficultyId, out WordCacheEntry existing))
                        previousVersion = existing.contentVersion;

                    cache?.Write(difficultyId, fetched.contentVersion, mapped, DateTimeOffset.UtcNow);

                    if (previousVersion != fetched.contentVersion)
                        OnContentChanged?.Invoke();

                    return new WordListResult(mapped, WordContentSource.Firestore, fetched.contentVersion);
                }

                fetchFailure = fetched.documents.Count == 0
                    ? "the query returned no documents"
                    : "none of the " + fetched.documents.Count + " documents passed validation";
                fallbackReason = fetched.documents.Count == 0
                    ? WordFallbackReason.Empty
                    : WordFallbackReason.Invalid;
            }
        }

        // Tier 2: the versioned cache. A stale entry is deleted and reported
        // as a miss, so a long-offline player lands on the bundled list.
        if (cache != null && cache.TryReadFresh(difficultyId, maxAge, DateTimeOffset.UtcNow, out WordCacheEntry cached))
        {
            if (fetchAttempted)
            {
                Debug.LogWarning(
                    LogPrefix + " serving " + cached.words.Count + " words from the local cache: " +
                    "the remote fetch yielded nothing usable (" + fetchFailure + ").");
            }

            return new WordListResult(cached.words, WordContentSource.Cache, cached.contentVersion,
                fallbackReason);
        }

        // Tier 3: the bundled words.json - ships in the build, always present,
        // validator-passing. Last resort before the built-in pool.
        List<WordData> words = null;
        bool reportedFailure = false;

        if (bundled == null)
        {
            Debug.LogError(LogPrefix + " no bundled word list configured. Using the built-in fallback word pool.");
            reportedFailure = true;
        }
        else
        {
            words = bundled.Load(difficultyId, minLength, maxLength);
            if (words.Count == 0)
            {
                Debug.LogError(
                    LogPrefix + " no usable words in " + bundled.SourceName +
                    ". Using the built-in fallback word pool.");
                reportedFailure = true;
                words = null;
            }
        }

        bool servedBundled = words != null;
        if (words == null)
            words = WordPool.CreateFallback();

        // One aggregated warning for the whole degradation, never one per word.
        // (When the bundled file itself was unusable the error above already
        // carries the report, so no second message is logged.)
        if (fetchAttempted && servedBundled && !reportedFailure)
        {
            Debug.LogWarning(
                LogPrefix + " serving " + words.Count + " words from the bundled list: " +
                "the remote fetch yielded nothing usable (" + fetchFailure + ") " +
                "and there is no fresh cache.");
        }

        return new WordListResult(words, WordContentSource.Bundled, 0, fallbackReason);
    }

    /// <summary>
    /// Maps every remote document through <see cref="FirestoreWordMapper"/> -
    /// the shared validation rules - then applies the bundled loader's
    /// selection semantics: other bands and soft-disabled entries drop
    /// silently (they are not rejections), duplicates and genuine validation
    /// failures are reported as one aggregated warning. Documents arrive in
    /// query order, which is preserved.
    /// </summary>
    private List<WordData> MapAndValidate(IReadOnlyList<IReadOnlyDictionary<string, object>> documents,
        string difficultyId)
    {
        var accepted = new List<WordData>();
        if (documents == null || documents.Count == 0)
            return accepted;

        string filter = WordDifficulty.Normalize(difficultyId);
        var seenAnswers = new HashSet<string>();
        var rejected = new List<string>();

        foreach (IReadOnlyDictionary<string, object> document in documents)
        {
            WordData word;
            string reason;
            if (!FirestoreWordMapper.TryMap(document, out word, out reason, minLength, maxLength))
            {
                rejected.Add(reason);
                continue;
            }

            if (filter.Length > 0 && word.DifficultyId != filter)
                continue; // another band's document; the query filters server-side too

            if (!word.IsActive || word.Weight <= 0)
                continue; // soft-disabled content, exactly like the bundled loader

            if (!seenAnswers.Add(word.Answer))
            {
                rejected.Add("duplicate answer '" + word.Answer + "'");
                continue;
            }

            accepted.Add(word);
        }

        if (rejected.Count > 0)
        {
            string noun = rejected.Count == 1 ? "document" : "documents";
            Debug.LogWarning(
                LogPrefix + " rejected " + rejected.Count + " of " + documents.Count +
                " remote word " + noun + " for difficulty '" + difficultyId +
                "':\n - " + string.Join("\n - ", rejected));
        }

        return accepted;
    }
}
