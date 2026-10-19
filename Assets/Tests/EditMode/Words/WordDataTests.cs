using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode coverage for the <see cref="WordData"/> content model itself
/// (Story 2.4, issue #33): the PascalCase member surface, the shared
/// normalisation step, and the promise that the type stays plain
/// JsonUtility-serialisable with no Firebase involvement.
/// </summary>
public class WordDataTests
{
    [Test]
    public void Members_AreExposedOverTheWireFields()
    {
        var word = new WordData("Largest Planet", "JUPITER", "Starts with J", "space", "medium", 3, false);

        Assert.AreEqual("Largest Planet", word.Clue);
        Assert.AreEqual("JUPITER", word.Answer);
        Assert.AreEqual("Starts with J", word.Hint);
        Assert.AreEqual("space", word.CategoryId);
        Assert.AreEqual("medium", word.DifficultyId);
        Assert.AreEqual(3, word.Weight);
        Assert.IsFalse(word.IsActive);
        Assert.AreEqual(7, word.Length);
    }

    [Test]
    public void Length_IsZeroWhenNoAnswerIsSet()
    {
        Assert.AreEqual(0, new WordData("clue", null, "hint").Length);
    }

    // --- The shared normalisation step ------------------------------------

    [Test]
    public void Normalize_InfersDifficultyFromAnswerLengthAndDefaultsCategory()
    {
        WordData easy = WordData.Normalize(new WordData("Clue", "TIGER", "Hint"));
        Assert.AreEqual(WordDifficulty.Easy, easy.DifficultyId);
        Assert.AreEqual(WordCategory.General, easy.CategoryId);

        WordData medium = WordData.Normalize(new WordData("Clue", "JUPITER", "Hint"));
        Assert.AreEqual(WordDifficulty.Medium, medium.DifficultyId);

        WordData hard = WordData.Normalize(new WordData("Clue", "HIPPOPOTAMUS", "Hint"));
        Assert.AreEqual(WordDifficulty.Hard, hard.DifficultyId);
    }

    [Test]
    public void Normalize_KeepsDeclaredMetadataAndNormalizesItsCase()
    {
        WordData word = WordData.Normalize(new WordData("Clue", "TIGER", "Hint", " Animals ", " EASY "));

        Assert.AreEqual("animals", word.CategoryId);
        Assert.AreEqual("easy", word.DifficultyId);
    }

    [Test]
    public void Normalize_LeavesDeclaredButUnknownValuesForTheValidatorToReject()
    {
        WordData word = WordData.Normalize(new WordData("Clue", "TIGER", "Hint", "animals", "nightmare"));

        Assert.AreEqual("nightmare", word.DifficultyId, "only absent metadata is completed, never corrected");

        string reason;
        Assert.IsFalse(WordValidator.IsValid(word, out reason));
        StringAssert.Contains("difficulty", reason);
    }

    [Test]
    public void Normalize_UpperCasesTheAnswerButLeavesClueAndHintIntact()
    {
        WordData word = WordData.Normalize(new WordData("  Largest  Planet ", "  jupiter ", "  Starts with J  "));

        Assert.AreEqual("JUPITER", word.Answer);
        Assert.AreEqual("  Largest  Planet ", word.Clue);
        Assert.AreEqual("  Starts with J  ", word.Hint);
    }

    [Test]
    public void Normalize_OnANullRawEntryReturnsNullRatherThanThrowing()
    {
        Assert.IsNull(WordData.Normalize(null));
    }

    // --- JsonUtility-serialisability (issue #33, acceptance criterion 2) ---

    /// <summary>
    /// JsonUtility binds keys by exact case, so the wire fields are lower-case and
    /// must round-trip the words.json shape directly - proof the bundled path needs
    /// no Firebase (or any other) dependency to work.
    /// </summary>
    [Test]
    public void StaysJsonUtilitySerializable_InTheBundledWireShape()
    {
        const string json =
            "{\"clue\":\"Largest Planet\",\"answer\":\"jupiter\",\"hint\":\"Starts with J\"," +
            "\"category\":\"space\",\"difficulty\":\"medium\",\"weight\":2,\"isActive\":false}";

        WordData word = JsonUtility.FromJson<WordData>(json);

        Assert.AreEqual("jupiter", word.answer);
        Assert.AreEqual("space", word.category);
        Assert.AreEqual("medium", word.difficulty);
        Assert.AreEqual(2, word.weight);
        Assert.IsFalse(word.isActive);

        WordData roundTripped = JsonUtility.FromJson<WordData>(JsonUtility.ToJson(word));

        Assert.AreEqual(word.clue, roundTripped.clue);
        Assert.AreEqual(word.answer, roundTripped.answer);
        Assert.AreEqual(word.category, roundTripped.category);
        Assert.AreEqual(word.difficulty, roundTripped.difficulty);
        Assert.AreEqual(word.weight, roundTripped.weight);
        Assert.AreEqual(word.isActive, roundTripped.isActive);
    }

    /// <summary>
    /// Entries without the optional keys (every pre-weight/pre-active file) must
    /// come back enabled at the default weight, not zero/false - the defaults live
    /// in field initialisers for exactly this reason.
    /// </summary>
    [Test]
    public void AbsentOptionalWireKeys_DefaultToWeightOneAndActive()
    {
        WordData word = JsonUtility.FromJson<WordData>(
            "{\"clue\":\"Largest Planet\",\"answer\":\"JUPITER\",\"hint\":\"Starts with J\"}");

        Assert.AreEqual(1, word.Weight);
        Assert.IsTrue(word.IsActive);
    }
}
