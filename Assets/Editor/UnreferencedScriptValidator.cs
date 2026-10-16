using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Guards against dead MonoBehaviours (issue #21): every script under
/// <c>Assets/Scripts/</c> that derives from <see cref="MonoBehaviour"/> must be
/// referenced by at least one scene or prefab, matched by its script GUID.
/// Plain classes and static helpers are ignored — they have no reason to be
/// attached.
///
/// Run it from the menu (<c>Hangaroo/Content/Validate Script References</c>) or
/// headlessly, which exits non-zero on a problem:
/// <code>-executeMethod UnreferencedScriptValidator.ValidateBatchmode</code>
/// </summary>
public static class UnreferencedScriptValidator
{
    public const string ScriptsFolder = "Assets/Scripts";
    private const string MenuPath = "Hangaroo/Content/Validate Script References";

    /// <summary>
    /// MonoBehaviours that are allowed to be unreferenced for now, each with the
    /// reason. Remove an entry the moment the script is attached. It is empty on
    /// purpose: <c>SettingsUIResponsiveAdapter</c> was the last entry and is now
    /// attached to <c>Settings_Panel.prefab</c> (issue #21), so the guard runs
    /// with no suppressions.
    /// </summary>
    private static readonly Dictionary<string, string> Allowlist = new Dictionary<string, string>();

    [MenuItem(MenuPath)]
    public static void ValidateFromMenu()
    {
        List<string> problems = Validate();

        if (problems.Count == 0)
        {
            Debug.Log("[UnreferencedScriptValidator] Every script under " + ScriptsFolder + " is referenced.");
            return;
        }

        foreach (string problem in problems)
            Debug.LogError("[UnreferencedScriptValidator] " + problem);

        Debug.LogError("[UnreferencedScriptValidator] " + problems.Count + " unreferenced script(s).");
    }

    /// <summary>Headless entry point; exits with code 1 when a script is unreferenced.</summary>
    public static void ValidateBatchmode()
    {
        List<string> problems = Validate();

        foreach (string problem in problems)
            Debug.LogError("[UnreferencedScriptValidator] " + problem);

        if (problems.Count == 0)
        {
            Debug.Log("[UnreferencedScriptValidator] OK.");
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError("[UnreferencedScriptValidator] " + problems.Count + " unreferenced script(s).");
        EditorApplication.Exit(1);
    }

    /// <summary>Returns one problem per unreferenced script (empty when clean).</summary>
    public static List<string> Validate()
    {
        string searchSpace = ReadSceneAndPrefabText();
        var problems = new List<string>();

        foreach (string scriptPath in FindCandidateScripts())
        {
            string guid = AssetDatabase.AssetPathToGUID(scriptPath);

            if (string.IsNullOrEmpty(guid))
                continue;

            if (searchSpace.Contains("guid: " + guid))
                continue;

            if (Allowlist.ContainsKey(scriptPath))
                continue;

            problems.Add(scriptPath + " is not referenced by any scene or prefab.");
        }

        return problems;
    }

    private static IEnumerable<string> FindCandidateScripts()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { ScriptsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

            if (!path.EndsWith(".cs"))
                continue;

            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            System.Type type = script != null ? script.GetClass() : null;

            if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type))
                continue;

            yield return path;
        }
    }

    private static string ReadSceneAndPrefabText()
    {
        var builder = new StringBuilder();

        foreach (string filter in new[] { "t:Scene", "t:Prefab" })
        {
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.StartsWith("Assets/"))
                    builder.Append(File.ReadAllText(path));
            }
        }

        return builder.ToString();
    }
}
