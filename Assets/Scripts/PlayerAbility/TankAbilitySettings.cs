using System;

// The Tank's tunables, grouped as one Inspector block on PlayerAbility:
// Taunt on key 1, Super Shield on E, and the always-on shield arc both of
// them build on. Placeholder numbers until a playtest; see
// docs/superpowers/specs/2026-11-18-tank-super-shield-taunt-design.md.
[Serializable]
public class TankAbilitySettings
{
    public float tauntCooldown = 4f;
    /// <summary>How long the boss is held on the Tank, and the magnet shows.</summary>
    public float tauntSeconds = 3f;

    /// <summary>Counted from the E press, like every other ability.</summary>
    public float superShieldCooldown = 25f;
    /// <summary>The whole cast, growth and collapse included. Arc hits are free for all of it.</summary>
    public float superShieldSeconds = 4f;
    /// <summary>Super Shield arc size as a multiple of the normal arc's width and height.</summary>
    public float superShieldScale = 2.5f;
    public float growSeconds = 0.4f;
    public float collapseSeconds = 0.25f;
    /// <summary>The flicker before the collapse, so the team sees the wall is about to drop.</summary>
    public float endingSeconds = 1f;

    /// <summary>Arc width = Tank's own collider width x this.</summary>
    public float shieldArcWidthMultiplier = 3f;
    /// <summary>Bulge/thickness of the curve.</summary>
    public float shieldArcHeight = 0.4f;
    /// <summary>Local offset above the ship.</summary>
    public float shieldArcYOffset = 0.3f;
}
