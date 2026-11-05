using UnityEngine;

// Flashes one part of a boss model when it takes damage, through
// ShipMesh.shader's _FlashColor, the same way the ships' PlayerDamageFlash
// does. node names the part: the whole model for Marauder and Halcyon,
// Warden's body, or one Warden arm. The boss's (or arm's) TakeDamage calls
// Flash only when the hit actually lands. Softer and shorter than the
// ships' flash, since the whole party shoots the boss constantly.
public class BossHitFlash : MonoBehaviour
{
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    public BossModel model;
    public string node;
    public PaletteSwatch flashColor = new PaletteSwatch(PaletteColor.White, 0.55f);
    public float flashDuration = 0.08f;

    public bool IsFlashing { get; private set; }

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float flashUntil;

    public void Flash()
    {
        flashUntil = Time.time + flashDuration;
        if (!IsFlashing) Apply(flashColor);
    }

    void LateUpdate()
    {
        if (IsFlashing && Time.time >= flashUntil) Apply(Color.clear);
    }

    private void Apply(Color color)
    {
        CacheOnce();
        IsFlashing = color.a > 0f;
        foreach (Renderer part in renderers)
        {
            part.GetPropertyBlock(block);
            block.SetColor(FlashColorId, color);
            part.SetPropertyBlock(block);
        }
    }

    private void CacheOnce()
    {
        if (renderers != null) return;
        block = new MaterialPropertyBlock();
        renderers = model.Node(node).GetComponentsInChildren<Renderer>(true);
    }
}
