using UnityEngine;

// The raw timers behind one ship's ability cell, read off PlayerAbility so
// AbilityCellState can stay a pure mapping. "Active" is the ability's own
// running effect (Speed Boost, Aura Boost); instant abilities have none.
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

    private static float ActiveRemaining(PlayerAbility ability, PlayerRole role)
    {
        if (role == PlayerRole.Support && ability.IsSpeedBoostActive) return ability.SpeedBoostRemaining;
        if (role == PlayerRole.Medic && ability.IsAuraBoosted) return ability.AuraBoostRemaining;
        return 0f;
    }

    private static float ActiveTotal(PlayerAbility ability, PlayerRole role)
    {
        if (role == PlayerRole.Support) return ability.speedBoostDuration;
        return role == PlayerRole.Medic ? ability.auraBoostDuration : 0f;
    }
}
