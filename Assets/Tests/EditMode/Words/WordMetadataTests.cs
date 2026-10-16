using NUnit.Framework;

/// <summary>EditMode coverage for the difficulty bands and category set (Story 1.4).</summary>
public class WordMetadataTests
{
    [Test]
    public void Difficulty_BandsMirrorTheDifficultyConfigs()
    {
        Assert.AreEqual(4, WordDifficulty.MinLength(WordDifficulty.Easy));
        Assert.AreEqual(6, WordDifficulty.MaxLength(WordDifficulty.Easy));

        Assert.AreEqual(6, WordDifficulty.MinLength(WordDifficulty.Medium));
        Assert.AreEqual(8, WordDifficulty.MaxLength(WordDifficulty.Medium));

        Assert.AreEqual(8, WordDifficulty.MinLength(WordDifficulty.Hard));
        Assert.AreEqual(12, WordDifficulty.MaxLength(WordDifficulty.Hard));
    }

    [Test]
    public void Difficulty_IsInBandTreatsBoundsAsInclusive()
    {
        Assert.IsTrue(WordDifficulty.IsInBand(WordDifficulty.Easy, 4));
        Assert.IsTrue(WordDifficulty.IsInBand(WordDifficulty.Easy, 6));
        Assert.IsFalse(WordDifficulty.IsInBand(WordDifficulty.Easy, 7));

        Assert.IsTrue(WordDifficulty.IsInBand(WordDifficulty.Hard, 8));
        Assert.IsTrue(WordDifficulty.IsInBand(WordDifficulty.Hard, 12));
        Assert.IsFalse(WordDifficulty.IsInBand(WordDifficulty.Hard, 13));
    }

    [Test]
    public void Difficulty_RejectsUnknownBands()
    {
        Assert.IsFalse(WordDifficulty.IsKnown("nightmare"));
        Assert.IsFalse(WordDifficulty.IsKnown(""));
        Assert.IsFalse(WordDifficulty.IsKnown(null));
        Assert.IsFalse(WordDifficulty.IsInBand("nightmare", 7));
    }

    [Test]
    public void Difficulty_IsCaseAndWhitespaceInsensitive()
    {
        Assert.AreEqual(WordDifficulty.Medium, WordDifficulty.Normalize("  Medium "));
        Assert.IsTrue(WordDifficulty.IsKnown("HARD"));
    }

    [Test]
    public void Category_KnowsTheDocumentedSetAndRejectsOthers()
    {
        foreach (string category in WordCategory.All)
            Assert.IsTrue(WordCategory.IsKnown(category), category);

        Assert.IsFalse(WordCategory.IsKnown("nonsense"));
        Assert.IsFalse(WordCategory.IsKnown(""));
        Assert.IsFalse(WordCategory.IsKnown(null));
    }

    [Test]
    public void Category_IsCaseAndWhitespaceInsensitive()
    {
        Assert.AreEqual("animals", WordCategory.Normalize("  Animals "));
        Assert.IsTrue(WordCategory.IsKnown("SPACE"));
    }
}
