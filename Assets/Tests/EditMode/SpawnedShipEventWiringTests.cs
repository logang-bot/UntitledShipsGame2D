using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

// Same harness approach as WardenBossDamageTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection. A runtime-added PlayerAbility has a null OnTaunt
// (only serialized ships get one), so each test ship is given a fresh event.
public class SpawnedShipEventWiringTests
{
    private static readonly MethodInfo WardenAwake = typeof(WardenBoss)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<GameObject> created = new List<GameObject>();

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

    private PlayerAbility Ship()
    {
        GameObject ship = Track(new GameObject("TestShip"));
        ship.AddComponent<PlayerHealth>();
        PlayerAbility ability = ship.AddComponent<PlayerAbility>();
        ability.OnTaunt = new UnityEvent();
        return ability;
    }

    private WardenBoss Warden()
    {
        WardenBoss boss = Track(new GameObject("TestWardenBoss")).AddComponent<WardenBoss>();
        WardenAwake.Invoke(boss, null);
        return boss;
    }

    [Test]
    public void Taunt_AgainstWarden_SetsTauntedShip()
    {
        WardenBoss boss = Warden();
        PlayerAbility ship = Ship();
        SpawnedShipEventWiring.Wire(ship.gameObject, boss, null);
        ship.OnTaunt.Invoke();
        Assert.AreSame(ship.gameObject, boss.TauntedShip);
    }

    [Test]
    public void Taunt_AgainstHalcyon_AddsNoListener()
    {
        HalcyonBoss boss = Track(new GameObject("TestHalcyonBoss")).AddComponent<HalcyonBoss>();
        PlayerAbility ship = Ship();
        SpawnedShipEventWiring.Wire(ship.gameObject, boss, null);
        Assert.DoesNotThrow(() => ship.OnTaunt.Invoke());
    }

    [Test]
    public void NullCameraShake_DoesNotThrow()
    {
        PlayerAbility ship = Ship();
        Assert.DoesNotThrow(() => SpawnedShipEventWiring.Wire(ship.gameObject, Warden(), null));
    }
}
