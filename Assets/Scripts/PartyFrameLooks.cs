// The party frame's looks from the HUD Party Frame design: local player
// (solid role stroke + glow), other human (dim role stroke), CPU (dashed
// slate), DOWN (slate), and the matching who-tag chips.
public static class PartyFrameLooks
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);
    private const float LocalGlowAlpha = 0.16f;

    public static NeonFrameLook Frame(PlayerRole role, FrameSeat seat) =>
        seat.isHuman ? Human(role, seat.isLocal) : NeonFrameLooks.Cpu;

    public static NeonFrameLook Down => new NeonFrameLook
    {
        glow = Off, fill = S(PaletteColor.Deep), hatch = Off, gradient = S(PaletteColor.HatchLight), stroke = S(PaletteColor.Slate),
    };

    public static TagChipLook WhoTag(PlayerRole role, FrameSeat seat)
    {
        if (!seat.isHuman) return Cpu(seat.label);
        return seat.isLocal ? Local(role, seat.label) : Outlined(role, seat.label);
    }

    private static NeonFrameLook Human(PlayerRole role, bool local) => new NeonFrameLook
    {
        glow = local ? new PaletteSwatch(RolePalette.TokenFor(role), LocalGlowAlpha) : Off,
        fill = S(PaletteColor.Deep),
        hatch = Off,
        gradient = S(RolePalette.BgTokenFor(role)),
        stroke = S(local ? RolePalette.TokenFor(role) : RolePalette.DimTokenFor(role)),
    };

    private static TagChipLook Local(PlayerRole role, string label) => new TagChipLook
    {
        text = label, glyph = TagChipGlyph.Corner, fill = S(RolePalette.TokenFor(role)), stroke = S(RolePalette.TokenFor(role)), textColor = S(PaletteColor.Ink),
    };

    private static TagChipLook Outlined(PlayerRole role, string label) => new TagChipLook
    {
        text = label, fill = Off, stroke = S(RolePalette.TokenFor(role)), textColor = S(RolePalette.TokenFor(role)),
    };

    private static TagChipLook Cpu(string label) => new TagChipLook
    {
        text = "◇ " + label, fill = Off, stroke = S(PaletteColor.Muted), textColor = S(PaletteColor.Text2), dashed = true,
    };

    private static PaletteSwatch S(PaletteColor key) => new PaletteSwatch(key);
}
