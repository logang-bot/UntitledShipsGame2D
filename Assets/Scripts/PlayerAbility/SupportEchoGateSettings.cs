using System;

// The Support's Echo Gate tunables, one Inspector block on PlayerAbility.
// Placeholder numbers until a playtest; see
// docs/superpowers/specs/2026-11-19-medic-sanctuary-support-echo-gate-design.md.
[Serializable]
public class SupportEchoGateSettings
{
    /// <summary>Counted from the Q press, like every other ability.</summary>
    public float cooldown = 25f;
    public ZonePhaseTiming timing = new ZonePhaseTiming(8f);
    /// <summary>World units between the gate's two end nodes (about 4 ship widths).</summary>
    public float width = 2.4f;
    public float thickness = 0.12f;
    /// <summary>How far above the Support's position the gate is dropped.</summary>
    public float heightAboveShip = 1.5f;
    /// <summary>Shots a regular shot becomes after crossing the gate.</summary>
    public int splitCount = 3;
    /// <summary>The outermost split shots lean this many degrees off the shot's line.</summary>
    public float spreadDegrees = 12f;
}
