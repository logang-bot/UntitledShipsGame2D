using NUnit.Framework;

// Covers the level-up card text (design: HUD Upgrade Card) - first picks of
// Crit Strike and Multishot are NEW, later picks show the stat before → after.
public class UpgradeCardCopyTests
{
    private static UpgradeCardCopy Copy(UpgradeCard card, int taken, float damage = 2f) =>
        UpgradeCardCopy.From(new UpgradeCardContext { card = card, taken = taken, damage = damage, critInterval = 3f });

    [Test]
    public void AttackDamage_ShowsLiveDamagePlusOne()
    {
        UpgradeCardCopy c = Copy(UpgradeCard.AttackDamage, 1, 3f);

        Assert.AreEqual("ATTACK DAMAGE", c.name);
        Assert.AreEqual("3", c.before);
        Assert.AreEqual("4", c.after);
        Assert.IsFalse(c.isNew);
    }

    [Test]
    public void CritStrike_FirstPick_IsNew_WithTheInterval()
    {
        UpgradeCardCopy c = Copy(UpgradeCard.CritStrike, 0);

        Assert.IsTrue(c.isNew);
        StringAssert.Contains("every 3 s", c.effect);
        Assert.AreEqual("—", c.before);
        Assert.AreEqual("+100%", c.after);
    }

    [Test]
    public void CritStrike_LaterPick_AddsOneHundredPercent()
    {
        UpgradeCardCopy c = Copy(UpgradeCard.CritStrike, 1);

        Assert.IsFalse(c.isNew);
        Assert.AreEqual("+100%", c.before);
        Assert.AreEqual("+200%", c.after);
    }

    [Test]
    public void Multishot_FirstPick_GoesFromOneToThreeShots()
    {
        UpgradeCardCopy c = Copy(UpgradeCard.Multishot, 0);

        Assert.IsTrue(c.isNew);
        Assert.AreEqual("1", c.before);
        Assert.AreEqual("3", c.after);
    }

    [Test]
    public void Multishot_SecondPick_GoesFromFiveToSeven()
    {
        UpgradeCardCopy c = Copy(UpgradeCard.Multishot, 2);

        Assert.AreEqual("5", c.before);
        Assert.AreEqual("7", c.after);
    }
}
