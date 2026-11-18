using NUnit.Framework;

// The Super Shield's 4 s cast: expand while growing to 2.5x, a shimmer
// loop at full size, the ending flicker, then collapse back to 1x.
public class SuperShieldTimelineTests
{
    private readonly TankAbilitySettings settings = new TankAbilitySettings();

    [Test]
    public void ScaleAt_GrowsHoldsAndShrinksBack()
    {
        Assert.AreEqual(1f, SuperShieldTimeline.ScaleAt(settings, 0f), 1e-5f);
        Assert.AreEqual(settings.superShieldScale, SuperShieldTimeline.ScaleAt(settings, settings.growSeconds), 1e-5f);
        Assert.AreEqual(settings.superShieldScale, SuperShieldTimeline.ScaleAt(settings, 2f), 1e-5f);
        Assert.AreEqual(1f, SuperShieldTimeline.ScaleAt(settings, settings.superShieldSeconds), 1e-5f);
    }

    [Test]
    public void FrameAt_ExpandsThenShimmers()
    {
        Assert.AreEqual(SuperShieldFrame.Expand, SuperShieldTimeline.FrameAt(settings, 0.1f));
        float shimmerStart = settings.growSeconds + 0.01f;
        Assert.AreEqual(SuperShieldFrame.Idle, SuperShieldTimeline.FrameAt(settings, shimmerStart));
        Assert.AreEqual(SuperShieldFrame.Pulse, SuperShieldTimeline.FrameAt(settings, shimmerStart + SuperShieldTimeline.ShimmerFrameSeconds));
    }

    [Test]
    public void FrameAt_FlickersBeforeTheEnd_ThenCollapses()
    {
        float endingStart = settings.superShieldSeconds - settings.collapseSeconds - settings.endingSeconds;
        Assert.AreEqual(SuperShieldFrame.Ending, SuperShieldTimeline.FrameAt(settings, 3.05f), "3.05 s is an even flicker step.");
        Assert.Greater(3.05f, endingStart);
        Assert.AreEqual(SuperShieldFrame.Collapse, SuperShieldTimeline.FrameAt(settings, settings.superShieldSeconds - 0.1f));
    }
}
