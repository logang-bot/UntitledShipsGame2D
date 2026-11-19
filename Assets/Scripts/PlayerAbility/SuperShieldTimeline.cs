using UnityEngine;

// The Super Shield's frame and arc size at a moment of the cast, in seconds
// from the Q press: the expand frame while the arc grows, a shimmer loop
// at full size, the ending flicker, then the collapse frame while it
// shrinks back. Pure, so it's unit-tested (SuperShieldTimelineTests).
public static class SuperShieldTimeline
{
    public const float ShimmerFrameSeconds = 0.1f;
    public const float FlickerSeconds = 0.08f;

    private static readonly SuperShieldFrame[] Shimmer =
        { SuperShieldFrame.Idle, SuperShieldFrame.Pulse, SuperShieldFrame.PlasmaA, SuperShieldFrame.PlasmaB };

    public static SuperShieldFrame FrameAt(TankAbilitySettings s, float elapsed)
    {
        float left = s.superShieldSeconds - elapsed;
        if (elapsed < s.growSeconds) return SuperShieldFrame.Expand;
        if (left <= s.collapseSeconds) return SuperShieldFrame.Collapse;
        if (left <= s.collapseSeconds + s.endingSeconds) return Flicker(elapsed);
        return Shimmer[Step(elapsed - s.growSeconds, ShimmerFrameSeconds) % Shimmer.Length];
    }

    /// <summary>The arc's size multiplier: 1 → superShieldScale while growing, held, then back to 1 while collapsing.</summary>
    public static float ScaleAt(TankAbilitySettings s, float elapsed)
    {
        float grown = Mathf.Clamp01(elapsed / s.growSeconds);
        float collapsed = Mathf.Clamp01((elapsed - (s.superShieldSeconds - s.collapseSeconds)) / s.collapseSeconds);
        return Mathf.Lerp(1f, s.superShieldScale, Mathf.Min(grown, 1f - collapsed));
    }

    private static SuperShieldFrame Flicker(float elapsed) =>
        Step(elapsed, FlickerSeconds) % 2 == 0 ? SuperShieldFrame.Ending : SuperShieldFrame.Idle;

    private static int Step(float seconds, float stepSeconds) => Mathf.FloorToInt(seconds / stepSeconds);
}
