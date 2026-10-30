using UnityEngine;
using UnityEngine.UI;

// A UI kit frame (panel, card, slot): colors its layer Images from a
// NeonFrameLook. Built as separate Images so the glow can be tinted and
// faded on its own, per the design's 9-slice notes. Applies in the editor
// too, so a frame's look is edited in the Inspector by token.
[ExecuteAlways]
public class NeonFrame : MonoBehaviour
{
    public NeonFrameLook look = NeonFrameLooks.Panel;

    [Header("Layers")]
    public Image glow;
    public Image fill;
    public Image hatch;
    public Image gradient;
    public Image stroke;
    public Image dashedStroke;

    void OnEnable() => Apply();

    void OnValidate() => Apply();

    public void SetLook(NeonFrameLook newLook)
    {
        look = newLook;
        Apply();
    }

    public void Apply()
    {
        Paint(glow, look.glow);
        Paint(fill, look.fill);
        Paint(hatch, look.hatch);
        Paint(gradient, look.gradient);
        Paint(stroke, look.dashed ? Hidden : look.stroke);
        Paint(dashedStroke, look.dashed ? look.stroke : Hidden);
    }

    private static readonly PaletteSwatch Hidden = new PaletteSwatch(PaletteColor.Bg, 0f);

    private static void Paint(Image layer, PaletteSwatch swatch)
    {
        if (layer == null) return;
        layer.color = swatch;
        layer.enabled = swatch.alpha > 0f;
    }
}
