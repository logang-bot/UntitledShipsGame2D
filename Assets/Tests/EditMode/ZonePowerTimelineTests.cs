using NUnit.Framework;
using UnityEngine;

// A placed superpower's cast (Sanctuary, Echo Gate): open while growing to
// full size, a 2-frame shimmer, the ending flicker, then collapse to nothing.
public class ZonePowerTimelineTests
{
    private readonly ZonePhaseTiming timing = new ZonePhaseTiming(6f);

    [Test]
    public void ScaleAt_GrowsHoldsAndShrinksAway()
    {
        Assert.AreEqual(0f, ZonePowerTimeline.ScaleAt(timing, 0f), 1e-5f);
        Assert.AreEqual(1f, ZonePowerTimeline.ScaleAt(timing, timing.openSeconds), 1e-5f);
        Assert.AreEqual(1f, ZonePowerTimeline.ScaleAt(timing, 3f), 1e-5f);
        Assert.AreEqual(0f, ZonePowerTimeline.ScaleAt(timing, timing.seconds), 1e-5f);
    }

    [Test]
    public void FrameAt_OpensThenShimmers()
    {
        Assert.AreEqual(ZoneFrame.Open, ZonePowerTimeline.FrameAt(timing, 0.1f));
        float shimmerStart = timing.openSeconds + 0.01f;
        Assert.AreEqual(ZoneFrame.Idle, ZonePowerTimeline.FrameAt(timing, shimmerStart));
        Assert.AreEqual(ZoneFrame.Pulse, ZonePowerTimeline.FrameAt(timing, shimmerStart + ZonePowerTimeline.ShimmerFrameSeconds));
    }

    [Test]
    public void FrameAt_FlickersBeforeTheEnd_ThenCollapses()
    {
        float endingStep = FirstOddFlickerStepMidpoint();
        Assert.Less(endingStep + ZonePowerTimeline.FlickerSeconds, timing.seconds - timing.collapseSeconds);
        Assert.AreEqual(ZoneFrame.Ending, ZonePowerTimeline.FrameAt(timing, endingStep));
        Assert.AreEqual(ZoneFrame.Idle, ZonePowerTimeline.FrameAt(timing, endingStep + ZonePowerTimeline.FlickerSeconds));
        Assert.AreEqual(ZoneFrame.Collapse, ZonePowerTimeline.FrameAt(timing, timing.seconds - 0.1f));
    }

    [Test]
    public void FrameAt_FollowsALongerCast()
    {
        ZonePhaseTiming gate = new ZonePhaseTiming(8f);
        Assert.AreNotEqual(ZoneFrame.Collapse, ZonePowerTimeline.FrameAt(gate, 7f - 0.5f));
        Assert.AreEqual(ZoneFrame.Collapse, ZonePowerTimeline.FrameAt(gate, 7.9f));
    }

    private float FirstOddFlickerStepMidpoint()
    {
        float endingStart = timing.seconds - timing.collapseSeconds - timing.endingSeconds;
        int step = Mathf.CeilToInt(endingStart / ZonePowerTimeline.FlickerSeconds);
        if (step % 2 == 0) step++;
        return (step + 0.5f) * ZonePowerTimeline.FlickerSeconds;
    }
}
