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
    [Header("Legacy fallback (opened directly / single-player via PartyRoleAssignment)")]
    public PlayerRoleComponent player;
    public PlayerRoleComponent[] teammates; // fixed order: [0]=Teammate_Tank, [1]=Teammate_Medic, [2]=Teammate_Support

    [Header("Dynamic co-op spawner")]
    public GameObject shipPrefab; // Ship.prefab
    public LevelSequencer levelSequencer;
    public PartyFrameManager partyFrameManager;
    public MarauderBoss boss;

    void Awake()
    {
        if (CoOpRoster.Players != null && CoOpRoster.Players.Count > 0)
        {
            SpawnDynamicParty();
            return;
        }

        if (!PartyRoleAssignment.HumanRole.HasValue) return;

        PlayerRole humanRole = PartyRoleAssignment.HumanRole.Value;
        player.role = humanRole;

        int teammateIndex = 0;
        foreach (PlayerRole candidate in System.Enum.GetValues(typeof(PlayerRole)))
        {
            if (candidate == humanRole) continue;
            if (teammateIndex >= teammates.Length) break;
            teammates[teammateIndex].role = candidate;
            teammateIndex++;
        }
    }

    // Spawns one Ship.prefab instance per PlayerRole slot: a human-paired
    // instance (PlayerInput.Instantiate, wired to that player's own device)
    // for each CoOpRoster.Players entry, and a plain AI-driven instance for
    // every role nobody picked. The 4 legacy scene objects (player/
    // teammates[]) are reused purely as position markers, then deactivated -
    // this keeps a single authored set of spawn points for both branches
    // instead of duplicating scene setup.
    private void SpawnDynamicParty()
    {
        PlayerRoleComponent[] markers = new PlayerRoleComponent[] { player, teammates[0], teammates[1], teammates[2] };
        Vector3[] positions = markers.Select(m => m.transform.position).ToArray();
        foreach (PlayerRoleComponent marker in markers) marker.gameObject.SetActive(false);

        List<PlayerRole> takenRoles = CoOpRoster.Players.Where(p => p.role.HasValue).Select(p => p.role.Value).ToList();
        Queue<PlayerRole> aiRoles = new Queue<PlayerRole>(
            System.Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().Where(r => !takenRoles.Contains(r)));

        GameObject[] spawned = new GameObject[4];
        int slot = 0;

        foreach (JoinedPlayer joined in CoOpRoster.Players)
        {
            if (!joined.role.HasValue) continue; // defensive - RoleSelect only proceeds once every player has locked a role

            GameObject shipGO;
            if (joined.devices != null && joined.devices.Length > 0 && joined.devices.All(d => d != null))
            {
                PlayerInput pi = PlayerInput.Instantiate(shipPrefab, controlScheme: joined.controlScheme, pairWithDevices: joined.devices);
                shipGO = pi.gameObject;
                // Ship.prefab's PlayerInput/AIController both default to the
                // "AI slot" shape (PlayerInput disabled, AIController
                // enabled) so a plain Instantiate() for an AI slot never
                // triggers PlayerInput's auto-pair-on-enable against an
                // already-claimed device. PlayerInput.Instantiate() only
                // handles device pairing/control-scheme selection - it does
                // not flip that serialized `enabled` back on, so both halves
                // need an explicit, opposite override here for the human
                // slot.
                pi.enabled = true;
                shipGO.GetComponent<AIController>().enabled = false;
            }
            else
            {
                // Device unplugged between JoinLobby and the level scene -
                // fall back this slot to AI rather than spawning an
                // uncontrollable ship.
                Debug.LogWarning("CoOpRoster: a joined player's device(s) are no longer available - falling back that slot to AI.");
                shipGO = Instantiate(shipPrefab);
                shipGO.GetComponent<PlayerInput>().enabled = false;
                shipGO.GetComponent<AIController>().enabled = true;
            }

            shipGO.transform.position = positions[slot];
            shipGO.GetComponent<PlayerRoleComponent>().role = joined.role.Value;
            spawned[slot] = shipGO;
            slot++;
        }

        while (slot < 4)
        {
            GameObject shipGO = Instantiate(shipPrefab);
            shipGO.transform.position = positions[slot];
            shipGO.GetComponent<PlayerInput>().enabled = false;
            shipGO.GetComponent<AIController>().enabled = true;
            shipGO.GetComponent<PlayerRoleComponent>().role = aiRoles.Dequeue();
            spawned[slot] = shipGO;
            slot++;
        }

        Transform[] allAllies = spawned.Select(go => go.transform).ToArray();
        List<Transform> aiOnly = new List<Transform>();
        List<PlayerController> controllers = new List<PlayerController>();
        foreach (GameObject shipGO in spawned)
        {
            shipGO.tag = "Player";
            shipGO.GetComponent<PlayerAbility>().allies = allAllies;
            shipGO.GetComponent<PlayerController>().boss = boss;

            AIController ai = shipGO.GetComponent<AIController>();
            if (ai.enabled)
            {
                ai.boss = boss;
                aiOnly.Add(shipGO.transform);
            }
            controllers.Add(shipGO.GetComponent<PlayerController>());
        }
        foreach (GameObject shipGO in spawned)
        {
            AIController ai = shipGO.GetComponent<AIController>();
            if (ai.enabled) ai.teammates = aiOnly.ToArray();
        }

        // Safe to assign directly (not via Inspector) because this script
        // runs at [DefaultExecutionOrder(-1000)], guaranteed before
        // LevelSequencer.Awake()/PartyFrameManager.Awake()/MarauderBoss.Awake()
        // read these fields.
        if (levelSequencer != null) levelSequencer.ships = controllers.ToArray();
        if (partyFrameManager != null) partyFrameManager.players = spawned;
        // Without this, the boss keeps pointing at the 4 legacy marker
        // objects this method just deactivated: MarauderBoss.Awake() builds its
        // aggro table from targets[], so every spawned ship would be absent
        // from it - damage would never register as threat, CurrentTarget
        // would stay a disabled marker, and Tank's taunt would silently
        // no-op (TauntedBy early-returns on an unknown taunter). The co-op
        // join flow is the *normal* path into a level scene, so this
        // affected every run that didn't open the scene directly.
        if (boss != null) boss.targets = spawned;
    }
}
