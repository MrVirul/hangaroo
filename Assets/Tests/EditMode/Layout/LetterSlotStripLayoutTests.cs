using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a real <see cref="HorizontalLayoutGroup"/> strip of N slots at a given
/// container width and asserts the fitted result fits - the acceptance test for
/// issue #16 part 2 ("builds N slots at a given container width and asserts the
/// resulting sizeDelta fits").
/// </summary>
public class LetterSlotStripLayoutTests
{
    const float Spacing = 12f;
    const float Preferred = 100f;
    const float ContainerWidth = 1067.5f;

    GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
            root = null;
        }
    }

    GameObject BuildStrip(int count, float containerWidth, float slotWidth)
    {
        root = new GameObject("Strip", typeof(RectTransform));

        var rt = (RectTransform)root.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(containerWidth, 135f);

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = Spacing;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < count; i++)
        {
            var child = new GameObject("slot" + i, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);

            var layout = child.AddComponent<LayoutElement>();
            layout.minWidth = slotWidth;
            layout.preferredWidth = slotWidth;
            layout.flexibleWidth = 0f;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        return root;
    }

    static float MeasuredStripWidth(RectTransform rt)
    {
        float sum = 0f;
        for (int i = 0; i < rt.childCount; i++)
        {
            var child = (RectTransform)rt.GetChild(i);
            sum += child.rect.width;
        }

        int gaps = Mathf.Max(0, rt.childCount - 1);
        return sum + gaps * Spacing;
    }

    [Test]
    public void ShortWord_UsesAuthoredWidthAndFits()
    {
        const int count = 8;
        float fitted = SlotStripLayout.FitSlotWidth(count, ContainerWidth, Spacing, Preferred, SlotStripLayout.MinSlotWidth);

        Assert.AreEqual(Preferred, fitted);

        var rt = (RectTransform)BuildStrip(count, ContainerWidth, fitted).transform;

        Assert.AreEqual(Preferred, ((RectTransform)rt.GetChild(0)).rect.width, 0.5f);
        Assert.LessOrEqual(MeasuredStripWidth(rt), ContainerWidth + 0.5f);
    }

    [Test]
    public void LongWord_ShrinksSlotsSoTheStripFits()
    {
        const int count = 12;
        float fitted = SlotStripLayout.FitSlotWidth(count, ContainerWidth, Spacing, Preferred, SlotStripLayout.MinSlotWidth);

        Assert.Less(fitted, Preferred);

        var rt = (RectTransform)BuildStrip(count, ContainerWidth, fitted).transform;

        for (int i = 0; i < count; i++)
            Assert.AreEqual(fitted, ((RectTransform)rt.GetChild(i)).rect.width, 0.5f);

        Assert.LessOrEqual(MeasuredStripWidth(rt), ContainerWidth + 0.5f);
    }

    /// <summary>
    /// The GamePlay scene's layout group does not control child width
    /// (<c>childControlWidth == false</c>), so it lays slots out at their
    /// <c>RectTransform.sizeDelta</c> and ignores <c>LayoutElement</c> widths.
    /// This mirrors that configuration to prove the fit still applies.
    /// </summary>
    [Test]
    public void LongWord_SizedViaRectTransform_AlsoFits()
    {
        const int count = 12;
        float fitted = SlotStripLayout.FitSlotWidth(count, ContainerWidth, Spacing, Preferred, SlotStripLayout.MinSlotWidth);

        root = new GameObject("Strip", typeof(RectTransform));

        var rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(ContainerWidth, 135f);

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = Spacing;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;

        for (int i = 0; i < count; i++)
        {
            var child = new GameObject("slot" + i, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            ((RectTransform)child.transform).sizeDelta = new Vector2(fitted, 128f);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        for (int i = 0; i < count; i++)
            Assert.AreEqual(fitted, ((RectTransform)rt.GetChild(i)).rect.width, 0.5f);

        Assert.LessOrEqual(MeasuredStripWidth(rt), ContainerWidth + 0.5f);
    }
}
