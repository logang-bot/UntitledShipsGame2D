using UnityEngine;

// A CanvasGroup that a menu tween slides and fades, plus the anchored
// position it rests at, which every tween offset is measured from.
public struct SlideTarget
{
    public CanvasGroup group;
    public RectTransform rect;
    public Vector2 home;

    public SlideTarget(CanvasGroup group)
    {
        this.group = group;
        rect = (RectTransform)group.transform;
        home = rect.anchoredPosition;
    }
}
