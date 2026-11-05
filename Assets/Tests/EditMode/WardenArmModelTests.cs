using NUnit.Framework;

public class WardenArmModelTests
{
    [TestCase(true, true, true, WardenArmLook.Down)]
    [TestCase(false, true, false, WardenArmLook.Locked)]
    [TestCase(false, false, true, WardenArmLook.Firing)]
    [TestCase(false, false, false, WardenArmLook.Idle)]
    public void LookFor_PicksDownFirstThenLockedThenFiring(bool isDown, bool isTelegraphing, bool isFiring, WardenArmLook expected)
    {
        Assert.AreEqual(expected, WardenArmModel.LookFor(isDown, isTelegraphing, isFiring));
    }

    [Test]
    public void RebootTicksLit_JustKnockedDown_LightsOneTick()
    {
        Assert.AreEqual(1, WardenArmModel.RebootTicksLit(15f, 15f));
    }

    [Test]
    public void RebootTicksLit_HalfwayDown_LightsSevenTicks()
    {
        Assert.AreEqual(7, WardenArmModel.RebootTicksLit(7.5f, 15f));
    }

    [Test]
    public void RebootTicksLit_AboutToRecover_LightsEveryTick()
    {
        Assert.AreEqual(WardenArmModel.RebootTickCount, WardenArmModel.RebootTicksLit(0f, 15f));
    }
}
