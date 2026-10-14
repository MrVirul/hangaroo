using NUnit.Framework;

/// <summary>
/// EditMode coverage for the corrected scoring formula (issue #11).
/// </summary>
public class ScoreCalculatorTests
{
    [Test]
    public void LetterValue_IsAlphabeticalAndCaseInsensitive()
    {
        Assert.AreEqual(1, ScoreCalculator.LetterValue('A'));
        Assert.AreEqual(1, ScoreCalculator.LetterValue('a'));
        Assert.AreEqual(26, ScoreCalculator.LetterValue('Z'));
        Assert.AreEqual(16, ScoreCalculator.LetterValue('P'));
    }

    [Test]
    public void BasePoints_MultipliesLetterValueBySlotsRevealedThisGuess()
    {
        Assert.AreEqual(20, ScoreCalculator.BasePoints('T', 1));
        Assert.AreEqual(16, ScoreCalculator.BasePoints('H', 2), "HEDGEHOG: a double-H press must pay 8 x 2.");
        Assert.AreEqual(10, ScoreCalculator.BasePoints('E', 2), "HEDGEHOG: a double-E press must pay 5 x 2.");
    }

    [Test]
    public void BasePoints_IsZeroWhenNothingWasRevealed()
    {
        Assert.AreEqual(0, ScoreCalculator.BasePoints('Q', 0));
        Assert.AreEqual(0, ScoreCalculator.BasePoints('Q', -3));
    }

    [Test]
    public void Jupiter_IsOrderIndependentAndTotals99()
    {
        // J=10, U=21, P=16, I=9, T=20, E=5, R=18 - every letter unique, so no
        // guess reveals more than one slot and order cannot affect the total.
        string word = "JUPITER";
        int forward = 0;
        foreach (char c in word)
        {
            forward += ScoreCalculator.BasePoints(c, 1);
        }

        int reverse = 0;
        for (int i = word.Length - 1; i >= 0; i--)
        {
            reverse += ScoreCalculator.BasePoints(word[i], 1);
        }

        Assert.AreEqual(99, forward, "JUPITER must total 99 before difficulty/hint multipliers.");
        Assert.AreEqual(forward, reverse, "Score contribution must be independent of guess order.");
    }
}
