using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode coverage for Story 1.2 / issue #15: the responsive adapter keeps the
/// panel and its rows inside the safe area at 16:9, 4:3 and 20:9, the settings
/// handlers persist what the player picks, and the Hints toggle changes what the
/// running game shows.
///
/// The Show Category preference still persists, but the clue line no longer
/// carries a category decoration - it shows the word's clue alone.
/// </summary>
public class SettingsPanelTests
{
    private const string GameplayScene = "GamePlay";

    // Screens representative of the three required aspect ratios.
    private static readonly Vector2[] Screens =
    {
        new Vector2(1920f, 1080f), // 16:9
        new Vector2(1024f, 768f),  // 4:3
        new Vector2(1080f, 2400f), // 20:9
    };

    // ---------------------------------------------------------------------
    // Pure sizing logic
    // ---------------------------------------------------------------------

    [Test]
    public void ComputePanelSize_NeverExceedsTheAvailableArea_AndKeepsTheAspect()
    {
        foreach (Vector2 screen in Screens)
        {
            Vector2 size = SettingsUIResponsiveAdapter.ComputePanelSize(screen.x, screen.y, 2f, 420f);

            Assert.LessOrEqual(size.x, screen.x + 0.01f, "width at " + screen);
            Assert.LessOrEqual(size.y, screen.y + 0.01f, "height at " + screen);
            Assert.AreEqual(2f, size.x / size.y, 0.001f, "aspect at " + screen);
        }
    }

    [Test]
    public void ComputePanelSize_RaisesToTheMinimumOnlyWhileItStillFits()
    {
        Vector2 roomy = SettingsUIResponsiveAdapter.ComputePanelSize(2000f, 2000f, 2f, 420f);
        Assert.GreaterOrEqual(roomy.y, 420f, "A roomy screen should honour the minimum height");

        Vector2 cramped = SettingsUIResponsiveAdapter.ComputePanelSize(200f, 100f, 2f, 420f);
        Assert.LessOrEqual(cramped.y, 100f, "A cramped screen must never overflow to reach the minimum");
        Assert.AreEqual(2f, cramped.x / cramped.y, 0.001f, "aspect must survive the clamp");
    }

    [Test]
    public void ComputePanelSize_DegenerateInputsReturnZero()
    {
        Assert.AreEqual(Vector2.zero, SettingsUIResponsiveAdapter.ComputePanelSize(0f, 100f, 2f, 420f));
        Assert.AreEqual(Vector2.zero, SettingsUIResponsiveAdapter.ComputePanelSize(100f, 0f, 2f, 420f));
        Assert.AreEqual(Vector2.zero, SettingsUIResponsiveAdapter.ComputePanelSize(100f, 100f, 0f, 420f));
    }

    // ---------------------------------------------------------------------
    // Adapter flow
    // ---------------------------------------------------------------------

    [UnityTest]
    public IEnumerator Adapter_KeepsEverythingInsideThePanel_AtEveryAspectRatio()
    {
        foreach (Vector2 screen in Screens)
        {
            Rig rig = BuildRig(screen);
            rig.adapter.ApplyLayout(rig.root.rect);
            yield return null;

            AssertInside(rig.root, rig.square, "square");

            for (int i = 0; i < rig.rows.Length; i++)
            {
                AssertInside(rig.square, rig.rows[i], "row " + i);
            }

            AssertInside(rig.square, rig.reset, "Reset button");
            AssertInside(rig.square, rig.save, "Save button");

            // A second pass must be a no-op (the old code derived the button
            // widths from the rect it had already overwritten).
            Vector2[] before = Positions(rig.rows);
            rig.adapter.ApplyLayout(rig.root.rect);
            Vector2[] after = Positions(rig.rows);

            for (int i = 0; i < before.Length; i++)
            {
                Assert.AreEqual(before[i], after[i], "row " + i + " moved on the second layout pass");
            }

            Object.Destroy(rig.go);
            yield return null;
        }
    }

    // ---------------------------------------------------------------------
    // Settings handlers persist
    // ---------------------------------------------------------------------

    [Test]
    public void SettingsHandlers_PersistTheirPreference()
    {
        var go = new GameObject("settings-test");
        SettingsManager manager = go.AddComponent<SettingsManager>();

        manager.OnHintsChanged(false);
        Assert.AreEqual(0, PlayerPrefs.GetInt("Hints", -1), "Hints off must persist");

        manager.OnHintsChanged(true);
        Assert.AreEqual(1, PlayerPrefs.GetInt("Hints", -1), "Hints on must persist");

        manager.OnShowCategoryChanged(false);
        Assert.AreEqual(0, PlayerPrefs.GetInt("ShowCategory", -1), "Show Category off must persist");

        manager.OnDifficultyChanged((int)DifficultyId.Hard);
        Assert.AreEqual(DifficultyId.Hard, DifficultyLibrary.Current,
            "Changing the dropdown must apply the difficulty immediately");

        Object.Destroy(go);
    }

    // ---------------------------------------------------------------------
    // Gameplay gating (issue #15)
    // ---------------------------------------------------------------------

    [UnitySetUp]
    public IEnumerator LoadGameplay()
    {
        Time.timeScale = 1f;

        PlayerPrefs.SetInt("Hints", 1);
        PlayerPrefs.SetInt("ShowCategory", 1);
        PlayerPrefs.Save();
        DifficultyLibrary.SetCurrent(DifficultyId.Easy);

        SceneManager.LoadScene(GameplayScene);

        float deadline = Time.realtimeSinceStartup + 15f;
        while (GameManager.Instance == null)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "GamePlay scene did not finish loading.");
            yield return null;
        }

        yield return null;
    }

    [TearDown]
    public void Cleanup()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey("Hints");
        PlayerPrefs.DeleteKey("ShowCategory");
        DifficultyLibrary.SetCurrent(DifficultyIdExtensions.Default);
    }

    [UnityTest]
    public IEnumerator HintsToggle_HidesAndShowsTheHintLine()
    {
        GameManager game = GameManager.Instance;

        PlayerPrefs.SetInt("Hints", 1);
        Assert.IsTrue(game.RefreshHint(), "Easy's immediate hint should be available with Hints on");
        Assert.IsTrue(game.hintText.gameObject.activeSelf);

        PlayerPrefs.SetInt("Hints", 0);
        Assert.IsFalse(game.RefreshHint(), "Turning Hints off must hide the hint line");
        Assert.IsFalse(game.hintText.gameObject.activeSelf, "The hint text object must be hidden");

        PlayerPrefs.SetInt("Hints", 1);
        Assert.IsTrue(game.RefreshHint(), "Turning Hints back on must show it again");
        Assert.IsTrue(game.hintText.gameObject.activeSelf);

        yield return null;
    }

    /// <summary>
    /// The clue line shows the word's bare clue, upper-cased. The Show Category
    /// preference no longer decorates it with a CATEGORY: line (or a CLUE:
    /// prefix); it still persists, which
    /// <see cref="PrefabEvents_DriveTheSettingsManager"/> covers.
    /// </summary>
    [UnityTest]
    public IEnumerator ClueText_ShowsTheBareClue_AndIgnoresTheShowCategoryPreference()
    {
        GameManager game = GameManager.Instance;

        var word = (WordData)GetField(game, "currentWord");
        Assert.IsNotNull(word, "A word must be loaded before the clue can be checked");

        string expected = word.Clue.ToUpper();
        Assert.IsNotEmpty(expected, "The loaded word must carry a clue");

        PlayerPrefs.SetInt("ShowCategory", 1);
        game.RefreshClue();
        Assert.IsTrue(game.clueText.gameObject.activeSelf, "The clue line must be visible");
        Assert.AreEqual(expected, game.clueText.text,
            "The clue line shows the clue alone - no CLUE: / CATEGORY: decoration");

        PlayerPrefs.SetInt("ShowCategory", 0);
        game.RefreshClue();
        Assert.AreEqual(expected, game.clueText.text,
            "Show Category no longer changes the clue line");

        yield return null;
    }

    // ---------------------------------------------------------------------
    // Prefab event wiring drives the manager (issue #15 / Story 1.2)
    // ---------------------------------------------------------------------

    [UnityTest]
    public IEnumerator PrefabEvents_DriveTheSettingsManager()
    {
        SettingsManager manager = FindPanelManager();
        Assert.IsNotNull(manager, "GamePlay must instance the shared Settings_Panel prefab");

        // Difficulty dropdown: onValueChanged -> OnDifficultyChanged (int).
        int target = manager.difficultyDropdown.value == (int)DifficultyId.Hard
            ? (int)DifficultyId.Easy
            : (int)DifficultyId.Hard;
        manager.difficultyDropdown.value = target;
        Assert.AreEqual((DifficultyId)target, DifficultyLibrary.Current,
            "Selecting a difficulty must apply it immediately, before Save");

        // Hints toggle: onValueChanged -> OnHintsChanged (bool).
        bool hints = !manager.hintsToggle.isOn;
        manager.hintsToggle.isOn = hints;
        Assert.AreEqual(hints ? 1 : 0, PlayerPrefs.GetInt("Hints", -1),
            "Toggling Hints must persist immediately");

        // Show Category toggle: onValueChanged -> OnShowCategoryChanged (bool).
        bool category = !manager.showCategoryToggle.isOn;
        manager.showCategoryToggle.isOn = category;
        Assert.AreEqual(category ? 1 : 0, PlayerPrefs.GetInt("ShowCategory", -1),
            "Toggling Show Category must persist immediately");

        yield return null;
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static SettingsManager FindPanelManager()
    {
        foreach (SettingsManager manager in
                 Object.FindObjectsByType<SettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            return manager;
        }

        return null;
    }

    private sealed class Rig
    {
        public GameObject go;
        public RectTransform root;
        public RectTransform square;
        public RectTransform[] rows;
        public RectTransform reset;
        public RectTransform save;
        public SettingsUIResponsiveAdapter adapter;
    }

    private static Rig BuildRig(Vector2 screen)
    {
        var rig = new Rig();

        rig.go = new GameObject("SettingsRoot", typeof(RectTransform));
        rig.root = rig.go.GetComponent<RectTransform>();
        RectTransform root = rig.root;
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = screen;

        rig.square = NewRect(root, "Square");
        RectTransform header = NewRect(rig.square, "Settings_Icon");
        RectTransform sounds = NewRect(rig.square, "Sounds");

        rig.reset = NewRect(rig.square, "Reset_To_Default");
        rig.reset.sizeDelta = new Vector2(160f, 50f);
        rig.save = NewRect(rig.square, "Save");
        rig.save.sizeDelta = new Vector2(160f, 50f);

        rig.rows = new RectTransform[4];
        for (int i = 0; i < rig.rows.Length; i++)
        {
            rig.rows[i] = NewRect(rig.square, "Row" + i);
        }

        rig.adapter = rig.go.AddComponent<SettingsUIResponsiveAdapter>();
        SetField(rig.adapter, "square", rig.square);
        SetField(rig.adapter, "header", header);
        SetField(rig.adapter, "soundsSection", sounds);
        SetField(rig.adapter, "resetButton", rig.reset);
        SetField(rig.adapter, "saveButton", rig.save);
        SetField(rig.adapter, "settingRows", rig.rows);

        return rig;
    }

    private static RectTransform NewRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void SetField(object target, string field, object value)
    {
        FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(info, "Missing field " + field);
        info.SetValue(target, value);
    }

    private static object GetField(object target, string field)
    {
        FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(info, "Missing field " + field);
        return info.GetValue(target);
    }

    private static void AssertInside(RectTransform container, RectTransform child, string label)
    {
        var outer = new Vector3[4];
        var inner = new Vector3[4];
        container.GetWorldCorners(outer);
        child.GetWorldCorners(inner);

        float minX = Mathf.Min(outer[0].x, outer[2].x);
        float maxX = Mathf.Max(outer[0].x, outer[2].x);
        float minY = Mathf.Min(outer[0].y, outer[2].y);
        float maxY = Mathf.Max(outer[0].y, outer[2].y);

        const float epsilon = 1f;

        for (int i = 0; i < 4; i++)
        {
            Assert.GreaterOrEqual(inner[i].x, minX - epsilon, label + " spills past the left edge");
            Assert.LessOrEqual(inner[i].x, maxX + epsilon, label + " spills past the right edge");
            Assert.GreaterOrEqual(inner[i].y, minY - epsilon, label + " spills past the bottom edge");
            Assert.LessOrEqual(inner[i].y, maxY + epsilon, label + " spills past the top edge");
        }
    }

    private static Vector2[] Positions(RectTransform[] rows)
    {
        var result = new Vector2[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            result[i] = rows[i].anchoredPosition;
        }

        return result;
    }
}
