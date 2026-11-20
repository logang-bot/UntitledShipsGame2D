using System;

// The Medic's Sanctuary tunables, one Inspector block on PlayerAbility.
// Placeholder numbers until a playtest; see
// docs/superpowers/specs/2026-11-19-medic-sanctuary-support-echo-gate-design.md.
[Serializable]
public class MedicSanctuarySettings
{
    /// <summary>Counted from the Q press, like every other ability.</summary>
    public float cooldown = 25f;
    public ZonePhaseTiming timing = new ZonePhaseTiming(6f);
    /// <summary>The dome's radius in world units (corner to centre of the drawn hexagon).</summary>
    public float radius = 2f;
    public float tickInterval = 0.25f;
    /// <summary>Enemy bullets inside fly at this fraction of their speed.</summary>
    public float bulletSpeedFactor = 0.4f;
    /// <summary>A CPU Medic casts once this many hurt allies are within the radius.</summary>
    public int hurtAlliesToCast = 2;
}
