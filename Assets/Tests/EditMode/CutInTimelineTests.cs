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
        Assert.AreEqual(1f, CutInTimeline.Coverage(timing, timing.slideOutAt));
    }

    [Test]
    public void Coverage_SlidesBackOutAndStaysHidden()
    {
        float halfway = timing.slideOutAt + timing.slideOutSeconds * 0.5f;
        Assert.AreEqual(1f - MenuEase.In(0.5f), CutInTimeline.Coverage(timing, halfway), 1e-5f);
        Assert.AreEqual(0f, CutInTimeline.Coverage(timing, timing.slideOutAt + timing.slideOutSeconds), 1e-5f);
        Assert.AreEqual(0f, CutInTimeline.Coverage(timing, 10f), 1e-5f);
    }
}
