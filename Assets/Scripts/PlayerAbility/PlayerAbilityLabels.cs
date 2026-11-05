/// <summary>
/// The HUD-facing ability name and one-line status for a ship's role, read
/// by PartyFrameUI through PlayerAbility.AbilityName/StatusText.
/// </summary>
public static class PlayerAbilityLabels
{
    public static string NameFor(PlayerRole role)
    {
        switch (role)
        {
            case PlayerRole.Tank: return "Taunt";
            case PlayerRole.Medic: return "Aura Boost";
            case PlayerRole.Support: return "Speed Boost";
            case PlayerRole.Attacker: return "Big Shot";
            default: return "None";
        }
    }

    public static string StatusFor(PlayerAbility ability, PlayerRole role)
    {
        if (role == PlayerRole.Support && ability.IsSpeedBoostActive)
            return $"+{(ability.speedBoostMultiplier - 1f) * 100f:0}% Spd/Rate ({ability.SpeedBoostRemaining:0.0}s)";
        if (role == PlayerRole.Medic && ability.IsAuraBoosted)
            return $"Boosted ({ability.AuraBoostRemaining:0.0}s)";
        string cooldownText = ability.CooldownRemaining > 0f ? $"{ability.CooldownRemaining:0.0}s" : "Ready";
        if (role == PlayerRole.Attacker)
            return $"{cooldownText}  |  Combo next: {ability.AttackerComboExpectedSlot}";
        return cooldownText;
    }
}
