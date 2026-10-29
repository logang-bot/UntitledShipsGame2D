using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Applies the role assignment carried over from the RoleSelect scene before
// any other script's Awake() runs, so PlayerRoleComponent/PlayerHealth/
// PlayerAbility/PlayerController all read the correct role on their own
// first Awake/Start (each of those reads role exactly once, at startup, and
// never re-applies it later).
//
// Two branches: the original fixed-4-scene-object path (CoOpRoster.Players
// unset - direct scene open, or a single human via PartyRoleAssignment) is
// left completely untouched as the fallback; a new dynamic branch spawns a
// Ship.prefab instance per role slot when the co-op join flow (JoinLobby ->
// RoleSelect) was actually used. See docs/systems/player-roles.md's "Co-op
// roster" section for the full design writeup.
[DefaultExecutionOrder(-1000)]
public class PartySetupBootstrap : MonoBehaviour
{
    private const int PartySize = 4;

    [Header("Legacy fallback (opened directly / single-player via PartyRoleAssignment)")]
    public PlayerRoleComponent player;
    public PlayerRoleComponent[] teammates; // fixed order: [0]=Teammate_Tank, [1]=Teammate_Medic, [2]=Teammate_Support

    [Header("Dynamic co-op spawner")]
    public GameObject shipPrefab; // Ship.prefab
    public LevelSequencer levelSequencer;
    public PartyFrameManager partyFrameManager;
    public MonoBehaviour bossObject; // must implement IBoss - MarauderBoss, HalcyonBoss or WardenBoss
    // Only set (and only used) in a Halcyon level scene - Halcyon has no
    // targets[]/aggro roster, but HalcyonStaticField still needs the live
    // spawned roster for its proximity scan, same reason MarauderBoss needs
    // targets[] set below.
    public HalcyonStaticField halcyonStaticField;
    // The Main Camera's CameraShake - spawned ships' hit/taunt shake is wired
    // to it at runtime (see SpawnedShipEventWiring).
    public CameraShake cameraShake;

    void Awake()
    {
        if (CoOpRoster.Players != null && CoOpRoster.Players.Count > 0)
        {
            SpawnDynamicParty();
            return;
        }
        ApplyLegacyRoles();
    }

    private void ApplyLegacyRoles()
    {
        if (!PartyRoleAssignment.HumanRole.HasValue) return;
        PlayerRole humanRole = PartyRoleAssignment.HumanRole.Value;
        player.role = humanRole;
        AssignTeammateRoles(humanRole);
    }

    private void AssignTeammateRoles(PlayerRole humanRole)
    {
        int teammateIndex = 0;
        foreach (PlayerRole candidate in System.Enum.GetValues(typeof(PlayerRole)))
        {
            if (candidate == humanRole) continue;
            if (teammateIndex >= teammates.Length) break;
            teammates[teammateIndex++].role = candidate;
        }
    }

    /// <summary>
    /// Spawns one Ship.prefab instance per PlayerRole slot: a human-paired
    /// instance for each CoOpRoster.Players entry, and an AI-driven instance
    /// for every role nobody picked. The 4 legacy scene objects (player/
    /// teammates[]) are reused purely as position markers, then deactivated -
    /// one authored set of spawn points serves both branches.
    /// </summary>
    private void SpawnDynamicParty()
    {
        Vector3[] positions = DeactivateMarkers();
        GameObject[] spawned = SpawnParty(positions);
        WireSpawnedShips(spawned);
        HandRosterToScene(spawned);
    }

    private Vector3[] DeactivateMarkers()
    {
        PlayerRoleComponent[] markers = { player, teammates[0], teammates[1], teammates[2] };
        Vector3[] positions = markers.Select(m => m.transform.position).ToArray();
        foreach (PlayerRoleComponent marker in markers) marker.gameObject.SetActive(false);
        return positions;
    }

    private GameObject[] SpawnParty(Vector3[] positions)
    {
        List<GameObject> spawned = new List<GameObject>();
        foreach (JoinedPlayer joined in CoOpRoster.Players)
            if (joined.role.HasValue) spawned.Add(SpawnJoinedPlayerShip(joined));
        Queue<PlayerRole> aiRoles = UnpickedRoles();
        while (spawned.Count < PartySize) spawned.Add(SpawnAIShip(aiRoles.Dequeue()));
        for (int i = 0; i < spawned.Count; i++) spawned[i].transform.position = positions[i];
        return spawned.ToArray();
    }

    private static Queue<PlayerRole> UnpickedRoles()
    {
        List<PlayerRole> taken = CoOpRoster.Players.Where(p => p.role.HasValue).Select(p => p.role.Value).ToList();
        IEnumerable<PlayerRole> allRoles = System.Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>();
        return new Queue<PlayerRole>(allRoles.Where(r => !taken.Contains(r)));
    }

    /// <summary>
    /// A device unplugged between JoinLobby and the level scene falls that
    /// slot back to AI rather than spawning an uncontrollable ship.
    /// </summary>
    private GameObject SpawnJoinedPlayerShip(JoinedPlayer joined)
    {
        GameObject ship = HasLiveDevices(joined) ? SpawnHumanShip(joined) : SpawnFallbackAIShip();
        ship.GetComponent<PlayerRoleComponent>().role = joined.role.Value;
        return ship;
    }

    private static bool HasLiveDevices(JoinedPlayer joined)
    {
        return joined.devices != null && joined.devices.Length > 0 && joined.devices.All(d => d != null);
    }

    /// <summary>
    /// Ship.prefab defaults to the "AI slot" shape (PlayerInput disabled,
    /// AIController enabled) so a plain Instantiate() never auto-pairs an
    /// already-claimed device. PlayerInput.Instantiate() only pairs devices
    /// and picks the control scheme - it doesn't flip that serialized
    /// `enabled` back on, so the human driver is set explicitly here.
    /// </summary>
    private GameObject SpawnHumanShip(JoinedPlayer joined)
    {
        PlayerInput pi = PlayerInput.Instantiate(shipPrefab, controlScheme: joined.controlScheme, pairWithDevices: joined.devices);
        SetDriver(pi.gameObject, true);
        return pi.gameObject;
    }

    private GameObject SpawnFallbackAIShip()
    {
        Debug.LogWarning("CoOpRoster: a joined player's device(s) are no longer available - falling back that slot to AI.");
        return SpawnAIDrivenShip();
    }

    private GameObject SpawnAIShip(PlayerRole role)
    {
        GameObject ship = SpawnAIDrivenShip();
        ship.GetComponent<PlayerRoleComponent>().role = role;
        return ship;
    }

    private GameObject SpawnAIDrivenShip()
    {
        GameObject ship = Instantiate(shipPrefab);
        SetDriver(ship, false);
        return ship;
    }

    private static void SetDriver(GameObject ship, bool human)
    {
        ship.GetComponent<PlayerInput>().enabled = human;
        ship.GetComponent<AIController>().enabled = !human;
    }

    private void WireSpawnedShips(GameObject[] spawned)
    {
        Transform[] allAllies = spawned.Select(go => go.transform).ToArray();
        Transform[] aiOnly = spawned.Where(go => go.GetComponent<AIController>().enabled).Select(go => go.transform).ToArray();
        foreach (GameObject ship in spawned) WireShip(ship, allAllies, aiOnly);
    }

    private void WireShip(GameObject ship, Transform[] allAllies, Transform[] aiOnly)
    {
        ship.tag = "Player";
        ship.GetComponent<PlayerAbility>().allies = allAllies;
        ship.GetComponent<PlayerController>().bossObject = bossObject;
        WireAIController(ship.GetComponent<AIController>(), aiOnly);
        SpawnedShipEventWiring.Wire(ship, bossObject, cameraShake);
    }

    private void WireAIController(AIController ai, Transform[] aiOnly)
    {
        if (!ai.enabled) return;
        ai.bossObject = bossObject;
        ai.teammates = aiOnly;
    }

    /// <summary>
    /// Safe to assign directly (not via Inspector) because this script runs
    /// at [DefaultExecutionOrder(-1000)], before LevelSequencer/
    /// PartyFrameManager/boss Awake() read these fields. Without the boss
    /// rosters, every boss would keep pointing at the deactivated markers -
    /// Marauder's aggro table would never see the spawned ships (no threat,
    /// a dead CurrentTarget, Taunt silently no-op), and Halcyon's Static
    /// Field/Warden's arms would never find a live ship.
    /// </summary>
    private void HandRosterToScene(GameObject[] spawned)
    {
        if (levelSequencer != null) levelSequencer.ships = spawned.Select(go => go.GetComponent<PlayerController>()).ToArray();
        if (partyFrameManager != null) partyFrameManager.players = spawned;
        if (bossObject is MarauderBoss marauderBoss) marauderBoss.targets = spawned;
        if (halcyonStaticField != null) halcyonStaticField.ships = spawned;
        if (bossObject is WardenBoss wardenBoss) wardenBoss.ships = spawned;
    }
}
