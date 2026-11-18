using UnityEngine;

// The Tank's shield arc as numbers: a parabola above the ship, in the ship's
// local space. Scale grows the width and the bulge together around the
// ship's centre line (the Super Shield); the offset above the ship stays.
// Pure, so it's unit-tested (ShieldArcShapeTests).
public struct ShieldArcShape
{
    public const int Segments = 16;

    public float halfWidth;
    public float height;
    public float yOffset;

    /// <summary>
    /// y = yOffset + height·(1 − (x / halfWidth)²) across [−halfWidth, halfWidth],
    /// with halfWidth and height both multiplied by scale.
    /// </summary>
    public Vector2[] Points(float scale)
    {
        Vector2[] points = new Vector2[Segments];
        for (int i = 0; i < Segments; i++) points[i] = PointAt(i / (float)(Segments - 1), scale);
        return points;
    }

    private Vector2 PointAt(float across01, float scale)
    {
        float x = Mathf.Lerp(-halfWidth, halfWidth, across01) * scale;
        float xOverEdge = x / (halfWidth * scale);
        return new Vector2(x, yOffset + height * scale * (1f - xOverEdge * xOverEdge));
    }
}
