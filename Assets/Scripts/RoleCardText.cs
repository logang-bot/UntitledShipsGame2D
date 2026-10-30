using System.Collections.Generic;

// The words on the role cards and tiles: short tag, role and ship names,
// and the ability lines, from the Pre-Fight Menus design. Stats are not here
// - they come from PlayerRoleStats through RoleStatScale.
public static class RoleCardText
{
    private static readonly Dictionary<PlayerRole, RoleAbilityLine[]> abilities = new Dictionary<PlayerRole, RoleAbilityLine[]>
    {
        { PlayerRole.Attacker, new[] { new RoleAbilityLine("ABILITY", "BIG SHOT", "A wide, heavy shot. Plus a 3-attack combo.") } },
        { PlayerRole.Tank, new[] { new RoleAbilityLine("ABILITY", "TAUNT", "Pulls the boss’s attention."), new RoleAbilityLine("PASSIVE", "SHIELD ARC", "An arc in front of the ship that blocks bullets.") } },
        { PlayerRole.Medic, new[] { new RoleAbilityLine("ABILITY", "AURA BOOST", "Heals and recharges shields of nearby allies.") } },
        { PlayerRole.Support, new[] { new RoleAbilityLine("ABILITY", "SPEED BOOST", "Speeds up the whole party.") } },
    };

    public static string RoleName(PlayerRole role) => role.ToString().ToUpperInvariant();

    public static string Tag(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Attacker: return "ATK";
            case PlayerRole.Tank: return "TANK";
            case PlayerRole.Medic: return "MED";
            default: return "SUP";
        }
    }

    public static string ShipName(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Attacker: return "NOVA-11";
            case PlayerRole.Tank: return "AEGIS-04";
            case PlayerRole.Medic: return "LUMEN-02";
            default: return "ECHO-07";
        }
    }

    public static RoleAbilityLine[] Abilities(PlayerRole role) => abilities[role];

    /// <summary>Support's damage and fire rate are still placeholders, flagged with an amber asterisk.</summary>
    public static bool IsTbd(PlayerRole role, RoleStat stat) =>
        role == PlayerRole.Support && (stat == RoleStat.Damage || stat == RoleStat.FireRate);
}
