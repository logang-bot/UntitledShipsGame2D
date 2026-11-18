// What the Tank's Taunt box shows (design: Neon Fleet HUD, Tank frame):
// READY, PULLING while the taunt holds the boss, COOLDOWN until key 1 works
// again, with the seconds left and how far along that state is (0..1).
// Pure, so it's unit-tested (TauntDisplayTests).
public struct TauntDisplay
{
    public TauntMood mood;
    public float seconds;
    public float progress;

    public string Label => mood switch
    {
        TauntMood.Pulling => "PULLING",
        TauntMood.Cooldown => "COOLDOWN",
        _ => "READY",
    };

    /// <summary>Pulling's progress is the share of the taunt still left; Cooldown's is how much of it has passed.</summary>
    public static TauntDisplay From(float activeRemaining, float cooldownRemaining, TankAbilitySettings settings)
    {
        if (activeRemaining > 0f)
            return new TauntDisplay { mood = TauntMood.Pulling, seconds = activeRemaining, progress = activeRemaining / settings.tauntSeconds };
        if (cooldownRemaining > 0f)
            return new TauntDisplay { mood = TauntMood.Cooldown, seconds = cooldownRemaining, progress = 1f - cooldownRemaining / settings.tauntCooldown };
        return new TauntDisplay { mood = TauntMood.Ready, progress = 1f };
    }
}
