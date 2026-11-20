using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// The Echo Gate splits only a player's regular, not-yet-split shot, into an
// even fan around its line.
public class EchoSplitTests
{
    private readonly List<GameObject> spawned = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in spawned) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    [Test]
    public void Directions_FanEvenlyAroundTheShot()
    {
        Vector2[] fan = EchoSplit.Directions(Vector2.up, 3, 12f);
        Assert.AreEqual(3, fan.Length);
        Assert.AreEqual(12f, Vector2.SignedAngle(Vector2.up, fan[0]), 1e-3f);
        Assert.AreEqual(0f, Vector2.SignedAngle(Vector2.up, fan[EchoSplit.MiddleIndex(3)]), 1e-3f);
        Assert.AreEqual(-12f, Vector2.SignedAngle(Vector2.up, fan[2]), 1e-3f);
    }

    [Test]
    public void Directions_OfOne_KeepsTheShotsLine()
    {
        Vector2[] fan = EchoSplit.Directions(Vector2.up, 1, 12f);
        Assert.AreEqual(0f, Vector2.SignedAngle(Vector2.up, fan[0]), 1e-3f);
    }

    [Test]
    public void ShouldSplit_OnlyAFreshRegularPlayerShot()
    {
        Assert.IsTrue(EchoSplit.ShouldSplit(Shot("Player", PlayerShotKind.Regular, false)));
        Assert.IsFalse(EchoSplit.ShouldSplit(Shot("Player", PlayerShotKind.Regular, true)));
        Assert.IsFalse(EchoSplit.ShouldSplit(Shot("Player", PlayerShotKind.Combo3, false)));
        Assert.IsFalse(EchoSplit.ShouldSplit(Shot("Enemy", PlayerShotKind.Regular, false)));
    }

    private Bullet Shot(string owner, PlayerShotKind kind, bool isEcho)
    {
        GameObject go = new GameObject("TestShot");
        spawned.Add(go);
        Bullet shot = go.AddComponent<Bullet>();
        shot.Init(Vector2.up, 1f, owner);
        shot.Kind = kind;
        shot.IsEcho = isEcho;
        return shot;
    }
}
