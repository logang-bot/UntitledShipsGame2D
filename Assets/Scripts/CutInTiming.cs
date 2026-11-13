using System;

// The anime cut-in card's timeline, in seconds from the ability press. The
// snap (talking to the shout) lands on the beam firing; the party frame
// syncs snapAt to the ship's real wind-up. See
// docs/superpowers/specs/2026-11-12-attacker-nova-beam-cutin-design.md.
[Serializable]
public class CutInTiming
{
    public float slideInSeconds = 0.25f;
    public float slideOutAt = 2.3f;
    public float slideOutSeconds = 0.25f;

    /// <summary>How long each talking frame shows before the mouth flips.</summary>
    public float talkFlipSeconds = 0.12f;
    public float snapAt = 0.8f;
    public float shoutHoldSeconds = 0.6f;
    public float flashSeconds = 0.18f;
    public float flashPeak = 0.85f;
    public float pushSeconds = 0.12f;
    public float pushScale = 1.06f;
    public float shakeSeconds = 0.15f;
}
