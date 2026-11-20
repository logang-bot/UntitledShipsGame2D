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
            case PlayerRole.Medic: return "Sanctuary";
            case PlayerRole.Support: return "Echo Gate";
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
        if (role == PlayerRole.Attacker) return ability.AttackerCastElapsed >= 0f ? "Firing" : null;
        float left = AbilityTimes.ActiveRemaining(ability, role);
        return left > 0f ? $"{RunningWord(role)} ({left:0.0}s)" : null;
    }

    private static string RunningWord(PlayerRole role) => role switch
    {
        PlayerRole.Tank => "Shielded",
        PlayerRole.Medic => "Sanctuary up",
        _ => "Gate up",
    };
}
