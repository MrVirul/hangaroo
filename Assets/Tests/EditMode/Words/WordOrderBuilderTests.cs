using System.Collections.Generic;
using NUnit.Framework;

/// <summary>EditMode coverage for the run shuffle (issue #18, part 1).</summary>
public class WordOrderBuilderTests
{
    [Test]
    public void Shuffle_ReturnsAPermutationOfEveryIndex()
    {
        List<int> order = WordOrderBuilder.Shuffle(10, new System.Random(1234));

        Assert.AreEqual(10, order.Count);

        var sorted = new List<int>(order);
        sorted.Sort();

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, sorted);
    }

    [Test]
    public void Shuffle_IsDeterministicForASeed()
    {
        List<int> first = WordOrderBuilder.Shuffle(20, new System.Random(42));
        List<int> second = WordOrderBuilder.Shuffle(20, new System.Random(42));

        CollectionAssert.AreEqual(first, second);
    }

    [Test]
    public void ShuffleDifferentFrom_NeverReplaysThePreviousOrder()
    {
        var rng = new System.Random(7);
        const int count = 65;

        List<int> firstRun = WordOrderBuilder.Shuffle(count, rng);
        List<int> secondRun = WordOrderBuilder.ShuffleDifferentFrom(count, firstRun, rng);

        Assert.AreEqual(count, secondRun.Count);
        Assert.IsFalse(WordOrderBuilder.SameOrder(firstRun, secondRun),
            "A second playthrough must not repeat the first playthrough's order.");
    }

    [Test]
    public void ShuffleDifferentFrom_HandlesDegenerateSizes()
    {
        var rng = new System.Random(1);

        Assert.AreEqual(0, WordOrderBuilder.ShuffleDifferentFrom(0, new List<int>(), rng).Count);
        CollectionAssert.AreEqual(new[] { 0 }, WordOrderBuilder.ShuffleDifferentFrom(1, new List<int> { 0 }, rng));

        // Null / mismatched previous order can't be compared, so a plain shuffle is returned.
        Assert.AreEqual(5, WordOrderBuilder.ShuffleDifferentFrom(5, null, rng).Count);
        Assert.AreEqual(5, WordOrderBuilder.ShuffleDifferentFrom(5, new List<int> { 0, 1 }, rng).Count);
    }

    [Test]
    public void SameOrder_ComparesPositionally()
    {
        Assert.IsTrue(WordOrderBuilder.SameOrder(new List<int> { 1, 2, 3 }, new List<int> { 1, 2, 3 }));
        Assert.IsFalse(WordOrderBuilder.SameOrder(new List<int> { 1, 2, 3 }, new List<int> { 3, 2, 1 }));
        Assert.IsFalse(WordOrderBuilder.SameOrder(new List<int> { 1, 2 }, new List<int> { 1, 2, 3 }));
        Assert.IsFalse(WordOrderBuilder.SameOrder(null, new List<int> { 1 }));
        Assert.IsTrue(WordOrderBuilder.SameOrder(null, null));
    }
}
