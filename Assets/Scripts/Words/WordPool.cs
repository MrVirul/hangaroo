using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads and validates the authored word list.
///
/// <see cref="Parse"/> never throws and never returns a partially-invalid pool:
/// every entry is run through <see cref="WordValidator"/>, malformed entries are
/// skipped and reported, and answers are normalized to upper-case so the letter
/// slots and the keyboard agree on case. <see cref="CreateFallback"/> provides a
/// small always-valid pool so an empty or broken content file still leaves the
/// player with a playable board instead of a blank one.
/// </summary>
public static class WordPool
{
    /// <summary>Shape of <c>words.json</c> / a Firestore word document.</summary>
    [Serializable]
    public sealed class RawWordList
    {
        public List<WordEntry> words;
    }

    /// <summary>Result of a parse: the valid words plus a reason for every rejection.</summary>
    public sealed class ParseResult
    {
        public readonly List<WordEntry> words = new List<WordEntry>();
        public readonly List<string> rejected = new List<string>();

        public bool HasWords => words.Count > 0;
    }

    /// <summary>
    /// Parses and validates a JSON word list. Skips invalid entries, normalizes
    /// accepted answers to upper-case, and records a descriptive reason for each
    /// rejection.
    ///
    /// When <paramref name="difficulty"/> is given, only entries that declare
    /// that difficulty are considered; the rest are ignored silently (they are
    /// not rejections - they simply belong to another band). The list is meant
    /// to be filtered once per run, never per word.
    /// </summary>
    public static ParseResult Parse(string json, int minLength, int maxLength, string difficulty = null)
    {
        var result = new ParseResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.rejected.Add("word list is empty");
            return result;
        }

        RawWordList payload = null;
        try
        {
            payload = JsonUtility.FromJson<RawWordList>(json);
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

        string filter = WordDifficulty.Normalize(difficulty);
        var seenAnswers = new HashSet<string>();

        foreach (WordEntry entry in payload.words)
        {
            if (entry == null)
            {
                result.rejected.Add("(null entry)");
                continue;
            }

            if (filter.Length > 0 && WordDifficulty.Normalize(entry.difficulty) != filter)
                continue;

            WordValidationResult check = WordValidator.Validate(
                entry.clue,
                entry.answer,
                entry.hint,
                entry.category,
                entry.difficulty,
                minLength,
                maxLength);

            if (check.IsValid)
            {
                string key = WordValidator.Normalize(entry.answer);
                if (!seenAnswers.Add(key))
                    check = WordValidationResult.Invalid("duplicate answer '" + key + "'");
            }

            if (!check.IsValid)
            {
                result.rejected.Add("'" + (entry.answer ?? "?") + "' - " + check.Reason);
                continue;
            }

            result.words.Add(new WordEntry(
                entry.clue,
                WordValidator.Normalize(entry.answer),
                entry.hint,
                WordCategory.Normalize(entry.category),
                WordDifficulty.Normalize(entry.difficulty)));
        }

        return result;
    }

    /// <summary>
    /// A tiny, always-valid pool used when the authored content is missing or
    /// entirely rejected, so the game degrades to a playable state. Every entry
    /// carries a category and difficulty so the fallback passes the same rules
    /// as authored content.
    /// </summary>
    public static List<WordEntry> CreateFallback()
    {
        return new List<WordEntry>
        {
            new WordEntry("Largest Planet", "JUPITER", "Starts with J and has 7 letters", "space", "medium"),
            new WordEntry("Natural Force", "GRAVITY", "Starts with G and has 7 letters", "science", "medium"),
            new WordEntry("Life Science", "BIOLOGY", "Starts with B and has 7 letters", "science", "medium"),
        };
    }
}
