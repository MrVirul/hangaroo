using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EditMode guards for the Settings panel wiring (Stories 1.1/1.2): the prefab -
/// not the scenes - owns the bindings, every field is assigned, and every event
/// targets the prefab's own <see cref="SettingsManager"/>.
/// </summary>
public class SettingsPrefabTests
{
    private const string PrefabPath = "Assets/Prefabs/Settings_Panel.prefab";

    private static GameObject LoadPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.IsNotNull(prefab, "Missing settings prefab at " + PrefabPath);
        return prefab;
    }

    [Test]
    public void SettingsManager_HasNoUnassignedObjectReferences()
    {
        SettingsManager manager = LoadPrefab().GetComponent<SettingsManager>();
        Assert.IsNotNull(manager, "The prefab root must carry the SettingsManager");

        var missing = new List<string>();
        var serialized = new SerializedObject(manager);
        SerializedProperty property = serialized.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                property.objectReferenceValue == null)
            {
                missing.Add(property.propertyPath);
            }
        }

        CollectionAssert.IsEmpty(missing,
            "Unassigned SettingsManager fields: " + string.Join(", ", missing));
    }

    [Test]
    public void DifficultyDropdown_OffersTheThreeBands()
    {
        TMP_Dropdown dropdown = LoadPrefab().GetComponent<SettingsManager>().difficultyDropdown;

        Assert.IsNotNull(dropdown, "difficultyDropdown must be assigned");
        Assert.AreEqual(3, dropdown.options.Count);
        CollectionAssert.AreEqual(
            new[] { "Easy", "Medium", "Hard" },
            dropdown.options.ConvertAll(option => option.text));
    }

    [Test]
    public void EveryPersistentCall_TargetsTheInPrefabSettingsManager()
    {
        GameObject prefab = LoadPrefab();
        SettingsManager manager = prefab.GetComponent<SettingsManager>();

        int calls = 0;

        foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.Generic ||
                    !property.propertyPath.EndsWith("m_PersistentCalls"))
                {
                    continue;
                }

                SerializedProperty list = property.FindPropertyRelative("m_Calls");
                if (list == null)
                {
                    continue;
                }

                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty target = list.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target");
                    Assert.AreSame(manager, target.objectReferenceValue,
                        component.GetType().Name + "." + property.propertyPath + "[" + i +
                        "] must target the prefab's SettingsManager, not fileID 0");
                    calls++;
                }
            }
        }

        Assert.GreaterOrEqual(calls, 8,
            "Expected the Difficulty/Hints/Show Category/Game Sounds/volume/audio/Reset/Save events to be wired");
    }

    [Test]
    public void ResponsiveAdapter_IsAttachedAndFullyConfigured()
    {
        GameObject prefab = LoadPrefab();
        SettingsUIResponsiveAdapter adapter = prefab.GetComponent<SettingsUIResponsiveAdapter>();

        Assert.IsNotNull(adapter,
            "SettingsUIResponsiveAdapter must be attached to the panel root (Story 1.2)");

        var serialized = new SerializedObject(adapter);

        foreach (string field in new[] { "square", "header", "soundsSection", "resetButton", "saveButton" })
        {
            Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, field + " must be assigned");
        }

        SerializedProperty rows = serialized.FindProperty("settingRows");
        Assert.AreEqual(4, rows.arraySize, "Difficulty/Hints/Show Category/Game Sounds rows must be registered");

        for (int i = 0; i < rows.arraySize; i++)
        {
            Assert.IsNotNull(rows.GetArrayElementAtIndex(i).objectReferenceValue, "settingRows[" + i + "]");
        }
    }

    [Test]
    public void SettingRows_ContainTheirControls()
    {
        GameObject prefab = LoadPrefab();
        SettingsUIResponsiveAdapter adapter = prefab.GetComponent<SettingsUIResponsiveAdapter>();
        var serialized = new SerializedObject(adapter);
        SerializedProperty rows = serialized.FindProperty("settingRows");

        var difficulty = (RectTransform)rows.GetArrayElementAtIndex(0).objectReferenceValue;
        var hints = (RectTransform)rows.GetArrayElementAtIndex(1).objectReferenceValue;
        var category = (RectTransform)rows.GetArrayElementAtIndex(2).objectReferenceValue;
        var sounds = (RectTransform)rows.GetArrayElementAtIndex(3).objectReferenceValue;

        Assert.IsNotNull(difficulty.GetComponentInChildren<TMP_Dropdown>(true),
            "Difficulty row needs a TMP_Dropdown");

        // Assigned directly (not GetComponentInChildren) so the TMP_Dropdown's
        // internal template Toggle is not mistaken for a settings toggle.
        Assert.AreEqual(1, hints.GetComponentsInChildren<Toggle>(true).Length,
            "Hints row needs a Toggle");
        Assert.AreEqual(1, category.GetComponentsInChildren<Toggle>(true).Length,
            "Show Category row needs a Toggle");

        Button[] soundsButtons = sounds.GetComponentsInChildren<Button>(true);
        Assert.AreEqual(1, soundsButtons.Length, "Game Sounds row needs a Button");
        Assert.AreEqual("Game Sounds Button", soundsButtons[0].name);
    }

    [Test]
    public void EachControl_CallsTheExpectedHandler()
    {
        GameObject prefab = LoadPrefab();

        var expected = new Dictionary<string, string>
        {
            { "Difficulty Dropdown", "OnDifficultyChanged" },
            { "Hints Toggle", "OnHintsChanged" },
            { "Show Category Toggle", "OnShowCategoryChanged" },
            { "Game Sounds Button", "ToggleGameSounds" },
            { "Reset_To_Default", "ResetToDefault" },
            { "Save", "Save" },
            { "Scrollbar", "OnVolumeChanged" },
            { "Tap Sound Button", "ToggleClickSound" },
            { "Bg Music Button", "ToggleBgMusic" },
        };

        var found = new Dictionary<string, string>();

        foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.GetIterator();

            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.Generic ||
                    !property.propertyPath.EndsWith("m_PersistentCalls"))
                {
                    continue;
                }

                SerializedProperty list = property.FindPropertyRelative("m_Calls");
                for (int i = 0; i < list.arraySize; i++)
                {
                    found[component.gameObject.name] =
                        list.GetArrayElementAtIndex(i).FindPropertyRelative("m_MethodName").stringValue;
                }
            }
        }

        foreach (KeyValuePair<string, string> pair in expected)
        {
            Assert.IsTrue(found.ContainsKey(pair.Key), pair.Key + " must have a wired event");
            Assert.AreEqual(pair.Value, found[pair.Key], pair.Key + " must call " + pair.Value);
        }
    }

    [Test]
    public void ResponsiveAdapter_KeepsEveryChildInsideTheSquare_AtEveryAspectRatio()
    {
        GameObject prefab = LoadPrefab();

        // 16:9, 4:3 and 20:9, the three ratios called out by Story 1.2.
        var screens = new[]
        {
            new Vector2(1920f, 1080f),
            new Vector2(1024f, 768f),
            new Vector2(1080f, 2400f),
        };

        foreach (Vector2 screen in screens)
        {
            var host = new GameObject("SettingsHost", typeof(RectTransform));
            try
            {
                ((RectTransform)host.transform).sizeDelta = screen;

                GameObject panel = Object.Instantiate(prefab, host.transform);
                var root = (RectTransform)panel.transform;
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;

                var adapter = panel.GetComponent<SettingsUIResponsiveAdapter>();
                Assert.IsNotNull(adapter, "The prefab must carry the responsive adapter");

                // The safe area is passed explicitly: reading the editor's
                // global Screen.safeArea here made this verification depend on
                // whatever the Game View last reported (issue #21).
                adapter.ApplyLayout(root.rect);

                var square = (RectTransform)panel.transform.Find("Sqaure");
                Assert.IsNotNull(square, "Sqaure must exist");
                AssertInside(root, square, "square at " + screen);

                AssertContentInsideSquare(square, screen.ToString());
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }

    /// <summary>
    /// Issue #21 also asks for verification "including a notched safe area":
    /// the panel must keep every child inside the square even when the device
    /// cuts a notch out of the top and a home indicator out of the bottom.
    /// </summary>
    [Test]
    public void ResponsiveAdapter_KeepsEveryChildInsideTheSquare_InANotchedSafeArea()
    {
        GameObject prefab = LoadPrefab();

        var host = new GameObject("SettingsHost", typeof(RectTransform));
        try
        {
            // A 20:9 phone: notch across the top, home indicator at the bottom.
            ((RectTransform)host.transform).sizeDelta = new Vector2(1080f, 2400f);

            GameObject panel = Object.Instantiate(prefab, host.transform);
            var root = (RectTransform)panel.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            Rect full = root.rect;
            Rect notched = Rect.MinMaxRect(
                full.xMin + 24f,       // rounded corner
                full.yMin + 40f,       // home indicator
                full.xMax - 24f,       // rounded corner
                full.yMax - 120f);     // notch

            var adapter = panel.GetComponent<SettingsUIResponsiveAdapter>();
            Assert.IsNotNull(adapter, "The prefab must carry the responsive adapter");
            adapter.ApplyLayout(notched);

            var square = (RectTransform)panel.transform.Find("Sqaure");
            Assert.IsNotNull(square, "Sqaure must exist");

            // The panel itself must sit inside the notched safe area, and the
            // safe area only - not the raw screen.
            AssertInside(root, notched, square, "square inside the notched safe area");

            AssertContentInsideSquare(square, "notched safe area");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    /// <summary>
    /// Every piece of settings content - header, rows, sounds block, buttons and
    /// the sounds block's own descendants - must stay inside the square.
    /// </summary>
    private static void AssertContentInsideSquare(RectTransform square, string label)
    {
        foreach (string child in new[]
                 {
                     "Settings_Icon", "Difficulty Row", "Hints Row", "Show Category Row",
                     "Game Sounds Row", "Sounds ", "Reset_To_Default", "Save"
                 })
        {
            var rect = (RectTransform)square.Find(child);
            Assert.IsNotNull(rect, child + " must exist in the prefab");
            AssertInside(square, rect, child + " at " + label);
        }

        // The Sounds block's own children (volume slider, Tap Sound,
        // Bg Music) are the deepest content in the panel; make sure the
        // whole block still clears the pinned button row.
        var soundsSection = (RectTransform)square.Find("Sounds ");
        foreach (RectTransform descendant in soundsSection.GetComponentsInChildren<RectTransform>(true))
        {
            if (descendant == soundsSection)
            {
                continue;
            }

            AssertInside(square, descendant, descendant.name + " (Sounds) at " + label);
        }
    }

    private static void AssertInside(RectTransform container, RectTransform child, string label)
    {
        var outer = new Vector3[4];
        var inner = new Vector3[4];
        container.GetWorldCorners(outer);
        child.GetWorldCorners(inner);

        AssertInsideBounds(outer, inner, label);
    }

    /// <summary>
    /// Same containment check, but the container's bounds are an arbitrary
    /// rectangle in the container's local space (the safe area) rather than the
    /// container's own rect.
    /// </summary>
    private static void AssertInside(RectTransform container, Rect containerLocalRect, RectTransform child, string label)
    {
        var outer = new Vector3[4];
        outer[0] = container.TransformPoint(new Vector3(containerLocalRect.xMin, containerLocalRect.yMin));
        outer[1] = container.TransformPoint(new Vector3(containerLocalRect.xMin, containerLocalRect.yMax));
        outer[2] = container.TransformPoint(new Vector3(containerLocalRect.xMax, containerLocalRect.yMax));
        outer[3] = container.TransformPoint(new Vector3(containerLocalRect.xMax, containerLocalRect.yMin));

        var inner = new Vector3[4];
        child.GetWorldCorners(inner);

        AssertInsideBounds(outer, inner, label);
    }

    private static void AssertInsideBounds(Vector3[] outer, Vector3[] inner, string label)
    {
        float minX = Mathf.Min(outer[0].x, Mathf.Min(outer[1].x, Mathf.Min(outer[2].x, outer[3].x)));
        float maxX = Mathf.Max(outer[0].x, Mathf.Max(outer[1].x, Mathf.Max(outer[2].x, outer[3].x)));
        float minY = Mathf.Min(outer[0].y, Mathf.Min(outer[1].y, Mathf.Min(outer[2].y, outer[3].y)));
        float maxY = Mathf.Max(outer[0].y, Mathf.Max(outer[1].y, Mathf.Max(outer[2].y, outer[3].y)));

        const float epsilon = 1f;

        for (int i = 0; i < 4; i++)
        {
            Assert.GreaterOrEqual(inner[i].x, minX - epsilon, label + " spills past the left edge");
            Assert.LessOrEqual(inner[i].x, maxX + epsilon, label + " spills past the right edge");
            Assert.GreaterOrEqual(inner[i].y, minY - epsilon, label + " spills past the bottom edge");
            Assert.LessOrEqual(inner[i].y, maxY + epsilon, label + " spills past the top edge");
        }
    }
}
