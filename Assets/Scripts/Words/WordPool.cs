using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads and validates the bundled word list - the offline half of issue #33's
/// two read paths (the other is <see cref="FirestoreWordMapper"/>).
///
/// <see cref="Parse"/> never throws and never returns a partially-invalid pool:
/// every entry is normalised through <see cref="WordData.Normalize"/>, then run
/// through <see cref="WordValidator"/>, malformed entries are skipped and
/// reported, and answers are upper-cased so the letter slots and the keyboard
/// agree on case. <see cref="CreateFallback"/> provides a small always-valid pool
/// so an empty or broken content file still leaves the player with a playable
/// board instead of a blank one.
/// </summary>
public static class WordPool
{
    /// <summary>
    /// Shape of the bundled <c>words.json</c> file (issue #33's
    /// <c>BundledWordList</c>). <see cref="schemaVersion"/> is written as 1 by the
    /// shipped file; an absent key reads as 0, the pre-Story-1.4 legacy shape that
    /// must keep loading, and a version newer than <see cref="CurrentSchemaVersion"/>
    /// fails the parse outright - so a future shape change is detected rather than
    /// silently mis-parsed.
    /// </summary>
    [Serializable]
    public sealed class BundledWordList
    {
        /// <summary>Highest file version this build knows how to read.</summary>
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion;
        public List<WordData> words;
    }

    /// <summary>Result of a parse: the valid words plus a reason for every rejection.</summary>
    public sealed class ParseResult
    {
        public readonly List<WordData> words = new List<WordData>();
        public readonly List<string> rejected = new List<string>();

        public bool HasWords => words.Count > 0;
    }

    /// <summary>
    /// Parses and validates a JSON word list. Skips invalid entries, normalizes
    /// accepted answers to upper-case, and records a descriptive reason for each
    /// rejection.
    ///
    /// When <paramref name="difficulty"/> is given, only entries that declare
    /// (or, for legacy entries, infer) that difficulty are considered; the rest
    /// are ignored silently (they are not rejections - they simply belong to
    /// another band). Entries with <c>isActive</c> false or a weight of 0 are
    /// likewise skipped silently, mirroring the Firestore query's selection
    /// semantics. The list is meant to be filtered once per run, never per word.
    ///
    /// Entries predating Story 1.4 (no <c>category</c>/<c>difficulty</c>) are
    /// completed by <see cref="WordData.Normalize"/> - difficulty inferred from
    /// the answer length, category defaulted to <see cref="WordCategory.General"/>
    /// - and the whole file is reported as <b>one</b> aggregated warning naming
    /// <paramref name="sourceName"/>, never one warning per entry.
    /// </summary>
    public static ParseResult Parse(string json, int minLength, int maxLength, string difficulty = null,
        string sourceName = null)
    {
        var result = new ParseResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.rejected.Add("word list is empty");
            return result;
        }

        BundledWordList payload = null;
        try
        {
            payload = JsonUtility.FromJson<BundledWordList>(json);
        }
        catch (Exception e)
        {
            result.rejected.Add("JSON parse error: " + e.Message);
            return result;
        }

        if (payload == null || payload.words == null || payload.words.Count == 0)
        {
            result.rejected.Add("word list is empty or malformed");
            return result;
        }

        if (payload.schemaVersion > BundledWordList.CurrentSchemaVersion)
        {
            result.rejected.Add(
                "unsupported words.json schemaVersion " + payload.schemaVersion +
                " (this build reads up to " + BundledWordList.CurrentSchemaVersion + ")");
            return result;
        }

        string filter = WordDifficulty.Normalize(difficulty);
        var seenAnswers = new HashSet<string>();
        int missingDifficulty = 0;
        int defaultedCategory = 0;
        var inferredBands = new Dictionary<string, int>();

        foreach (WordData raw in payload.words)
        {
            if (raw == null)
            {
                result.rejected.Add("(null entry)");
                continue;
            }

            WordData word = WordData.Normalize(raw);

            if (string.IsNullOrWhiteSpace(raw.difficulty))
            {
                missingDifficulty++;
                int count;
                inferredBands.TryGetValue(word.DifficultyId, out count);
                inferredBands[word.DifficultyId] = count + 1;
            }

            if (string.IsNullOrWhiteSpace(raw.category))
                defaultedCategory++;

            if (filter.Length > 0 && word.DifficultyId != filter)
                continue;

            if (!word.IsActive || word.Weight <= 0)
                continue;

            WordValidationResult check = WordValidator.Validate(word, minLength, maxLength);

            if (check.IsValid && !seenAnswers.Add(word.Answer))
                check = WordValidationResult.Invalid("duplicate answer '" + word.Answer + "'");

            if (!check.IsValid)
            {
                result.rejected.Add("'" + (raw.answer ?? "?") + "' - " + check.Reason);
                continue;
            }

            result.words.Add(word);
        }

        if (missingDifficulty > 0 || defaultedCategory > 0)
        {
            Debug.LogWarning(BuildLegacyMetadataWarning(
                payload.words.Count, missingDifficulty, inferredBands, defaultedCategory, sourceName));
        }

        return result;
    }

    /// <summary>
    /// The single aggregated warning for a file using the pre-Story-1.4 shape:
    /// counts by inferred band plus the defaulted categories, naming the file
    /// once (issue #33, acceptance criterion 4).
    /// </summary>
    private static string BuildLegacyMetadataWarning(int total, int missingDifficulty,
        Dictionary<string, int> inferredBands, int defaultedCategory, string sourceName)
    {
        var bands = new List<string>();
        foreach (string band in WordDifficulty.All)
        {
            int count;
            inferredBands.TryGetValue(band, out count);
            if (count > 0)
                bands.Add(band + "=" + count);
        }

        return
            "WordPool: " + missingDifficulty + "/" + total + " entries have no difficulty (inferred from " +
            "answer length: " + string.Join(", ", bands) + "), " + defaultedCategory + "/" + total +
            " have no category (defaulted to '" + WordCategory.General + "') in " +
            (string.IsNullOrEmpty(sourceName) ? "the word list" : sourceName) +
            ". This is the pre-Story-1.4 shape; add explicit 'category' and 'difficulty' " +
            "fields to silence this warning.";
    }

    /// <summary>
    /// A tiny, always-valid pool used when the authored content is missing or
    /// entirely rejected, so the game degrades to a playable state. Every entry
    /// carries a category and difficulty so the fallback passes the same rules
    /// as authored content.
    /// </summary>
    public static List<WordData> CreateFallback()
    {
        return new List<WordData>
        {
            new WordData("Largest Planet", "JUPITER", "Starts with J and has 7 letters", "space", "medium"),
            new WordData("Natural Force", "GRAVITY", "Starts with G and has 7 letters", "science", "medium"),
            new WordData("Life Science", "BIOLOGY", "Starts with B and has 7 letters", "science", "medium"),
        };
    }
}
