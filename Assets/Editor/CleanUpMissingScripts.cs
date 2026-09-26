using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class CleanUpMissingScripts
{
    [MenuItem("Tools/Clean Up Missing Scripts", false, 100)]
    private static void CleanUpMissingScriptsMenuItem()
    {
        List<string> prefabPaths = new List<string>();
        List<GameObject> roots = new List<GameObject>();

        foreach (GameObject go in Selection.gameObjects)
        {
            if (go == null)
                continue;
            roots.Add(go);
        }

        foreach (Object selected in Selection.objects)
        {
            if (selected == null)
                continue;
            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                continue;
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || prefabPaths.Contains(path))
                continue;
            prefabPaths.Add(path);
            roots.Add(asset);
        }

        if (roots.Count == 0)
        {
            EditorUtility.DisplayDialog("Clean Up Missing Scripts", "Select a GameObject (or a Prefab asset) in the Project window first.", "OK");
            return;
        }

        List<GameObject> targets = new List<GameObject>();
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;
            targets.Add(root);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.gameObject != root && !targets.Contains(child.gameObject))
                    targets.Add(child.gameObject);
            }
        }

        int removedTotal = 0;
        int affectedObjects = 0;

        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                GameObject go = targets[i];
                if (go == null)
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Clean Up Missing Scripts",
                        "Scanning " + go.name + " (" + (i + 1) + "/" + targets.Count + ")",
                        (float)i / targets.Count))
                {
                    break;
                }

                int removed = RemoveMissingComponents(go);
                removedTotal += removed;
                if (removed > 0)
                    affectedObjects++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        foreach (string path in prefabPaths)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
                continue;
            if (PrefabUtility.SavePrefabAsset(asset) == null)
                Debug.LogWarning("[CleanUpMissingScripts] Could not re-save prefab '" + path +
                                 "' (Prefab Variants need to be re-saved inside the Prefab editor). Re-run after opening it.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CleanUpMissingScripts] Removed " + removedTotal + " missing script(s) across " +
                  affectedObjects + " GameObject(s).");
        EditorUtility.DisplayDialog("Clean Up Missing Scripts",
            "Removed " + removedTotal + " missing script(s) across " + affectedObjects + " GameObject(s).", "OK");
    }

    [MenuItem("Tools/Clean Up Missing Scripts", true)]
    private static bool CleanUpMissingScriptsValidation()
    {
        return Selection.activeObject != null;
    }

    private static int RemoveMissingComponents(GameObject go)
    {
        SerializedObject so = new SerializedObject(go);
        SerializedProperty components = so.FindProperty("m_Component");
        if (components == null || !components.isArray)
        {
            so.Dispose();
            return 0;
        }

        int count = 0;
        for (int i = components.arraySize - 1; i >= 0; i--)
        {
            if (components.GetArrayElementAtIndex(i).objectReferenceValue == null)
            {
                components.DeleteArrayElementAtIndex(i);
                count++;
            }
        }

        if (count > 0)
        {
            if (!EditorUtility.IsPersistent(go))
                Undo.RegisterCompleteObjectUndo(go, "Clean Up Missing Scripts");
            so.ApplyModifiedProperties();
        }

        so.Dispose();
        return count;
    }
}