using UnityEngine;

/// <summary>
/// Local personal-best storage backed by PlayerPrefs. Deliberately the only
/// place a best is persisted until the cloud-save epic lifts it out.
/// </summary>
public class PlayerPrefsBestScoreStore : IBestScoreStore
{
    public const string BestScoreKey = "Run.BestScore";
    public const string BestStreakKey = "Run.BestStreak";
    public const string LastPlayedKey = "Run.LastPlayedAtUnixMs";

    public PersonalBest Load()
    {
        return new PersonalBest
        {
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0),
            bestStreak = PlayerPrefs.GetInt(BestStreakKey, 0),
            lastPlayedAtUnixMs = ParseLong(PlayerPrefs.GetString(LastPlayedKey, "0")),
        };
    }

    public void Save(PersonalBest best)
    {
        if (best == null)
        {
            return;
        }

        PlayerPrefs.SetInt(BestScoreKey, best.bestScore);
        PlayerPrefs.SetInt(BestStreakKey, best.bestStreak);
        PlayerPrefs.SetString(LastPlayedKey, best.lastPlayedAtUnixMs.ToString());
        PlayerPrefs.Save();
    }

    private static long ParseLong(string value)
    {
        long parsed;
        return long.TryParse(value, out parsed) ? parsed : 0L;
    }
}
