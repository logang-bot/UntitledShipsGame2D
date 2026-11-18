using NUnit.Framework;

// The Taunt magnet: snaps on, loops for the taunt's 3 s, collapses, then hides.
public class TauntMagnetTimelineTests
{
    private const float Taunt = 3f;

    [Test]
    public void FrameAt_ActivatesThenLoopsTheSustainFrames()
    {
        Assert.AreEqual(TauntMagnetFrame.Activate, TauntMagnetTimeline.FrameAt(0.05f, Taunt));
        float loop = TauntMagnetTimeline.ActivateSeconds + 0.01f;
        float step = TauntMagnetTimeline.SustainFrameSeconds;
        Assert.AreEqual(TauntMagnetFrame.SustainA, TauntMagnetTimeline.FrameAt(loop, Taunt));
        Assert.AreEqual(TauntMagnetFrame.SustainB, TauntMagnetTimeline.FrameAt(loop + step, Taunt));
        Assert.AreEqual(TauntMagnetFrame.SustainA, TauntMagnetTimeline.FrameAt(loop + step * 3f, Taunt));
    }

    [Test]
    public void FrameAt_EndsAfterTheTaunt_ThenHides()
    {
        Assert.AreEqual(TauntMagnetFrame.End, TauntMagnetTimeline.FrameAt(Taunt + 0.05f, Taunt));
        Assert.AreEqual(TauntMagnetFrame.None, TauntMagnetTimeline.FrameAt(Taunt + TauntMagnetTimeline.EndSeconds, Taunt));
        Assert.AreEqual(TauntMagnetFrame.None, TauntMagnetTimeline.FrameAt(float.PositiveInfinity, Taunt), "Never taunted.");
    }
}
