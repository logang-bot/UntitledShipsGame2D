using NUnit.Framework;

// Covers the two clocks the boss-panel feeds keep so the HUD can show
// timers without the boss mechanics exposing more state: WarningClock
// (how long a telegraph flag has been on) and CooldownClock (a cooldown's
// progress, with its total learned from the largest remaining value seen).
public class HudClockTests
{
    [Test]
    public void WarningClock_CountsFromWhenTheFlagTurnedOn()
    {
        WarningClock clock = new WarningClock();

        clock.Track(true, 10f);
        clock.Track(true, 10.5f);

        Assert.AreEqual(0.5f, clock.Elapsed, 0.0001f);
    }

    [Test]
    public void WarningClock_RestartsAfterTheFlagDrops()
    {
        WarningClock clock = new WarningClock();
        clock.Track(true, 10f);
        clock.Track(false, 11f);

        clock.Track(true, 20f);
        clock.Track(true, 20.25f);

        Assert.AreEqual(0.25f, clock.Elapsed, 0.0001f);
    }

    [Test]
    public void WarningClock_OffIsZero()
    {
        WarningClock clock = new WarningClock();
        clock.Track(true, 10f);

        Assert.IsFalse(clock.Track(false, 12f));
        Assert.AreEqual(0f, clock.Elapsed);
    }

    [Test]
    public void CooldownClock_ReadyIsFull()
    {
        Assert.AreEqual(1f, new CooldownClock().Progress(0f));
    }

    [Test]
    public void CooldownClock_FillsAsTheRemainingTimeRunsDown()
    {
        CooldownClock clock = new CooldownClock();

        clock.Progress(8f);

        Assert.AreEqual(0.75f, clock.Progress(2f), 0.0001f);
    }

    [Test]
    public void CooldownClock_ANewCooldownResetsTheTotal()
    {
        CooldownClock clock = new CooldownClock();
        clock.Progress(8f);
        clock.Progress(0f);

        clock.Progress(4f);

        Assert.AreEqual(0.5f, clock.Progress(2f), 0.0001f);
    }
}
