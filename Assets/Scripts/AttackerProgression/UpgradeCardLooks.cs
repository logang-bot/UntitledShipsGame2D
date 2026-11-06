// The level-up card's frame, keycap and opacity per state (design: HUD
// Upgrade Card): Attacker-dim frame at rest, orange frame with glow under
// focus, solid orange once picked, 55% while input is locked and 30% while
// another card's pick confirms.
public static class UpgradeCardLooks
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);
    private const float FocusGlowAlpha = 0.22f;
    private const float FocusGradientAlpha = 0.18f;
    private const float PickedGlowAlpha = 0.3f;

    public static NeonFrameLook Frame(UpgradeCardState state)
    {
        switch (state)
        {
            case UpgradeCardState.Focus: return Focus;
            case UpgradeCardState.Picked: return Picked;
            default: return Rest;
        }
    }

    public static float Opacity(UpgradeCardState state)
    {
        switch (state)
        {
            case UpgradeCardState.Locked: return 0.55f;
            case UpgradeCardState.Dim: return 0.3f;
            default: return 1f;
        }
    }

    public static PaletteColor KeyFill(UpgradeCardState state)
    {
        switch (state)
        {
            case UpgradeCardState.Focus: return PaletteColor.Text;
            case UpgradeCardState.Picked: return PaletteColor.Ink;
            default: return PaletteColor.HatchLight;
        }
    }

    public static PaletteColor KeyStroke(UpgradeCardState state)
    {
        switch (state)
        {
            case UpgradeCardState.Focus: return PaletteColor.Text;
            case UpgradeCardState.Picked: return PaletteColor.Ink;
            case UpgradeCardState.Locked: return PaletteColor.Line2;
            default: return PaletteColor.Slate;
        }
    }

    public static PaletteColor KeyInk(UpgradeCardState state)
    {
        switch (state)
        {
            case UpgradeCardState.Focus: return PaletteColor.Ink;
            case UpgradeCardState.Picked: return PaletteColor.Attacker;
            case UpgradeCardState.Locked: return PaletteColor.Muted;
            default: return PaletteColor.Text;
        }
    }

    private static NeonFrameLook Rest => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.AttackerBg), stroke = S(PaletteColor.AttackerDim),
    };

    private static NeonFrameLook Focus => new NeonFrameLook
    {
        glow = new PaletteSwatch(PaletteColor.Attacker, FocusGlowAlpha), fill = S(PaletteColor.Deep), hatch = Off,
        gradient = new PaletteSwatch(PaletteColor.Attacker, FocusGradientAlpha), stroke = S(PaletteColor.Attacker),
    };

    private static NeonFrameLook Picked => new NeonFrameLook
    {
        glow = new PaletteSwatch(PaletteColor.Attacker, PickedGlowAlpha), fill = S(PaletteColor.Attacker), hatch = Off,
        gradient = Off, stroke = S(PaletteColor.Attacker),
    };

    private static PaletteSwatch S(PaletteColor key) => new PaletteSwatch(key);
}
