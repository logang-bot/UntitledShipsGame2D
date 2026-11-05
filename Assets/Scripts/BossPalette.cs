// Each boss's color family, the boss-side twin of RolePalette. Values are
// the HUD Boss Panel design's signature / dim / bg per boss.
public static class BossPalette
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);

    public static BossTokens For(Level level) => level switch
    {
        Level.Level1 => new BossTokens { main = PaletteColor.Marauder, dim = PaletteColor.BossDim, bg = PaletteColor.BossBg, glowAlpha = 0.16f },
        Level.Level2 => new BossTokens { main = PaletteColor.Halcyon, dim = PaletteColor.HalcyonDim, bg = PaletteColor.HalcyonBg, glowAlpha = 0.14f },
        _ => new BossTokens { main = PaletteColor.Warden, dim = PaletteColor.WardenDim, bg = PaletteColor.WardenBg, glowAlpha = 0.18f },
    };

    public static string Kind(Level level) => $"{LevelCardText.FightType(level)} · {LevelCardText.Number(level)}";

    public static NeonFrameLook PanelLook(BossTokens boss) => new NeonFrameLook
    {
        glow = new PaletteSwatch(boss.main, boss.glowAlpha), fill = S(PaletteColor.Deep), hatch = Off, gradient = S(boss.bg), stroke = S(boss.main),
    };

    public static NeonFrameLook DefeatedLook => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.HatchLight), stroke = S(PaletteColor.Slate),
    };

    private static PaletteSwatch S(PaletteColor key) => new PaletteSwatch(key);
}
