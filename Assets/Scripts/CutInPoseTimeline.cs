using UnityEngine;

// What the cut-in character does at a moment of the cast, anime "limited
// animation" style: talking (two mouth frames flipping) until the snap, then
// the shout with a white flash, a push-in and a short shake as the power
// fires, then a held afterglow frame until the card leaves.
public static class CutInPoseTimeline
{
    public static CutInFrame FrameAt(CutInTiming timing, float elapsed)
    {
        float sinceSnap = elapsed - timing.snapAt;
        if (sinceSnap < 0f) return TalkFrameAt(timing, elapsed);
        return sinceSnap < timing.shoutHoldSeconds ? CutInFrame.Shout : CutInFrame.Afterglow;
    }

    public static float FlashAlpha(CutInTiming timing, float elapsed)
    {
        float sinceSnap = elapsed - timing.snapAt;
        if (sinceSnap < 0f) return 0f;
        return timing.flashPeak * (1f - Mathf.Clamp01(sinceSnap / timing.flashSeconds));
    }

    public static float PushScale(CutInTiming timing, float elapsed)
    {
        float sinceSnap = elapsed - timing.snapAt;
        if (sinceSnap < 0f) return 1f;
        return Mathf.Lerp(1f, timing.pushScale, MenuEase.Out(Mathf.Clamp01(sinceSnap / timing.pushSeconds)));
    }

    /// <summary>1 at the snap, easing linearly to 0 over shakeSeconds.</summary>
    public static float ShakeStrength(CutInTiming timing, float elapsed)
    {
        float sinceSnap = elapsed - timing.snapAt;
        if (sinceSnap < 0f || sinceSnap >= timing.shakeSeconds) return 0f;
        return 1f - sinceSnap / timing.shakeSeconds;
    }

    private static CutInFrame TalkFrameAt(CutInTiming timing, float elapsed)
    {
        bool mouthOpen = Mathf.FloorToInt(elapsed / timing.talkFlipSeconds) % 2 == 1;
        return mouthOpen ? CutInFrame.TalkOpen : CutInFrame.TalkClosed;
    }
}
