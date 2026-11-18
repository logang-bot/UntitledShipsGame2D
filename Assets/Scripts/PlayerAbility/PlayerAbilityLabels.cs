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
            case PlayerRole.Tank: return "Super Shield";
            case PlayerRole.Medic: return "Aura Boost";
            case PlayerRole.Support: return "Speed Boost";
            case PlayerRole.Attacker: return "Nova Beam";
            default: return "None";
        }
    }

    public static string StatusFor(PlayerAbility ability, PlayerRole role)
    {
        string running = RunningStatus(ability, role);
        if (running != null) return running;
        string cooldownText = ability.CooldownRemaining > 0f ? $"{ability.CooldownRemaining:0.0}s" : "Ready";
        if (role == PlayerRole.Attacker)
            return $"{cooldownText}  |  Combo next: {ability.AttackerComboExpectedSlot}";
        return cooldownText;
    }

    /// <summary>The status while the role's ability is running, or null when it isn't.</summary>
    private static string RunningStatus(PlayerAbility ability, PlayerRole role)
    {
        if (role == PlayerRole.Support && ability.IsSpeedBoostActive)
            return $"+{(ability.speedBoostMultiplier - 1f) * 100f:0}% Spd/Rate ({ability.SpeedBoostRemaining:0.0}s)";
        if (role == PlayerRole.Medic && ability.IsAuraBoosted)
            return $"Boosted ({ability.AuraBoostRemaining:0.0}s)";
        if (role == PlayerRole.Tank && ability.Tank.SuperShieldRemaining > 0f)
            return $"Shielded ({ability.Tank.SuperShieldRemaining:0.0}s)";
        if (role == PlayerRole.Attacker && ability.AttackerCastElapsed >= 0f)
            return "Firing";
        return null;
    }
}
