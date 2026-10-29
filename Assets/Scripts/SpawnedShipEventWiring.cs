using UnityEngine;

// Runtime replacement for the camera-shake listeners Ship.prefab can't
// carry: a prefab's persistent listener can't target a scene object (the
// Main Camera's CameraShake), so ships spawned by the co-op flow would
// otherwise get null-target listeners that silently do nothing. Taunt is
// wired separately by PartyBossWiring. Hand-placed scene ships keep their
// scene overrides, so this is only applied to spawned ships.
public static class SpawnedShipEventWiring
{
    public static void Wire(GameObject ship, CameraShake cameraShake)
    {
        if (cameraShake == null) return;
        ship.GetComponent<PlayerHealth>().OnDamaged.AddListener(cameraShake.Shake);
        ship.GetComponent<PlayerAbility>().OnTaunt.AddListener(cameraShake.Shake);
    }
}
