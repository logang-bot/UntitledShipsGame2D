using NUnit.Framework;
using UnityEngine;

public class ShipCollisionUtilTests
{
    [Test]
    public void ResolveBoxOverlap_NoOverlap_ReturnsPositionUnchanged()
    {
        Vector2 candidate = new Vector2(10f, 10f);
        Vector2 result = ShipCollisionUtil.ResolveBoxOverlap(
            candidate, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(0.5f, 0.5f),
            out bool wasOverlapping);

        Assert.IsFalse(wasOverlapping);
        Assert.AreEqual(candidate, result);
    }

    [Test]
    public void ResolveBoxOverlap_ShallowerOnX_PushesAlongXOnly()
    {
        // Self box overlaps other by 1.5 on X, 2 on Y -> X is shallower.
        Vector2 candidate = new Vector2(0.5f, 0f);
        Vector2 result = ShipCollisionUtil.ResolveBoxOverlap(
            candidate, Vector2.one,
            Vector2.zero, Vector2.one,
            out bool wasOverlapping);

        Assert.IsTrue(wasOverlapping);
        Assert.That(result.x, Is.EqualTo(2f).Within(1e-4f));
        Assert.That(result.y, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void ResolveBoxOverlap_ShallowerOnY_PushesAlongYOnly()
    {
        // Self box overlaps other by 2 on X, 1.5 on Y -> Y is shallower.
        Vector2 candidate = new Vector2(0f, 0.5f);
        Vector2 result = ShipCollisionUtil.ResolveBoxOverlap(
            candidate, Vector2.one,
            Vector2.zero, Vector2.one,
            out bool wasOverlapping);

        Assert.IsTrue(wasOverlapping);
        Assert.That(result.x, Is.EqualTo(0f).Within(1e-4f));
        Assert.That(result.y, Is.EqualTo(2f).Within(1e-4f));
    }

    [Test]
    public void ResolveBoxOverlap_ExactEdgeTouch_IsNotOverlapping()
    {
        // Boxes exactly touching (overlap == 0 on X) should not count as
        // overlapping - ResolveBoxOverlap uses "<= 0f" as the no-overlap test.
        Vector2 candidate = new Vector2(2f, 0f);
        Vector2 result = ShipCollisionUtil.ResolveBoxOverlap(
            candidate, Vector2.one,
            Vector2.zero, Vector2.one,
            out bool wasOverlapping);

        Assert.IsFalse(wasOverlapping);
        Assert.AreEqual(candidate, result);
    }
}
