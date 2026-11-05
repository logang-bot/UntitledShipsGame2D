using System;
using System.Linq;

// Role card stat bars. Every row shares one scale - each role's value over
// the largest value any role has for that stat - so roles compare by bar
// length, per the Pre-Fight Menus design. Values come from PlayerRoleStats,
// never from the design's numbers, so a rebalance shows up here for free.
public static class RoleStatScale
{
    private static readonly PlayerRole[] Roles = (PlayerRole[])Enum.GetValues(typeof(PlayerRole));

    public static float Value(PlayerRole role, RoleStat stat)
    {
        RoleStats s = PlayerRoleStats.Get(role);
        switch (stat)
        {
            case RoleStat.Health: return s.maxHealth;
            case RoleStat.Shield: return s.maxShield;
            case RoleStat.Damage: return s.fireDamage;
            case RoleStat.FireRate: return s.shotsPerSecond;
            default: return s.moveSpeed;
        }
    }

    public static float Fraction(PlayerRole role, RoleStat stat)
    {
        float max = Roles.Max(r => Value(r, stat));
        return max > 0f ? Value(role, stat) / max : 0f;
    }

    public static string Format(PlayerRole role, RoleStat stat)
    {
        float value = Value(role, stat);
        if (stat == RoleStat.Health || stat == RoleStat.Shield) return value.ToString("0");
        if (stat == RoleStat.FireRate) return value.ToString("0.#") + "/s";
        return value.ToString("0.0");
    }

    public static string Label(RoleStat stat)
    {
        switch (stat)
        {
            case RoleStat.Health: return "HP";
            case RoleStat.Shield: return "SHIELD";
            case RoleStat.Damage: return "DAMAGE";
            case RoleStat.FireRate: return "FIRE RATE";
            default: return "SPEED";
        }
    }
}
