using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Covers MarauderBoss's damage-tracking table (the data behind DpsMeterUI) and
// the aggro/target invariants it has to stay separate from.
//
// Same harness approach as PlayerHealthTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection. OnEnable() is deliberately NOT invoked - it calls
// StartCoroutine, which has no meaning outside Play mode. That's why
// CombatElapsed is only asserted in its pre-combat state here.
public class MarauderBossDamageTests
{
    private static readonly MethodInfo AwakeMethod = typeof(MarauderBoss)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo PickTargetMethod = typeof(MarauderBoss)
        .GetMethod("PickTarget", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject Ship(string name)
    {
        GameObject go = new GameObject(name);
        created.Add(go);
        return go;
    }

    private MarauderBoss CreateBoss(params GameObject[] targets)
    {
        GameObject go = new GameObject("TestBoss");
        created.Add(go);
        MarauderBoss boss = go.AddComponent<MarauderBoss>();
        boss.targets = targets;
        AwakeMethod.Invoke(boss, null);
        return boss;
    }

    private static void PickTarget(MarauderBoss boss)
    {
        PickTargetMethod.Invoke(boss, null);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
        created.Clear();
    }

    // ------------------------------------------------------------ seeding

    [Test]
    public void Awake_SeedsDamageTable_AtZeroForEveryTarget()
    {
        GameObject a = Ship("A");
        GameObject b = Ship("B");
        MarauderBoss boss = CreateBoss(a, b);

        Assert.AreEqual(0f, boss.GetDamageDealt(a));
        Assert.AreEqual(0f, boss.GetDamageDealt(b));
    }

    [Test]
    public void GetDamageDealt_NullSource_ReturnsZero()
    {
        MarauderBoss boss = CreateBoss(Ship("A"));

        Assert.AreEqual(0f, boss.GetDamageDealt(null));
    }

    [Test]
    public void GetDamageDealt_UnknownSource_ReturnsZeroRatherThanThrowing()
    {
        MarauderBoss boss = CreateBoss(Ship("A"));

        Assert.AreEqual(0f, boss.GetDamageDealt(Ship("NeverDealtAnything")));
    }

    // ---------------------------------------------------- accumulation

    [Test]
    public void TakeDamage_AccumulatesPerSource_Independently()
    {
        GameObject a = Ship("A");
        GameObject b = Ship("B");
        MarauderBoss boss = CreateBoss(a, b);

        boss.TakeDamage(2f, a);
        boss.TakeDamage(3f, a);
        boss.TakeDamage(5f, b);

        Assert.AreEqual(5f, boss.GetDamageDealt(a), 0.0001f);
        Assert.AreEqual(5f, boss.GetDamageDealt(b), 0.0001f);
    }

    [Test]
    public void TakeDamage_KeepsFractionalPrecision_EvenThoughHealthRounds()
    {
        // Role fire damage is fractional (Medic deals 0.7 a shot). The health
        // pool rounds, but the meter must not - three Medic shots should read
        // as 2.1, not 2.
        GameObject medic = Ship("Medic");
        MarauderBoss boss = CreateBoss(medic);

        boss.TakeDamage(0.7f, medic);
        boss.TakeDamage(0.7f, medic);
        boss.TakeDamage(0.7f, medic);

        Assert.AreEqual(2.1f, boss.GetDamageDealt(medic), 0.0001f);
    }

    [Test]
    public void TakeDamage_FromSourceNotInTargets_StillRecordsDamage()
    {
        // The damage table uses TryGetValue rather than gating on ContainsKey
        // the way aggro does, so a shot from something that never made it into
        // targets[] shows up on the meter instead of silently vanishing.
        GameObject known = Ship("Known");
        GameObject stranger = Ship("Stranger");
        MarauderBoss boss = CreateBoss(known);

        boss.TakeDamage(4f, stranger);

        Assert.AreEqual(4f, boss.GetDamageDealt(stranger), 0.0001f);
    }

    [Test]
    public void TakeDamage_NullSource_DoesNotThrow_AndStillCostsHealth()
    {
        MarauderBoss boss = CreateBoss(Ship("A"));
        int before = boss.CurrentHealth;

        Assert.DoesNotThrow(() => boss.TakeDamage(3f, null));
        Assert.AreEqual(before - 3, boss.CurrentHealth);
    }

    // ------------------------------------------- damage vs aggro separation

    [Test]
    public void TauntedBy_DoesNotCorruptDamageDealt()
    {
        // The regression this whole separate table exists for. TauntedBy
        // overwrites the taunter's *aggro* with (highest + tauntBonus); if the
        // DPS meter read aggro, Tank pressing E would instantly show it as the
        // top damage dealer in the party.
        GameObject attacker = Ship("Attacker");
        GameObject tank = Ship("Tank");
        MarauderBoss boss = CreateBoss(attacker, tank);

        boss.TakeDamage(20f, attacker);
        boss.TakeDamage(3f, tank);

        boss.TauntedBy(tank);

        Assert.AreEqual(3f, boss.GetDamageDealt(tank), 0.0001f,
            "Taunt must not inflate the taunter's recorded damage.");
        Assert.AreEqual(20f, boss.GetDamageDealt(attacker), 0.0001f,
            "Taunt must not affect anyone else's recorded damage either.");
    }

    [Test]
    public void TauntedBy_StillRedirectsAggro()
    {
        // The other half of the same contract: separating the tables must not
        // have broken taunt itself.
        GameObject attacker = Ship("Attacker");
        GameObject tank = Ship("Tank");
        MarauderBoss boss = CreateBoss(attacker, tank);

        boss.TakeDamage(20f, attacker);
        PickTarget(boss);
        Assert.AreSame(attacker, boss.CurrentTarget, "Top damage should hold aggro before the taunt.");

        boss.TauntedBy(tank);
        PickTarget(boss);

        Assert.AreSame(tank, boss.CurrentTarget, "Taunt should pull the boss onto the Tank.");
    }

    // -------------------------------------------------- targeting roster

    [Test]
    public void PickTarget_SkipsInactiveTargets()
    {
        // Why PartySetupBootstrap has to reassign boss.targets to the ships it
        // actually spawned: PickTarget ignores inactive objects, so a targets[]
        // array full of the deactivated marker objects leaves the boss stuck on
        // whatever Awake() picked and never able to retarget.
        GameObject active = Ship("Active");
        GameObject inactive = Ship("Inactive");
        MarauderBoss boss = CreateBoss(active, inactive);

        boss.TakeDamage(50f, inactive);
        inactive.SetActive(false);
        PickTarget(boss);

        Assert.AreSame(active, boss.CurrentTarget,
            "An inactive object must never become the boss's target, however much aggro it holds.");
    }

    // ------------------------------------------------------ combat clock

    [Test]
    public void CombatElapsed_BeforeCombatBegins_IsZero()
    {
        // The clock starts in OnEnable (when LevelSequencer hands over to boss
        // combat), so it must read exactly 0 beforehand - DpsMeterUI divides by
        // it and relies on this to avoid a divide-by-zero.
        MarauderBoss boss = CreateBoss(Ship("A"));

        Assert.AreEqual(0f, boss.CombatElapsed);
    }
}
