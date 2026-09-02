using System.Collections.Generic;
using UnityEngine;

public enum PlayerRole
{
    Attacker,
    Tank,
    Medic,
    Support
}

// Fixed, absolute per-role stats - the single source of truth for a role's
// base numbers. No multipliers here: temporary effects (buffs/abilities)
// are applied non-destructively at the point of use (see
// PlayerController.speedBuffMultiplier/fireRateBuffMultiplier), never by
// scaling these base values.
public struct RoleStats
{
    public int maxHealth;
    public int maxShield;
    public float fireDamage;
    public float shotsPerSecond; // higher = faster, unlike the old inverted "seconds between shots" field
    public float moveSpeed; // world units/second
    public Color tintColor;

    // This role's designed damage-per-second, assuming every shot lands.
    // Derived, never stored - fireDamage and shotsPerSecond stay the single
    // source of truth, so there's no third number to keep in sync. Note this
    // is a ceiling: ships fire straight up (Vector2.up) at a boss that moves
    // side to side, so what DpsMeterUI reports is always this minus whatever
    // missed. That gap is the positioning skill.
    public float Dps => fireDamage * shotsPerSecond;

    public RoleStats(int maxHealth, int maxShield, float fireDamage, float shotsPerSecond, float moveSpeed, Color tintColor)
    {
        this.maxHealth = maxHealth;
        this.maxShield = maxShield;
        this.fireDamage = fireDamage;
        this.shotsPerSecond = shotsPerSecond;
        this.moveSpeed = moveSpeed;
        this.tintColor = tintColor;
    }
}

public static class PlayerRoleStats
{
    // Placeholder balancing values, tunable until real playtesting lands.
    // See docs/systems/player-roles.md for the full table/reasoning.
    private static readonly Dictionary<PlayerRole, RoleStats> stats = new Dictionary<PlayerRole, RoleStats>
    {
        { PlayerRole.Attacker, new RoleStats(maxHealth: 6, maxShield: 5, fireDamage: 2.0f, shotsPerSecond: 2.5f, moveSpeed: 2.4f, tintColor: new Color(1f, 0.3f, 0.3f)) },
        { PlayerRole.Tank, new RoleStats(maxHealth: 8, maxShield: 20, fireDamage: 1.0f, shotsPerSecond: 1f, moveSpeed: 1.2f, tintColor: new Color(0.3f, 0.5f, 1f)) },
        { PlayerRole.Medic, new RoleStats(maxHealth: 4, maxShield: 3, fireDamage: 0.7f, shotsPerSecond: 1.5f, moveSpeed: 2.4f, tintColor: new Color(0.3f, 1f, 0.4f)) },
        { PlayerRole.Support, new RoleStats(maxHealth: 5, maxShield: 3, fireDamage: 1.0f, shotsPerSecond: 2f, moveSpeed: 3.6f, tintColor: new Color(1f, 0.85f, 0.3f)) },
    };

    public static RoleStats Get(PlayerRole role) => stats[role];
}
