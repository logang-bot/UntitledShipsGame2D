using UnityEngine;

// How far the cut-in card covers its party frame at a moment of the cast:
// it slides in, holds, then slides back out the way it came.
public static class CutInTimeline
{
    /// <summary>0 = hidden off the frame's left edge, 1 = fully covering the frame.</summary>
    public static float Coverage(CutInTiming timing, float elapsed)
    {
        if (elapsed < 0f) return 0f;
        if (elapsed < timing.slideInSeconds) return MenuEase.Out(elapsed / timing.slideInSeconds);
        float slideOut = (elapsed - timing.SlideOutAt) / timing.slideOutSeconds;
        return slideOut <= 0f ? 1f : 1f - MenuEase.In(Mathf.Clamp01(slideOut));
    }

    /// <summary>True on the first frame of a cast: elapsed is -1 between casts.</summary>
    public static bool CastStarted(float previousElapsed, float elapsed)
    {
        return previousElapsed < 0f && elapsed >= 0f;
    }
}
