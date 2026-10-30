using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The one HUD element inside the playfield during a fight (design: "06 /
// Playfield callouts"): a thin band in the upper third with kicker / title /
// sub, framed by two 1 px lines or, for a boss entry, two hazard bands that
// scroll sideways. No backing panel, so bullets stay visible through it.
public class CalloutBannerView : MonoBehaviour
{
    public CanvasGroup group;
    public TMP_Text kicker;
    public TMP_Text title;
    public TMP_Text sub;
    public Image[] lines;
    public RawImage[] hazardBands;
    public float bandScrollPixelsPerSecond = 40f;
    public float bandTilePixels = 20f;

    void Awake() => group.alpha = 0f;

    void Update()
    {
        float shift = bandScrollPixelsPerSecond / bandTilePixels * Time.deltaTime;
        foreach (RawImage band in hazardBands) band.uvRect = Scrolled(band, shift);
    }

    public void Play(CalloutSpec spec, Action onDone = null)
    {
        StopAllCoroutines();
        Paint(spec);
        StartCoroutine(Run(spec.timing, onDone));
    }

    private void Paint(CalloutSpec spec)
    {
        Color tint = NeonPalette.Get(spec.color);
        kicker.text = spec.kicker;
        kicker.color = tint;
        title.text = spec.title;
        sub.text = spec.sub;
        foreach (Image line in lines) Show(line, !spec.hazardBands, tint);
        foreach (RawImage band in hazardBands) Show(band, spec.hazardBands, NeonPalette.Get(spec.color, 0.85f));
    }

    private IEnumerator Run(CalloutTiming timing, Action onDone)
    {
        yield return Fade(0f, 1f, timing.fadeIn);
        yield return new WaitForSeconds(timing.hold);
        yield return Fade(1f, 0f, timing.fadeOut);
        onDone?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        group.alpha = to;
    }

    private static void Show(Graphic graphic, bool visible, Color color)
    {
        graphic.enabled = visible;
        graphic.color = color;
    }

    private Rect Scrolled(RawImage band, float shift)
    {
        Rect size = band.rectTransform.rect;
        float x = Mathf.Repeat(band.uvRect.x + shift, 1f);
        return new Rect(x, 0f, size.width / bandTilePixels, size.height / bandTilePixels);
    }
}
