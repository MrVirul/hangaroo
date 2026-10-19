using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode coverage for the word-content pipeline (issue #18, part 2; Story 1.4).</summary>
public class WordPoolTests
{
    const int Min = WordValidator.MinAnswerLength;
    const int Max = WordValidator.MaxAnswerLength;

    static string Json(params string[] objects)
    {
        return "{ \"words\": [ " + string.Join(", ", objects) + " ] }";
    }

    static string Entry(string clue, string answer, string hint, string category, string difficulty)
    {
        return "{ \"clue\": \"" + clue + "\", \"answer\": \"" + answer + "\", \"hint\": \"" + hint +
               "\", \"category\": \"" + category + "\", \"difficulty\": \"" + difficulty + "\" }";
    }

    [Test]
    public void Parse_AcceptsValidEntriesAndCarriesMetadata()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(Entry("Largest Planet", "jupiter", "Starts with J", "space", "medium")), Min, Max);

        Assert.AreEqual(0, result.rejected.Count);
        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].Answer);
        Assert.AreEqual("space", result.words[0].CategoryId);
        Assert.AreEqual("medium", result.words[0].DifficultyId);
    }

    [Test]
    public void Parse_SkipsMalformedEntriesAndReportsWhy()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(
                Entry("City", "NEW YORK", "Big Apple", "geography", "hard"),   // space
                Entry("Contraction", "DON'T", "Negation", "general", "medium"), // apostrophe
                Entry("Largest Planet", "JUPITER", "Starts with J", "space", "medium")),
            Min, Max);

        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].Answer);
        Assert.AreEqual(2, result.rejected.Count);
        Assert.IsTrue(result.rejected[0].Contains("NEW YORK"));
        Assert.IsTrue(result.rejected[1].Contains("DON'T"));
    }

    [Test]
    public void Parse_RejectsDuplicateAnswersCaseInsensitively()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(
                Entry("Largest Planet", "JUPITER", "Starts with J", "space", "medium"),
                Entry("Another clue", "jupiter", "Same answer", "space", "medium")),
            Min, Max);

        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual(1, result.rejected.Count);
        StringAssert.Contains("duplicate", result.rejected[0]);
    }

    [Test]
    public void Parse_EmptyOrGarbageInputYieldsNoWords()
    {
        Assert.IsFalse(WordPool.Parse("", Min, Max).HasWords);
        Assert.IsFalse(WordPool.Parse("not json", Min, Max).HasWords);
        Assert.IsFalse(WordPool.Parse("{ \"words\": [] }", Min, Max).HasWords);
        Assert.IsFalse(WordPool.Parse("{ \"words\": [ null ] }", Min, Max).HasWords);
    }

    [Test]
    public void Parse_InfersDifficultyAndDefaultsCategoryForUnmigratedEntries()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(
                "{ \"clue\": \"Largest Planet\", \"answer\": \"JUPITER\", \"hint\": \"Starts with J\" }",
                Entry("Red fruit", "APPLE", "Starts with A", "food", "easy")),
            Min, Max);

        Assert.AreEqual(2, result.words.Count, string.Join("\n", result.rejected));
        Assert.AreEqual("JUPITER", result.words[0].Answer);
        Assert.AreEqual(WordDifficulty.Medium, result.words[0].DifficultyId);
        Assert.AreEqual(WordCategory.General, result.words[0].CategoryId);
        Assert.AreEqual("APPLE", result.words[1].Answer);
    }

    [Test]
    public void Parse_FiltersToTheRequestedDifficulty()
    {
        string json = Json(
            Entry("Largest Planet", "JUPITER", "Starts with J", "space", "medium"),
            Entry("Red fruit", "APPLE", "Starts with A", "food", "easy"),
            Entry("Huge river mammal", "HIPPOPOTAMUS", "Starts with H", "animals", "hard"));

        WordPool.ParseResult medium = WordPool.Parse(json, Min, Max, "medium");

        Assert.AreEqual(1, medium.words.Count);
        Assert.AreEqual("JUPITER", medium.words[0].Answer);

        // Entries from other bands are ignored, not reported as rejections.
        CollectionAssert.IsEmpty(medium.rejected);
    }

    [Test]
    public void Parse_RejectsAnswerOutsideItsDeclaredBand()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(Entry("Largest Planet", "JUPITER", "Starts with J", "space", "easy")),
            Min, Max);

        Assert.AreEqual(0, result.words.Count);
        Assert.AreEqual(1, result.rejected.Count);
        StringAssert.Contains("outside the easy band", result.rejected[0]);
    }

    [Test]
    public void Parse_RejectsClueOrHintContainingTheAnswer()
    {
        WordPool.ParseResult clue = WordPool.Parse(
            Json(Entry("Largest Planet Jupiter", "JUPITER", "Starts with J", "space", "medium")),
            Min, Max);
        Assert.AreEqual(1, clue.rejected.Count);
        StringAssert.Contains("giveaway", clue.rejected[0]);

        WordPool.ParseResult hint = WordPool.Parse(
            Json(Entry("Largest Planet", "JUPITER", "JUPITER has 7 letters", "space", "medium")),
            Min, Max);
        Assert.AreEqual(1, hint.rejected.Count);
        StringAssert.Contains("giveaway", hint.rejected[0]);
    }

    [Test]
    public void Parse_SkipsSoftDeletedAndZeroWeightEntriesSilently()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(
                Entry("Largest Planet", "JUPITER", "Starts with J", "space", "medium"),
                "{ \"clue\": \"Red fruit\", \"answer\": \"APPLE\", \"hint\": \"Starts with A\", \"category\": \"food\", \"difficulty\": \"easy\", \"isActive\": false }",
                "{ \"clue\": \"Striped cat\", \"answer\": \"TIGER\", \"hint\": \"Starts with T\", \"category\": \"animals\", \"difficulty\": \"easy\", \"weight\": 0 }"),
            Min, Max);

        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].Answer);
        CollectionAssert.IsEmpty(result.rejected, "disabled entries are skipped, not rejected");
    }

    [Test]
    public void Parse_RejectsAnUnsupportedSchemaVersionInsteadOfMisParsingIt()
    {
        WordPool.ParseResult result = WordPool.Parse(
            "{ \"schemaVersion\": 99, \"words\": [ " +
            Entry("Largest Planet", "JUPITER", "Starts with J", "space", "medium") + " ] }",
            Min, Max);

        Assert.IsFalse(result.HasWords);
        Assert.AreEqual(1, result.rejected.Count);
        StringAssert.Contains("schemaVersion", result.rejected[0]);
    }

    /// <summary>
    /// Regression protection for the offline path (issue #33): the pre-Story-1.4
    /// 65-entry words.json, declaring only clue/answer/hint, must still load
    /// without any change to the file itself - difficulty inferred from answer
    /// length, category defaulted, and exactly one aggregated warning naming the
    /// file for the whole parse.
    /// </summary>
    [Test]
    public void LegacySixtyFiveEntryFile_StillLoadsWithOneAggregatedWarning()
    {
        var wordPoolWarnings = new List<string>();
        Application.LogCallback onLog = (condition, stackTrace, type) =>
        {
            if (type == LogType.Warning && condition.Contains("WordPool:"))
                wordPoolWarnings.Add(condition);
        };

        WordPool.ParseResult result;
        Application.logMessageReceived += onLog;
        try
        {
            result = WordPool.Parse(LegacyJson(), Min, Max, null, "legacy-words-65.json");
        }
        finally
        {
            Application.logMessageReceived -= onLog;
        }

        // 64 load; the single rejection is Story 1.4's giveaway rule (the clue
        // contains the answer), which this file predates.
        Assert.AreEqual(64, result.words.Count, string.Join("\n", result.rejected));
        Assert.AreEqual(1, result.rejected.Count);
        StringAssert.Contains("giveaway", result.rejected[0]);

        // Every entry is seven letters, so all infer the medium band.
        foreach (WordData word in result.words)
            Assert.AreEqual(WordDifficulty.Medium, word.DifficultyId);

        Assert.AreEqual(1, wordPoolWarnings.Count,
            "exactly one aggregated warning expected, got: " + string.Join(" | ", wordPoolWarnings));
        StringAssert.Contains("legacy-words-65.json", wordPoolWarnings[0]);
        StringAssert.Contains("65/65 entries have no difficulty", wordPoolWarnings[0]);
        StringAssert.Contains("medium=65", wordPoolWarnings[0]);
    }

    [Test]
    public void FallbackPool_IsNonEmptyAndFullyValid()
    {
        var fallback = WordPool.CreateFallback();

        Assert.Greater(fallback.Count, 0);

        foreach (WordData entry in fallback)
        {
            WordValidationResult result = WordValidator.Validate(entry, Min, Max);
            Assert.IsTrue(result.IsValid, entry.Answer + ": " + result.Reason);
        }
    }

    /// <summary>
    /// Integration guard: the word list that actually ships must pass the same
    /// validator the editor pipeline and the difficulty banks use.
    /// </summary>
    [Test]
    public void ShippedWordsJson_PassesValidation()
    {
        WordPool.ParseResult result = WordPool.Parse(ShippedJson(), Min, Max);

        CollectionAssert.IsEmpty(result.rejected, string.Join("\n", result.rejected));
        Assert.Greater(result.words.Count, 0);
    }

    /// <summary>
    /// Every difficulty must offer enough words, and Hard must actually reach the
    /// top of its band, so the 8-12 promise is real (Story 1.4).
    /// </summary>
    [Test]
    public void ShippedWordsJson_EveryBandMeetsTheMinimumAndHardReachesTwelve()
    {
        WordPool.ParseResult result = WordPool.Parse(ShippedJson(), Min, Max);

        var counts = new Dictionary<string, int>
        {
            { WordDifficulty.Easy, 0 },
            { WordDifficulty.Medium, 0 },
            { WordDifficulty.Hard, 0 },
        };

        int hardLongest = 0;
        foreach (WordData entry in result.words)
        {
            if (!counts.ContainsKey(entry.DifficultyId))
                Assert.Fail("Unexpected difficulty '" + entry.DifficultyId + "' for " + entry.Answer);

            counts[entry.DifficultyId]++;

            if (entry.DifficultyId == WordDifficulty.Hard && entry.Answer.Length > hardLongest)
                hardLongest = entry.Answer.Length;
        }

        foreach (KeyValuePair<string, int> pair in counts)
            Assert.GreaterOrEqual(pair.Value, 60, pair.Key + " band is too small");

        Assert.AreEqual(12, hardLongest, "Hard must reach 12 letters");
    }

    /// <summary>
    /// The shipped file must declare the version this build reads, so bumping
    /// <see cref="WordPool.BundledWordList.CurrentSchemaVersion"/> without
    /// re-authoring the file (or vice versa) fails here rather than at runtime.
    /// </summary>
    [Test]
    public void ShippedWordsJson_DeclaresTheSchemaVersionThisBuildReads()
    {
        var payload = JsonUtility.FromJson<WordPool.BundledWordList>(ShippedJson());

        Assert.AreEqual(WordPool.BundledWordList.CurrentSchemaVersion, payload.schemaVersion);
    }

    static string ShippedJson()
    {
        string path = Path.Combine(Application.dataPath, "Data", "words.json");
        Assert.IsTrue(File.Exists(path), "Missing shipped word list at " + path);
        return File.ReadAllText(path);
    }

    static string LegacyJson()
    {
        string path = Path.Combine(Application.dataPath, "Tests", "EditMode", "Words", "legacy-words-65.json");
        Assert.IsTrue(File.Exists(path), "Missing legacy fixture at " + path);
        return File.ReadAllText(path);
    }
}
