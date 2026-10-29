using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Spawns one Ship.prefab per role for the co-op join flow: a device-paired
// ship for each CoOpRoster player, then an AI ship for every role nobody
// picked. Ship.prefab defaults to the AI shape (PlayerInput disabled,
// AIController enabled) so a plain Instantiate never auto-pairs an already
// claimed device; PlayerInput.Instantiate pairs devices but doesn't flip
// those flags, so a human ship sets both explicitly. A player whose device
// was unplugged after JoinLobby falls back to AI instead of an
// uncontrollable ship.
public static class PartyShipSpawner
{
    public static GameObject[] SpawnParty(GameObject prefab, Vector3[] positions)
    {
        List<GameObject> ships = CoOpRoster.Players.Where(p => p.role.HasValue).Select(p => SpawnPlayerSlot(prefab, p)).ToList();
        foreach (PlayerRole role in UnpickedRoles()) ships.Add(SpawnAi(prefab, role));
        for (int i = 0; i < ships.Count; i++) ships[i].transform.position = positions[i];
        return ships.ToArray();
    }

    private static IEnumerable<PlayerRole> UnpickedRoles()
    {
        IEnumerable<PlayerRole> taken = CoOpRoster.Players.Where(p => p.role.HasValue).Select(p => p.role.Value);
        return System.Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().Except(taken);
    }

    private static GameObject SpawnPlayerSlot(GameObject prefab, JoinedPlayer joined)
    {
        GameObject ship = DevicesAvailable(joined) ? SpawnHuman(prefab, joined) : SpawnUnpluggedFallback(prefab);
        ship.GetComponent<PlayerRoleComponent>().role = joined.role.Value;
        return ship;
    }

    private static bool DevicesAvailable(JoinedPlayer joined)
    {
        return joined.devices != null && joined.devices.Length > 0 && joined.devices.All(d => d != null);
    }

    private static GameObject SpawnHuman(GameObject prefab, JoinedPlayer joined)
    {
        PlayerInput input = PlayerInput.Instantiate(prefab, controlScheme: joined.controlScheme, pairWithDevices: joined.devices);
        input.enabled = true;
        input.GetComponent<AIController>().enabled = false;
        return input.gameObject;
    }

    private static GameObject SpawnUnpluggedFallback(GameObject prefab)
    {
        Debug.LogWarning("CoOpRoster: a joined player's device(s) are no longer available - falling back that slot to AI.");
        return SpawnAiShip(prefab);
    }

    private static GameObject SpawnAi(GameObject prefab, PlayerRole role)
    {
        GameObject ship = SpawnAiShip(prefab);
        ship.GetComponent<PlayerRoleComponent>().role = role;
        return ship;
    }

    private static GameObject SpawnAiShip(GameObject prefab)
    {
        GameObject ship = Object.Instantiate(prefab);
        ship.GetComponent<PlayerInput>().enabled = false;
        ship.GetComponent<AIController>().enabled = true;
        return ship;
    }
}
