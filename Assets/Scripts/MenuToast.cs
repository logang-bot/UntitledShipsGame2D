using System.Collections;
using TMPro;
using UnityEngine;

// A one-line notice that fades in, holds, and fades out - e.g. Lobby's
// "Online co-op arrives in a later update." when the locked card is clicked.
[RequireComponent(typeof(CanvasGroup))]
public class MenuToast : MonoBehaviour
{
    private const float FadeInSeconds = 0.12f;
    private const float HoldSeconds = 2f;
    private const float FadeOutSeconds = 0.2f;
    private const float Rise = 8f;

    public TMP_Text label;

    private SlideTarget target;

    void Awake()
    {
        target = new SlideTarget(GetComponent<CanvasGroup>());
        target.group.alpha = 0f;
    }

    public void Show(string message)
    {
        label.text = message;
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return MenuTween.Slide(target, Fade(FadeInSeconds, 0f, 1f));
        yield return new WaitForSecondsRealtime(HoldSeconds);
        yield return MenuTween.Slide(target, Fade(FadeOutSeconds, 1f, 0f));
    }

    private static TweenSpec Fade(float seconds, float fromAlpha, float toAlpha) => new TweenSpec
    {
        duration = seconds,
        fromOffset = new Vector2(0f, fromAlpha < toAlpha ? -Rise : 0f),
        fromAlpha = fromAlpha,
        toAlpha = toAlpha,
        easeOut = fromAlpha < toAlpha,
    };
}
