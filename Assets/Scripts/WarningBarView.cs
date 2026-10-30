using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A full-width warning bar on the boss panel (design: HUD Boss Panel): solid
// boss color with ink text, a hazard edge and a draining base line - the only
// solid boss-color fill on the rail, so it can't be mistaken for a cooldown.
// Once the attack fires it flips to an outlined bar.
public class WarningBarView : MonoBehaviour
{
    public Image fill;
    public Image stroke;
    public Image hazard;
    public Image icon;
    public TMP_Text title;
    public TMP_Text sub;
    public TMP_Text timer;
    public Image drain;
    public RoleChipView target;

    private BossTokens boss;

    public void Bind(BossTokens tokens) => boss = tokens;

    public void Show(WarningRowState state)
    {
        gameObject.SetActive(true);
        PaintBar(state.fired);
        icon.sprite = state.icon;
        title.text = state.title;
        sub.text = state.sub;
        timer.text = $"{state.seconds:0.0}s";
        drain.fillAmount = Mathf.Clamp01(state.drain);
        if (state.target.HasValue) target.Show(state.target.Value);
        else target.Hide();
    }

    public void Hide() => gameObject.SetActive(false);

    private void PaintBar(bool fired)
    {
        Color main = NeonPalette.Get(boss.main);
        Color ink = fired ? main : NeonPalette.Get(PaletteColor.Ink);
        fill.color = fired ? NeonPalette.Get(PaletteColor.HatchDark) : main;
        stroke.color = main;
        hazard.color = fired ? NeonPalette.Get(boss.main, 0.5f) : NeonPalette.Get(PaletteColor.Ink, 0.6f);
        foreach (Graphic part in new Graphic[] { icon, title, sub, timer }) part.color = ink;
        drain.color = new Color(ink.r, ink.g, ink.b, 0.7f);
    }
}
