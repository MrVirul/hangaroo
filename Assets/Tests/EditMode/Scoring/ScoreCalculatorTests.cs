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

    [Test]
    public void GuessPoints_AppliesTheDifficultyMultiplier()
    {
        // base x multiplier, rounded away from zero per guess.
        Assert.AreEqual(30, ScoreCalculator.GuessPoints(20, 1.5f), "Medium: T (20) x 1.5");
        Assert.AreEqual(50, ScoreCalculator.GuessPoints(20, 2.5f), "Hard: T (20) x 2.5");
        Assert.AreEqual(26, ScoreCalculator.GuessPoints(17, 1.5f), "25.5 rounds away from zero");
        Assert.AreEqual(20, ScoreCalculator.GuessPoints(20, 1f), "Easy: multiplier 1");
    }

    [Test]
    public void GuessPoints_IsZeroWhenBaseIsZero()
    {
        Assert.AreEqual(0, ScoreCalculator.GuessPoints(0, 2.5f));
    }

    [Test]
    public void GuessPoints_TreatsNonPositiveMultiplierAsOne()
    {
        Assert.AreEqual(20, ScoreCalculator.GuessPoints(20, 0f));
        Assert.AreEqual(20, ScoreCalculator.GuessPoints(20, -3f));
    }

    [Test]
    public void MultipliedWord_IsStillOrderIndependent()
    {
        string word = "JUPITER";
        float multiplier = 1.5f;

        int forward = 0;
        foreach (char c in word)
        {
            forward += ScoreCalculator.GuessPoints(ScoreCalculator.BasePoints(c, 1), multiplier);
        }

        int reverse = 0;
        for (int i = word.Length - 1; i >= 0; i--)
        {
            reverse += ScoreCalculator.GuessPoints(ScoreCalculator.BasePoints(word[i], 1), multiplier);
        }

        Assert.AreEqual(forward, reverse, "Applying a constant multiplier must not introduce order dependence.");
    }

    [Test]
    public void HintPenalty_IsAFractionOfTheWordPoints()
    {
        Assert.AreEqual(25, ScoreCalculator.HintPenalty(100, 0.25f), "Hard: 25% of 100");
        Assert.AreEqual(0, ScoreCalculator.HintPenalty(0, 0.25f), "Nothing earned yet, nothing to penalise");
        Assert.AreEqual(0, ScoreCalculator.HintPenalty(100, 0f), "Easy/Medium apply no hint penalty");
        Assert.AreEqual(10, ScoreCalculator.HintPenalty(40, 0.25f));
    }

    [Test]
    public void HintPenalty_ClampsTheFractionToOne()
    {
        Assert.AreEqual(50, ScoreCalculator.HintPenalty(50, 2f));
    }
}
