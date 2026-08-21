using UnityEngine;

// Applies the role assignment carried over from the RoleSelect scene before
// any other script's Awake() runs, so PlayerRoleComponent/PlayerHealth/
// PlayerAbility/PlayerController all read the correct role on their own
// first Awake/Start (each of those reads role exactly once, at startup, and
// never re-applies it later).
[DefaultExecutionOrder(-1000)]
public class PartySetupBootstrap : MonoBehaviour
{
    public PlayerRoleComponent player;
    public PlayerRoleComponent[] teammates; // fixed order: [0]=Teammate_Tank, [1]=Teammate_Medic, [2]=Teammate_Support

    void Awake()
    {
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
}
