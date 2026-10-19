using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode coverage for the local-cache tier of the word read path (issue #32).</summary>
public class WordCacheTests
{
    string directory;
    WordCache cache;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "hangaroo-wordcache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        cache = new WordCache(directory);
    }

    [TearDown]
    public void TearDown()
    {
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

    static List<WordData> Words(params string[] answers)
    {
        var words = new List<WordData>();
        foreach (string answer in answers)
            words.Add(new WordData("Mystery word", answer, "Spell it out", "space", "medium"));
        return words;
    }

    [Test]
    public void WriteThenTryRead_RoundTripsTheEntry()
    {
        Assert.IsTrue(cache.Write("medium", 14, Words("JUPITER", "GRAVITY"), DateTimeOffset.UtcNow));

        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.AreEqual(14, entry.contentVersion);
        Assert.AreEqual("medium", entry.difficultyId);
        Assert.AreEqual(2, entry.words.Count);
        Assert.AreEqual("JUPITER", entry.words[0].Answer);
        Assert.IsFalse(string.IsNullOrEmpty(entry.fetchedAt));
    }

    [Test]
    public void Write_UsesTheIssuesJsonShape()
    {
        cache.Write("medium", 14, Words("JUPITER"), DateTimeOffset.UtcNow);

        string json = File.ReadAllText(cache.PathFor("medium"));
        StringAssert.Contains("contentVersion", json);
        StringAssert.Contains("fetchedAt", json);
        StringAssert.Contains("difficultyId", json);
        StringAssert.Contains("words", json);
        StringAssert.Contains("JUPITER", json);
    }

    [Test]
    public void TryRead_MissingFile_IsAMiss()
    {
        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.IsNull(entry);
    }

    [Test]
    public void TryRead_CorruptFile_IsAMissWithoutThrowing()
    {
        File.WriteAllText(cache.PathFor("medium"), "{ this is not json");

        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.IsNull(entry);
    }

    [Test]
    public void TryRead_UnversionedEntry_IsInvalid()
    {
        var unversioned = new WordCacheEntry
        {
            contentVersion = 0, // an absent contentVersion reads as 0: unversioned
            fetchedAt = DateTimeOffset.UtcNow.ToString("O"),
            difficultyId = "medium",
            words = Words("JUPITER"),
        };
        File.WriteAllText(cache.PathFor("medium"), JsonUtility.ToJson(unversioned));

        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.IsNull(entry);
    }

    [Test]
    public void TryRead_MismatchedDifficultyKey_IsInvalid()
    {
        var misKeyed = new WordCacheEntry
        {
            contentVersion = 14,
            fetchedAt = DateTimeOffset.UtcNow.ToString("O"),
            difficultyId = "easy", // file sits under the medium key but claims another band
            words = Words("COMET"),
        };
        File.WriteAllText(cache.PathFor("medium"), JsonUtility.ToJson(misKeyed));

        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.IsNull(entry);
    }

    [Test]
    public void TryRead_UnreadableTimestamp_IsInvalid()
    {
        var undated = new WordCacheEntry
        {
            contentVersion = 14,
            fetchedAt = "not a date",
            difficultyId = "medium",
            words = Words("JUPITER"),
        };
        File.WriteAllText(cache.PathFor("medium"), JsonUtility.ToJson(undated));

        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.IsNull(entry);
    }

    [Test]
    public void Write_EmptyWordList_IsRefused()
    {
        Assert.IsFalse(cache.Write("medium", 14, new List<WordData>(), DateTimeOffset.UtcNow));
        Assert.IsFalse(File.Exists(cache.PathFor("medium")));
        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry _));
    }

    [Test]
    public void Write_MissingContentVersion_IsRefusedAndPreservesTheExistingEntry()
    {
        Assert.IsTrue(cache.Write("medium", 14, Words("JUPITER"), DateTimeOffset.UtcNow));

        Assert.IsFalse(cache.Write("medium", 0, Words("GRAVITY"), DateTimeOffset.UtcNow));

        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.AreEqual(14, entry.contentVersion);
        Assert.AreEqual("JUPITER", entry.words[0].Answer);
    }

    [Test]
    public void TryReadFresh_FreshEntry_IsServed()
    {
        cache.Write("medium", 14, Words("JUPITER"), DateTimeOffset.UtcNow);

        Assert.IsTrue(cache.TryReadFresh("medium", WordCache.DefaultMaxAge, DateTimeOffset.UtcNow,
            out WordCacheEntry entry));
        Assert.AreEqual(14, entry.contentVersion);
    }

    [Test]
    public void TryReadFresh_StaleEntry_IsDeletedAndReportedAsAMiss()
    {
        cache.Write("medium", 14, Words("JUPITER"), DateTimeOffset.UtcNow.AddDays(-40));

        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry _), "stale but valid should still parse");

        Assert.IsFalse(cache.TryReadFresh("medium", WordCache.DefaultMaxAge, DateTimeOffset.UtcNow,
            out WordCacheEntry entry));
        Assert.IsNull(entry);
        Assert.IsFalse(File.Exists(cache.PathFor("medium")), "a stale entry is invalidated on the spot");
    }

    [Test]
    public void Write_NormalisesTheDifficultyKey()
    {
        cache.Write("Medium", 14, Words("JUPITER"), DateTimeOffset.UtcNow);

        Assert.IsTrue(File.Exists(cache.PathFor("medium")));
        Assert.IsTrue(cache.TryRead("medium", out WordCacheEntry entry));
        Assert.AreEqual("medium", entry.difficultyId);
    }

    [Test]
    public void Invalidate_RemovesTheEntry()
    {
        cache.Write("medium", 14, Words("JUPITER"), DateTimeOffset.UtcNow);

        cache.Invalidate("medium");

        Assert.IsFalse(cache.TryRead("medium", out WordCacheEntry _));
    }
}
