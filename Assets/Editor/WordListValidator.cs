using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Editor-side content pipeline for the authored word list (issue #18).
///
/// Fails loudly rather than silently shipping content the game cannot render:
///   - "Hangaroo/Content/Validate Word List" runs it on demand from the menu;
///   - <see cref="WordListPostprocessor"/> runs it whenever <c>words.json</c> is
///     imported/reimported, so a bad edit surfaces in the Console immediately;
///   - <see cref="WordListBuildValidator"/> runs it before every build and fails
///     the build on any problem.
///
/// Problems are reported with <see cref="Debug.LogError"/> (a console error, not
/// a warning), and every message names the offending entry and the exact reason.
/// </summary>
public static class WordListValidator
{
    public const string WordsAssetPath = "Assets/Data/words.json";

    /// <summary>
    /// Minimum valid words a difficulty must offer (Story 1.4). Below this a run
    /// can end early or repeat words, so it is treated as a content failure.
    /// </summary>
    public const int MinWordsPerBand = 60;

    /// <summary>Returns every problem found in the shipped word list (empty when clean).</summary>
    public static List<string> Validate()
    {
        return Validate(WordsAssetPath);
    }

    public static List<string> Validate(string assetPath)
    {
        var problems = new List<string>();

        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (asset == null)
        {
            problems.Add("Word list asset not found at " + assetPath + ".");
            return problems;
        }

        // Parse covers the per-entry rules: non-empty fields, A-Z answers, the
        // union length band, a known category, a known difficulty, the band the
        // entry declares, and the two copy-hygiene rules. The asset path is passed
        // through so any legacy-shape warning names this file.
        WordPool.ParseResult parsed = WordPool.Parse(
            asset.text,
            WordValidator.MinAnswerLength,
            WordValidator.MaxAnswerLength,
            null,
            assetPath);

        foreach (string rejection in parsed.rejected)
            problems.Add(rejection);

        if (!parsed.HasWords)
        {
            problems.Add("No valid words remained after validation.");
            return problems;
        }

        problems.AddRange(CheckBandPopulation(parsed.words));
        problems.AddRange(CheckBandsMatchConfigs());

        return problems;
    }

    /// <summary>
    /// Asserts every difficulty has enough words, and that Hard actually reaches
    /// the top of its band so the 8-12 promise is real rather than nominal.
    /// </summary>
    private static List<string> CheckBandPopulation(List<WordData> words)
    {
        var problems = new List<string>();
        var counts = new Dictionary<string, int>();
        var longest = new Dictionary<string, int>();

        foreach (WordData entry in words)
        {
            string band = WordDifficulty.Normalize(entry.DifficultyId);

            int count;
            counts.TryGetValue(band, out count);
            counts[band] = count + 1;

            int current;
            longest.TryGetValue(band, out current);
            if (entry.Answer.Length > current)
                longest[band] = entry.Answer.Length;
        }

        foreach (string band in WordDifficulty.All)
        {
            int count;
            counts.TryGetValue(band, out count);

            if (count < MinWordsPerBand)
            {
                problems.Add(
                    "difficulty '" + band + "' has only " + count + " valid word(s); at least " +
                    MinWordsPerBand + " are required or a run can repeat.");
            }
        }

        int hardLongest;
        longest.TryGetValue(WordDifficulty.Hard, out hardLongest);
        int hardMax = WordDifficulty.MaxLength(WordDifficulty.Hard);

        if (hardLongest < hardMax)
        {
            problems.Add(
                "difficulty 'hard' tops out at " + hardLongest + " letters; at least one word must reach " +
                hardMax + " so the 8-12 band is not silently degenerate.");
        }

        return problems;
    }

    /// <summary>
    /// Fails when the word-content bands drift from the authored DifficultyConfig
    /// assets, so the validator and the gameplay band can never disagree.
    /// </summary>
    private static List<string> CheckBandsMatchConfigs()
    {
        var problems = new List<string>();

        foreach (string band in WordDifficulty.All)
        {
            DifficultyConfig config = DifficultyLibrary.GetConfig(ToDifficultyId(band));

            if (config == null)
            {
                problems.Add("no DifficultyConfig asset is registered for '" + band + "'.");
                continue;
            }

            int expectedMin = WordDifficulty.MinLength(band);
            int expectedMax = WordDifficulty.MaxLength(band);

            if (config.minWordLength != expectedMin || config.maxWordLength != expectedMax)
            {
                problems.Add(
                    "difficulty '" + band + "' band (" + config.minWordLength + "-" + config.maxWordLength +
                    ") does not match WordDifficulty (" + expectedMin + "-" + expectedMax + ").");
            }
        }

        return problems;
    }

    private static DifficultyId ToDifficultyId(string band)
    {
        switch (WordDifficulty.Normalize(band))
        {
            case WordDifficulty.Easy: return DifficultyId.Easy;
            case WordDifficulty.Hard: return DifficultyId.Hard;
            default: return DifficultyId.Medium;
        }
    }

    [MenuItem("Hangaroo/Content/Validate Word List")]
    public static void ValidateFromMenu()
    {
        List<string> problems = Validate();

        if (problems.Count == 0)
        {
            Debug.Log("[WordListValidator] " + WordsAssetPath + " is valid.");
            return;
        }

        foreach (string problem in problems)
            Debug.LogError("[WordListValidator] " + problem);

        Debug.LogError("[WordListValidator] " + problems.Count + " problem(s) in " + WordsAssetPath + ".");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Batchmode entry point for CI. Logs any problems and exits with a non-zero
    /// code on failure so the workflow gates the PR.
    /// </summary>
    public static void ValidateWordListBatchmode()
    {
        List<string> problems = Validate();

        if (problems.Count == 0)
        {
            Debug.Log("[WordListValidator] " + WordsAssetPath + " is valid.");
            EditorApplication.Exit(0);
            return;
        }

        foreach (string problem in problems)
            Debug.LogError("[WordListValidator] " + problem);

        Debug.LogError("[WordListValidator] " + problems.Count + " problem(s) in " + WordsAssetPath + ".");
        EditorApplication.Exit(1);
    }
#endif
}

/// <summary>Validates the word list on every build and fails the build if it is malformed.</summary>
internal sealed class WordListBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        List<string> problems = WordListValidator.Validate();

        if (problems.Count == 0)
            return;

        foreach (string problem in problems)
            Debug.LogError("[WordListValidator] " + problem);

        throw new BuildFailedException(
            "Word list validation failed with " + problems.Count + " problem(s) in " +
            WordListValidator.WordsAssetPath + ". See the Console for details.");
    }
}

/// <summary>Validates the word list as soon as it is imported or changed.</summary>
internal sealed class WordListPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (path.Replace('\\', '/') != WordListValidator.WordsAssetPath)
                continue;

            List<string> problems = WordListValidator.Validate();
            foreach (string problem in problems)
                Debug.LogError("[WordListValidator] " + problem);

            return;
        }
    }
}
