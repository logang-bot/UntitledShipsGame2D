// The tag chips from the Pre-Fight Menus design's role select and level
// select screens.
public static class TagChipLooks
{
    private static readonly PaletteSwatch Off = new PaletteSwatch(PaletteColor.Bg, 0f);

    public static TagChipLook RoleTag(PlayerRole role) => Filled(RoleCardText.Tag(role), RolePalette.TokenFor(role));

    public static TagChipLook Focus => new TagChipLook
    {
        text = "▸ FOCUS", fill = Off, stroke = S(PaletteColor.Text), textColor = S(PaletteColor.Text),
    };

    public static TagChipLook Cpu => new TagChipLook
    {
        text = "◇ CPU", fill = Off, stroke = S(PaletteColor.Muted), textColor = S(PaletteColor.Text2), dashed = true,
    };

    public static TagChipLook You(PlayerRole role, int player)
    {
        TagChipLook look = Filled($"YOU · P{player}", RolePalette.TokenFor(role));
        look.glyph = TagChipGlyph.Corner;
        return look;
    }

    public static TagChipLook Locked(PlayerRole role)
    {
        TagChipLook look = Filled("LOCKED", RolePalette.TokenFor(role));
        look.glyph = TagChipGlyph.Check;
        return look;
    }

    public static TagChipLook Hover(int player) => Filled($"▸ P{player}", PaletteColor.Text);

    public static TagChipLook Player(PlayerRole role, int player) => Filled($"P{player}", RolePalette.TokenFor(role));

    public static TagChipLook Taken(int player) => new TagChipLook
    {
        text = $"TAKEN · P{player}", fill = S(PaletteColor.HatchLight), stroke = S(PaletteColor.Slate), textColor = S(PaletteColor.Text2),
    };

    private static TagChipLook Filled(string text, PaletteColor color) => new TagChipLook
    {
        text = text, fill = S(color), stroke = S(color), textColor = S(PaletteColor.Ink),
    };

    private static PaletteSwatch S(PaletteColor key) => new PaletteSwatch(key);
}
