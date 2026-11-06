using System.Collections.Generic;
using NUnit.Framework;

// Covers the picked-upgrade chips on the roguelike Attacker frame (design:
// HUD Party Frame) - DMG +n, CRIT ×n, MULTI ×shots, only for cards taken.
public class UpgradeChipsTests
{
    [Test]
    public void NoPicks_NoChips()
    {
        Assert.AreEqual(0, UpgradeChips.From(new AttackerUpgrades()).Count);
    }

    [Test]
    public void EveryCardTaken_ShowsAllThreeInCardOrder()
    {
        AttackerUpgrades upgrades = new AttackerUpgrades();
        upgrades.Apply(UpgradeCard.Multishot);
        upgrades.Apply(UpgradeCard.Multishot);
        upgrades.Apply(UpgradeCard.AttackDamage);
        upgrades.Apply(UpgradeCard.AttackDamage);
        upgrades.Apply(UpgradeCard.CritStrike);

        List<UpgradeChip> chips = UpgradeChips.From(upgrades);

        Assert.AreEqual(3, chips.Count);
        Assert.AreEqual("DMG +2", chips[0].text);
        Assert.AreEqual("CRIT ×1", chips[1].text);
        Assert.AreEqual("MULTI ×5", chips[2].text);
    }
}
