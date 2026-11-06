using System.Collections.Generic;

// The chips for every upgrade taken so far, in card order, skipping cards
// never picked. Pure, so it's unit-tested (UpgradeChipsTests).
public static class UpgradeChips
{
    public static List<UpgradeChip> From(AttackerUpgrades upgrades)
    {
        List<UpgradeChip> chips = new List<UpgradeChip>();
        if (upgrades.DamageBonus > 0) chips.Add(Chip(UpgradeCard.AttackDamage, $"DMG +{upgrades.DamageBonus}"));
        if (upgrades.CritUnlocked) chips.Add(Chip(UpgradeCard.CritStrike, $"CRIT ×{upgrades.PicksOf(UpgradeCard.CritStrike)}"));
        if (upgrades.ShotCount > 1) chips.Add(Chip(UpgradeCard.Multishot, $"MULTI ×{upgrades.ShotCount}"));
        return chips;
    }

    private static UpgradeChip Chip(UpgradeCard card, string text) => new UpgradeChip { card = card, text = text };
}
