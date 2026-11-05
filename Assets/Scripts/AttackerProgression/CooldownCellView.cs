using TMPro;
using UnityEngine;
using UnityEngine.UI;

// An Attack or Crit cell on the roguelike Attacker frame (design: HUD Party
// Frame): the 40 px square sweep refilling as the attack cools, its name with
// an optional tag (×5), a sub line (DMG 4) and the seconds left. The whole
// cell lights up in its colour for the fire flash.
public class CooldownCellView : MonoBehaviour
{
    private const float FlashFillAlpha = 0.16f;
    private const float RestFillAlpha = 0.6f;

    public Image boxFill;
    public Image boxStroke;
    public Image ringTrack;
    public Image ringFill;
    public Image ringInner;
    public Image icon;
    public GameObject tag;
    public TMP_Text tagText;
    public Image tagStroke;
    public TMP_Text sub;
    public TMP_Text timer;
    public PaletteColor main = PaletteColor.Attacker;
    public PaletteColor dim = PaletteColor.AttackerDim;

    public void Show(CooldownCellState state, string subLine, string tagLine)
    {
        PaintBox(state.flashing);
        PaintRing(state);
        PaintTag(tagLine);
        sub.text = subLine;
        timer.text = state.timer;
        timer.color = NeonPalette.Get(state.flashing ? main : PaletteColor.Text);
    }

    private void PaintBox(bool flashing)
    {
        boxStroke.color = NeonPalette.Get(flashing ? main : dim);
        boxFill.color = flashing ? NeonPalette.Get(main, FlashFillAlpha) : NeonPalette.Get(PaletteColor.HatchDark, RestFillAlpha);
    }

    private void PaintRing(CooldownCellState state)
    {
        ringTrack.color = NeonPalette.Get(PaletteColor.Line2);
        ringFill.color = NeonPalette.Get(main);
        ringFill.fillAmount = state.flashing ? 1f : state.sweep;
        ringInner.color = NeonPalette.Get(state.flashing ? main : PaletteColor.HatchDark);
        icon.color = NeonPalette.Get(state.flashing ? PaletteColor.Ink : main);
    }

    private void PaintTag(string tagLine)
    {
        tag.SetActive(!string.IsNullOrEmpty(tagLine));
        tagText.text = tagLine;
        tagText.color = NeonPalette.Get(main);
        tagStroke.color = NeonPalette.Get(main);
    }
}
