using UnityEngine;

// One engine's flame for one frame, ported from the design's thrusters()
// (Ship Designs project, Ship Models 3D.html / ships3d.js): a flicker at
// cruise, longer, brighter and faster at boost. Scales are relative to the
// modelled plume; opacities and the glow factor multiply the materials'
// emission. Pure, so it's unit-tested (ThrusterPoseTests).
public struct ThrusterPose
{
    private const float CruiseRadiansPerSecond = 28f;
    private const float BoostRadiansPerSecond = 30f;
    private const float SecondWaveRatio = 2.7f;

    public float length;
    public float width;
    public float coreLength;
    public float outerOpacity;
    public float coreOpacity;
    public float glowFactor;

    /// <summary>
    /// The design's smooth flicker, 0.5·sin(ωt + s) + 0.3·sin(2.7·ωt + 3s),
    /// with ω = 28 + 30·boost rad/s (the design's 0.028 + 0.03·boost per ms).
    /// </summary>
    public static float Flicker(float timeSeconds, float seed, float boost)
    {
        float phase = timeSeconds * (CruiseRadiansPerSecond + BoostRadiansPerSecond * boost);
        return Mathf.Sin(phase + seed) * 0.5f + Mathf.Sin(phase * SecondWaveRatio + seed * 3f) * 0.3f;
    }

    /// <summary>Half-width of the design's per-frame random jitter, (rand − 0.5)·(0.12 + 0.2·boost).</summary>
    public static float NoiseAmplitude(float boost) => 0.06f + boost * 0.1f;

    /// <summary>The design's pose coefficients for a flicker value and an eased boost in [0, 1].</summary>
    public static ThrusterPose From(float flicker, float boost) => new ThrusterPose
    {
        length = 1f + boost * 1.4f + flicker * (0.14f + boost * 0.18f),
        width = 1f + boost * 0.18f + flicker * 0.04f,
        coreLength = 1f + boost * 0.5f,
        outerOpacity = 0.32f + boost * 0.3f + flicker * 0.05f,
        coreOpacity = 0.18f + boost * 0.5f + flicker * 0.06f,
        glowFactor = 1f + boost * 1.2f + flicker * 0.15f,
    };
}
