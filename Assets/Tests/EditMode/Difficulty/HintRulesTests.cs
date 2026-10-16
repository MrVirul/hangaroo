using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode coverage for the combined hint decision (Story 1.3): the player's
/// Hints preference gates every difficulty's <see cref="HintPolicy"/>.
/// </summary>
public class HintRulesTests
{
    static DifficultyConfig Config(HintPolicy policy, int threshold)
    {
        DifficultyConfig config = ScriptableObject.CreateInstance<DifficultyConfig>();
        config.hintPolicy = policy;
        config.hintThresholdWrongGuesses = threshold;
        return config;
    }

    [Test]
    public void PlayerPreferenceOff_HidesTheHintOnEveryPolicy()
    {
        foreach (HintPolicy policy in System.Enum.GetValues(typeof(HintPolicy)))
        {
            DifficultyConfig config = Config(policy, 0);
            Assert.IsFalse(
                HintRules.ShouldShow(config, playerHintsEnabled: false, wrongGuessesThisWord: 5),
                $"Hints off must hide the hint for policy {policy}.");

            Object.DestroyImmediate(config);
        }
    }

    [Test]
    public void FreeImmediate_ShowsAsSoonAsTheWordLoads()
    {
        DifficultyConfig config = Config(HintPolicy.FreeImmediate, 0);
        Assert.IsTrue(HintRules.ShouldShow(config, true, 0));
        Object.DestroyImmediate(config);
    }

    [Test]
    public void FreeAfterWrongGuesses_OnlyShowsAtTheThreshold()
    {
        DifficultyConfig config = Config(HintPolicy.FreeAfterWrongGuesses, 2);

        Assert.IsFalse(HintRules.ShouldShow(config, true, 0));
        Assert.IsFalse(HintRules.ShouldShow(config, true, 1));
        Assert.IsTrue(HintRules.ShouldShow(config, true, 2));
        Assert.IsTrue(HintRules.ShouldShow(config, true, 3));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void Penalised_IsShownAtTheThresholdSoTheCostCanApply()
    {
        DifficultyConfig config = Config(HintPolicy.Penalised, 2);

        Assert.IsFalse(HintRules.ShouldShow(config, true, 1));
        Assert.IsTrue(HintRules.ShouldShow(config, true, 2));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void Disabled_IsHiddenEvenWithHintsOnAndManyMisses()
    {
        DifficultyConfig config = Config(HintPolicy.Disabled, 0);
        Assert.IsFalse(HintRules.ShouldShow(config, true, 99));
        Object.DestroyImmediate(config);
    }

    [Test]
    public void MissingConfig_FallsBackToThePlayersPreference()
    {
        Assert.IsTrue(HintRules.ShouldShow(null, playerHintsEnabled: true, wrongGuessesThisWord: 0));
        Assert.IsFalse(HintRules.ShouldShow(null, playerHintsEnabled: false, wrongGuessesThisWord: 0));
    }
}
