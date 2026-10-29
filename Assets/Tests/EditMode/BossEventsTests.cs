using NUnit.Framework;
using UnityEngine;

public class BossEventsTests
{
    private GameObject bossGO;
    private GameObject shipGO;

    private T CreateBoss<T>() where T : MonoBehaviour
    {
        bossGO = new GameObject("TestBoss");
        return bossGO.AddComponent<T>();
    }

    private GameObject[] Ships()
    {
        shipGO = new GameObject("TestShip");
        return new[] { shipGO };
    }

    [TearDown]
    public void TearDown()
    {
        if (bossGO != null) Object.DestroyImmediate(bossGO);
        if (shipGO != null) Object.DestroyImmediate(shipGO);
    }

    [Test]
    public void AssignShips_Marauder_SetsTargets()
    {
        MarauderBoss boss = CreateBoss<MarauderBoss>();
        GameObject[] ships = Ships();
        BossEvents.AssignShips(boss, ships);
        Assert.AreSame(ships, boss.targets);
    }

    [Test]
    public void AssignShips_Warden_SetsShips()
    {
        WardenBoss boss = CreateBoss<WardenBoss>();
        GameObject[] ships = Ships();
        BossEvents.AssignShips(boss, ships);
        Assert.AreSame(ships, boss.ships);
    }

    [Test]
    public void AssignShips_Halcyon_SetsStaticFieldShips()
    {
        HalcyonBoss boss = CreateBoss<HalcyonBoss>();
        HalcyonStaticField staticField = bossGO.AddComponent<HalcyonStaticField>();
        GameObject[] ships = Ships();
        BossEvents.AssignShips(boss, ships);
        Assert.AreSame(ships, staticField.ships);
    }

    [Test]
    public void Taunt_Warden_OpensTauntWindowOnTaunter()
    {
        WardenBoss boss = CreateBoss<WardenBoss>();
        GameObject taunter = Ships()[0];
        BossEvents.Taunt(boss, taunter);
        Assert.AreSame(taunter, boss.TauntedShip);
    }

    [Test]
    public void Taunt_Halcyon_DoesNothing()
    {
        HalcyonBoss boss = CreateBoss<HalcyonBoss>();
        Assert.DoesNotThrow(() => BossEvents.Taunt(boss, Ships()[0]));
    }

    [Test]
    public void PhaseAndDefeatedEvents_AreTheBossOwnEvents()
    {
        WardenBoss boss = CreateBoss<WardenBoss>();
        Assert.AreSame(boss.OnPhase2, BossEvents.Phase2(boss));
        Assert.AreSame(boss.OnDefeated, BossEvents.Defeated(boss));
    }
}
