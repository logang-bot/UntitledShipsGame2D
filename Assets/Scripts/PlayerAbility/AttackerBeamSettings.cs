using System;

// The Attacker's Nova Beam tunables, grouped as one Inspector block on
// PlayerAbility. Placeholder numbers until a playtest; see
// docs/superpowers/specs/2026-11-12-attacker-nova-beam-cutin-design.md.
[Serializable]
public class AttackerBeamSettings
{
    /// <summary>Seconds between the E press and the beam (the cut-in's wind-up).</summary>
    public float windupSeconds = 0.8f;
    public float beamSeconds = 3f;
    /// <summary>Counted from the E press, like every other ability.</summary>
    public float cooldown = 20f;
    public float tickInterval = 0.1f;
    /// <summary>Damage per tick as a live multiple of the caster's fireDamage (never below 1).</summary>
    public float tickDamageMultiplier = 0.5f;
    /// <summary>Beam width as a fraction of the ship's collider width.</summary>
    public float widthOfShip = 0.9f;

    public float TotalSeconds => windupSeconds + beamSeconds;
}
