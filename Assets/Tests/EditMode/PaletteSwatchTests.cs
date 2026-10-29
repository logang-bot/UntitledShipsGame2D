using NUnit.Framework;
using UnityEngine;

public class PaletteSwatchTests
{
    [Test]
    public void Resolve_AppliesTokenAndAlpha()
    {
        Color resolved = new PaletteSwatch(PaletteColor.Boss, 0.4f).Resolve();
        Assert.AreEqual(NeonPalette.Get(PaletteColor.Boss, 0.4f), resolved);
    }

    [Test]
    public void Constructor_DefaultsToOpaque()
    {
        Assert.AreEqual(1f, new PaletteSwatch(PaletteColor.Text).alpha);
    }

    [Test]
    public void ImplicitConversion_MatchesResolve()
    {
        PaletteSwatch swatch = new PaletteSwatch(PaletteColor.Notice, 0.6f);
        Color converted = swatch;
        Assert.AreEqual(swatch.Resolve(), converted);
    }
}
