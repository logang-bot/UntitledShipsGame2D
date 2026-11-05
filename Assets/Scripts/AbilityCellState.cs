using UnityEngine;

// What a party frame's ability cell draws: its phase, how full the square
// sweep is (0..1) and the seconds readout. Pure, so it's unit-tested
// (AbilityCellStateTests).
public struct AbilityCellState
{
    public AbilityPhase phase;
    public float sweep;
    public float seconds;

    public bool IsCoolingDown => phase == AbilityPhase.Cooldown;

    public static AbilityCellState From(AbilityTimes t)
    {
        if (t.down) return new AbilityCellState { phase = AbilityPhase.Offline };
        if (t.activeRemaining > 0f) return Timed(AbilityPhase.Active, t.activeRemaining, Ratio(t.activeRemaining, t.activeTotal));
        if (t.cooldownRemaining > 0f) return Timed(AbilityPhase.Cooldown, t.cooldownRemaining, 1f - Ratio(t.cooldownRemaining, t.cooldownTotal));
        return new AbilityCellState { phase = AbilityPhase.Ready, sweep = 1f };
    }

    private static AbilityCellState Timed(AbilityPhase phase, float seconds, float sweep) =>
        new AbilityCellState { phase = phase, seconds = seconds, sweep = Mathf.Clamp01(sweep) };

    private static float Ratio(float part, float whole) => whole > 0f ? part / whole : 1f;
}
