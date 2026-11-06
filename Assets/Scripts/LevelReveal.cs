using System.Collections;
using UnityEngine;

// Hides the level's slow first frames behind a black cover, then fades it
// out once frames run smoothly (FrameSettle). Runs on every Level load:
// from Level Select, Restart and Retry. See docs/systems/scene-flow.md.
public class LevelReveal : MonoBehaviour
{
    private const float FadeSeconds = 0.25f;

    public CanvasGroup cover;

    IEnumerator Start()
    {
        cover.gameObject.SetActive(true);
        cover.alpha = 1f;
        yield return FrameSettle.UntilSmooth();
        yield return MenuTween.Slide(new SlideTarget(cover), new TweenSpec { duration = FadeSeconds, fromAlpha = 1f, toAlpha = 0f });
        cover.gameObject.SetActive(false);
    }
}
