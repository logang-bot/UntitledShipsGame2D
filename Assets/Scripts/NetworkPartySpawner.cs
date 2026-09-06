using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Network-mode counterpart to PartySetupBootstrap.SpawnDynamicParty() - kept
// in its own file since that method alone is already near this project's
// per-file line budget. One Ship.prefab instance per PlayerRole slot from
// NetworkSession.Players: on the host, NetworkInputReceiver drives every
// remote-owned ship and the host's own PlayerInput drives its own; on a
// client, NetworkInputSender drives its own ship and every ship (including
// that one) has PlayerController disabled - the host is the sole simulator,
// this scene only renders RemoteStateReceiver's broadcast state. Unpicked
// roles still fall back to AIController, exactly like SpawnDynamicParty().
public class NetworkPartySpawner
{
    readonly PartySetupBootstrap bootstrap;

    public NetworkPartySpawner(PartySetupBootstrap bootstrap)
    {
        this.bootstrap = bootstrap;
    }

    public void Spawn()
    {
        Vector3[] positions = bootstrap.CaptureMarkerPositions();
        List<NetworkShipEntry> roster = SpawnShips(positions);
        bootstrap.WireSpawnedParty(roster.Select(entry => entry.ship).ToArray());
        InitializeNetworkSync(roster);
    }

    List<NetworkShipEntry> SpawnShips(Vector3[] positions)
    {
        var roster = new List<NetworkShipEntry>();
        Queue<PlayerRole> aiRoles = BuildUnpickedRoleQueue();
        int slot = 0;
        foreach (NetworkPlayer player in NetworkSession.Players)
            roster.Add(SpawnPlayerShip(player, positions[slot++]));
        while (slot < 4)
            roster.Add(SpawnAiShip(aiRoles.Dequeue(), positions[slot++]));
        return roster;
    }

    Queue<PlayerRole> BuildUnpickedRoleQueue()
    {
        List<PlayerRole> taken = NetworkSession.Players.Where(p => p.role.HasValue).Select(p => p.role.Value).ToList();
        return new Queue<PlayerRole>(
            System.Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().Where(r => !taken.Contains(r)));
    }

    NetworkShipEntry SpawnAiShip(PlayerRole role, Vector3 position)
    {
        GameObject shipGO = Object.Instantiate(bootstrap.shipPrefab, position, Quaternion.identity);
        shipGO.GetComponent<PlayerInput>().enabled = false;
        shipGO.GetComponent<AIController>().enabled = true;
        shipGO.GetComponent<PlayerRoleComponent>().role = role;
        return new NetworkShipEntry { id = "ai:" + role, ship = shipGO };
    }

    NetworkShipEntry SpawnPlayerShip(NetworkPlayer player, Vector3 position)
    {
        GameObject shipGO = Object.Instantiate(bootstrap.shipPrefab, position, Quaternion.identity);
        shipGO.GetComponent<PlayerRoleComponent>().role = player.role ?? PlayerRole.Attacker;
        ConfigurePlayerShipDriver(shipGO, player.presenceId);
        return new NetworkShipEntry { id = player.presenceId, ship = shipGO };
    }

    void ConfigurePlayerShipDriver(GameObject shipGO, string presenceId)
    {
        shipGO.GetComponent<PlayerInput>().enabled = false;
        shipGO.GetComponent<AIController>().enabled = false;
        if (presenceId == NakamaConnection.Instance.LocalUserId) ConfigureLocalHumanDriver(shipGO);
        else if (NetworkSession.IsHost) ConfigureRemoteReceiver(shipGO, presenceId);
    }

    void ConfigureLocalHumanDriver(GameObject shipGO)
    {
        if (NetworkSession.IsHost) { shipGO.GetComponent<PlayerInput>().enabled = true; return; }
        NetworkInputSender sender = shipGO.GetComponent<NetworkInputSender>();
        sender.controlsAsset = bootstrap.networkControlsAsset;
        sender.enabled = true;
    }

    void ConfigureRemoteReceiver(GameObject shipGO, string presenceId)
    {
        NetworkInputReceiver receiver = shipGO.GetComponent<NetworkInputReceiver>();
        receiver.ownerUserId = presenceId;
        receiver.enabled = true;
    }

    void InitializeNetworkSync(List<NetworkShipEntry> roster)
    {
        if (NetworkSession.IsHost) bootstrap.hostStateBroadcaster.Initialize(roster, bootstrap.boss);
        else ConfigureThinClient(roster);
    }

    void ConfigureThinClient(List<NetworkShipEntry> roster)
    {
        foreach (NetworkShipEntry entry in roster) entry.ship.GetComponent<PlayerController>().enabled = false;
        bootstrap.remoteStateReceiver.Initialize(roster);
    }
}
