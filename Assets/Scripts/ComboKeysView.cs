using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The Attacker's combo box (design: HUD Party Frame): the 1-2-3 keycaps
// (landed = filled role color, next = white edge, rest = slate) and its
// headline, NEXT ▸ n / COMBO ✓ / BROKEN. Hidden on every other role.
public class ComboKeysView : MonoBehaviour
{
    public Image boxStroke;
    public TMP_Text label;
    public Image checkIcon;
    public Image[] keyFills;
    public Image[] keyStrokes;
    public TMP_Text[] keyTexts;

    private RoleColors colors;

    public void Bind(PlayerRole role)
    {
        colors = RolePalette.For(role);
        gameObject.SetActive(role == PlayerRole.Attacker);
    }

    public void Show(ComboDisplay display)
    {
        PaintLabel(display);
        for (int i = 0; i < keyFills.Length; i++) PaintKey(i, display);
    }

    public void Hide() => gameObject.SetActive(false);

    private void PaintLabel(ComboDisplay display)
    {
        label.text = display.Label;
        label.color = display.mood switch
        {
            ComboMood.Broken => NeonPalette.Get(PaletteColor.Notice),
            ComboMood.Complete => colors.main,
            _ => NeonPalette.Get(PaletteColor.Text2),
        };
        checkIcon.gameObject.SetActive(display.mood == ComboMood.Complete);
        checkIcon.color = colors.main;
        boxStroke.color = NeonPalette.Get(display.mood == ComboMood.Broken ? PaletteColor.Notice : PaletteColor.Line2);
    }

    private void PaintKey(int index, ComboDisplay display)
    {
        bool broken = display.mood == ComboMood.Broken;
        bool landed = !broken && index < display.landed;
        bool next = display.mood == ComboMood.Next && index == display.landed;
        keyFills[index].enabled = landed;
        keyFills[index].color = colors.main;
        keyStrokes[index].color = broken ? NeonPalette.Get(PaletteColor.Notice) : landed ? colors.main : NeonPalette.Get(next ? PaletteColor.Text : PaletteColor.Line2);
        keyTexts[index].color = broken ? NeonPalette.Get(PaletteColor.Notice) : NeonPalette.Get(landed ? PaletteColor.Ink : next ? PaletteColor.Text : PaletteColor.Muted);
    }
}
