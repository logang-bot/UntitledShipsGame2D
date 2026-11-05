using TMPro;
using UnityEngine;

// The design's neon text-shadow (wordmark, mode and player titles): a soft
// TMP underlay in a palette color. The screen-space UI isn't touched by the
// camera's bloom, so this glow is what makes the big titles read as lit.
// Runs at play time only; it gives the text its own material instance.
[RequireComponent(typeof(TMP_Text))]
public class TextGlow : MonoBehaviour
{
    public PaletteSwatch color = new PaletteSwatch(PaletteColor.Tank, 0.55f);
    [Range(0f, 1f)] public float softness = 0.35f;
    [Range(-1f, 1f)] public float dilate;

    void Awake()
    {
        TMP_Text text = GetComponent<TMP_Text>();
        Material material = text.fontMaterial;
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, color);
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, dilate);
        text.UpdateMeshPadding();
    }
}
