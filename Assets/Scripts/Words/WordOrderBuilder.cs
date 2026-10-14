using System.Collections.Generic;

/// <summary>
/// Builds the randomized order in which words are played.
///
/// Pulled out of <c>GameManager</c> so the shuffle is a pure function of an
/// injected <see cref="System.Random"/> and can be asserted on deterministically
/// in tests. The shuffle is an unbiased Fisher-Yates (the upper bound of
/// <see cref="System.Random.Next(int, int)"/> is exclusive), so every permutation
/// is equally likely.
/// </summary>
public static class WordOrderBuilder
{
    /// <summary>Number of times a new run tries to avoid repeating the previous order.</summary>
    public const int DifferentOrderAttempts = 16;

    /// <summary>Returns a shuffled list of the indices <c>0..count-1</c>.</summary>
    public static List<int> Shuffle(int count, System.Random rng)
    {
        var order = new List<int>(count);

        for (int i = 0; i < count; i++)
            order.Add(i);

        for (int i = 0; i < count; i++)
        {
            int swap = rng.Next(i, count);
            int temp = order[i];
            order[i] = order[swap];
            order[swap] = temp;
        }

        return order;
    }

    /// <summary>
    /// Shuffles like <see cref="Shuffle"/>, but re-rolls (up to
    /// <see cref="DifferentOrderAttempts"/> times) while the result is identical
    /// to <paramref name="previous"/>, so a new playthrough never replays the
    /// exact order of the one before it. Degenerate sizes (null, mismatched
    /// length, or a single word) are returned as-is.
    /// </summary>
    public static List<int> ShuffleDifferentFrom(int count, IList<int> previous, System.Random rng)
    {
        List<int> order = Shuffle(count, rng);

        if (count <= 1 || previous == null || previous.Count != count)
            return order;

        for (int attempt = 1; attempt < DifferentOrderAttempts && SameOrder(order, previous); attempt++)
            order = Shuffle(count, rng);

        return order;
    }

    /// <summary>True when both sequences contain the same indices in the same positions.</summary>
    public static bool SameOrder(IList<int> a, IList<int> b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a == null || b == null || a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }
}
