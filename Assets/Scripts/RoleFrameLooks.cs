// Role-colored frame looks for the role select cards and tiles and the
// squad chips, built from each role's NeonPalette tokens.
public static class RoleFrameLooks
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);

    public static NeonFrameLook Card(PlayerRole role, RoleCardState state)
    {
        if (state == RoleCardState.Cpu) return NeonFrameLooks.Cpu;
        if (state == RoleCardState.Normal) return Idle(role);
        return Lit(role, state == RoleCardState.You ? 0.36f : 0.3f);
    }

    public static NeonFrameLook Tile(PlayerRole role, RoleTileState state)
    {
        if (state == RoleTileState.Taken) return Taken;
        if (state == RoleTileState.Hover) return Lit(role, 0.22f);
        if (state == RoleTileState.Locked) return LockedTile(role);
        return Idle(role);
    }

    public static NeonFrameLook Chip(PlayerRole role, bool human) => human ? Lit(role, 0f) : NeonFrameLooks.Cpu;

    private static NeonFrameLook Taken => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.HatchDark), hatch = S(PaletteColor.HatchLight), gradient = Off, stroke = S(PaletteColor.Line),
    };

    private static NeonFrameLook Idle(PlayerRole role) => new NeonFrameLook
    {
        glow = Off,
        fill = S(PaletteColor.Deep),
        hatch = Off,
        gradient = S(RolePalette.BgTokenFor(role)),
        stroke = S(RolePalette.DimTokenFor(role)),
    };

    private static NeonFrameLook Lit(PlayerRole role, float glowAlpha)
    {
        NeonFrameLook look = Idle(role);
        look.glow = new PaletteSwatch(RolePalette.TokenFor(role), glowAlpha);
        look.stroke = S(RolePalette.TokenFor(role));
        return look;
    }

    private static NeonFrameLook LockedTile(PlayerRole role)
    {
        NeonFrameLook look = Lit(role, 0.3f);
        look.fill = S(RolePalette.BgTokenFor(role));
        look.gradient = new PaletteSwatch(RolePalette.TokenFor(role), 0.12f);
        return look;
    }

    private static PaletteSwatch S(PaletteColor key) => new PaletteSwatch(key);
}
