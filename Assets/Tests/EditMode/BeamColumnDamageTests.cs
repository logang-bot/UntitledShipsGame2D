using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BeamColumnDamageTests
{
    private static readonly Rect Column = new Rect(-0.25f, 0f, 0.5f, 10f);

    private readonly List<GameObject> created = new List<GameObject>();

    private Enemy CreateEnemy(Vector2 position, string tag = "Enemy")
    {
        GameObject go = new GameObject("Enemy") { tag = tag };
        created.Add(go);
        go.transform.position = position;
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.health = 10;
        Physics2D.SyncTransforms();
        return enemy;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created) if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    [Test]
    public void Strike_EnemyInsideColumn_TakesTheTickAndReportsAnImpact()
    {
        Enemy enemy = CreateEnemy(new Vector2(0.1f, 4f));

        List<Vector2> impacts = new BeamColumnDamage().Strike(Column, 2f, null);

        Assert.AreEqual(8, enemy.health);
        Assert.AreEqual(1, impacts.Count);
        Assert.AreEqual(3.5f, impacts[0].y, 0.0001f);
    }

    [Test]
    public void Strike_EnemyOutsideColumn_IsUntouched()
    {
        Enemy enemy = CreateEnemy(new Vector2(3f, 4f));

        new BeamColumnDamage().Strike(Column, 2f, null);

        Assert.AreEqual(10, enemy.health);
    }

    [Test]
    public void Strike_UntaggedObjectInColumn_IsIgnored()
    {
        Enemy notAnEnemy = CreateEnemy(new Vector2(0f, 4f), "Untagged");

        List<Vector2> impacts = new BeamColumnDamage().Strike(Column, 2f, null);

        Assert.AreEqual(10, notAnEnemy.health);
        Assert.AreEqual(0, impacts.Count);
    }

    [Test]
    public void Strike_HitsEveryEnemyInTheColumn_Once()
    {
        Enemy low = CreateEnemy(new Vector2(0f, 2f));
        Enemy high = CreateEnemy(new Vector2(0f, 7f));

        new BeamColumnDamage().Strike(Column, 1f, null);

        Assert.AreEqual(9, low.health);
        Assert.AreEqual(9, high.health);
    }
}
