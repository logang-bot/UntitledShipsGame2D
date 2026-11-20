using UnityEngine;
using UnityEngine.UI;

// Colors the sibling Graphic (Image, RawImage, TMP text), SpriteRenderer,
// Camera background or ParticleSystem start color from a NeonPalette token, so scene and prefab colors are
// set by name instead of by value. Applies in the editor too (OnValidate),
// so what the Scene view shows is what the palette says.
[ExecuteAlways]
public class PaletteTint : MonoBehaviour
{
    public PaletteSwatch swatch = new PaletteSwatch(PaletteColor.Text);

    private void Awake() => Apply();

    private void OnValidate() => Apply();

    public void Set(PaletteSwatch newSwatch)
    {
        swatch = newSwatch;
        Apply();
    }

    public void Apply()
    {
        Color color = swatch;
        if (TryGetComponent(out Graphic graphic)) graphic.color = color;
        if (TryGetComponent(out SpriteRenderer sprite)) sprite.color = color;
        if (TryGetComponent(out Camera cam)) cam.backgroundColor = color;
        if (TryGetComponent(out ParticleSystem particles)) SetStartColor(particles, color);
    }

    private static void SetStartColor(ParticleSystem particles, Color color)
    {
        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
    }
}
