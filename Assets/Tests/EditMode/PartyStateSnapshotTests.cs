using NUnit.Framework;
using UnityEngine;

public class PartyStateSnapshotTests
{
    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesEachShip()
    {
        var snapshot = new PartyStateSnapshot();
        snapshot.ships.Add(new ShipStateSnapshot
        {
            id = "ai:Tank",
            position = new Vector2(1f, 2f),
            health = 8,
            shield = 20,
            firing = true
        });

        PartyStateSnapshot result = PartyStateSnapshot.FromJson(snapshot.ToJson());

        Assert.AreEqual(1, result.ships.Count);
        Assert.AreEqual("ai:Tank", result.ships[0].id);
        Assert.AreEqual(new Vector2(1f, 2f), result.ships[0].position);
        Assert.AreEqual(8, result.ships[0].health);
        Assert.AreEqual(20, result.ships[0].shield);
        Assert.IsTrue(result.ships[0].firing);
    }

    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesMultipleShipsInOrder()
    {
        var snapshot = new PartyStateSnapshot();
        snapshot.ships.Add(new ShipStateSnapshot { id = "a" });
        snapshot.ships.Add(new ShipStateSnapshot { id = "b" });

        PartyStateSnapshot result = PartyStateSnapshot.FromJson(snapshot.ToJson());

        Assert.AreEqual(2, result.ships.Count);
        Assert.AreEqual("a", result.ships[0].id);
        Assert.AreEqual("b", result.ships[1].id);
    }

    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesBossState()
    {
        var snapshot = new PartyStateSnapshot
        {
            boss = new BossStateSnapshot { active = true, health = 42, maxHealth = 150, phase2 = true, currentTargetId = "ai:Tank" }
        };

        PartyStateSnapshot result = PartyStateSnapshot.FromJson(snapshot.ToJson());

        Assert.IsTrue(result.boss.active);
        Assert.AreEqual(42, result.boss.health);
        Assert.AreEqual(150, result.boss.maxHealth);
        Assert.IsTrue(result.boss.phase2);
        Assert.AreEqual("ai:Tank", result.boss.currentTargetId);
    }

    [Test]
    public void ToJson_FromJson_RoundTrip_PreservesBullets()
    {
        var snapshot = new PartyStateSnapshot();
        snapshot.bullets.Add(new BulletStateSnapshot { id = 7, position = new Vector2(3f, 4f), owner = "Enemy" });

        PartyStateSnapshot result = PartyStateSnapshot.FromJson(snapshot.ToJson());

        Assert.AreEqual(1, result.bullets.Count);
        Assert.AreEqual(7, result.bullets[0].id);
        Assert.AreEqual(new Vector2(3f, 4f), result.bullets[0].position);
        Assert.AreEqual("Enemy", result.bullets[0].owner);
    }
}
