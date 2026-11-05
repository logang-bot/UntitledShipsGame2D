using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One picked-upgrade chip on the XP row (design: HUD Party Frame): a 14 px
// icon and DMG +2 / CRIT ×1 / MULTI ×5, stroked in the card's dim colour.
public class UpgradeChipView : MonoBehaviour
{
    public Image icon;
    public TMP_Text label;
    public Image stroke;
    public UpgradeCardIcons icons;

    public void Show(UpgradeChip chip)
    {
        PaletteColor main = UpgradeCardPalette.MainFor(chip.card);
        icon.sprite = icons.For(chip.card);
        icon.color = NeonPalette.Get(main);
        label.text = chip.text;
        label.color = NeonPalette.Get(main);
        stroke.color = NeonPalette.Get(UpgradeCardPalette.DimFor(chip.card));
    }
}
