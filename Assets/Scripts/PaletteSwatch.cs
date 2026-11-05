using System;
using UnityEngine;

// An Inspector-editable reference to a NeonPalette token plus an alpha, used
// in place of a raw Color field so prefabs and scenes never store a color
// value of their own. Converts implicitly to Color, so call sites read the
// same as before.
[Serializable]
public struct PaletteSwatch
{
    public PaletteColor key;
    [Range(0f, 1f)] public float alpha;

    public PaletteSwatch(PaletteColor key, float alpha = 1f)
    {
        this.key = key;
        this.alpha = alpha;
    }

    public Color Resolve() => NeonPalette.Get(key, alpha);

    public static implicit operator Color(PaletteSwatch swatch) => swatch.Resolve();
}
