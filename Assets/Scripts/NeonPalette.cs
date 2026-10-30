using System.Collections.Generic;
using UnityEngine;

// The project's single source of truth for color. No other script, prefab
// or scene holds a color literal: code calls Get(), Inspector fields use a
// PaletteSwatch, and scene Graphics/SpriteRenderers/Cameras use PaletteTint.
// UI tokens come from the "Pre-Fight Menus" design's colour table
// (docs/systems/ui-kit.md); role and boss colors match the ship and boss
// designs (docs/art-roadmap.md).
public static class NeonPalette
{
    private static readonly Dictionary<PaletteColor, Color> colors = new Dictionary<PaletteColor, Color>
    {
        { PaletteColor.Bg, Hex(0x04050A) },
        { PaletteColor.Panel, Hex(0x080B14) },
        { PaletteColor.Line, Hex(0x16203A) },
        { PaletteColor.Line2, Hex(0x1D2A45) },
        { PaletteColor.Slate, Hex(0x2E3A55) },
        { PaletteColor.Muted, Hex(0x3A4868) },
        { PaletteColor.Text, Hex(0xE9F4FF) },
        { PaletteColor.Text2, Hex(0x8FA3C8) },
        { PaletteColor.Text3, Hex(0x5C6E90) },
        { PaletteColor.Text4, Hex(0x6E86AC) },
        { PaletteColor.TextSoft, Hex(0xB7C7DE) },
        { PaletteColor.Ink, Hex(0x04050A) },
        { PaletteColor.White, Hex(0xFFFFFF) },
        { PaletteColor.Notice, Hex(0xFFB627) },
        { PaletteColor.NoticeSoft, Hex(0xFFCE7A) },
        { PaletteColor.NoticeBg, Hex(0x140E04) },
        { PaletteColor.Boss, Hex(0xFF2D95) },
        { PaletteColor.BossSoft, Hex(0xFF7ABE) },
        { PaletteColor.BossPale, Hex(0xFFE3F1) },
        { PaletteColor.BossLine, Hex(0x5A1E40) },

        { PaletteColor.Attacker, Hex(0xFF8A3D) },
        { PaletteColor.Tank, Hex(0x00E5FF) },
        { PaletteColor.Medic, Hex(0xB6FF3C) },
        { PaletteColor.Support, Hex(0xA855F7) },
        { PaletteColor.MedicSoft, Hex(0xDFFF9C) },
        { PaletteColor.AttackerDim, Hex(0x4A2810) },
        { PaletteColor.TankDim, Hex(0x0E3A48) },
        { PaletteColor.MedicDim, Hex(0x2F4214) },
        { PaletteColor.SupportDim, Hex(0x3A1F5C) },
        { PaletteColor.AttackerBg, Hex(0x1A0E08) },
        { PaletteColor.TankBg, Hex(0x0A1826) },
        { PaletteColor.MedicBg, Hex(0x111A0C) },
        { PaletteColor.SupportBg, Hex(0x150C22) },

        { PaletteColor.Marauder, Hex(0xFF2D95) },
        { PaletteColor.Halcyon, Hex(0xFFE14A) },
        { PaletteColor.Warden, Hex(0x5470FF) },

        { PaletteColor.ShockwaveRing, Hex(0xFF661A) },
        { PaletteColor.ShockwaveTelegraph, Hex(0xFF261A) },
        { PaletteColor.ShockwaveImpact, Hex(0xFFE64D) },
        { PaletteColor.StaticFieldRing, Hex(0x4D99FF) },
        { PaletteColor.StaticFieldTelegraph, Hex(0x80D9FF) },
        { PaletteColor.StaticFieldImpact, Hex(0xE6F2FF) },
        { PaletteColor.ExplosiveTint, Hex(0xFF731A) },

        { PaletteColor.HudPanel, Hex(0x0D0D14) },
        { PaletteColor.HudLabel, Hex(0xA8A8B8) },
        { PaletteColor.HudBarFallback, Hex(0x808CA6) },
        { PaletteColor.HudInactive, Hex(0x808080) },

        { PaletteColor.GridLine, Hex(0x0E1B33) },
        { PaletteColor.Vignette, Hex(0x0A1428) },
        { PaletteColor.Deep, Hex(0x06070D) },
        { PaletteColor.HatchLight, Hex(0x0A0F1B) },
        { PaletteColor.HatchDark, Hex(0x06080F) },
        { PaletteColor.PanelDark, Hex(0x070A11) },
        { PaletteColor.SlotTop, Hex(0x0C1424) },
        { PaletteColor.SlotDim, Hex(0x111A2C) },
        { PaletteColor.TankHover, Hex(0x0E2C42) },
        { PaletteColor.TextDisabled, Hex(0x4A5A7A) },
        { PaletteColor.BossBg, Hex(0x120914) },
        { PaletteColor.BossDim, Hex(0x3A1530) },
        { PaletteColor.NoticeLine, Hex(0x3A2A0C) },
        { PaletteColor.NoticeDark, Hex(0x100B04) },
        { PaletteColor.SlotBottom, Hex(0x070A12) },
        { PaletteColor.BossVignette, Hex(0x140A18) },
        { PaletteColor.Text5, Hex(0x3F5170) },
        { PaletteColor.BarTrack, Hex(0x0D1524) },
        { PaletteColor.SurgeOpening, Hex(0x59FF73) },
        { PaletteColor.SurgeTelegraphText, Hex(0xFFE64D) },
        { PaletteColor.WardenArmDown, Hex(0x595959) },
        { PaletteColor.WardenBigShotTelegraph, Hex(0xFF5926) },

        { PaletteColor.Shield, Hex(0x9CCBFF) },
        { PaletteColor.SupportLight, Hex(0xC79BF7) },
        { PaletteColor.HalcyonDim, Hex(0x3D3510) },
        { PaletteColor.HalcyonBg, Hex(0x14120A) },
        { PaletteColor.WardenDim, Hex(0x1C2552) },
        { PaletteColor.WardenBg, Hex(0x0A0D1E) },
    };

    public static Color Get(PaletteColor key) => colors[key];

    public static Color Get(PaletteColor key, float alpha)
    {
        Color c = colors[key];
        c.a = alpha;
        return c;
    }

    public static string RichTextHex(PaletteColor key) => "#" + ColorUtility.ToHtmlStringRGB(colors[key]);

    public static bool Contains(PaletteColor key) => colors.ContainsKey(key);

    private static Color Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 0xFF);
}
