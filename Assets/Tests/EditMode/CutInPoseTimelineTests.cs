using NUnit.Framework;

public class CutInPoseTimelineTests
{
    private readonly CutInTiming timing = new CutInTiming();

    [Test]
    public void FrameAt_BeforeTheSnap_FlipsBetweenTheTalkFrames()
    {
        float flip = timing.talkFlipSeconds;
        Assert.AreEqual(CutInFrame.TalkClosed, CutInPoseTimeline.FrameAt(timing, flip * 0.5f));
        Assert.AreEqual(CutInFrame.TalkOpen, CutInPoseTimeline.FrameAt(timing, flip * 1.5f));
        Assert.AreEqual(CutInFrame.TalkClosed, CutInPoseTimeline.FrameAt(timing, flip * 2.5f));
    }

    [Test]
    public void FrameAt_ShoutsFromTheSnap_ThenHoldsTheAfterglow()
    {
        Assert.AreEqual(CutInFrame.Shout, CutInPoseTimeline.FrameAt(timing, timing.snapAt + 0.01f));
        Assert.AreEqual(CutInFrame.Afterglow, CutInPoseTimeline.FrameAt(timing, timing.snapAt + timing.shoutHoldSeconds + 0.01f));
        Assert.AreEqual(CutInFrame.Afterglow, CutInPoseTimeline.FrameAt(timing, timing.slideOutAt));
    }

    [Test]
    public void FlashAlpha_PeaksAtTheSnap_AndFades()
    {
        Assert.AreEqual(0f, CutInPoseTimeline.FlashAlpha(timing, timing.snapAt - 0.01f));
        Assert.AreEqual(timing.flashPeak, CutInPoseTimeline.FlashAlpha(timing, timing.snapAt), 1e-5f);
        Assert.AreEqual(0f, CutInPoseTimeline.FlashAlpha(timing, timing.snapAt + timing.flashSeconds), 1e-5f);
    }

    [Test]
    public void PushScale_GrowsFromOneToThePushScale_AfterTheSnap()
    {
        Assert.AreEqual(1f, CutInPoseTimeline.PushScale(timing, 0.2f));
        Assert.AreEqual(timing.pushScale, CutInPoseTimeline.PushScale(timing, timing.snapAt + timing.pushSeconds), 1e-5f);
        Assert.AreEqual(timing.pushScale, CutInPoseTimeline.PushScale(timing, 2f), 1e-5f);
    }

    [Test]
    public void ShakeStrength_OnlyRightAfterTheSnap()
    {
        Assert.AreEqual(0f, CutInPoseTimeline.ShakeStrength(timing, 0.5f));
        Assert.AreEqual(1f, CutInPoseTimeline.ShakeStrength(timing, timing.snapAt), 1e-5f);
        Assert.AreEqual(0f, CutInPoseTimeline.ShakeStrength(timing, timing.snapAt + timing.shakeSeconds));
    }
}
