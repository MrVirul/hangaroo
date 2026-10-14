using NUnit.Framework;
using UnityEngine;

/// <summary>EditMode coverage for the local personal best (issue #19).</summary>
public class PersonalBestTests
{
    static RunSummary Summary(int score, int bestStreak, long finishedAt)
    {
        return new RunSummary
        {
            score = score,
            bestStreak = bestStreak,
            finishedAtUnixMs = finishedAt,
        };
    }

    [Test]
    public void Record_FirstRun_SetsTheBest()
    {
        var best = new PersonalBest();

        bool isNewBest = best.Record(Summary(120, 3, 5000));

        Assert.IsTrue(isNewBest);
        Assert.AreEqual(120, best.bestScore);
        Assert.AreEqual(3, best.bestStreak);
        Assert.AreEqual(5000L, best.lastPlayedAtUnixMs);
    }

    [Test]
    public void Record_LowerScore_KeepsTheExistingBest()
    {
        var best = new PersonalBest { bestScore = 200, bestStreak = 5 };

        bool isNewBest = best.Record(Summary(100, 2, 6000));

        Assert.IsFalse(isNewBest);
        Assert.AreEqual(200, best.bestScore);
        Assert.AreEqual(5, best.bestStreak);
        Assert.AreEqual(6000L, best.lastPlayedAtUnixMs);
    }

    [Test]
    public void Record_HigherScore_ReportsANewBest()
    {
        var best = new PersonalBest { bestScore = 200, bestStreak = 5 };

        bool isNewBest = best.Record(Summary(250, 1, 7000));

        Assert.IsTrue(isNewBest);
        Assert.AreEqual(250, best.bestScore);
        Assert.AreEqual(5, best.bestStreak);
    }

    [Test]
    public void Record_NullSummary_IsIgnored()
    {
        var best = new PersonalBest { bestScore = 10 };

        Assert.IsFalse(best.Record(null));
        Assert.AreEqual(10, best.bestScore);
    }

    [Test]
    public void PlayerPrefsStore_RoundTrips()
    {
        IBestScoreStore store = new PlayerPrefsBestScoreStore();

        store.Save(new PersonalBest
        {
            bestScore = 321,
            bestStreak = 7,
            lastPlayedAtUnixMs = 9999999999999L,
        });

        PersonalBest loaded = store.Load();

        Assert.AreEqual(321, loaded.bestScore);
        Assert.AreEqual(7, loaded.bestStreak);
        Assert.AreEqual(9999999999999L, loaded.lastPlayedAtUnixMs);

        PlayerPrefs.DeleteKey(PlayerPrefsBestScoreStore.BestScoreKey);
        PlayerPrefs.DeleteKey(PlayerPrefsBestScoreStore.BestStreakKey);
        PlayerPrefs.DeleteKey(PlayerPrefsBestScoreStore.LastPlayedKey);
        PlayerPrefs.Save();
    }
}
