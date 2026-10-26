using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WardenLockdownVolleyTests
{
    [Test]
    public void DirectionFor_Left_PointsRight()
    {
        Assert.AreEqual(Vector2.right, WardenLockdownVolley.DirectionFor(WardenLockdownVolley.Edge.Left));
    }

    [Test]
    public void DirectionFor_Right_PointsLeft()
    {
        Assert.AreEqual(Vector2.left, WardenLockdownVolley.DirectionFor(WardenLockdownVolley.Edge.Right));
    }

    [Test]
    public void DirectionFor_Top_PointsDown()
    {
        Assert.AreEqual(Vector2.down, WardenLockdownVolley.DirectionFor(WardenLockdownVolley.Edge.Top));
    }

    [Test]
    public void PickGapIndices_ReturnsExactlyGapCount_EvenlyDistributed()
    {
        List<int> gaps = WardenLockdownVolley.PickGapIndices(bulletCount: 12, gapCount: 2);

        Assert.AreEqual(2, gaps.Count);
        foreach (int gap in gaps) Assert.That(gap, Is.InRange(0, 11));
    }

    [Test]
    public void PickGapIndices_ZeroGapCount_ReturnsEmpty()
    {
        Assert.IsEmpty(WardenLockdownVolley.PickGapIndices(bulletCount: 12, gapCount: 0));
    }

    [Test]
    public void PickGapIndices_NeverExceedsBulletCountMinusOne()
    {
        List<int> gaps = WardenLockdownVolley.PickGapIndices(bulletCount: 3, gapCount: 2);
        foreach (int gap in gaps) Assert.Less(gap, 3);
    }
}
