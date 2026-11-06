using System.Collections;
using UnityEngine;

// The first frames after a scene loads are slow (asset integration, GC,
// first-use uploads), so an animation started straight away stutters or
// skips. Waits until frames run smoothly again, or gives up after a cap so a
// slow machine still gets in. See docs/systems/scene-flow.md.
public static class FrameSettle
{
    private const float SmoothFrameSeconds = 1f / 30f;
    private const int SmoothFramesNeeded = 2;
    private const float MaxWaitSeconds = 0.75f;

    public static IEnumerator UntilSmooth()
    {
        float giveUpAt = Time.realtimeSinceStartup + MaxWaitSeconds;
        int smoothFrames = 0;
        while (smoothFrames < SmoothFramesNeeded && Time.realtimeSinceStartup < giveUpAt)
        {
            yield return null;
            smoothFrames = Time.unscaledDeltaTime < SmoothFrameSeconds ? smoothFrames + 1 : 0;
        }
    }
}
