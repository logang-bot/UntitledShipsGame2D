using System;

// Who flies each role, as player numbers indexed by (int)PlayerRole (null =
// CPU): from the live locks while choosing, or from the static carriers once
// Role Select is done (CoOpRoster, else the single-player
// PartyRoleAssignment).
public static class SquadLineup
{
    private static readonly int RoleCount = Enum.GetValues(typeof(PlayerRole)).Length;

    public static int?[] FromLocks(PlayerRole?[] lockedRoleBySlot)
    {
        int?[] lineup = new int?[RoleCount];
        for (int slot = 0; slot < lockedRoleBySlot.Length; slot++)
            if (lockedRoleBySlot[slot].HasValue) lineup[(int)lockedRoleBySlot[slot].Value] = slot + 1;
        return lineup;
    }

    public static int?[] Current()
    {
        if (CoOpRoster.Players != null) return FromLocks(CoOpRoster.Players.ConvertAll(p => p.role).ToArray());
        int?[] lineup = new int?[RoleCount];
        if (PartyRoleAssignment.HumanRole.HasValue) lineup[(int)PartyRoleAssignment.HumanRole.Value] = 1;
        return lineup;
    }
}
