using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// EditMode coverage for the Firestore document → <see cref="WordData"/> mapping
/// (Story 2.4, issue #33). Runs entirely on plain dictionaries - no SDK - which
/// is exactly the point: the mapper must compile and behave without Firebase.
/// </summary>
public class FirestoreWordMapperTests
{
    const int Min = WordValidator.MinAnswerLength;
    const int Max = WordValidator.MaxAnswerLength;

    static Dictionary<string, object> Doc(string answer, string clue = "Largest Planet",
        string hint = "Starts with J", string categoryId = "space", string difficultyId = "medium")
    {
        return new Dictionary<string, object>
        {
            { "answer", answer },
            { "clue", clue },
            { "hint", hint },
            { "categoryId", categoryId },
            { "difficultyId", difficultyId },
        };
    }

    [Test]
    public void TryMap_ValidDocumentMapsEveryModelMember()
    {
        Dictionary<string, object> fields = Doc("jupiter");
        WordData word;
        string reason;

        bool mapped = FirestoreWordMapper.TryMap(fields, out word, out reason, Min, Max);

        Assert.IsTrue(mapped, reason);
        Assert.AreEqual("JUPITER", word.Answer); // normalised, like the bundled path
        Assert.AreEqual("Largest Planet", word.Clue);
        Assert.AreEqual("Starts with J", word.Hint);
        Assert.AreEqual("space", word.CategoryId);
        Assert.AreEqual("medium", word.DifficultyId);
        Assert.AreEqual(1, word.Weight); // schema default when the key is absent
        Assert.IsTrue(word.IsActive);
        Assert.AreEqual(7, word.Length);
    }

    [TestCase("DON'T", "outside A-Z")]
    [TestCase("NEW YORK", "outside A-Z")]
    public void TryMap_RejectsAnswersTheGameCannotRender(string answer, string expectedFragment)
    {
        WordData word;
        string reason;

        bool mapped = FirestoreWordMapper.TryMap(Doc(answer), out word, out reason, Min, Max);

        Assert.IsFalse(mapped);
        Assert.IsNull(word);
        StringAssert.Contains(expectedFragment, reason);
        StringAssert.Contains(answer, reason);
    }

    [Test]
    public void TryMap_RejectsAnEmptyHintWithAReason()
    {
        WordData word;
        string reason;

        bool mapped = FirestoreWordMapper.TryMap(Doc("JUPITER", hint: ""), out word, out reason, Min, Max);

        Assert.IsFalse(mapped);
        StringAssert.Contains("hint is empty", reason);
    }

    [Test]
    public void TryMap_RejectsAMissingAnswerWithAReason()
    {
        Dictionary<string, object> fields = Doc("JUPITER");
        fields.Remove("answer");

        WordData word;
        string reason;

        bool mapped = FirestoreWordMapper.TryMap(fields, out word, out reason, Min, Max);

        Assert.IsFalse(mapped);
        StringAssert.Contains("answer is empty", reason);
    }

    [Test]
    public void TryMap_CarriesSelectionWeightAndActiveFlag()
    {
        Dictionary<string, object> fields = Doc("JUPITER");
        fields["weight"] = 2L; // Firestore integers arrive boxed as long
        fields["isActive"] = false;

        WordData word;
        string reason;

        bool mapped = FirestoreWordMapper.TryMap(fields, out word, out reason, Min, Max);

        Assert.IsTrue(mapped, reason);
        Assert.AreEqual(2, word.Weight);
        Assert.IsFalse(word.IsActive); // the validator judges content, not availability
    }

    [Test]
    public void TryMap_RejectsFieldsWithImpossibleTypesRatherThanGuessing()
    {
        Dictionary<string, object> badWeight = Doc("JUPITER");
        badWeight["weight"] = "heavy";

        WordData word;
        string reason;
        Assert.IsFalse(FirestoreWordMapper.TryMap(badWeight, out word, out reason, Min, Max));
        StringAssert.Contains("weight", reason);

        Dictionary<string, object> badActive = Doc("JUPITER");
        badActive["isActive"] = "yes";

        Assert.IsFalse(FirestoreWordMapper.TryMap(badActive, out word, out reason, Min, Max));
        StringAssert.Contains("isActive", reason);
    }

    [Test]
    public void TryMap_RejectsAnUnknownCategoryOrDifficultyFromFirestore()
    {
        WordData word;
        string reason;

        Assert.IsFalse(FirestoreWordMapper.TryMap(
            Doc("JUPITER", categoryId: "nonsense"), out word, out reason, Min, Max));
        StringAssert.Contains("category", reason);

        Assert.IsFalse(FirestoreWordMapper.TryMap(
            Doc("JUPITER", difficultyId: "nightmare"), out word, out reason, Min, Max));
        StringAssert.Contains("difficulty", reason);
    }

    /// <summary>
    /// Issue #33, acceptance criterion 5: the bundled offline path and the
    /// Firestore path must produce identical <see cref="WordData"/> for the same
    /// content - the wire keys differ (<c>category</c> vs <c>categoryId</c>) but
    /// the shared normalisation and validation steps must not.
    /// </summary>
    [Test]
    public void BundledAndFirestorePaths_ProduceIdenticalWordDataForTheSameContent()
    {
        var fields = new Dictionary<string, object>
        {
            { "answer", "jupiter" },
            { "clue", "Largest Planet" },
            { "hint", "Starts with J" },
            { "categoryId", "space" },
            { "difficultyId", "medium" },
            { "weight", 2 },
            { "isActive", true },
        };

        WordData fromFirestore;
        string reason;
        Assert.IsTrue(FirestoreWordMapper.TryMap(fields, out fromFirestore, out reason, Min, Max), reason);

        WordPool.ParseResult parsed = WordPool.Parse(
            "{ \"words\": [ { \"clue\": \"Largest Planet\", \"answer\": \"jupiter\"," +
            " \"hint\": \"Starts with J\", \"category\": \"space\", \"difficulty\": \"medium\"," +
            " \"weight\": 2, \"isActive\": true } ] }",
            Min, Max);

        Assert.AreEqual(1, parsed.words.Count, string.Join("\n", parsed.rejected));
        WordData fromBundled = parsed.words[0];

        Assert.AreEqual(fromBundled.Answer, fromFirestore.Answer);
        Assert.AreEqual(fromBundled.Clue, fromFirestore.Clue);
        Assert.AreEqual(fromBundled.Hint, fromFirestore.Hint);
        Assert.AreEqual(fromBundled.CategoryId, fromFirestore.CategoryId);
        Assert.AreEqual(fromBundled.DifficultyId, fromFirestore.DifficultyId);
        Assert.AreEqual(fromBundled.Weight, fromFirestore.Weight);
        Assert.AreEqual(fromBundled.IsActive, fromFirestore.IsActive);
    }
}
