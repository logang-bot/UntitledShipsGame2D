using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Same harness approach as MarauderBossDamageTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection.
public class WardenBossDamageTests
{
    private static readonly MethodInfo AwakeMethod = typeof(WardenBoss)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

    private GameObject bossGO;
    private GameObject shipGO;

    private WardenBoss CreateBoss(int maxHealth = 130)
    {
        bossGO = new GameObject("TestWardenBoss");
        WardenBoss boss = bossGO.AddComponent<WardenBoss>();
        boss.maxHealth = maxHealth;
        AwakeMethod.Invoke(boss, null);
        return boss;
    }

    private GameObject Ship()
    {
        shipGO = new GameObject("TestShip");
        shipGO.AddComponent<PlayerHealth>();
        return shipGO;
    }

    [TearDown]
    public void TearDown()
    {
        if (bossGO != null) Object.DestroyImmediate(bossGO);
        if (shipGO != null) Object.DestroyImmediate(shipGO);
    }

    [Test]
    public void TakeDamage_ReducesHealthByRoundedAmount()
    {
        WardenBoss boss = CreateBoss();
        boss.TakeDamage(12.6f);
        Assert.AreEqual(130 - 13, boss.CurrentHealth);
    }

    [Test]
    public void TakeDamage_AtHalfHealth_EntersPhase2()
    {
        WardenBoss boss = CreateBoss(maxHealth: 100);
        boss.TakeDamage(50f);
        Assert.IsTrue(boss.IsPhase2);
    }

    [Test]
    public void TakeDamage_AboveHalfHealth_StaysPhase1()
    {
        WardenBoss boss = CreateBoss(maxHealth: 100);
        boss.TakeDamage(49f);
        Assert.IsFalse(boss.IsPhase2);
    }

    [Test]
    public void TauntedBy_SetsTauntedShipAndFutureWindow()
    {
        WardenBoss boss = CreateBoss();
        GameObject taunter = Ship();
        boss.tauntWindowDuration = 3f;

        boss.TauntedBy(taunter);

        Assert.AreSame(taunter, boss.TauntedShip);
        Assert.Greater(boss.TauntActiveUntil, Time.time);
    }

    [Test]
    public void ApplyContactDamage_DealsRoundedMultipliedDamage()
    {
        WardenBoss boss = CreateBoss();
        boss.bulletDamage = 1f;
        boss.bodyContactDamageMultiplier = 2f;
        GameObject ship = Ship();
        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        int before = health.CurrentHealth;

        boss.ApplyContactDamage(ship);

        Assert.AreEqual(before - 2, health.CurrentHealth);
    }

    [Test]
    public void ApplyContactDamage_WithinCooldown_DoesNotDoubleHit()
    {
        WardenBoss boss = CreateBoss();
        boss.contactDamageCooldown = 1f;
        GameObject ship = Ship();
        PlayerHealth health = ship.GetComponent<PlayerHealth>();

        boss.ApplyContactDamage(ship);
        int afterFirst = health.CurrentHealth;
        boss.ApplyContactDamage(ship);

        Assert.AreEqual(afterFirst, health.CurrentHealth);
    }
}
