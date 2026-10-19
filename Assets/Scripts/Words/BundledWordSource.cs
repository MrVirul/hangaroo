using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The bundled tier of the word read path (Story 2.3, issue #32): the
/// <c>words.json</c> <c>TextAsset</c> that ships in the build, parsed with the
/// same <see cref="WordPool.Parse"/> rules the editor tooling uses, so the
/// fallback is validator-passing by construction (issue #32, acceptance
/// criterion 5 - the build-time validator enforces the file itself).
///
/// The band is filtered here, mirroring how the game resolved the pool before
/// the repository existed: a band with no authored words degrades to the full
/// pool (with one warning naming the file) rather than a blank board.
/// </summary>
public sealed class BundledWordSource
{
    private readonly string json;
    private readonly string sourceName;

    public BundledWordSource(string json, string sourceName)
    {
        this.json = json ?? string.Empty;
        this.sourceName = string.IsNullOrEmpty(sourceName) ? "words.json" : sourceName;
    }

    /// <summary>The file this source reads; surfaced so the repository's messages can name it.</summary>
    public string SourceName => sourceName;

    /// <summary>
    /// Parses the bundled list and returns the words for one difficulty band.
    /// Returns an empty list only when the file itself is unusable (garbage,
    /// unsupported schema, every entry rejected) - the repository then
    /// substitutes the built-in fallback pool. Malformed entries are reported
    /// as one aggregated error naming the file, never one message per word.
    /// </summary>
    public List<WordData> Load(string difficultyId, int minLength, int maxLength)
    {
        WordPool.ParseResult parsed = WordPool.Parse(json, minLength, maxLength, null, sourceName);

        if (parsed.rejected.Count > 0)
        {
            string noun = parsed.rejected.Count == 1 ? "entry" : "entries";
            Debug.LogError(
                "BundledWordSource: rejected " + parsed.rejected.Count + " malformed word " + noun +
                " from " + sourceName + ":\n - " + string.Join("\n - ", parsed.rejected));
        }

        if (!parsed.HasWords)
            return parsed.words; // the repository reports the fallback and substitutes the built-in pool

        string filter = WordDifficulty.Normalize(difficultyId);
        if (filter.Length == 0)
            return parsed.words;

        var band = new List<WordData>();
        foreach (WordData word in parsed.words)
        {
            if (word.DifficultyId == filter)
                band.Add(word);
        }

        if (band.Count > 0)
            return band;

        // A thin bank degrades to a playable board rather than a blank one.
        Debug.LogWarning(
            "BundledWordSource: no words authored for difficulty '" + difficultyId +
            "' in " + sourceName + ". Falling back to the full pool.");
        return parsed.words;
    }
}
