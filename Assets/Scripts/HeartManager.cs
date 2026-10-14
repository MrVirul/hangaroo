using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current word's remaining hearts. It deliberately owns no run
/// rules: <see cref="GameManager"/> drives a <see cref="RunLifecycle"/> and
/// mirrors the resulting lives here, so "how many lives and when they refill"
/// lives in one testable place.
///
/// The row is sized from the difficulty config, not from the number of Images
/// authored in the scene: extra hearts are cloned from the first one and the
/// rest are hidden. Attach to the layout-group parent of the heart Images.
/// </summary>
public class HeartManager : MonoBehaviour
{
    public static HeartManager Instance;

    public Image[] hearts;

    public Sprite fullHeart;
    public Sprite emptyHeart;

    private readonly List<Image> displayed = new List<Image>();

    private int lives;
    private int maxLives;

    /// <summary>Hearts left on the current word.</summary>
    public int Lives => lives;

    /// <summary>Hearts granted at the start of the current word.</summary>
    public int MaxLives => maxLives;

    void Awake()
    {
        Instance = this;
        SeedFromAuthoredHearts();
    }

    /// <summary>
    /// Refills to the difficulty's per-word maximum, cloning hearts if the config
    /// asks for more than the scene authored.
    /// </summary>
    public void ResetLives(int max)
    {
        EnsureHeartCount(max);

        maxLives = Mathf.Max(0, max);
        lives = maxLives;

        UpdateHearts();
    }

    /// <summary>Mirrors an externally tracked life count (never above the max).</summary>
    public void SetLives(int value)
    {
        lives = Mathf.Clamp(value, 0, maxLives);
        UpdateHearts();
    }

    public bool HasLives()
    {
        return lives > 0;
    }

    private void SeedFromAuthoredHearts()
    {
        displayed.Clear();

        if (hearts != null)
        {
            foreach (Image heart in hearts)
            {
                if (heart != null)
                {
                    displayed.Add(heart);
                }
            }
        }

        maxLives = displayed.Count;
        lives = maxLives;

        UpdateHearts();
    }

    private void EnsureHeartCount(int count)
    {
        if (count <= displayed.Count || displayed.Count == 0)
        {
            return;
        }

        // The authored hearts share a layout group, so a clone slots into the row.
        Transform parent = displayed[0].transform.parent;

        while (displayed.Count < count)
        {
            Image clone = Instantiate(displayed[0], parent);
            clone.name = "Heart " + (displayed.Count + 1);
            displayed.Add(clone);
        }
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < displayed.Count; i++)
        {
            Image heart = displayed[i];

            if (heart == null)
            {
                continue;
            }

            bool shown = i < maxLives;
            heart.gameObject.SetActive(shown);

            if (shown)
            {
                heart.sprite = i < lives ? fullHeart : emptyHeart;
            }
        }
    }
}
