using System;
using NUnit.Framework;
using UnityEngine;

public class NeonPaletteTests
{
    [Test]
    public void Get_EveryToken_HasAValue()
    {
        foreach (PaletteColor key in Enum.GetValues(typeof(PaletteColor)))
            Assert.IsTrue(NeonPalette.Contains(key), $"{key} has no palette value");
    }

    [Test]
    public void Get_WithAlpha_KeepsRgbAndSetsAlpha()
    {
        Color full = NeonPalette.Get(PaletteColor.Tank);
        Color faded = NeonPalette.Get(PaletteColor.Tank, 0.25f);
        Assert.AreEqual(new Color(full.r, full.g, full.b, 0.25f), faded);
    }

    [TestCase(PlayerRole.Attacker, "FF8A3D")]
    [TestCase(PlayerRole.Tank, "00E5FF")]
    [TestCase(PlayerRole.Medic, "B6FF3C")]
    [TestCase(PlayerRole.Support, "A855F7")]
    public void RoleTint_MatchesDesignRoleColor(PlayerRole role, string hex)
    {
        Assert.AreEqual(hex, ColorUtility.ToHtmlStringRGB(PlayerRoleStats.Get(role).tintColor));
    }

    [Test]
    public void RichTextHex_IsHashPrefixedRgb()
    {
        Assert.AreEqual("#A8A8B8", NeonPalette.RichTextHex(PaletteColor.HudLabel));
    }

    [Test]
    public void RolePalette_FillIsFainterThanGlow()
    {
        RoleColors family = RolePalette.For(PlayerRole.Medic);
        Assert.Less(family.fill.a, family.glow.a);
    }
}
