using UnityEngine;

// A boss's danger-zone ring (Marauder's shockwave, Halcyon's Static Field)
// drawn with the Combat FX sprites. The owning mechanic reports its state
// every frame; this picks the frame and scales the sprite so its drawn ring
// sits exactly on the gameplay radius.
[RequireComponent(typeof(SpriteRenderer))]
public class DangerZoneVisual : MonoBehaviour
{
    public Sprite idle;
    public Sprite telegraphA;
    public Sprite telegraphB;
    public Sprite impact;
    public Sprite afterImpact;
    [Range(0f, 1f)] public float idleAlpha = 1f;
    public float ringRadiusPx = 183f;
    public float telegraphSwapSpeed = 12f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Show(DangerZoneState state, float radiusWorld)
    {
        sr.sprite = SpriteFor(state);
        sr.color = WhiteWithAlpha(state == DangerZoneState.Idle ? idleAlpha : 1f);
        FxSprites.FitRingRadius(transform, ringRadiusPx, radiusWorld);
    }

    private Sprite SpriteFor(DangerZoneState state)
    {
        switch (state)
        {
            case DangerZoneState.Telegraph: return TelegraphOnB() && telegraphB != null ? telegraphB : telegraphA;
            case DangerZoneState.Impact: return impact;
            case DangerZoneState.AfterImpact: return afterImpact != null ? afterImpact : idle;
            default: return idle;
        }
    }

    private bool TelegraphOnB() => Mathf.Sin(Time.time * telegraphSwapSpeed) > 0f;

    private static Color WhiteWithAlpha(float alpha)
    {
        Color color = Color.white;
        color.a = alpha;
        return color;
    }
}
