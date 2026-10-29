using UnityEngine;

// Shockwave's tunable values, grouped into one Inspector-visible field on
// MarauderBoss instead of a dozen top-level fields - keeps MarauderBoss.cs
// under this project's file-size cap without losing Inspector tunability.
// Read exclusively by MarauderBossShockwave.cs.
[System.Serializable]
public class MarauderBossShockwaveSettings
{
    public float radius = 1.7f; // boss half-extent (0.8) + ~1.5 ship-widths (0.9) from its edge
    public float damageMultiplier = 3f;
    public float knockback = 33f; // ~3.5 units of total displacement, see AddRecoil's decay math in PlayerController.cs
    public float cooldown = 3f;
    public float telegraphTime = 0.3f;

    // Always-visible dim ring at radius so the danger zone reads before it
    // ever triggers; pulses to a bright warning color during the telegraph
    // wind-up, then flashes on the frame it actually hits. See
    // MarauderBossShockwave for the LineRenderer setup.
    public PaletteSwatch ringColor = new PaletteSwatch(PaletteColor.ShockwaveRing, 0.25f);
    public PaletteSwatch ringTelegraphColor = new PaletteSwatch(PaletteColor.ShockwaveTelegraph, 0.85f);
    public PaletteSwatch ringImpactColor = new PaletteSwatch(PaletteColor.ShockwaveImpact);
    public float ringWidth = 0.06f;
    public float ringTelegraphWidth = 0.14f;
    public float telegraphPulseSpeed = 12f;
    public float impactFlashDuration = 0.15f;
}
