using UnityEngine;

// Runtime replacement for the Inspector listeners Ship.prefab can't carry:
// a prefab's persistent listener can't target a scene object (the Main
// Camera's CameraShake, the level's boss), so ships spawned by the co-op
// flow would otherwise get null-target listeners that silently do nothing.
// Hand-placed legacy scene ships keep their per-scene overrides instead, so
// this is only applied to spawned ships. GameOverUI/PartyFrameManager wire
// their own OnDeath listeners the same way.
public static class SpawnedShipEventWiring
{
    public static void Wire(GameObject ship, MonoBehaviour boss, CameraShake cameraShake)
    {
        WireCameraShake(ship, cameraShake);
        WireTaunt(ship, boss);
    }

    private static void WireCameraShake(GameObject ship, CameraShake cameraShake)
    {
        if (cameraShake == null) return;
        ship.GetComponent<PlayerHealth>().OnDamaged.AddListener(cameraShake.Shake);
        ship.GetComponent<PlayerAbility>().OnTaunt.AddListener(cameraShake.Shake);
    }

    /// <summary>
    /// Only bosses with a TauntedBy listener are wired - Halcyon has none,
    /// so Taunt stays a deliberate no-op against it.
    /// </summary>
    private static void WireTaunt(GameObject ship, MonoBehaviour boss)
    {
        PlayerAbility ability = ship.GetComponent<PlayerAbility>();
        if (boss is MarauderBoss marauder) ability.OnTaunt.AddListener(() => marauder.TauntedBy(ship));
        if (boss is WardenBoss warden) ability.OnTaunt.AddListener(() => warden.TauntedBy(ship));
    }
}
