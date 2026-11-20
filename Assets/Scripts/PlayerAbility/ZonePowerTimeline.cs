using UnityEngine;

// A placed superpower's frame and size at a moment of the cast: the open
// frame while it grows, a 2-frame shimmer at full size, the ending flicker,
// then the collapse frame while it shrinks away. Pure, so it's unit-tested
// (ZonePowerTimelineTests).
public static class ZonePowerTimeline
{
    public const float ShimmerFrameSeconds = 0.25f;
    public const float FlickerSeconds = 0.08f;

    public static ZoneFrame FrameAt(ZonePhaseTiming t, float elapsed)
    {
        float left = t.seconds - elapsed;
        if (elapsed < t.openSeconds) return ZoneFrame.Open;
        if (left <= t.collapseSeconds) return ZoneFrame.Collapse;
        if (left <= t.collapseSeconds + t.endingSeconds) return Alternate(elapsed, FlickerSeconds, ZoneFrame.Ending);
        return Alternate(elapsed - t.openSeconds, ShimmerFrameSeconds, ZoneFrame.Pulse);
    }

    /// <summary>The size as a fraction of full: 0 → 1 while opening, held, then 1 → 0 while collapsing.</summary>
    public static float ScaleAt(ZonePhaseTiming t, float elapsed)
    {
        float opened = Mathf.Clamp01(elapsed / t.openSeconds);
        float collapsed = Mathf.Clamp01((elapsed - (t.seconds - t.collapseSeconds)) / t.collapseSeconds);
        return Mathf.Min(opened, 1f - collapsed);
    }

    private static ZoneFrame Alternate(float seconds, float stepSeconds, ZoneFrame other) =>
        Mathf.FloorToInt(seconds / stepSeconds) % 2 == 0 ? ZoneFrame.Idle : other;
}
