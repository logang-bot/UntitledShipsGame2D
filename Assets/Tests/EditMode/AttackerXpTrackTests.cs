using NUnit.Framework;

public class AttackerXpTrackTests
{
    [Test]
    public void LevelUps_CostOneTwoThreeFourFiveXp()
    {
        AttackerXpTrack track = new AttackerXpTrack();
        foreach (int cost in new[] { 1, 2, 3, 4, 5 })
        {
            for (int i = 1; i < cost; i++) Assert.IsFalse(track.AddXp());
            Assert.IsTrue(track.AddXp());
        }
        Assert.AreEqual(5, track.Level);
    }

    [Test]
    public void AddXp_AtMaxLevel_DoesNothing()
    {
        AttackerXpTrack track = new AttackerXpTrack();
        for (int i = 0; i < 15; i++) track.AddXp();

        Assert.IsTrue(track.IsMaxed);
        Assert.IsFalse(track.AddXp());
        Assert.AreEqual(5, track.Level);
        Assert.AreEqual(0, track.XpIntoLevel);
    }
}
