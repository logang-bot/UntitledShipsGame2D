using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A boss attack's cooldown tile (design: HUD Boss Panel): a dark tile with a
// round radial timer that fills in the boss color, READY, or a solid disc
// reading NOW while the attack is in progress.
public class CooldownTileView : MonoBehaviour
{
    public Image stroke;
    public Image ringTrack;
    public Image ringFill;
    public Image ringInner;
    public Image icon;
    public TMP_Text label;
    public TMP_Text value;

    private BossTokens boss;

    public void Bind(BossTokens tokens) => boss = tokens;

    public void Show(CooldownTileState state)
    {
        bool ready = !state.inUse && state.remaining <= 0f;
        bool cooling = !state.inUse && !ready;
        gameObject.SetActive(true);
        icon.sprite = state.icon;
        label.text = state.label;
        PaintRing(state, cooling);
        value.text = state.inUse ? "NOW" : ready ? "READY" : $"{state.remaining:0.0}s";
        value.color = cooling ? NeonPalette.Get(PaletteColor.Text) : NeonPalette.Get(boss.main);
        stroke.color = NeonPalette.Get(cooling ? PaletteColor.Line2 : boss.main);
    }

    public void Hide() => gameObject.SetActive(false);

    private void PaintRing(CooldownTileState state, bool cooling)
    {
        Color main = NeonPalette.Get(boss.main);
        ringTrack.color = state.inUse ? main : NeonPalette.Get(PaletteColor.Line2);
        ringFill.fillAmount = cooling ? state.progress : 1f;
        ringFill.color = main;
        ringInner.enabled = !state.inUse;
        icon.color = state.inUse ? NeonPalette.Get(PaletteColor.Ink) : cooling ? NeonPalette.Get(PaletteColor.Text2) : main;
    }
}
