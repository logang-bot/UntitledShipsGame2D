using UnityEngine;

// Shared helpers for the Combat FX sprites, which are all exported at the
// design's 192 px per world unit (a 1080 px playfield is 5.625 units).
public static class FxSprites
{
    public const float PixelsPerUnit = 192f;
    private const string UnlitShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";

    public static SpriteRenderer CreateChild(Transform parent, string name, int sortingOrder)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        Shader shader = Shader.Find(UnlitShader);
        if (shader != null) sr.material = new Material(shader);
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    /// <summary>
    /// Scales <paramref name="ring"/> so a ring drawn ringRadiusPx from the
    /// sprite's centre lands on radiusWorld, undoing the parent's scale
    /// (ships are scaled 0.6).
    /// </summary>
    public static void FitRingRadius(Transform ring, float ringRadiusPx, float radiusWorld)
    {
        float spriteRadius = ringRadiusPx / PixelsPerUnit;
        ring.localScale = Vector3.one * (radiusWorld / spriteRadius / ParentScale(ring));
    }

    public static void UndoParentScale(Transform child)
    {
        child.localScale = Vector3.one / ParentScale(child);
    }

    private static float ParentScale(Transform child)
    {
        float scale = child.parent != null ? child.parent.lossyScale.x : 1f;
        return scale > 0f ? scale : 1f;
    }
}
