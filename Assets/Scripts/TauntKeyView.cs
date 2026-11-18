using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The Tank's Taunt box (design: Neon Fleet HUD, Tank frame), in the spot the
// Attacker's combo box takes: the state label, the key-1 keycap (white edge
// when ready, filled while pulling, a sweep refilling it on cooldown), the
// magnet and the seconds, plus a bar for the taunt time left. Hidden on
// every other role.
public class TauntKeyView : MonoBehaviour
{
    public Image boxFill;
    public Image boxStroke;
    public TMP_Text label;
    public Image keyFill;
    public Image keyStroke;
    public TMP_Text keyText;
    public Image magnet;
    public TMP_Text seconds;
    public Image activeBar;

    private RoleColors colors;

    public void Bind(PlayerRole role)
    {
        colors = RolePalette.For(role);
        gameObject.SetActive(role == PlayerRole.Tank);
    }

    public void Show(TauntDisplay display)
    {
        PaintBox(display);
        PaintKey(display);
        PaintTimer(display);
    }

    private void PaintBox(TauntDisplay display)
    {
        bool pulling = display.mood == TauntMood.Pulling;
        label.text = display.Label;
        label.color = pulling ? colors.main : NeonPalette.Get(display.mood == TauntMood.Ready ? PaletteColor.Text2 : PaletteColor.Muted);
        boxStroke.color = pulling ? colors.main : NeonPalette.Get(PaletteColor.Line2);
        boxFill.color = pulling ? colors.fill : NeonPalette.Get(PaletteColor.HatchDark, 0.6f);
    }

    private void PaintKey(TauntDisplay display)
    {
        bool pulling = display.mood == TauntMood.Pulling;
        bool cooling = display.mood == TauntMood.Cooldown;
        keyFill.enabled = pulling || cooling;
        keyFill.fillAmount = cooling ? display.progress : 1f;
        keyFill.color = pulling ? colors.main : NeonPalette.Get(PaletteColor.Line2);
        keyStroke.color = pulling ? colors.main : NeonPalette.Get(cooling ? PaletteColor.Line2 : PaletteColor.Text);
        keyText.color = NeonPalette.Get(pulling ? PaletteColor.Ink : cooling ? PaletteColor.Text2 : PaletteColor.Text);
    }

    private void PaintTimer(TauntDisplay display)
    {
        bool pulling = display.mood == TauntMood.Pulling;
        magnet.color = display.mood == TauntMood.Cooldown ? NeonPalette.Get(PaletteColor.Muted) : colors.main;
        seconds.gameObject.SetActive(display.mood != TauntMood.Ready);
        seconds.text = $"{display.seconds:0.0}s";
        seconds.color = NeonPalette.Get(pulling ? PaletteColor.Text : PaletteColor.Text2);
        activeBar.enabled = pulling;
        activeBar.fillAmount = display.progress;
        activeBar.color = colors.main;
    }
}
