using UnityEngine;

// Idle drift for a menu ship: eases back and forth along a small diagonal
// with a slight tilt, like the design's 'drift' keyframes (translate 8px,
// -16px and rotate ±1.5° over 5-7 s, alternating). Runs on unscaled time.
public class MenuShipDrift : MonoBehaviour
{
    public Vector2 driftOffset = new Vector2(0.08f, 0.16f);
    public float tiltDegrees = 1.5f;
    public float periodSeconds = 6f;
    public float phaseSeconds;

    private Vector3 home;

    void Awake() => home = transform.localPosition;

    void Update()
    {
        float swing = DriftSwing(Time.unscaledTime + phaseSeconds);
        transform.localPosition = home + (Vector3)(driftOffset * swing);
        transform.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees * (2f * swing - 1f));
    }

    /// <summary>
    /// Ease-in-out ping-pong in [0, 1]: one period goes 0 → 1 → 0, shaped by
    /// (1 - cos(πu)) / 2 per half so it slows at both ends like CSS
    /// "ease-in-out alternate".
    /// </summary>
    private float DriftSwing(float time)
    {
        float u = Mathf.PingPong(time * 2f / periodSeconds, 1f);
        return (1f - Mathf.Cos(Mathf.PI * u)) * 0.5f;
    }
}
