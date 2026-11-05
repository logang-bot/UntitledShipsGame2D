using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Covers BossDamageLedger, the shared per-ship damage table behind the
// damage meter and every party frame's DPS line, in all three boss fights.
public class BossDamageLedgerTests
{
    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject Ship(string name)
    {
        GameObject go = new GameObject(name);
        created.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    [Test]
    public void Record_AccumulatesPerSource_Independently()
    {
        GameObject a = Ship("A");
        GameObject b = Ship("B");
        BossDamageLedger ledger = new BossDamageLedger();

        ledger.Record(a, 2f);
        ledger.Record(a, 3f);
        ledger.Record(b, 0.7f);

        Assert.AreEqual(5f, ledger.GetDamageDealt(a), 0.0001f);
        Assert.AreEqual(0.7f, ledger.GetDamageDealt(b), 0.0001f);
    }

    [Test]
    public void Record_NullSource_IsIgnored()
    {
        BossDamageLedger ledger = new BossDamageLedger();

        Assert.DoesNotThrow(() => ledger.Record(null, 4f));
        Assert.AreEqual(0f, ledger.GetDamageDealt(null));
    }

    [Test]
    public void GetDamageDealt_UnknownSource_IsZero()
    {
        Assert.AreEqual(0f, new BossDamageLedger().GetDamageDealt(Ship("Stranger")));
    }

    [Test]
    public void ElapsedAt_BeforeCombat_IsZero()
    {
        BossDamageLedger ledger = new BossDamageLedger();

        Assert.IsFalse(ledger.CombatStarted);
        Assert.AreEqual(0f, ledger.ElapsedAt(50f));
    }

    [Test]
    public void ElapsedAt_AfterBegin_CountsFromTheStart()
    {
        BossDamageLedger ledger = new BossDamageLedger();

        ledger.BeginCombatAt(10f);

        Assert.AreEqual(4f, ledger.ElapsedAt(14f), 0.0001f);
    }

    [Test]
    public void BeginCombatAt_SecondCall_DoesNotRestartTheClock()
    {
        BossDamageLedger ledger = new BossDamageLedger();

        ledger.BeginCombatAt(10f);
        ledger.BeginCombatAt(20f);

        Assert.AreEqual(15f, ledger.ElapsedAt(25f), 0.0001f);
    }

    [Test]
    public void BeginCombatAt_TimeZero_StillCountsAsStarted()
    {
        BossDamageLedger ledger = new BossDamageLedger();

        ledger.BeginCombatAt(0f);

        Assert.IsTrue(ledger.CombatStarted);
        Assert.AreEqual(3f, ledger.ElapsedAt(3f), 0.0001f);
    }

    [Test]
    public void EndCombatAt_FreezesTheClock()
    {
        BossDamageLedger ledger = new BossDamageLedger();
        ledger.BeginCombatAt(10f);

        ledger.EndCombatAt(30f);

        Assert.IsTrue(ledger.CombatEnded);
        Assert.AreEqual(20f, ledger.ElapsedAt(99f), 0.0001f);
    }

    [Test]
    public void EndCombatAt_BeforeBegin_IsIgnored()
    {
        BossDamageLedger ledger = new BossDamageLedger();

        ledger.EndCombatAt(30f);

        Assert.IsFalse(ledger.CombatEnded);
    }

    [Test]
    public void DpsAt_DividesDamageByElapsed()
    {
        GameObject a = Ship("A");
        BossDamageLedger ledger = new BossDamageLedger();
        ledger.BeginCombatAt(0f);
        ledger.Record(a, 12f);

        Assert.AreEqual(3f, ledger.DpsAt(a, 4f), 0.0001f);
    }

    [Test]
    public void DpsAt_BeforeCombat_IsZero()
    {
        GameObject a = Ship("A");
        BossDamageLedger ledger = new BossDamageLedger();
        ledger.Record(a, 12f);

        Assert.AreEqual(0f, ledger.DpsAt(a, 4f));
    }
}
