// One small badge from the UI kit: its text, an optional leading glyph,
// fill, stroke (solid or dashed) and text color. A swatch with alpha 0
// hides that layer.
public struct TagChipLook
{
    public string text;
    public TagChipGlyph glyph;
    public PaletteSwatch fill;
    public PaletteSwatch stroke;
    public PaletteSwatch textColor;
    public bool dashed;
}
