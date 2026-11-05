// Easing curves for menu tweens, mapping linear progress t in [0, 1] to
// eased progress in [0, 1].
public static class MenuEase
{
    /// <summary>Cubic ease-in: slow start, fast finish (t^3).</summary>
    public static float In(float t) => t * t * t;

    /// <summary>Cubic ease-out: fast start, slow finish (1 - (1 - t)^3).</summary>
    public static float Out(float t)
    {
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }
}
