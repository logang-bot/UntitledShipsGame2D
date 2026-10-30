// The panel frames from the Pre-Fight Menus design's UI kit ("01 · Panel
// frames"), plus the role-colored card frames built from RolePalette tokens.
public static class NeonFrameLooks
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);

    public static NeonFrameLook Panel => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.Panel), stroke = S(PaletteColor.Line),
    };

    public static NeonFrameLook Cpu => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.PanelDark), hatch = Off, gradient = Off, stroke = S(PaletteColor.Slate), dashed = true,
    };

    public static NeonFrameLook Locked => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.HatchDark), hatch = S(PaletteColor.HatchLight), gradient = Off, stroke = S(PaletteColor.Line2),
    };

    public static NeonFrameLook Joined => new NeonFrameLook
    {
        glow = S(PaletteColor.Text, 0.12f), fill = S(PaletteColor.SlotBottom), hatch = Off, gradient = S(PaletteColor.SlotTop), stroke = S(PaletteColor.Text),
    };

    public static NeonFrameLook OpenSlot => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.HatchDark, 0.82f), hatch = Off, gradient = Off, stroke = S(PaletteColor.Line),
    };

    public static NeonFrameLook Boss => new NeonFrameLook
    {
        glow = S(PaletteColor.Boss, 0.28f), fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.BossBg), stroke = S(PaletteColor.Boss),
    };

    public static NeonFrameLook BossIdle => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.BossBg), stroke = S(PaletteColor.BossDim),
    };

    public static NeonFrameLook Mode => new NeonFrameLook
    {
        glow = S(PaletteColor.Tank, 0.25f), fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.TankBg), stroke = S(PaletteColor.Tank),
    };

    private static PaletteSwatch S(PaletteColor key, float alpha = 1f) => new PaletteSwatch(key, alpha);
}
