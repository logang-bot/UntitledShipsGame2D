using System;

// The layers of one UI kit frame, back to front: glow, flat fill, 135°
// hatch, top-down gradient, then a 1px stroke (solid or dashed). A layer
// whose swatch alpha is 0 is hidden.
[Serializable]
public struct NeonFrameLook
{
    public PaletteSwatch glow;
    public PaletteSwatch fill;
    public PaletteSwatch hatch;
    public PaletteSwatch gradient;
    public PaletteSwatch stroke;
    public bool dashed;
}
