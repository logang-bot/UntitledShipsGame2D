using UnityEngine;

// Sibling component on the Boss GameObject: tints the boss sprite so the
// Surge window reads as an opening to attack, not a threat - pulses toward
// openingColor during the telegraph, holds it solid while active. Purely
// visual; only reads HalcyonSurge's flags.
public class HalcyonSurgeTint : MonoBehaviour
{
    public Color openingColor = new Color(0.35f, 1f, 0.45f);
    public float telegraphPulseSpeed = 12f;

    private SpriteRenderer sr;
    private HalcyonSurge surge;
    private Color baseColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        surge = GetComponent<HalcyonSurge>();
        if (sr != null) baseColor = sr.color;
    }

    void LateUpdate()
    {
        if (sr == null || surge == null) return;
        sr.color = CurrentTint();
    }

    private Color CurrentTint()
    {
        if (surge.IsActive) return openingColor;
        if (!surge.IsTelegraphing) return baseColor;
        float pulse = (Mathf.Sin(Time.time * telegraphPulseSpeed) + 1f) * 0.5f;
        return Color.Lerp(baseColor, openingColor, pulse);
    }
}
