// Each upgrade card's colour family (design: Neon Fleet HUD v2, section 10):
// Crit Strike is vermilion, Attack Damage and Multishot use the Attacker's
// orange. Shared by the cards, the chips and the frame's cells.
public static class UpgradeCardPalette
{
    public static PaletteColor MainFor(UpgradeCard card) =>
        card == UpgradeCard.CritStrike ? PaletteColor.Crit : PaletteColor.Attacker;

    public static PaletteColor DimFor(UpgradeCard card) =>
        card == UpgradeCard.CritStrike ? PaletteColor.CritDim : PaletteColor.AttackerDim;
}
