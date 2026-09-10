using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UITools
{
    [MenuItem("Tools/UI Setup/Configure LetterSlot BG")]
    public static void ConfigureLetterSlotBG()
    {
        ConfigureTarget(ConfigureBanner);
    }

    [MenuItem("Tools/UI Setup/Configure Letter Prefab")]
    public static void ConfigureLetterPrefab()
    {
        ConfigureTarget(ConfigureLetter);
    }

    private static void ConfigureTarget(Action<GameObject, bool> configure)
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("UI Setup", "Select a GameObject in the Hierarchy or Project first.", "OK");
            return;
        }

        if (AssetDatabase.Contains(selected))
        {
            ConfigurePrefabAsset(selected, configure);
            return;
        }

        configure(selected, true);
        if (selected.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(selected.scene);
        }
    }

    private static void ConfigurePrefabAsset(GameObject selected, Action<GameObject, bool> configure)
    {
        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        try
        {
            configure(prefabRoot, false);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void ConfigureBanner(GameObject go, bool recordUndo)
    {
        const string menuName = "Configure LetterSlot BG";

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            EditorUtility.DisplayDialog(menuName, "Selected GameObject has no RectTransform.", "OK");
            return;
        }

        if (recordUndo)
        {
            Undo.RecordObject(rt, menuName);
        }
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);

        Image image = go.GetComponent<Image>();
        if (image != null)
        {
            if (recordUndo)
            {
                Undo.RecordObject(image, menuName);
            }
            image.type = Image.Type.Sliced;
        }

        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = recordUndo ? Undo.AddComponent<ContentSizeFitter>(go) : go.AddComponent<ContentSizeFitter>();
        }
        if (recordUndo)
        {
            Undo.RecordObject(fitter, menuName);
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        HorizontalLayoutGroup group = go.GetComponent<HorizontalLayoutGroup>();
        if (group == null)
        {
            group = recordUndo ? Undo.AddComponent<HorizontalLayoutGroup>(go) : go.AddComponent<HorizontalLayoutGroup>();
        }
        if (recordUndo)
        {
            Undo.RecordObject(group, menuName);
        }
        group.padding = new RectOffset(24, 24, 12, 12);
        group.spacing = 12f;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = false;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;
    }

    private static void ConfigureLetter(GameObject go, bool recordUndo)
    {
        const string menuName = "Configure Letter Prefab";

        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = recordUndo ? Undo.AddComponent<LayoutElement>(go) : go.AddComponent<LayoutElement>();
        }
        if (recordUndo)
        {
            Undo.RecordObject(element, menuName);
        }
        element.preferredWidth = 76f;
        element.preferredHeight = 72f;
        element.flexibleWidth = 0f;
    }
}