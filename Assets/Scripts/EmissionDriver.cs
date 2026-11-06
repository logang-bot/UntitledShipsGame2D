using UnityEngine;

// Scales one renderer's ShipMesh emission per frame without touching the
// shared material: a MaterialPropertyBlock read before it's written, so the
// hit flash ShipVisual sets on the same renderer survives.
public class EmissionDriver
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly Renderer target;
    private readonly Color baseEmission;
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    public EmissionDriver(Renderer target)
    {
        this.target = target;
        baseEmission = target.sharedMaterial.GetColor(EmissionColorId);
    }

    public void Scale(float factor)
    {
        target.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, baseEmission * factor);
        target.SetPropertyBlock(block);
    }
}
