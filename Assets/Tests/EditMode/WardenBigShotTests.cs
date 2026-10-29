using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Same harness approach as WardenBossDamageTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection. Spawned bullets are tracked and destroyed in
// TearDown, since Bullet only schedules its lifetime cleanup in Play mode.
public class WardenBigShotTests
{
    private static readonly MethodInfo BossAwake = typeof(WardenBoss)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo BigShotAwake = typeof(WardenBigShot)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<GameObject> created = new List<GameObject>();
    private WardenBoss boss;
    private WardenBigShot bigShot;

    [SetUp]
    public void SetUp()
    {
        GameObject bossGO = Track(new GameObject("TestWardenBoss"));
        boss = bossGO.AddComponent<WardenBoss>();
        bigShot = bossGO.AddComponent<WardenBigShot>();
        bigShot.bulletPrefab = Track(new GameObject("TestBulletPrefab"));
        bigShot.bulletPrefab.AddComponent<Bullet>();
        BossAwake.Invoke(boss, null);
        BigShotAwake.Invoke(bigShot, null);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    private GameObject Track(GameObject go)
    {
        created.Add(go);
        return go;
    }

    private Bullet Fire()
    {
        Bullet b = bigShot.SpawnWideBullet();
        Track(b.gameObject);
        return b;
    }

    [Test]
    public void SpawnWideBullet_IsTripleWidthDoubleDamage()
    {
        Bullet b = Fire();
        Assert.AreEqual(3f, b.transform.localScale.x, 1e-4f);
        Assert.AreEqual(2f, b.damage, 1e-4f);
    }

    [Test]
    public void SpawnWideBullet_FiresStraightDownAsEnemy()
    {
        Bullet b = Fire();
        Assert.AreEqual(Vector2.down, b.Direction);
        Assert.AreEqual("Enemy", b.Owner);
    }

    [Test]
    public void NextCooldown_Phase1_WithinJitterOfBase()
    {
        for (int i = 0; i < 50; i++)
            Assert.That(bigShot.NextCooldown(), Is.InRange(8f - 1.5f, 8f + 1.5f));
    }

    [Test]
    public void NextCooldown_Phase2_WithinJitterOfPhase2Base()
    {
        boss.TakeDamage(boss.maxHealth / 2);
        for (int i = 0; i < 50; i++)
            Assert.That(bigShot.NextCooldown(), Is.InRange(6f - 1.5f, 6f + 1.5f));
    }
}
