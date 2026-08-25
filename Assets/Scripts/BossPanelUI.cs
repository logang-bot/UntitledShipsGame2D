using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossPanelUI : MonoBehaviour
{
    public Level1Boss boss; // direct scene reference - BossPanel is scene-bound, not a reusable prefab
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI warningText;
    public TextMeshProUGUI shockwaveCooldownText;
    public TextMeshProUGUI guidedMissileCooldownText;
    public TextMeshProUGUI patternBarrageWarningText;
    public TextMeshProUGUI patternBarrageCooldownText;

    void Update()
    {
        if (boss == null) return;

        healthBarFill.fillAmount = (float)boss.CurrentHealth / boss.maxHealth;
        healthText.text = $"HP: {boss.CurrentHealth}/{boss.maxHealth}";
        phaseText.text = boss.IsPhase2 ? "Phase 2" : "Phase 1";

        if (boss.CurrentTarget != null)
        {
            PlayerRoleComponent targetRole = boss.CurrentTarget.GetComponent<PlayerRoleComponent>();
            targetText.text = targetRole != null ? $"Target: {targetRole.role}" : "Target: --";
        }
        else
        {
            targetText.text = "Target: --";
        }

        if (warningText != null)
        {
            warningText.text = boss.GuidedMissileTargetRole.HasValue
                ? $"Guided missile: {boss.GuidedMissileTargetRole.Value}"
                : "";
        }

        if (shockwaveCooldownText != null)
        {
            shockwaveCooldownText.text = boss.ShockwaveCooldownRemaining > 0f
                ? $"Shockwave: {boss.ShockwaveCooldownRemaining:0.0}s"
                : "Shockwave: Ready";
        }

        if (guidedMissileCooldownText != null)
        {
            guidedMissileCooldownText.text = boss.GuidedMissileCooldownRemaining > 0f
                ? $"Guided Missile: {boss.GuidedMissileCooldownRemaining:0.0}s"
                : "Guided Missile: Ready";
        }

        if (patternBarrageWarningText != null)
        {
            patternBarrageWarningText.text = boss.PatternBarrageActivePattern.HasValue
                ? $"Incoming: {boss.PatternBarrageActivePattern.Value} Barrage"
                : "";
        }

        if (patternBarrageCooldownText != null)
        {
            patternBarrageCooldownText.text = boss.PatternBarrageCooldownRemaining > 0f
                ? $"Pattern Barrage: {boss.PatternBarrageCooldownRemaining:0.0}s"
                : "Pattern Barrage: Ready";
        }
    }

    public void ShowDefeated()
    {
        phaseText.text = "DEFEATED";
    }
}
