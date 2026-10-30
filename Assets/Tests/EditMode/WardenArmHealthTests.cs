using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Same harness approach as WardenBossDamageTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection. Recover() is invoked the same way, since
// Time.time doesn't advance here to reach downUntil.
public class WardenArmHealthTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly MethodInfo HealthAwake = typeof(WardenArmHealth).GetMethod("Awake", Private);
    private static readonly MethodInfo HealthRecover = typeof(WardenArmHealth).GetMethod("Recover", Private);
    private static readonly MethodInfo BossAwake = typeof(WardenBoss).GetMethod("Awake", Private);

    private readonly List<GameObject> created = new List<GameObject>();

    private WardenArmHealth CreateArm(Transform parent = null)
    {
        GameObject go = new GameObject("TestArm");
        created.Add(go);
        go.transform.SetParent(parent);
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<WardenArm>();
        WardenArmHealth health = go.AddComponent<WardenArmHealth>();
        HealthAwake.Invoke(health, null);
        return health;
    }

    private WardenBoss CreateBoss()
    {
        GameObject go = new GameObject("TestWardenBoss");
        created.Add(go);
        WardenBoss boss = go.AddComponent<WardenBoss>();
        BossAwake.Invoke(boss, null);
        return boss;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    [Test]
    public void TakeDamage_BelowMaxHealth_ArmStaysUp()
    {
        WardenArmHealth arm = CreateArm();

        arm.TakeDamage(arm.maxHealth - 1f);

        Assert.IsFalse(arm.IsDown);
    }

    [Test]
    public void TakeDamage_ReachingMaxHealth_KnocksArmDown()
    {
        WardenArmHealth arm = CreateArm();

        arm.TakeDamage(arm.maxHealth);

        Assert.IsTrue(arm.IsDown);
        Assert.IsFalse(arm.GetComponent<WardenArm>().enabled, "A downed arm must stop firing.");
        Assert.IsFalse(arm.GetComponent<Collider2D>().enabled, "A downed arm must lose its hitbox.");
    }

    [Test]
    public void TakeDamage_FractionalHits_Accumulate()
    {
        WardenArmHealth arm = CreateArm();
        arm.maxHealth = 2f;
        HealthAwake.Invoke(arm, null);

        arm.TakeDamage(0.7f);
        arm.TakeDamage(0.7f);
        arm.TakeDamage(0.7f);

        Assert.IsTrue(arm.IsDown);
    }

    [Test]
    public void TakeDamage_WhileDown_IsIgnored()
    {
        WardenArmHealth arm = CreateArm();
        arm.TakeDamage(arm.maxHealth);
        float healthWhenDowned = arm.CurrentHealth;

        arm.TakeDamage(5f);

        Assert.AreEqual(healthWhenDowned, arm.CurrentHealth);
    }

    [Test]
    public void Recover_RestoresHealthHitboxAndFiring()
    {
        WardenArmHealth arm = CreateArm();
        arm.TakeDamage(arm.maxHealth);

        HealthRecover.Invoke(arm, null);

        Assert.IsFalse(arm.IsDown);
        Assert.AreEqual(arm.maxHealth, arm.CurrentHealth);
        Assert.IsTrue(arm.GetComponent<WardenArm>().enabled);
        Assert.IsTrue(arm.GetComponent<Collider2D>().enabled);
    }

    [Test]
    public void Boss_WhileAnArmIsUp_IgnoresDamage()
    {
        WardenBoss boss = CreateBoss();
        boss.armA = CreateArm(boss.transform).GetComponent<WardenArm>();
        int before = boss.CurrentHealth;

        boss.TakeDamage(10f);

        Assert.IsTrue(boss.IsShielded);
        Assert.AreEqual(before, boss.CurrentHealth);
    }

    [Test]
    public void Boss_WithEveryActiveArmDown_TakesDamage()
    {
        WardenBoss boss = CreateBoss();
        WardenArmHealth a = CreateArm(boss.transform);
        WardenArmHealth b = CreateArm(boss.transform);
        boss.armA = a.GetComponent<WardenArm>();
        boss.armB = b.GetComponent<WardenArm>();
        a.TakeDamage(a.maxHealth);
        b.TakeDamage(b.maxHealth);
        int before = boss.CurrentHealth;

        boss.TakeDamage(10f);

        Assert.IsFalse(boss.IsShielded);
        Assert.AreEqual(before - 10, boss.CurrentHealth);
    }

    [Test]
    public void Boss_InactiveArmC_DoesNotShield()
    {
        WardenBoss boss = CreateBoss();
        WardenArmHealth c = CreateArm(boss.transform);
        c.gameObject.SetActive(false);
        boss.armC = c.GetComponent<WardenArm>();

        Assert.IsFalse(boss.IsShielded);
    }
}
