using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// EditMode coverage for the three-tier word read path (issue #32): the remote
/// source is stubbed, so every tier and every degradation rule is exercised
/// without Firebase - which is the point of <see cref="IWordRepository"/>.
/// </summary>
public class WordRepositoryTests
{
    const int Min = WordValidator.MinAnswerLength;
    const int Max = WordValidator.MaxAnswerLength;

    string directory;
    WordCache cache;
    LogCapture log;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "hangaroo-wordrepo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        cache = new WordCache(directory);
        log = new LogCapture();
    }

    [TearDown]
    public void TearDown()
    {
        log.Dispose();

        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // Best effort: a leftover temp directory must not fail the run.
        }
    }

    // --- helpers -------------------------------------------------------------

    /// <summary>Stub remote source: a fixed result, or a thrown transport error.</summary>
    private sealed class FakeWordSource : IWordSource
    {
        public WordSourceResult result;
        public bool throws;
        public int fetchCount;

        public Task<WordSourceResult> FetchAsync(string difficultyId)
        {
            fetchCount++;
            if (throws)
                throw new InvalidOperationException("network down");
            return Task.FromResult(result);
        }
    }

    /// <summary>Captures warnings/errors so tests can assert "one aggregated warning".</summary>
    private sealed class LogCapture : IDisposable
    {
        public readonly List<string> warnings = new List<string>();
        public readonly List<string> errors = new List<string>();

        public LogCapture()
        {
            Application.logMessageReceived += OnLog;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
                warnings.Add(condition);
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                errors.Add(condition);
        }
    }

    /// <summary>
    /// One remote document in the Firestore shape. The clue and hint deliberately
    /// never contain the answer, so a document is only rejected for the rule the
    /// test is actually about.
    /// </summary>
    static IReadOnlyDictionary<string, object> Doc(string answer, string difficultyId = "medium",
        bool isActive = true, int weight = 1)
    {
        return new Dictionary<string, object>
        {
            { "clue", "Mystery word" },
            { "answer", answer },
            { "hint", "Spell it out" },
            { "categoryId", "space" },
            { "difficultyId", difficultyId },
            { "weight", (long)weight },
            { "isActive", isActive },
        };
    }

    static WordSourceResult Success(int contentVersion, params IReadOnlyDictionary<string, object>[] documents)
    {
        return WordSourceResult.From(contentVersion, documents);
    }

    static string Json(params string[] objects)
    {
        return "{ \"schemaVersion\": 1, \"words\": [ " + string.Join(", ", objects) + " ] }";
    }

    static string Entry(string clue, string answer, string hint, string category, string difficulty)
    {
        return "{ \"clue\": \"" + clue + "\", \"answer\": \"" + answer + "\", \"hint\": \"" + hint +
               "\", \"category\": \"" + category + "\", \"difficulty\": \"" + difficulty + "\" }";
    }

    /// <summary>Two medium words plus one easy word, so band filtering is observable.</summary>
    static BundledWordSource MediumBundled()
    {
        return new BundledWordSource(
            Json(
                Entry("Largest Planet", "JUPITER", "Starts with J", "space", "medium"),
                Entry("Natural Force", "GRAVITY", "Starts with G", "science", "medium"),
                Entry("Streak Across Sky", "COMET", "Starts with C", "space", "easy")),
            "words-test.json");
    }

    WordRepository Build(IWordSource source, BundledWordSource bundled, TimeSpan? maxAge = null)
    {
        return new WordRepository(source, cache, bundled, Min, Max, maxAge);
    }

    // --- the tiers -----------------------------------------------------------

    [Test]
    public async Task GetWords_WhenTheFetchThrows_ServesTheBundledListWithOneAggregatedWarning()
    {
        var source = new FakeWordSource { throws = true };
        WordRepository repo = Build(source, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(1, source.fetchCount, "one fetch per GetWords call; GameManager calls once per run");
        Assert.AreEqual(WordContentSource.Bundled, result.source);
        Assert.IsTrue(result.isStale);
        Assert.IsTrue(result.isOffline);
        Assert.AreEqual(2, result.words.Count, "only the medium band");
        Assert.AreEqual("JUPITER", result.words[0].Answer);
        Assert.AreEqual("GRAVITY", result.words[1].Answer);

        Assert.AreEqual(1, log.warnings.Count, "exactly one aggregated warning, not one per word:\n" +
                                               string.Join("\n", log.warnings));
        StringAssert.Contains("network down", log.warnings[0]);
        StringAssert.Contains("bundled", log.warnings[0]);
        Assert.AreEqual(0, log.errors.Count);
    }

    [Test]
    public async Task GetWords_WhenTheFetchReportsFailure_ServesTheCache()
    {
        cache.Write("medium", 14, new List<WordData>
        {
            new WordData("Mystery word", "ALPHA", "Spell it out", "space", "medium"),
        }, DateTimeOffset.UtcNow);
        var source = new FakeWordSource { result = WordSourceResult.Failed() };
        WordRepository repo = Build(source, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Cache, result.source);
        Assert.IsTrue(result.isStale);
        Assert.IsFalse(result.isOffline);
        Assert.AreEqual(14, result.contentVersion);
        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("ALPHA", result.words[0].Answer);

        Assert.AreEqual(1, log.warnings.Count);
        StringAssert.Contains("local cache", log.warnings[0]);
        StringAssert.Contains("the source reported a failure", log.warnings[0]);
        Assert.AreEqual(0, log.errors.Count);
    }

    [Test]
    public async Task GetWords_WhenTheFetchSucceeds_ServesRemoteWritesTheCacheAndRaisesOnContentChanged()
    {
        var source = new FakeWordSource { result = Success(15, Doc("jupiter")) };
        WordRepository repo = Build(source, MediumBundled());
        int changes = 0;
        repo.OnContentChanged += () => changes++;

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Firestore, result.source);
        Assert.IsFalse(result.isStale);
        Assert.IsFalse(result.isOffline);
        Assert.AreEqual(15, result.contentVersion);
        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].Answer, "remote answers are normalised like bundled ones");

        Assert.AreEqual(1, source.fetchCount, "one fetch per GetWords call");
        Assert.AreEqual(1, changes);
        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry cached));
        Assert.AreEqual(15, cached.contentVersion);
        Assert.AreEqual("JUPITER", cached.words[0].Answer);

        Assert.AreEqual(0, log.warnings.Count, "a healthy fetch logs nothing:\n" + string.Join("\n", log.warnings));
        Assert.AreEqual(0, log.errors.Count);
    }

    [Test]
    public async Task GetWords_WhenTheFetchIsEmpty_DoesNotOverwriteTheCache()
    {
        cache.Write("medium", 14, new List<WordData>
        {
            new WordData("Mystery word", "ALPHA", "Spell it out", "space", "medium"),
        }, DateTimeOffset.UtcNow);
        var source = new FakeWordSource { result = Success(15) }; // succeeded, zero documents
        WordRepository repo = Build(source, MediumBundled());
        int changes = 0;
        repo.OnContentChanged += () => changes++;

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Cache, result.source);
        Assert.AreEqual("ALPHA", result.words[0].Answer, "the good cache survives the empty result");
        Assert.AreEqual(0, changes);

        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry cached));
        Assert.AreEqual(14, cached.contentVersion, "an empty fetch never overwrites a valid cache");
        Assert.AreEqual("ALPHA", cached.words[0].Answer);

        Assert.AreEqual(1, log.warnings.Count);
        StringAssert.Contains("the query returned no documents", log.warnings[0]);
    }

    [Test]
    public async Task GetWords_WhenEveryDocumentIsInvalid_DoesNotOverwriteTheCache()
    {
        cache.Write("medium", 14, new List<WordData>
        {
            new WordData("Mystery word", "ALPHA", "Spell it out", "space", "medium"),
        }, DateTimeOffset.UtcNow);
        var source = new FakeWordSource
        {
            result = Success(15, Doc("bad answer!"), Doc("12345")), // both fail the shared rules
        };
        WordRepository repo = Build(source, MediumBundled());
        int changes = 0;
        repo.OnContentChanged += () => changes++;

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Cache, result.source);
        Assert.AreEqual("ALPHA", result.words[0].Answer);
        Assert.AreEqual(0, changes);
        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry cached));
        Assert.AreEqual(14, cached.contentVersion);

        Assert.AreEqual(2, log.warnings.Count, "one reason-listing warning + one degradation warning");
        StringAssert.Contains("rejected 2 of 2", log.warnings[0]);
        StringAssert.Contains("local cache", log.warnings[1]);
    }

    // --- the fallback reasons (issue #40's content_fallback) ----------------

    [Test]
    public async Task GetWords_WhenTheFetchThrows_ReportsTheNetworkFallbackReason()
    {
        WordRepository repo = Build(new FakeWordSource { throws = true }, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordFallbackReason.Network, result.fallbackReason,
            "a thrown transport error is the Firestore-outage reason");
    }

    [Test]
    public async Task GetWords_WhenTheSourceReportsFailure_ReportsTheNetworkFallbackReason()
    {
        WordRepository repo = Build(new FakeWordSource { result = WordSourceResult.Failed() },
            MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordFallbackReason.Network, result.fallbackReason);
    }

    [Test]
    public async Task GetWords_WhenTheQueryReturnsNoDocuments_ReportsTheEmptyFallbackReason()
    {
        WordRepository repo = Build(new FakeWordSource { result = Success(15) }, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordFallbackReason.Empty, result.fallbackReason,
            "an empty collection is distinct from an outage and from a bad deploy");
    }

    [Test]
    public async Task GetWords_WhenNoDocumentSurvivesValidation_ReportsTheInvalidFallbackReason()
    {
        WordRepository repo = Build(
            new FakeWordSource { result = Success(15, Doc("bad answer!"), Doc("12345")) },
            MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordFallbackReason.Invalid, result.fallbackReason,
            "documents that all fail validation point at a bad deploy");
    }

    [Test]
    public async Task GetWords_WithNoRemoteSource_ReportsNoFallback()
    {
        WordRepository repo = Build(null, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordFallbackReason.None, result.fallbackReason,
            "with no source configured nothing degraded - the tiers are the normal path");
        Assert.AreEqual(0, log.warnings.Count, "and nothing is warned:\n" + string.Join("\n", log.warnings));
    }

    [Test]
    public async Task GetWords_WhenTheFetchSucceeds_ReportsNoFallback()
    {
        WordRepository repo = Build(new FakeWordSource { result = Success(15, Doc("jupiter")) },
            MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordFallbackReason.None, result.fallbackReason,
            "the remote tier served - no fallback happened");
    }

    [Test]
    public async Task GetWords_ANewContentVersion_ReplacesTheCacheWholesale()
    {
        cache.Write("medium", 14, new List<WordData>
        {
            new WordData("Mystery word", "ALPHA", "Spell it out", "space", "medium"),
        }, DateTimeOffset.UtcNow);
        var source = new FakeWordSource { result = Success(15, Doc("jupiter")) };
        WordRepository repo = Build(source, MediumBundled());
        int changes = 0;
        repo.OnContentChanged += () => changes++;

        WordListResult first = await repo.GetWordsAsync("medium");
        Assert.AreEqual(WordContentSource.Firestore, first.source);
        Assert.AreEqual("JUPITER", first.words[0].Answer);
        Assert.AreEqual(1, changes);
        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry afterFirst));
        Assert.AreEqual(15, afterFirst.contentVersion);
        Assert.AreEqual("JUPITER", afterFirst.words[0].Answer, "the v14 words are gone, not merged");

        source.result = WordSourceResult.Failed();
        WordListResult second = await repo.GetWordsAsync("medium");
        Assert.AreEqual(WordContentSource.Cache, second.source);
        Assert.AreEqual(15, second.contentVersion, "the failed fetch serves the replacement, never the old version");
        Assert.AreEqual("JUPITER", second.words[0].Answer);
        Assert.AreEqual(1, changes, "only the version change raises OnContentChanged");
        Assert.AreEqual(2, source.fetchCount);
    }

    [Test]
    public async Task GetWords_WithAStaleCache_FallsBackToTheBundledListAndInvalidatesTheEntry()
    {
        cache.Write("medium", 14, new List<WordData>
        {
            new WordData("Mystery word", "ALPHA", "Spell it out", "space", "medium"),
        }, DateTimeOffset.UtcNow.AddDays(-40));
        var source = new FakeWordSource { result = WordSourceResult.Failed() };
        WordRepository repo = Build(source, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Bundled, result.source);
        Assert.AreEqual("JUPITER", result.words[0].Answer, "a month-old snapshot is not served; the build's list is");
        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry _), "the stale entry was invalidated");

        Assert.AreEqual(1, log.warnings.Count);
        StringAssert.Contains("bundled", log.warnings[0]);
    }

    [Test]
    public async Task GetWords_WithoutASource_ServesCacheOrBundledWithoutWarning()
    {
        // No remote configured (today's state, before issue #30): the cache and
        // bundled tiers are the normal path, not a degradation.
        WordRepository repo = Build(null, MediumBundled());
        cache.Write("medium", 14, new List<WordData>
        {
            new WordData("Mystery word", "ALPHA", "Spell it out", "space", "medium"),
        }, DateTimeOffset.UtcNow);

        WordListResult withCache = await repo.GetWordsAsync("medium");
        Assert.AreEqual(WordContentSource.Cache, withCache.source);
        Assert.AreEqual("ALPHA", withCache.words[0].Answer);

        cache.Invalidate("medium");
        WordListResult bundled = await repo.GetWordsAsync("medium");
        Assert.AreEqual(WordContentSource.Bundled, bundled.source);
        Assert.AreEqual("JUPITER", bundled.words[0].Answer);

        Assert.AreEqual(0, log.warnings.Count, "nothing about this path is a failure:\n" +
                                                string.Join("\n", log.warnings));
        Assert.AreEqual(0, log.errors.Count);
    }

    [Test]
    public async Task GetWords_WhenEveryTierFails_DegradesToTheBuiltInPool()
    {
        LogAssert.Expect(LogType.Error, new Regex("rejected 1 malformed word entry"));
        LogAssert.Expect(LogType.Error, new Regex("Using the built-in fallback word pool"));

        var source = new FakeWordSource { throws = true };
        WordRepository repo = Build(source, new BundledWordSource("{ this is not json", "broken.json"));

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Bundled, result.source);
        Assert.AreEqual(WordPool.CreateFallback().Count, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].Answer, "never empty, even with every tier broken");
        Assert.AreEqual(0, log.warnings.Count, "the error report already carries this degradation");
        Assert.AreEqual(2, log.errors.Count);
    }

    [Test]
    public async Task GetWords_FiltersRemoteDocumentsByBandSelectionAndDuplicates()
    {
        var source = new FakeWordSource
        {
            result = Success(15,
                Doc("jupiter"),                    // accepted: the requested band
                Doc("blackhole", "hard"),          // dropped silently: another band
                Doc("saturn", "medium", false),    // dropped silently: soft-disabled
                Doc("neptune", "medium", true, 0), // dropped silently: weight 0
                Doc("jupiter")),                   // rejected: duplicate
        };
        WordRepository repo = Build(source, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("medium");

        Assert.AreEqual(WordContentSource.Firestore, result.source);
        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].Answer);

        Assert.AreEqual(1, log.warnings.Count, "selection drops are silent; only real rejections are listed");
        StringAssert.Contains("rejected 1 of 5", log.warnings[0]);
        StringAssert.Contains("duplicate", log.warnings[0]);
    }

    [Test]
    public async Task GetWords_WhenTheBandIsEmpty_FallsBackToTheFullPool()
    {
        WordRepository repo = Build(null, MediumBundled());

        WordListResult result = await repo.GetWordsAsync("hard"); // no hard words in the file

        Assert.AreEqual(WordContentSource.Bundled, result.source);
        Assert.AreEqual(3, result.words.Count, "the full pool, so the board is playable");
        Assert.AreEqual(1, log.warnings.Count);
        StringAssert.Contains("no words authored for difficulty 'hard'", log.warnings[0]);
    }

    [Test]
    public async Task GetWords_WithTheSameContentVersion_RaisesOnContentChangedOnlyOnce()
    {
        var source = new FakeWordSource { result = Success(15, Doc("jupiter")) };
        WordRepository repo = Build(source, MediumBundled());
        int changes = 0;
        repo.OnContentChanged += () => changes++;

        await repo.GetWordsAsync("medium");
        await repo.GetWordsAsync("medium");

        Assert.AreEqual(2, source.fetchCount, "exactly one fetch per GetWords call");
        Assert.AreEqual(1, changes, "an unchanged contentVersion is not a content change");
        Assert.AreEqual(0, log.warnings.Count);
    }
}
