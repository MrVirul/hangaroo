using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Coverage for the display-only heart row that <see cref="GameManager"/> mirrors
/// its run state onto (issue #19, E5). Lives in the PlayMode assembly because
/// <see cref="HeartManager"/> seeds itself from its authored hearts in
/// <c>Awake</c>, which the editor does not call in EditMode.
/// </summary>
public class HeartManagerTests
{
    /// <summary>
    /// Builds an inactive object first so the serialized <c>hearts</c> array is in
    /// place before activation runs <c>Awake</c> / <c>SeedFromAuthoredHearts</c>.
    /// </summary>
    private static HeartManager Create(int authoredHearts, out GameObject root)
    {
        root = new GameObject("Hearts");
        root.SetActive(false);

        Image[] hearts = new Image[authoredHearts];
        for (int i = 0; i < authoredHearts; i++)
        {
            GameObject heart = new GameObject("Heart " + (i + 1));
            heart.transform.SetParent(root.transform, false);
            hearts[i] = heart.AddComponent<Image>();
        }

        HeartManager manager = root.AddComponent<HeartManager>();
        manager.hearts = hearts;

        root.SetActive(true);
        return manager;
    }

    [Test]
    public void Awake_SeedsLivesFromTheAuthoredHearts()
    {
        HeartManager manager = Create(4, out GameObject root);

        Assert.AreEqual(4, manager.MaxLives);
        Assert.AreEqual(4, manager.Lives);

        Object.DestroyImmediate(root);
    }

    [Test]
    public void ResetLives_ClonesUpToTheConfiguredMaximum()
    {
        HeartManager manager = Create(4, out GameObject root);

        manager.ResetLives(6);

        Assert.AreEqual(6, manager.MaxLives);
        Assert.AreEqual(6, manager.Lives);
        Assert.AreEqual(6, root.transform.childCount, "Hearts should be cloned to match the config.");

        Object.DestroyImmediate(root);
    }

    [Test]
    public void ResetLives_RefillsToTheMaximum()
    {
        HeartManager manager = Create(4, out GameObject root);

        manager.SetLives(1);
        Assert.AreEqual(1, manager.Lives);

        manager.ResetLives(4);
        Assert.AreEqual(4, manager.Lives, "Each word must start with a full set (issue #19).");

        Object.DestroyImmediate(root);
    }

    [Test]
    public void SetLives_ClampsBetweenZeroAndTheMaximum()
    {
        HeartManager manager = Create(3, out GameObject root);

        manager.SetLives(99);
        Assert.AreEqual(3, manager.Lives);

        manager.SetLives(-5);
        Assert.AreEqual(0, manager.Lives);
        Assert.IsFalse(manager.HasLives());

        Object.DestroyImmediate(root);
    }

    [Test]
    public void SetLives_UsesEmptySpritesForSpentHearts()
    {
        HeartManager manager = Create(3, out GameObject root);

        Texture2D texture = new Texture2D(1, 1);
        Sprite full = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        Sprite empty = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        manager.fullHeart = full;
        manager.emptyHeart = empty;

        manager.SetLives(2);

        Image[] hearts = root.GetComponentsInChildren<Image>();
        Assert.AreEqual(full, hearts[0].sprite);
        Assert.AreEqual(full, hearts[1].sprite);
        Assert.AreEqual(empty, hearts[2].sprite);

        Object.DestroyImmediate(root);
        Object.DestroyImmediate(full);
        Object.DestroyImmediate(empty);
        Object.DestroyImmediate(texture);
    }
}
