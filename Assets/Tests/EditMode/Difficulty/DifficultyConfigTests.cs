using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode coverage for <see cref="DifficultyConfig"/> predicates (Story 1.1).
/// </summary>
public class DifficultyConfigTests
{
    [Test]
    public void IsWordLengthValid_RespectsTheBand()
    {
        DifficultyConfig config = ScriptableObject.CreateInstance<DifficultyConfig>();
        config.minWordLength = 6;
        config.maxWordLength = 8;

        Assert.IsFalse(config.IsWordLengthValid(5));
        Assert.IsTrue(config.IsWordLengthValid(6));
        Assert.IsTrue(config.IsWordLengthValid(8));
        Assert.IsFalse(config.IsWordLengthValid(9));

        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void ShouldShowHint_RespectsEachPolicy()
    {
        DifficultyConfig config = ScriptableObject.CreateInstance<DifficultyConfig>();

        config.hintPolicy = HintPolicy.FreeImmediate;
        Assert.IsTrue(config.ShouldShowHint(0), "FreeImmediate should always show.");

        config.hintPolicy = HintPolicy.FreeAfterWrongGuesses;
        config.hintThresholdWrongGuesses = 2;
        Assert.IsFalse(config.ShouldShowHint(1));
        Assert.IsTrue(config.ShouldShowHint(2));

        config.hintPolicy = HintPolicy.Penalised;
        config.hintThresholdWrongGuesses = 2;
        Assert.IsFalse(config.ShouldShowHint(1));
        Assert.IsTrue(config.ShouldShowHint(3), "Penalised still shows once the threshold is met.");

        config.hintPolicy = HintPolicy.Disabled;
        Assert.IsFalse(config.ShouldShowHint(99));

        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void ShouldShowHint_IsReachableBeforeTheWordFails()
    {
        // Hard's shape: two lives and a two-miss threshold. The second miss
        // fails the word and the gameplay loop stops evaluating the hint then,
        // so the hint must arrive on the first miss instead of never.
        DifficultyConfig config = ScriptableObject.CreateInstance<DifficultyConfig>();
        config.livesPerWord = 2;
        config.hintPolicy = HintPolicy.Penalised;
        config.hintThresholdWrongGuesses = 2;

        Assert.IsFalse(config.ShouldShowHint(0));
        Assert.IsTrue(config.ShouldShowHint(1),
            "The hint must be reachable while a life is still left.");

        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void AuthoredAssets_MatchTheirDifficultyId()
    {
        DifficultyLibrary library = Resources.Load<DifficultyLibrary>("Difficulty/DifficultyLibrary");
        Assert.IsNotNull(library);

        foreach (DifficultyId id in System.Enum.GetValues(typeof(DifficultyId)))
        {
            DifficultyConfig config = library.Get(id);
            Assert.IsNotNull(config, $"No config authored for {id}.");
            Assert.AreEqual(id, config.id, $"Config for {id} has a mismatched id.");
        }
    }
}
