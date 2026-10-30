// The Warden arm card frames from the HUD Boss Panel design, one per
// ArmStatus, built from the boss's color tokens.
public static class ArmCardLooks
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);

    public static NeonFrameLook For(ArmStatus status, BossTokens boss) => status switch
    {
        ArmStatus.Off => new NeonFrameLook { glow = Off, fill = S(PaletteColor.PanelDark), hatch = Off, gradient = Off, stroke = S(PaletteColor.Slate), dashed = true },
        ArmStatus.Idle => new NeonFrameLook { glow = Off, fill = S(PaletteColor.HatchLight), hatch = Off, gradient = Off, stroke = S(boss.dim) },
        ArmStatus.Lock => new NeonFrameLook { glow = new PaletteSwatch(boss.main, boss.glowAlpha), fill = S(boss.bg), hatch = Off, gradient = Off, stroke = S(boss.main) },
        _ => new NeonFrameLook { glow = Off, fill = S(PaletteColor.HatchDark), hatch = S(PaletteColor.HatchLight), gradient = Off, stroke = S(PaletteColor.Slate) },
    };

    private static PaletteSwatch S(PaletteColor key) => new PaletteSwatch(key);
}
