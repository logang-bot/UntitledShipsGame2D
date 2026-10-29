using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small UI kit badge (▸ FOCUS, ◤ YOU · P1, ◇ CPU, ✓ LOCKED, TAKEN · P2,
// role tags): shows whatever TagChipLook it's given.
public class TagChip : MonoBehaviour
{
    private static readonly PaletteSwatch Hidden = new PaletteSwatch(PaletteColor.Bg, 0f);

    public Image fill;
    public Image stroke;
    public Image dashedStroke;
    public Image icon;
    public TMP_Text label;
    public Sprite checkSprite;
    public Sprite cornerSprite;

    public void Show(TagChipLook look)
    {
        label.text = look.text;
        label.color = look.textColor;
        Paint(fill, look.fill);
        Paint(stroke, look.dashed ? Hidden : look.stroke);
        Paint(dashedStroke, look.dashed ? look.stroke : Hidden);
        ShowGlyph(look);
    }

    private void ShowGlyph(TagChipLook look)
    {
        icon.gameObject.SetActive(look.glyph != TagChipGlyph.None);
        icon.sprite = look.glyph == TagChipGlyph.Check ? checkSprite : cornerSprite;
        icon.color = look.textColor;
    }

    private static void Paint(Image layer, PaletteSwatch swatch)
    {
        layer.color = swatch;
        layer.enabled = swatch.alpha > 0f;
    }
}
