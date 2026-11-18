using NUnit.Framework;
using UnityEngine;

// The Tank's shield arc points, and the Super Shield's 2.5x growth around
// the ship's centre line.
public class ShieldArcShapeTests
{
    private static readonly ShieldArcShape Shape = new ShieldArcShape { halfWidth = 0.9f, height = 0.2f, yOffset = 0.3f };

    [Test]
    public void Points_SpanTheWidth_WithTheBulgeInTheMiddle()
    {
        Vector2[] points = Shape.Points(1f);

        Assert.AreEqual(-0.9f, points[0].x, 1e-5f);
        Assert.AreEqual(0.9f, points[points.Length - 1].x, 1e-5f);
        Assert.AreEqual(0.3f, points[0].y, 1e-5f);
        Assert.Greater(points[ShieldArcShape.Segments / 2].y, 0.49f);
    }

    [Test]
    public void Points_Scaled_GrowWidthAndBulgeTogether_StayingCentred()
    {
        Vector2[] points = Shape.Points(2.5f);

        Assert.AreEqual(-2.25f, points[0].x, 1e-5f);
        Assert.AreEqual(2.25f, points[points.Length - 1].x, 1e-5f);
        Assert.AreEqual(0.3f, points[points.Length - 1].y, 1e-5f, "The offset above the ship stays put.");
        Assert.Greater(points[ShieldArcShape.Segments / 2].y, 0.3f + 0.2f * 2.5f - 0.01f);
    }
}
