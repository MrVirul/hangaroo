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

        WordPool.ParseResult parsed = WordPool.Parse(
            asset.text,
            WordValidator.MinAnswerLength,
            WordValidator.MaxAnswerLength);

        foreach (string rejection in parsed.rejected)
            problems.Add(rejection);

        if (!parsed.HasWords)
            problems.Add("No valid words remained after validation.");

        return problems;
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
