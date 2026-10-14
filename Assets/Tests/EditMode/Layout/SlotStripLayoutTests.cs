using NUnit.Framework;

/// <summary>EditMode coverage for the letter-strip fit math (issue #16, part 2).</summary>
public class SlotStripLayoutTests
{
    const float Preferred = 100f;
    const float Spacing = 12f;
    const float Floor = SlotStripLayout.MinSlotWidth;

    [Test]
    public void TotalWidth_SumsSlotsAndGaps()
    {
        Assert.AreEqual(0f, SlotStripLayout.TotalWidth(0, Preferred, Spacing));
        Assert.AreEqual(100f, SlotStripLayout.TotalWidth(1, Preferred, Spacing));
        Assert.AreEqual(100f * 3 + 12f * 2, SlotStripLayout.TotalWidth(3, Preferred, Spacing));
    }

    [Test]
    public void FitSlotWidth_ReturnsPreferredWhenTheStripAlreadyFits()
    {
        // 8 x 100 + 7 x 12 = 884 <= 1067.5
        Assert.AreEqual(Preferred, SlotStripLayout.FitSlotWidth(8, 1067.5f, Spacing, Preferred, Floor));
    }

    [Test]
    public void FitSlotWidth_ShrinksSoTheStripFitsWhenItOverflows()
    {
        // 12 x 100 + 11 x 12 = 1332 > 1067.5, so it must shrink.
        float fitted = SlotStripLayout.FitSlotWidth(12, 1067.5f, Spacing, Preferred, Floor);

        Assert.Less(fitted, Preferred);
        Assert.LessOrEqual(SlotStripLayout.TotalWidth(12, fitted, Spacing), 1067.5f + 0.001f);
        Assert.AreEqual((1067.5f - 11f * Spacing) / 12f, fitted, 0.001f);
    }

    [Test]
    public void FitSlotWidth_NeverExceedsPreferred()
    {
        // Huge container: still returns the authored width, never larger.
        Assert.AreEqual(Preferred, SlotStripLayout.FitSlotWidth(4, 100000f, Spacing, Preferred, Floor));
    }

    [Test]
    public void FitSlotWidth_ClampsAtTheLegibilityFloor()
    {
        // 200 slots in 1067.5px cannot fit; the result must stop at the floor.
        float fitted = SlotStripLayout.FitSlotWidth(200, 1067.5f, Spacing, Preferred, Floor);

        Assert.AreEqual(Floor, fitted, 0.001f);
    }

    [Test]
    public void FitSlotWidth_HandlesDegenerateInput()
    {
        Assert.AreEqual(Preferred, SlotStripLayout.FitSlotWidth(0, 1067.5f, Spacing, Preferred, Floor));
        Assert.AreEqual(Preferred, SlotStripLayout.FitSlotWidth(5, 0f, Spacing, Preferred, Floor));
        Assert.AreEqual(Preferred, SlotStripLayout.FitSlotWidth(5, -10f, Spacing, Preferred, Floor));
    }

    [Test]
    public void FitSlotWidth_WithASingleSlot_UsesPreferredOrFits()
    {
        Assert.AreEqual(Preferred, SlotStripLayout.FitSlotWidth(1, 1067.5f, Spacing, Preferred, Floor));
        Assert.AreEqual(40f, SlotStripLayout.FitSlotWidth(1, 40f, Spacing, Preferred, Floor), 0.001f);
    }
}
