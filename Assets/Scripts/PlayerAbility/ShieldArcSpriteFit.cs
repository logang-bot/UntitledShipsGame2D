using UnityEngine;

// Where an arc sprite's two end caps sit in its image, so it can be scaled
// and moved until the caps land on the collider's first and last point.
// Measured from the Combat FX exports.
public struct ShieldArcSpriteFit
{
    public float capSpanPx;
    public float capsBelowCenterPx;

    public static readonly ShieldArcSpriteFit Normal = new ShieldArcSpriteFit { capSpanPx = 360f, capsBelowCenterPx = 37f };
    public static readonly ShieldArcSpriteFit Super = new ShieldArcSpriteFit { capSpanPx = 900f, capsBelowCenterPx = 64f };

    public void Place(Transform sprite, Vector2[] points)
    {
        float scale = (points[points.Length - 1].x - points[0].x) / (capSpanPx / FxSprites.PixelsPerUnit);
        sprite.localScale = Vector3.one * scale;
        sprite.localPosition = new Vector3(0f, points[0].y + capsBelowCenterPx / FxSprites.PixelsPerUnit * scale, 0f);
    }
}
