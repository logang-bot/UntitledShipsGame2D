using NUnit.Framework;

// Covers banked level-ups opening back to back (design: Neon Fleet HUD v2,
// section 10) - PICK i / n and the amber "more after this" line.
public class PickStreakTests
{
    [Test]
    public void SinglePick_IsNotMulti_AndSaysChooseOne()
    {
        PickStreak streak = new PickStreak();
        streak.Start(1);

        Assert.IsFalse(streak.IsMulti);
        Assert.IsFalse(streak.HasMore);
        Assert.AreEqual("FIGHT FROZEN · CHOOSE ONE", streak.SubLine);
    }

    [Test]
    public void TwoBanked_CountsThroughBothScreens()
    {
        PickStreak streak = new PickStreak();
        streak.Start(2);

        Assert.AreEqual("PICK 1 / 2", streak.PickText);
        Assert.AreEqual("+1 MORE PICK AFTER THIS", streak.SubLine);

        streak.Advance();

        Assert.AreEqual("PICK 2 / 2", streak.PickText);
        Assert.IsFalse(streak.HasMore);
    }
}
