using UnityEngine;

// Puts a ship's flames into BOOST while its speed is buffed (Support's
// party-wide Speed Boost raises speedBuffMultiplier above 1). The model is
// spawned by ShipVisual in Start, so its ShipThrusters is found lazily.
public class ShipBoostFeed : MonoBehaviour
{
    public PlayerController controller;

    private ShipThrusters thrusters;

    void Update()
    {
        if (thrusters == null) thrusters = GetComponentInChildren<ShipThrusters>();
        if (thrusters != null) thrusters.BoostTarget = controller.speedBuffMultiplier > 1f ? 1f : 0f;
    }
}
