using NUnit.Framework;

public class AttackerUpgradesTests
{
    [Test]
    public void NoPicks_IsBaseAttacker()
    {
        AttackerUpgrades upgrades = new AttackerUpgrades();

        Assert.AreEqual(0, upgrades.DamageBonus);
        Assert.AreEqual(1, upgrades.ShotCount);
        Assert.IsFalse(upgrades.CritUnlocked);
    }

    [Test]
    public void AttackDamage_StacksByOneWholeDamage()
    {
        AttackerUpgrades upgrades = new AttackerUpgrades();
        upgrades.Apply(UpgradeCard.AttackDamage);
        upgrades.Apply(UpgradeCard.AttackDamage);

        Assert.AreEqual(2, upgrades.DamageBonus);
    }

    [Test]
    public void Multishot_FiresThreeThenAddsTwoPerPick()
    {
        AttackerUpgrades upgrades = new AttackerUpgrades();
        upgrades.Apply(UpgradeCard.Multishot);
        Assert.AreEqual(3, upgrades.ShotCount);

        upgrades.Apply(UpgradeCard.Multishot);
        Assert.AreEqual(5, upgrades.ShotCount);
    }

    [Test]
    public void CritDamage_IsAttackDamagePlusStackingBonus()
    {
        AttackerUpgrades upgrades = new AttackerUpgrades();
        upgrades.Apply(UpgradeCard.CritStrike);
        Assert.IsTrue(upgrades.CritUnlocked);
        Assert.AreEqual(6f, upgrades.CritDamage(3f));

        upgrades.Apply(UpgradeCard.CritStrike);
        Assert.AreEqual(9f, upgrades.CritDamage(3f));
    }
}
