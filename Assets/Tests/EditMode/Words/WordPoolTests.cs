using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode coverage for the word-content pipeline (issue #18, part 2).</summary>
public class WordPoolTests
{
    const int Min = WordValidator.MinAnswerLength;
    const int Max = WordValidator.MaxAnswerLength;

    static string Json(params string[] objects)
    {
        return "{ \"words\": [ " + string.Join(", ", objects) + " ] }";
    }

    static string Entry(string clue, string answer, string hint)
    {
        return "{ \"clue\": \"" + clue + "\", \"answer\": \"" + answer + "\", \"hint\": \"" + hint + "\" }";
    }

    [Test]
    public void Parse_AcceptsValidEntriesAndNormalizesAnswerCase()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(Entry("Largest Planet", "jupiter", "Starts with J")), Min, Max);

        Assert.AreEqual(0, result.rejected.Count);
        Assert.AreEqual(1, result.words.Count);
        Assert.AreEqual("JUPITER", result.words[0].answer);
    }

    [Test]
    public void Parse_SkipsMalformedEntriesAndReportsWhy()
    {
        WordPool.ParseResult result = WordPool.Parse(
            Json(
                Entry("City", "NEW YORK", "Big Apple"),   // space
                Entry("Contraction", "DON'T", "Negation"), // apostrophe
                Entry("Largest Planet", "JUPITER", "Starts with J")),
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
                Entry("Largest Planet", "JUPITER", "Starts with J"),
                Entry("Another clue", "jupiter", "Same answer")),
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
    public void FallbackPool_IsNonEmptyAndFullyValid()
    {
        var fallback = WordPool.CreateFallback();

        Assert.Greater(fallback.Count, 0);

        foreach (WordEntry entry in fallback)
        {
            WordValidationResult result =
                WordValidator.Validate(entry.clue, entry.answer, entry.hint, Min, Max);
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
        string path = Path.Combine(Application.dataPath, "Data", "words.json");
        Assert.IsTrue(File.Exists(path), "Missing shipped word list at " + path);

        WordPool.ParseResult result = WordPool.Parse(File.ReadAllText(path), Min, Max);

        CollectionAssert.IsEmpty(result.rejected, string.Join("\n", result.rejected));
        Assert.Greater(result.words.Count, 0);
    }
}
