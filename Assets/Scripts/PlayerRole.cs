using System.Collections.Generic;
using UnityEngine;

public enum PlayerRole
{
    Attacker,
    Tank,
    Medic,
    Support
}

public struct RoleStats
{
    public float healthMultiplier;
    public float fireRateMultiplier;
    public float moveSpeedMultiplier;
    public Color tintColor;

    public RoleStats(float healthMultiplier, float fireRateMultiplier, float moveSpeedMultiplier, Color tintColor)
    {
        this.healthMultiplier = healthMultiplier;
        this.fireRateMultiplier = fireRateMultiplier;
        this.moveSpeedMultiplier = moveSpeedMultiplier;
        this.tintColor = tintColor;
    }
}

public static class PlayerRoleStats
{
    // Placeholder balancing values, tunable until real playtesting/art lands.
    private static readonly Dictionary<PlayerRole, RoleStats> stats = new Dictionary<PlayerRole, RoleStats>
    {
        { PlayerRole.Attacker, new RoleStats(healthMultiplier: 0.8f, fireRateMultiplier: 0.75f, moveSpeedMultiplier: 1f, tintColor: new Color(1f, 0.3f, 0.3f)) },
        { PlayerRole.Tank, new RoleStats(healthMultiplier: 1.6f, fireRateMultiplier: 1.2f, moveSpeedMultiplier: 0.8f, tintColor: new Color(0.3f, 0.5f, 1f)) },
        { PlayerRole.Medic, new RoleStats(healthMultiplier: 1f, fireRateMultiplier: 1f, moveSpeedMultiplier: 1f, tintColor: new Color(0.3f, 1f, 0.4f)) },
        { PlayerRole.Support, new RoleStats(healthMultiplier: 1f, fireRateMultiplier: 1f, moveSpeedMultiplier: 1.15f, tintColor: new Color(1f, 0.85f, 0.3f)) },
    };

    public static RoleStats Get(PlayerRole role) => stats[role];
}
