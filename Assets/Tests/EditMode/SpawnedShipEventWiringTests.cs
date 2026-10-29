using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

// A runtime-added PlayerAbility has a null OnTaunt (only serialized ships
// get one), so the test ship is given a fresh event. Taunt wiring is
// covered by BossEventsTests.
public class SpawnedShipEventWiringTests
{
    private GameObject shipGO;

    [TearDown]
    public void TearDown()
    {
        if (shipGO != null) Object.DestroyImmediate(shipGO);
    }

    [Test]
    public void NullCameraShake_DoesNotThrow()
    {
        shipGO = new GameObject("TestShip");
        shipGO.AddComponent<PlayerHealth>();
        shipGO.AddComponent<PlayerAbility>().OnTaunt = new UnityEvent();
        Assert.DoesNotThrow(() => SpawnedShipEventWiring.Wire(shipGO, null));
    }
}
