using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossPanelUI : MonoBehaviour
{
    public MarauderBoss boss; // set by Bind() when LevelSetup spawns the boss
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI warningText;
    public TextMeshProUGUI shockwaveCooldownText;
    public TextMeshProUGUI guidedMissileCooldownText;
    public TextMeshProUGUI patternBarrageWarningText;
    public TextMeshProUGUI patternBarrageCooldownText;

    public void Bind(GameObject bossObject)
    {
        boss = bossObject.GetComponent<MarauderBoss>();
    }

    void Update()
    {
        if (boss == null) return;
        UpdateHealth();
        targetText.text = $"Target: {TargetRoleLabel()}";
        SetText(warningText, boss.GuidedMissileTargetRole.HasValue ? $"Guided missile: {boss.GuidedMissileTargetRole.Value}" : "");
        SetText(shockwaveCooldownText, FormatCooldown("Shockwave", boss.ShockwaveCooldownRemaining));
        SetText(guidedMissileCooldownText, FormatCooldown("Guided Missile", boss.GuidedMissileCooldownRemaining));
        SetText(patternBarrageWarningText, boss.PatternBarrageActivePattern.HasValue ? $"Incoming: {boss.PatternBarrageActivePattern.Value} Barrage" : "");
        SetText(patternBarrageCooldownText, FormatCooldown("Pattern Barrage", boss.PatternBarrageCooldownRemaining));
    }

    private void UpdateHealth()
    {
        healthBarFill.fillAmount = (float)boss.CurrentHealth / boss.maxHealth;
        healthText.text = $"HP: {boss.CurrentHealth}/{boss.maxHealth}";
        phaseText.text = boss.IsPhase2 ? "Phase 2" : "Phase 1";
    }

    private string TargetRoleLabel()
    {
        PlayerRoleComponent targetRole = boss.CurrentTarget != null ? boss.CurrentTarget.GetComponent<PlayerRoleComponent>() : null;
        return targetRole != null ? targetRole.role.ToString() : "--";
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null) text.text = value;
    }

    private static string FormatCooldown(string label, float remaining)
    {
        return remaining > 0f ? $"{label}: {remaining:0.0}s" : $"{label}: Ready";
    }

    public void ShowDefeated()
    {
        phaseText.text = "DEFEATED";
    }
}
