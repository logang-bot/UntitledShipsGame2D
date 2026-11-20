using NUnit.Framework;

public class CutInTimelineTests
{
    private readonly CutInTiming timing = new CutInTiming();

    [Test]
    public void Coverage_BeforeTheCast_IsHidden()
    {
        Assert.AreEqual(0f, CutInTimeline.Coverage(timing, -1f));
    }

    [Test]
    public void Coverage_SlidesInOverSlideInSeconds()
    {
        Assert.AreEqual(0f, CutInTimeline.Coverage(timing, 0f), 1e-5f);
        Assert.AreEqual(MenuEase.Out(0.5f), CutInTimeline.Coverage(timing, timing.slideInSeconds * 0.5f), 1e-5f);
        Assert.AreEqual(1f, CutInTimeline.Coverage(timing, timing.slideInSeconds), 1e-5f);
    }

    [Test]
    public void Coverage_HoldsFullBetweenSlides()
    {
        Assert.AreEqual(1f, CutInTimeline.Coverage(timing, timing.snapAt));
        Assert.AreEqual(1f, CutInTimeline.Coverage(timing, timing.SlideOutAt));
    }

    [Test]
    public void Coverage_SlidesBackOutAndStaysHidden()
    {
        float halfway = timing.SlideOutAt + timing.slideOutSeconds * 0.5f;
        Assert.AreEqual(1f - MenuEase.In(0.5f), CutInTimeline.Coverage(timing, halfway), 1e-5f);
        Assert.AreEqual(0f, CutInTimeline.Coverage(timing, timing.SlideOutAt + timing.slideOutSeconds), 1e-5f);
        Assert.AreEqual(0f, CutInTimeline.Coverage(timing, 10f), 1e-5f);
    }

    [Test]
    public void SlideOut_StartsAFixedTimeAfterTheSnap()
    {
        CutInTiming late = new CutInTiming { snapAt = 1.8f, slideOutAfterSnapSeconds = 1.5f };
        Assert.AreEqual(3.3f, late.SlideOutAt, 1e-5f);
        Assert.AreEqual(1f, CutInTimeline.Coverage(late, 3.29f));
        Assert.Less(CutInTimeline.Coverage(late, 3.4f), 1f);
    }

    [Test]
    public void CastStarted_OnTheFirstFrameOfACast_IsTrue()
    {
        Assert.IsTrue(CutInTimeline.CastStarted(-1f, 0f));
        Assert.IsTrue(CutInTimeline.CastStarted(-1f, 0.1f));
    }

    [Test]
    public void CastStarted_MidCastOrIdle_IsFalse()
    {
        Assert.IsFalse(CutInTimeline.CastStarted(0.1f, 0.2f));
        Assert.IsFalse(CutInTimeline.CastStarted(-1f, -1f));
        Assert.IsFalse(CutInTimeline.CastStarted(0.2f, -1f));
    }
}
