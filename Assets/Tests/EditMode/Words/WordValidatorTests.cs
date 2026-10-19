using System.Collections.Generic;
using NUnit.Framework;

/// <summary>EditMode coverage for the word-content rules (issue #18, part 2).</summary>
public class WordValidatorTests
{
    const int Min = WordValidator.MinAnswerLength;
    const int Max = WordValidator.MaxAnswerLength;

    [Test]
    public void Normalize_TrimsAndUpperCases()
    {
        Assert.AreEqual("JUPITER", WordValidator.Normalize("  jupiter "));
        Assert.AreEqual(string.Empty, WordValidator.Normalize(null));
        Assert.AreEqual(string.Empty, WordValidator.Normalize(""));
    }

    [Test]
    public void IsLetterOnly_RejectsNonAZ()
    {
        Assert.IsTrue(WordValidator.IsLetterOnly("JUPITER"));
        Assert.IsTrue(WordValidator.IsLetterOnly("A"));

        Assert.IsFalse(WordValidator.IsLetterOnly("NEW YORK"));
        Assert.IsFalse(WordValidator.IsLetterOnly("DON'T"));
        Assert.IsFalse(WordValidator.IsLetterOnly("CAFE\u0301")); // accented
        Assert.IsFalse(WordValidator.IsLetterOnly("HI5"));
        Assert.IsFalse(WordValidator.IsLetterOnly(""));
        Assert.IsFalse(WordValidator.IsLetterOnly(null));
    }

    [Test]
    public void Validate_AcceptsAWellFormedEntry()
    {
        WordValidationResult result = WordValidator.Validate("Largest Planet", "JUPITER", "Starts with J", Min, Max);
        Assert.IsTrue(result.IsValid, result.Reason);
    }

    [Test]
    public void Validate_AcceptsLowercaseAnswerAndNormalizes()
    {
        WordValidationResult result = WordValidator.Validate("Largest Planet", "jupiter", "Starts with J", Min, Max);
        Assert.IsTrue(result.IsValid, result.Reason);
    }

    [TestCase("", "JUPITER", "hint", "clue")]
    [TestCase("clue", "", "hint", "answer")]
    [TestCase("clue", "JUPITER", "", "hint")]
    public void Validate_RejectsEmptyFields(string clue, string answer, string hint, string expectedFragment)
    {
        WordValidationResult result = WordValidator.Validate(clue, answer, hint, Min, Max);
        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(expectedFragment, result.Reason);
    }

    [Test]
    public void Validate_RejectsNonLetterAnswer()
    {
        WordValidationResult result = WordValidator.Validate("City", "NEW YORK", "Big Apple", Min, Max);
        Assert.IsFalse(result.IsValid);
        StringAssert.Contains("outside A-Z", result.Reason);
    }

    [Test]
    public void Validate_RejectsTooShortAndTooLongAnswers()
    {
        WordValidationResult tooShort = WordValidator.Validate("clue", "AT", "hint", Min, Max);
        StringAssert.Contains("shorter", tooShort.Reason);

        WordValidationResult tooLong = WordValidator.Validate("clue", new string('A', Max + 1), "hint", Min, Max);
        StringAssert.Contains("longer", tooLong.Reason);
    }

    [Test]
    public void Validate_RejectsHintThatDuplicatesClue()
    {
        WordValidationResult result = WordValidator.Validate("Gravity", "GRAVITY", "gravity", Min, Max);
        Assert.IsFalse(result.IsValid);
        StringAssert.Contains("duplicates the clue", result.Reason);
    }

    [Test]
    public void FindDuplicateAnswers_ReturnsRepeatsOnceInFirstSeenOrder()
    {
        List<string> duplicates = WordValidator.FindDuplicateAnswers(
            new[] { "JUPITER", "gravity", "GRAVITY", "BIOLOGY", "jupiter", "" });

        CollectionAssert.AreEqual(new[] { "GRAVITY", "JUPITER" }, duplicates);
    }

    // --- Model-level entry points (Story 2.4, issue #33) -------------------

    /// <summary>
    /// The <see cref="WordData"/> overloads must be thin delegations to the string
    /// rules above: one implementation, reachable from the bundled loader, the
    /// editor tool and the Firestore mapper alike.
    /// </summary>
    [Test]
    public void ValidateModel_DelegatesToTheStringRules()
    {
        var valid = new WordData("Largest Planet", "JUPITER", "Starts with J", "space", "medium");
        WordValidationResult ok = WordValidator.Validate(valid, Min, Max);
        Assert.IsTrue(ok.IsValid, ok.Reason);

        string reason;
        var invalid = new WordData("City", "NEW YORK", "Big Apple", "geography", "hard");
        Assert.IsFalse(WordValidator.IsValid(invalid, out reason));
        StringAssert.Contains("outside A-Z", reason);

        Assert.IsFalse(WordValidator.IsValid(null, out reason));
        StringAssert.Contains("null", reason);
    }

    // --- Story 1.4: category, difficulty and copy hygiene ------------------

    [Test]
    public void ValidateFull_AcceptsAWellFormedEntry()
    {
        WordValidationResult result = WordValidator.Validate(
            "Largest Planet", "JUPITER", "Starts with J", "space", "medium", Min, Max);
        Assert.IsTrue(result.IsValid, result.Reason);
    }

    [Test]
    public void ValidateFull_RequiresKnownCategoryAndDifficulty()
    {
        StringAssert.Contains("category", WordValidator.Validate(
            "Largest Planet", "JUPITER", "Starts with J", "", "medium", Min, Max).Reason);
        StringAssert.Contains("category", WordValidator.Validate(
            "Largest Planet", "JUPITER", "Starts with J", "nonsense", "medium", Min, Max).Reason);
        StringAssert.Contains("difficulty", WordValidator.Validate(
            "Largest Planet", "JUPITER", "Starts with J", "space", "", Min, Max).Reason);
        StringAssert.Contains("difficulty", WordValidator.Validate(
            "Largest Planet", "JUPITER", "Starts with J", "space", "nightmare", Min, Max).Reason);
    }

    [Test]
    public void ValidateFull_RejectsAnswerOutsideItsDeclaredBand()
    {
        WordValidationResult result = WordValidator.Validate(
            "Largest Planet", "JUPITER", "Starts with J", "space", "hard", Min, Max);

        Assert.IsFalse(result.IsValid);
        StringAssert.Contains("outside the hard band", result.Reason);
    }

    [Test]
    public void ValidateFull_RejectsClueOrHintContainingTheAnswer()
    {
        WordValidationResult clue = WordValidator.Validate(
            "Largest Planet JUPITER", "JUPITER", "Starts with J", "space", "medium", Min, Max);
        Assert.IsFalse(clue.IsValid);
        StringAssert.Contains("giveaway", clue.Reason);

        WordValidationResult hint = WordValidator.Validate(
            "Largest Planet", "JUPITER", "JUPITER is big", "space", "medium", Min, Max);
        Assert.IsFalse(hint.IsValid);
        StringAssert.Contains("giveaway", hint.Reason);
    }
}
