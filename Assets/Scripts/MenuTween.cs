using System.Collections;
using UnityEngine;

// Coroutine tweens for menu screens. Runs on unscaled time so a paused game
// (Time.timeScale = 0) can't freeze a menu transition.
public static class MenuTween
{
    public static IEnumerator Slide(SlideTarget target, TweenSpec spec)
    {
        Apply(target, spec, 0f);
        if (spec.delay > 0f) yield return new WaitForSecondsRealtime(spec.delay);
        for (float elapsed = 0f; elapsed < spec.duration; elapsed += Time.unscaledDeltaTime)
        {
            Apply(target, spec, Eased(spec, elapsed / spec.duration));
            yield return null;
        }
        Apply(target, spec, 1f);
    }

    /// <summary>Scales a UI element up from its current size while gliding it to the centre of its canvas (ease-in).</summary>
    public static IEnumerator GrowToCenter(RectTransform target, float toScale, float seconds)
    {
        Vector3 from = target.position;
        Vector3 to = target.root.position;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = MenuEase.In(elapsed / seconds);
            target.position = Vector3.Lerp(from, to, t);
            target.localScale = Vector3.one * Mathf.Lerp(1f, toScale, t);
            yield return null;
        }
    }

    private static float Eased(TweenSpec spec, float t) => spec.easeOut ? MenuEase.Out(t) : MenuEase.In(t);

    private static void Apply(SlideTarget target, TweenSpec spec, float progress)
    {
        target.rect.anchoredPosition = target.home + Vector2.LerpUnclamped(spec.fromOffset, spec.toOffset, progress);
        target.group.alpha = Mathf.LerpUnclamped(spec.fromAlpha, spec.toAlpha, progress);
    }
}
