using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EnemyDamageTests
{
    private static readonly MethodInfo MarauderAwake = typeof(MarauderBoss)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject Track(GameObject go)
    {
        created.Add(go);
        return go;
    }

    private MarauderBoss CreateMarauder(GameObject ship)
    {
        MarauderBoss boss = Track(new GameObject("Boss")).AddComponent<MarauderBoss>();
        boss.targets = new[] { ship };
        MarauderAwake.Invoke(boss, null);
        return boss;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created) if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    [Test]
    public void TryDamage_Boss_DealsDamageCreditedToSource()
    {
        GameObject ship = Track(new GameObject("Ship"));
        MarauderBoss boss = CreateMarauder(ship);

        bool hit = EnemyDamage.TryDamage(boss.gameObject, 3f, ship);

        Assert.IsTrue(hit);
        Assert.AreEqual(3f, boss.GetDamageDealt(ship), 0.0001f);
    }

    [Test]
    public void TryDamage_WaveEnemy_LowersItsHealth()
    {
        Enemy enemy = Track(new GameObject("Enemy")).AddComponent<Enemy>();
        enemy.health = 5;

        bool hit = EnemyDamage.TryDamage(enemy.gameObject, 2f, null);

        Assert.IsTrue(hit);
        Assert.AreEqual(3, enemy.health);
    }

    [Test]
    public void TryDamage_ObjectWithNoEnemyScript_ReportsNoHit()
    {
        GameObject rock = Track(new GameObject("Rock"));

        Assert.IsFalse(EnemyDamage.TryDamage(rock, 2f, null));
    }
}
