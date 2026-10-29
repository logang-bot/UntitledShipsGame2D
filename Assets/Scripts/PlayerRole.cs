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
}

public static class PlayerRoleStats
{
    // Placeholder balancing values, tunable until real playtesting lands.
    // See docs/systems/player-roles.md for the full table/reasoning. Tint
    // colors match the ship designs (docs/art-roadmap.md's "Source designs").
    private static readonly Dictionary<PlayerRole, RoleStats> stats = new Dictionary<PlayerRole, RoleStats>
    {
        { PlayerRole.Attacker, new RoleStats { maxHealth = 6, maxShield = 5, fireDamage = 2.0f, shotsPerSecond = 2.5f, moveSpeed = 2.4f, tintColor = new Color32(0xFF, 0x8A, 0x3D, 0xFF) } },
        { PlayerRole.Tank, new RoleStats { maxHealth = 8, maxShield = 20, fireDamage = 1.0f, shotsPerSecond = 1f, moveSpeed = 1.2f, tintColor = new Color32(0x00, 0xE5, 0xFF, 0xFF) } },
        { PlayerRole.Medic, new RoleStats { maxHealth = 4, maxShield = 3, fireDamage = 0.7f, shotsPerSecond = 1.5f, moveSpeed = 2.4f, tintColor = new Color32(0xB6, 0xFF, 0x3C, 0xFF) } },
        { PlayerRole.Support, new RoleStats { maxHealth = 5, maxShield = 3, fireDamage = 1.0f, shotsPerSecond = 2f, moveSpeed = 3.6f, tintColor = new Color32(0xA8, 0x55, 0xF7, 0xFF) } },
    };

    public static RoleStats Get(PlayerRole role) => stats[role];
}
