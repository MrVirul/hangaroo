using UnityEngine;

/// <summary>
/// Pure layout math for a single horizontal strip of letter slots (issue #16,
/// part 2). Kept free of Unity UI types so the fit can be unit-tested without a
/// scene, and so <c>GameManager</c> only has to apply the result.
///
/// The strip is a <c>HorizontalLayoutGroup</c> whose children each carry a
/// <c>LayoutElement</c>. A layout group drives child <c>RectTransform</c> sizes
/// and ignores <c>localScale</c>, so the only way to make a row fit is to shrink
/// the children's <c>LayoutElement</c> widths - never their transforms.
/// </summary>
public static class SlotStripLayout
{
    /// <summary>
    /// Smallest width the fitter will shrink a slot to. A strip that cannot fit
    /// even at this floor is left to overflow rather than become unreadable.
    /// </summary>
    public const float MinSlotWidth = 32f;

    /// <summary>Total width of <paramref name="count"/> slots plus the gaps between them.</summary>
    public static float TotalWidth(int count, float slotWidth, float spacing)
    {
        if (count <= 0)
            return 0f;

        return count * slotWidth + (count - 1) * spacing;
    }

    /// <summary>
    /// The width each slot should use so that <paramref name="count"/> slots fit
    /// inside <paramref name="availableWidth"/>. Never exceeds
    /// <paramref name="preferredSlotWidth"/>, and never drops below
    /// <paramref name="minSlotWidth"/>. Returns the preferred width for
    /// degenerate input (no slots, unknown width) so callers can reset.
    /// </summary>
    public static float FitSlotWidth(
        int count,
        float availableWidth,
        float spacing,
        float preferredSlotWidth,
        float minSlotWidth)
    {
        if (count <= 0 || preferredSlotWidth <= 0f || availableWidth <= 0f)
            return preferredSlotWidth;

        if (TotalWidth(count, preferredSlotWidth, spacing) <= availableWidth)
            return preferredSlotWidth;

        float usable = availableWidth - spacing * (count - 1);
        float fitted = usable / count;
        float floor = Mathf.Min(minSlotWidth, preferredSlotWidth);

        return Mathf.Clamp(fitted, floor, preferredSlotWidth);
    }
}
