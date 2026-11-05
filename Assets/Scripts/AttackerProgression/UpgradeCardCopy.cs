using System.Globalization;
using UnityEngine;

// The text on one level-up card (design: HUD Upgrade Card): name, what the
// card does, and the stat it changes, before → after. First picks of Crit
// Strike and Multishot unlock something new, so they carry a NEW chip. Pure,
// so it's unit-tested (UpgradeCardCopyTests).
public struct UpgradeCardCopy
{
    public string name;
    public string effect;
    public string statLabel;
    public string before;
    public string after;
    public bool isNew;

    public static UpgradeCardCopy From(UpgradeCardContext c)
    {
        switch (c.card)
        {
            case UpgradeCard.AttackDamage: return AttackDamage(c);
            case UpgradeCard.CritStrike: return c.taken == 0 ? FirstCrit(c) : MoreCrit(c);
            default: return c.taken == 0 ? FirstMultishot() : MoreMultishot(c);
        }
    }

    private static UpgradeCardCopy AttackDamage(UpgradeCardContext c) => new UpgradeCardCopy
    {
        name = "ATTACK DAMAGE", effect = "+1 damage per shot.", statLabel = "DAMAGE",
        before = Number(c.damage), after = Number(c.damage + AttackerUpgrades.DamagePerPick),
    };

    private static UpgradeCardCopy FirstCrit(UpgradeCardContext c) => new UpgradeCardCopy
    {
        name = "CRIT STRIKE", effect = $"Red shot every {Number(c.critInterval)} s, +100% damage", statLabel = "CRIT BONUS",
        before = "—", after = Percent(AttackerUpgrades.CritBonusFor(1)), isNew = true,
    };

    private static UpgradeCardCopy MoreCrit(UpgradeCardContext c) => new UpgradeCardCopy
    {
        name = "CRIT STRIKE", effect = "Each pick adds +100% crit bonus.", statLabel = "CRIT BONUS",
        before = Percent(AttackerUpgrades.CritBonusFor(c.taken)), after = Percent(AttackerUpgrades.CritBonusFor(c.taken + 1)),
    };

    private static UpgradeCardCopy FirstMultishot() => new UpgradeCardCopy
    {
        name = "MULTISHOT", effect = $"Your attack fires {AttackerUpgrades.ShotsFor(1)} shots in a cone", statLabel = "SHOTS",
        before = AttackerUpgrades.ShotsFor(0).ToString(), after = AttackerUpgrades.ShotsFor(1).ToString(), isNew = true,
    };

    private static UpgradeCardCopy MoreMultishot(UpgradeCardContext c) => new UpgradeCardCopy
    {
        name = "MULTISHOT", effect = "Each pick adds +2 shots. The cone widens.", statLabel = "SHOTS",
        before = AttackerUpgrades.ShotsFor(c.taken).ToString(), after = AttackerUpgrades.ShotsFor(c.taken + 1).ToString(),
    };

    private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static string Percent(float fraction) => $"+{Mathf.RoundToInt(fraction * 100f)}%";
}
