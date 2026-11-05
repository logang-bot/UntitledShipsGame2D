using System.Collections.Generic;
using UnityEngine;

// A boss's live 3D model (docs/art-roadmap.md's Phase 5), on the boss
// prefab's Visual child that holds the <Boss>Model prefab. Gives the boss's
// state scripts its parts by GLB node name: Show turns a part on or off, and
// Glow overrides a material's emission through a MaterialPropertyBlock, the
// same way ShipVisual drives the ships' hit flash. ResetGlow writes the
// material's own emission back rather than clearing the block, so a
// BossHitFlash on the same renderers survives it.
public class BossModel : MonoBehaviour
{
    /// <summary>The factor the boss materials were baked with: emission =
    /// color x intensity x GlowScale, so a Glow call matches the design's
    /// emissive intensities.</summary>
    public const float GlowScale = 1.6f;

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private readonly Dictionary<string, Transform> nodes = new Dictionary<string, Transform>();
    private readonly Dictionary<string, List<Renderer>> renderersByMaterial = new Dictionary<string, List<Renderer>>();
    private MaterialPropertyBlock block;

    void Awake() => IndexOnce();

    public Transform Node(string nodeName)
    {
        IndexOnce();
        return nodes[nodeName];
    }

    public void Show(string nodeName, bool visible)
    {
        GameObject part = Node(nodeName).gameObject;
        if (part.activeSelf != visible) part.SetActive(visible);
    }

    public void SetVisible(bool visible) => gameObject.SetActive(visible);

    public void Glow(string materialName, PaletteColor color, float intensity)
    {
        Color emission = NeonPalette.Get(color) * (intensity * GlowScale);
        foreach (Renderer part in RenderersUsing(materialName)) SetEmission(part, emission);
    }

    public void ResetGlow(string materialName)
    {
        foreach (Renderer part in RenderersUsing(materialName)) SetEmission(part, part.sharedMaterial.GetColor(EmissionId));
    }

    private void SetEmission(Renderer part, Color emission)
    {
        part.GetPropertyBlock(block);
        block.SetColor(EmissionId, emission);
        part.SetPropertyBlock(block);
    }

    private List<Renderer> RenderersUsing(string materialName)
    {
        IndexOnce();
        return renderersByMaterial[materialName];
    }

    private void IndexOnce()
    {
        if (block != null) return;
        block = new MaterialPropertyBlock();
        foreach (Transform part in GetComponentsInChildren<Transform>(true)) nodes[part.name] = part;
        foreach (Renderer part in GetComponentsInChildren<Renderer>(true)) IndexMaterials(part);
    }

    private void IndexMaterials(Renderer part)
    {
        foreach (Material material in part.sharedMaterials)
        {
            if (!renderersByMaterial.TryGetValue(material.name, out List<Renderer> users))
                renderersByMaterial[material.name] = users = new List<Renderer>();
            users.Add(part);
        }
    }
}
