using System.Collections.Generic;

// The Pre-Fight Menus design's "Menu Button" state table, per variant.
// Focus shares Hover's colors; MenuButton adds the white outline and the
// marker on top.
public static class MenuButtonStyles
{
    private static readonly PaletteSwatch NoGlow = new PaletteSwatch(PaletteColor.Bg, 0f);

    private static readonly Dictionary<MenuButtonState, MenuButtonLook> primary = new Dictionary<MenuButtonState, MenuButtonLook>
    {
        { MenuButtonState.Normal, new MenuButtonLook { fill = S(PaletteColor.TankBg), stroke = S(PaletteColor.Tank), text = S(PaletteColor.Text), glow = S(PaletteColor.Tank, 0.15f) } },
        { MenuButtonState.Hover, new MenuButtonLook { fill = S(PaletteColor.TankHover), stroke = S(PaletteColor.Tank), text = S(PaletteColor.White), glow = S(PaletteColor.Tank, 0.4f) } },
        { MenuButtonState.Pressed, new MenuButtonLook { fill = S(PaletteColor.Tank), stroke = S(PaletteColor.Tank), text = S(PaletteColor.Ink), glow = S(PaletteColor.Tank, 0.6f) } },
        { MenuButtonState.Disabled, new MenuButtonLook { fill = S(PaletteColor.PanelDark), stroke = S(PaletteColor.Line2), text = S(PaletteColor.TextDisabled), glow = NoGlow } },
    };

    private static readonly Dictionary<MenuButtonState, MenuButtonLook> secondary = new Dictionary<MenuButtonState, MenuButtonLook>
    {
        { MenuButtonState.Normal, new MenuButtonLook { fill = S(PaletteColor.HatchDark, 0.6f), stroke = S(PaletteColor.Slate), text = S(PaletteColor.Text2), glow = NoGlow } },
        { MenuButtonState.Hover, new MenuButtonLook { fill = S(PaletteColor.HatchLight), stroke = S(PaletteColor.Text2), text = S(PaletteColor.Text), glow = NoGlow } },
        { MenuButtonState.Pressed, new MenuButtonLook { fill = S(PaletteColor.Line), stroke = S(PaletteColor.Text2), text = S(PaletteColor.Text), glow = NoGlow } },
        { MenuButtonState.Disabled, new MenuButtonLook { fill = S(PaletteColor.Bg, 0f), stroke = S(PaletteColor.Line), text = S(PaletteColor.Muted), glow = NoGlow } },
    };

    public static MenuButtonLook For(MenuButtonVariant variant, MenuButtonState state)
    {
        MenuButtonState lookup = state == MenuButtonState.Focus ? MenuButtonState.Hover : state;
        return variant == MenuButtonVariant.Primary ? primary[lookup] : secondary[lookup];
    }

    private static PaletteSwatch S(PaletteColor key, float alpha = 1f) => new PaletteSwatch(key, alpha);
}
