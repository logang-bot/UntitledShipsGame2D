using UnityEngine;

// One menu tween: how long, after what delay, from/to which offset (in
// canvas units, relative to the element's resting position) and alpha, and
// with which easing.
public struct TweenSpec
{
    public float duration;
    public float delay;
    public Vector2 fromOffset;
    public Vector2 toOffset;
    public float fromAlpha;
    public float toAlpha;
    public bool easeOut;
}
