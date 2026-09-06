using UnityEngine;

// One spawned ship's stable network id plus its GameObject - the handshake
// between NetworkPartySpawner (creates it) and HostStateBroadcaster/
// RemoteStateReceiver (consume it) for matching a ShipStateSnapshot.id back
// to the right ship in the scene. id is a presence UserId for a
// player-owned ship, or "ai:<role>" for an AI-filled slot - stable across
// machines only once every machine builds the same 4-slot roster in the
// same order from the same NetworkSession.Players (guaranteed by the
// online role-select flow, not by this type itself).
public struct NetworkShipEntry
{
    public string id;
    public GameObject ship;
}
