using UnityEngine;

// Draws the Nova Beam's body: a quad stretched from the muzzle to the top of
// the screen, running the AttackerBeam shader. Pushes the palette colors and
// the live intensity into it through a MaterialPropertyBlock.
public class AttackerBeamVisual : MonoBehaviour
{
    private const int AbilityEffectSortingOrder = -2;
    private static readonly int CoreId = Shader.PropertyToID("_CoreColor");
    private static readonly int EdgeAId = Shader.PropertyToID("_EdgeColorA");
    private static readonly int EdgeBId = Shader.PropertyToID("_EdgeColorB");
    private static readonly int EdgeCId = Shader.PropertyToID("_EdgeColorC");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int LengthId = Shader.PropertyToID("_Length");

    public MeshRenderer body;
    /// <summary>The glow reaches past the damaging width; the white core stays inside it.</summary>
    public float glowWidthScale = 1.5f;
    /// <summary>HDR multiplier so the bloom (threshold 1) catches the beam.</summary>
    public float coreBrightness = 1.3f;
    public float edgeBrightness = 1.6f;
    public PaletteSwatch coreColor = new PaletteSwatch(PaletteColor.BeamCore);
    public PaletteSwatch edgeColorA = new PaletteSwatch(PaletteColor.Attacker);
    public PaletteSwatch edgeColorB = new PaletteSwatch(PaletteColor.BeamAmber);
    public PaletteSwatch edgeColorC = new PaletteSwatch(PaletteColor.BeamRedGold);

    private MaterialPropertyBlock block;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        body.sortingOrder = AbilityEffectSortingOrder;
        block.SetColor(CoreId, coreColor.Resolve() * coreBrightness);
        block.SetColor(EdgeAId, edgeColorA.Resolve() * edgeBrightness);
        block.SetColor(EdgeBId, edgeColorB.Resolve() * edgeBrightness);
        block.SetColor(EdgeCId, edgeColorC.Resolve() * edgeBrightness);
        SetIntensity(0f);
    }

    public void Fit(float width, float length)
    {
        body.transform.localScale = new Vector3(width * glowWidthScale, length, 1f);
        body.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
        block.SetFloat(LengthId, length);
    }

    public void SetIntensity(float intensity)
    {
        block.SetFloat(IntensityId, intensity);
        body.SetPropertyBlock(block);
    }
}
