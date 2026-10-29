using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Same harness approach as WardenBossDamageTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so Awake() and
// the private pulse-damage step are invoked explicitly by reflection.
// PlayerHealth.Start() never runs here either, so ships start at 0 HP / 0
// shield and every assertion measures the drop from 0.
public class HalcyonStaticFieldTests
{
    private static readonly MethodInfo AwakeMethod = typeof(HalcyonStaticField)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo ApplyPulseDamageMethod = typeof(HalcyonStaticField)
        .GetMethod("ApplyPulseDamage", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<GameObject> created = new List<GameObject>();
    private HalcyonStaticField field;

    [SetUp]
    public void SetUp()
    {
        GameObject bossGO = Track(new GameObject("TestHalcyonBoss"));
        bossGO.AddComponent<HalcyonBoss>().bulletDamage = 1f;
        field = bossGO.AddComponent<HalcyonStaticField>();
        AwakeMethod.Invoke(field, null);
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

    private PlayerHealth ShipAt(float x, float y)
    {
        GameObject ship = Track(new GameObject("TestShip"));
        ship.transform.position = new Vector3(x, y, 0f);
        return ship.AddComponent<PlayerHealth>();
    }

    private void Pulse(params PlayerHealth[] ships)
    {
        field.ships = System.Array.ConvertAll(ships, s => s.gameObject);
        ApplyPulseDamageMethod.Invoke(field, null);
    }

    [Test]
    public void LoneShipInRange_TakesSoloDamage()
    {
        PlayerHealth ship = ShipAt(1f, 0f);
        Pulse(ship);
        Assert.AreEqual(-1, ship.CurrentHealth);
    }

    [Test]
    public void TwoClusteredShipsInRange_EachTakeClusterDamage()
    {
        PlayerHealth a = ShipAt(1f, 0f);
        PlayerHealth b = ShipAt(1.3f, 0f);
        Pulse(a, b);
        Assert.AreEqual(-3, a.CurrentHealth);
        Assert.AreEqual(-3, b.CurrentHealth);
    }

    [Test]
    public void ThreeClusteredShipsInRange_EachTakeDamagePerPartner()
    {
        PlayerHealth a = ShipAt(1f, 0f);
        PlayerHealth b = ShipAt(1.2f, 0f);
        PlayerHealth c = ShipAt(1f, 0.2f);
        Pulse(a, b, c);
        Assert.AreEqual(-6, a.CurrentHealth);
        Assert.AreEqual(-6, b.CurrentHealth);
        Assert.AreEqual(-6, c.CurrentHealth);
    }

    [Test]
    public void ClusterOutsideBossRange_TakesNoDamage()
    {
        PlayerHealth a = ShipAt(4f, 0f);
        PlayerHealth b = ShipAt(4.2f, 0f);
        Pulse(a, b);
        Assert.AreEqual(0, a.CurrentHealth);
        Assert.AreEqual(0, b.CurrentHealth);
    }

    [Test]
    public void ShipJustOutsideRing_TakesNoDamage()
    {
        PlayerHealth ship = ShipAt(field.bossRange + 0.05f, 0f);
        Pulse(ship);
        Assert.AreEqual(0, ship.CurrentHealth);
    }
}
