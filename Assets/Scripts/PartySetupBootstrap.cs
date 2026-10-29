using System.Linq;
using UnityEngine;

// Sets up the party for LevelSetup, before any ship's own Awake/Start reads
// its role (each reads it exactly once, at startup). Two paths: the co-op
// join flow (JoinLobby -> RoleSelect) spawns one Ship.prefab per role via
// PartyShipSpawner, reusing the 4 scene ships purely as position markers;
// otherwise (scene opened directly, or a single human via
// PartyRoleAssignment) the 4 scene ships are used as-is. Either way the
// ships are then wired to the level's boss. See docs/systems/player-roles.md's
// "Co-op roster" section.
public class PartySetupBootstrap : MonoBehaviour
{
    [Header("Scene ships (spawn markers on the co-op path)")]
    public PlayerRoleComponent player;
    public PlayerRoleComponent[] teammates; // fixed order: [0]=Teammate_Tank, [1]=Teammate_Medic, [2]=Teammate_Support

    [Header("Dynamic co-op spawner")]
    public GameObject shipPrefab; // Ship.prefab
    public LevelSequencer levelSequencer;
    public PartyFrameManager partyFrameManager;
    public CameraShake cameraShake; // shaken by spawned ships' hits and taunts

    public void Setup(MonoBehaviour boss)
    {
        bool coOpFlowUsed = CoOpRoster.Players != null && CoOpRoster.Players.Count > 0;
        GameObject[] ships = coOpFlowUsed ? SpawnCoOpParty() : PrepareSceneShips();
        PartyBossWiring.Wire(ships, boss);
    }

    private GameObject[] SceneShips()
    {
        return new[] { player, teammates[0], teammates[1], teammates[2] }.Select(ship => ship.gameObject).ToArray();
    }

    private GameObject[] PrepareSceneShips()
    {
        if (PartyRoleAssignment.HumanRole.HasValue) AssignSceneRoles(PartyRoleAssignment.HumanRole.Value);
        return SceneShips();
    }

    private void AssignSceneRoles(PlayerRole humanRole)
    {
        player.role = humanRole;
        PlayerRole[] others = System.Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().Where(r => r != humanRole).ToArray();
        for (int i = 0; i < teammates.Length && i < others.Length; i++) teammates[i].role = others[i];
    }

    private GameObject[] SpawnCoOpParty()
    {
        GameObject[] ships = PartyShipSpawner.SpawnParty(shipPrefab, HideSceneShips());
        WireAllies(ships);
        foreach (GameObject ship in ships) SpawnedShipEventWiring.Wire(ship, cameraShake);
        if (levelSequencer != null) levelSequencer.ships = ships.Select(s => s.GetComponent<PlayerController>()).ToArray();
        if (partyFrameManager != null) partyFrameManager.players = ships;
        return ships;
    }

    private Vector3[] HideSceneShips()
    {
        GameObject[] markers = SceneShips();
        foreach (GameObject marker in markers) marker.SetActive(false);
        return markers.Select(marker => marker.transform.position).ToArray();
    }

    private static void WireAllies(GameObject[] ships)
    {
        Transform[] allies = ships.Select(s => s.transform).ToArray();
        Transform[] aiShips = ships.Where(IsAi).Select(s => s.transform).ToArray();
        foreach (GameObject ship in ships)
        {
            ship.tag = "Player";
            ship.GetComponent<PlayerAbility>().allies = allies;
            if (IsAi(ship)) ship.GetComponent<AIController>().teammates = aiShips;
        }
    }

    private static bool IsAi(GameObject ship) => ship.GetComponent<AIController>().enabled;
}
