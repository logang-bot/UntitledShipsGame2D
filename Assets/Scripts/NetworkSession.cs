using System.Collections.Generic;

// Carries the online Nakama match's connected players (presence id, picked
// role) and whether this client is the match host, from OnlineJoin/
// NetworkRoleSelect into Gameplay.unity. A plain static class, mirroring
// CoOpRoster.cs/PartyRoleAssignment.cs's exact pattern: survives
// SceneManager.LoadScene within one Play session, resets to null on domain
// reload. Players is left null whenever the online flow wasn't used - every
// consumer falls back to CoOpRoster.Players/PartyRoleAssignment.HumanRole,
// the same fallback chain those two already use for their own unset case.
public struct NetworkPlayer
{
    public string presenceId;
    public PlayerRole? role;
}

public static class NetworkSession
{
    public static string MatchId;
    public static bool IsHost;
    public static List<NetworkPlayer> Players;
}
