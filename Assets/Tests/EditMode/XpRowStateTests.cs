using NUnit.Framework;

// Covers the XP row on the roguelike Attacker frame (design: HUD Party Frame)
// - one cell per XP the next level costs, and one solid MAX cell at level 5.
public class XpRowStateTests
{
    private static AttackerXpTrack TrackWith(int xp)
    {
        AttackerXpTrack track = new AttackerXpTrack();
        for (int i = 0; i < xp; i++) track.AddXp();
        return track;
    }

    [Test]
    public void FightStart_IsLevelZero_WithOneEmptyCell()
    {
        XpRowState s = XpRowState.From(TrackWith(0));

        Assert.AreEqual(0, s.level);
        Assert.AreEqual(1, s.cells);
        Assert.AreEqual(0, s.filled);
        Assert.AreEqual("0/1 XP", s.readout);
    }

    [Test]
    public void MidLevelThree_ShowsFourCells_WithTwoFilled()
    {
        XpRowState s = XpRowState.From(TrackWith(1 + 2 + 3 + 2));

        Assert.AreEqual(3, s.level);
        Assert.AreEqual(4, s.cells);
        Assert.AreEqual(2, s.filled);
        Assert.AreEqual("2/4 XP", s.readout);
    }

    [Test]
    public void LevelFive_IsOneSolidMaxCell()
    {
        XpRowState s = XpRowState.From(TrackWith(15));

        Assert.IsTrue(s.maxed);
        Assert.AreEqual(1, s.cells);
        Assert.AreEqual(1, s.filled);
        Assert.AreEqual("MAX", s.readout);
    }
}
