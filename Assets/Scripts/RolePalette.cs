using System.Collections.Generic;

// Each role's color family, assembled from NeonPalette tokens: the role
// color, its dim stroke and dark card fill, plus glow/fill at the design's
// per-role alphas.
public static class RolePalette
{
    private const float FillAlphaBelowGlow = 0.12f;

    private static readonly Dictionary<PlayerRole, RoleColors> families = new Dictionary<PlayerRole, RoleColors>
    {
        { PlayerRole.Attacker, Family(PlayerRole.Attacker, 0.32f) },
        { PlayerRole.Tank, Family(PlayerRole.Tank, 0.30f) },
        { PlayerRole.Medic, Family(PlayerRole.Medic, 0.28f) },
        { PlayerRole.Support, Family(PlayerRole.Support, 0.32f) },
    };

    public static RoleColors For(PlayerRole role) => families[role];

    public static PaletteColor TokenFor(PlayerRole role) => role switch
    {
        PlayerRole.Attacker => PaletteColor.Attacker,
        PlayerRole.Tank => PaletteColor.Tank,
        PlayerRole.Medic => PaletteColor.Medic,
        _ => PaletteColor.Support,
    };

    public static PaletteColor DimTokenFor(PlayerRole role) => role switch
    {
        PlayerRole.Attacker => PaletteColor.AttackerDim,
        PlayerRole.Tank => PaletteColor.TankDim,
        PlayerRole.Medic => PaletteColor.MedicDim,
        _ => PaletteColor.SupportDim,
    };

    public static PaletteColor BgTokenFor(PlayerRole role) => role switch
    {
        PlayerRole.Attacker => PaletteColor.AttackerBg,
        PlayerRole.Tank => PaletteColor.TankBg,
        PlayerRole.Medic => PaletteColor.MedicBg,
        _ => PaletteColor.SupportBg,
    };

    private static RoleColors Family(PlayerRole role, float glowAlpha) => new RoleColors
    {
        main = NeonPalette.Get(TokenFor(role)),
        dim = NeonPalette.Get(DimTokenFor(role)),
        bg = NeonPalette.Get(BgTokenFor(role)),
        glow = NeonPalette.Get(TokenFor(role), glowAlpha),
        fill = NeonPalette.Get(TokenFor(role), glowAlpha - FillAlphaBelowGlow),
    };
}
