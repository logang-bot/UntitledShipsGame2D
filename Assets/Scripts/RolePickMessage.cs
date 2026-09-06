// Match-state payload for NetworkOpCodes.RolePick - sent by a client the
// instant it locks a role, so every other connected client (and the host,
// which spawns ships from NetworkSession.Players) can reflect the pick.
[System.Serializable]
public struct RolePickMessage
{
    public string presenceId;
    public PlayerRole role;
}
