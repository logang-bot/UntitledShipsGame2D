using System;

// How long a placed superpower lasts and how its open, ending flicker and
// collapse are paced, in seconds from the Q press.
[Serializable]
public class ZonePhaseTiming
{
    /// <summary>The whole cast, open and collapse included.</summary>
    public float seconds = 6f;
    public float openSeconds = 0.3f;
    /// <summary>The flicker before the collapse, so the team sees it's about to drop.</summary>
    public float endingSeconds = 1f;
    public float collapseSeconds = 0.25f;

    public ZonePhaseTiming() { }

    public ZonePhaseTiming(float seconds)
    {
        this.seconds = seconds;
    }
}
