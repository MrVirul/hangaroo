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
        Assert.AreEqual("JUPITER", result.words[0].answer);
        Assert.AreEqual("space", result.words[0].category);
        Assert.AreEqual("medium", result.words[0].difficulty);
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
        Assert.AreEqual("JUPITER", result.words[0].answer);
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
    public void Parse_RejectsEntriesMissingCategoryOrDifficulty()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(
                "{ \"clue\": \"Largest Planet\", \"answer\": \"JUPITER\", \"hint\": \"Starts with J\" }",
                Entry("Red fruit", "APPLE", "Starts with A", "food", "easy")),
            Min, Max);

        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("APPLE", result.words[0].answer);
        Assert.AreEqual(1, result.rejected.Count);
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
        Assert.AreEqual("JUPITER", medium.words[0].answer);

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
    public void FallbackPool_IsNonEmptyAndFullyValid()
    {
        var fallback = WordPool.CreateFallback();

        Assert.Greater(fallback.Count, 0);

        foreach (WordEntry entry in fallback)
        {
            WordValidationResult result = WordValidator.Validate(
                entry.clue, entry.answer, entry.hint, entry.category, entry.difficulty, Min, Max);
            Assert.IsTrue(result.IsValid, entry.answer + ": " + result.Reason);
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
        foreach (WordEntry entry in result.words)
        {
            if (!counts.ContainsKey(entry.difficulty))
                Assert.Fail("Unexpected difficulty '" + entry.difficulty + "' for " + entry.answer);

            counts[entry.difficulty]++;

            if (entry.difficulty == WordDifficulty.Hard && entry.answer.Length > hardLongest)
                hardLongest = entry.answer.Length;
        }

        foreach (KeyValuePair<string, int> pair in counts)
            Assert.GreaterOrEqual(pair.Value, 60, pair.Key + " band is too small");

        Assert.AreEqual(12, hardLongest, "Hard must reach 12 letters");
    }

    static string ShippedJson()
    {
        string path = Path.Combine(Application.dataPath, "Data", "words.json");
        Assert.IsTrue(File.Exists(path), "Missing shipped word list at " + path);
        return File.ReadAllText(path);
    }
}
