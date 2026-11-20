using UnityEngine;

// The raw timers behind one ship's ability cell, read off PlayerAbility so
// AbilityCellState can stay a pure mapping. "Active" is the superpower's own
// running effect (Nova Beam, Super Shield, Sanctuary, Echo Gate).
public struct AbilityTimes
{
    public float cooldownRemaining;
    public float cooldownTotal;
    public float activeRemaining;
    public float activeTotal;
    public bool down;

    public static AbilityTimes Read(PlayerAbility ability, PlayerRole role, bool down) => new AbilityTimes
    {
        cooldownRemaining = ability.CooldownRemaining,
        cooldownTotal = ability.CooldownFor(role),
        activeRemaining = ActiveRemaining(ability, role),
        activeTotal = ActiveTotal(ability, role),
        down = down,
    };

    public static float ActiveRemaining(PlayerAbility ability, PlayerRole role) => role switch
    {
        PlayerRole.Tank => ability.Tank.SuperShieldRemaining,
        PlayerRole.Medic => ability.Medic.Sanctuary.Remaining,
        PlayerRole.Support => ability.Support.Gate.Remaining,
        _ => AttackerBeamRemaining(ability),
    };

    private static float AttackerBeamRemaining(PlayerAbility ability) =>
        ability.AttackerCastElapsed >= 0f ? Mathf.Max(0f, ability.beam.TotalSeconds - ability.AttackerCastElapsed) : 0f;

    private static float ActiveTotal(PlayerAbility ability, PlayerRole role) => role switch
    {
        PlayerRole.Tank => ability.tankPowers.superShieldSeconds,
        PlayerRole.Medic => ability.medicPowers.timing.seconds,
        PlayerRole.Support => ability.supportPowers.timing.seconds,
        _ => ability.beam.TotalSeconds,
    };
}
