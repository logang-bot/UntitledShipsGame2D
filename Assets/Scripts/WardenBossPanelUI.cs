// Assets/Scripts/WardenBossPanelUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level 3's BossPanel script - same "HUD only reads, never owns game state"
// pattern as BossPanelUI.cs/HalcyonBossPanelUI.cs. No single target/aggro
// text - there's no single-target concept in this fight. See
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenBossPanelUI : MonoBehaviour
{
    public WardenBoss boss; // direct scene reference - scene-bound, not a reusable prefab
    public WardenArm armA;
    public WardenArm armB;
    public WardenArm armC;
    public WardenShockwave shockwave;
    public WardenLockdownVolley lockdownVolley;

    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI armAWarningText;
    public TextMeshProUGUI armBWarningText;
    public TextMeshProUGUI armCWarningText;
    public TextMeshProUGUI shockwaveCooldownText;
    public TextMeshProUGUI lockdownWarningText;
    public TextMeshProUGUI lockdownCooldownText;

    void Update()
    {
        if (boss == null) return;
        UpdateHealth();
        UpdateArmText(armA, armAWarningText, "Arm A");
        UpdateArmText(armB, armBWarningText, "Arm B");
        UpdateArmText(armC, armCWarningText, "Arm C");
        UpdateShockwaveText();
        UpdateLockdownText();
    }

    private void UpdateHealth()
    {
        healthBarFill.fillAmount = (float)boss.CurrentHealth / boss.maxHealth;
        healthText.text = $"HP: {boss.CurrentHealth}/{boss.maxHealth}";
        phaseText.text = boss.IsPhase2 ? "Phase 2" : "Phase 1";
    }

    private void UpdateArmText(WardenArm arm, TextMeshProUGUI text, string label)
    {
        if (text == null) return;
        bool armLive = arm != null && arm.isActiveAndEnabled;
        text.text = armLive ? $"{label}: {ArmTargetLabel(arm)}" : $"{label}: —";
    }

    private string ArmTargetLabel(WardenArm arm)
    {
        return arm.CurrentTargetRole.HasValue ? arm.CurrentTargetRole.Value.ToString() : "--";
    }

    private void UpdateShockwaveText()
    {
        if (shockwave == null || shockwaveCooldownText == null) return;
        shockwaveCooldownText.text = FormatCooldown("Shockwave", shockwave.CooldownRemaining);
    }

    private void UpdateLockdownText()
    {
        if (lockdownVolley == null) return;
        if (lockdownWarningText != null)
            lockdownWarningText.text = lockdownVolley.IsTelegraphing ? $"Incoming: {lockdownVolley.IncomingEdge} Lockdown" : "";
        if (lockdownCooldownText != null)
            lockdownCooldownText.text = FormatCooldown("Lockdown", lockdownVolley.CooldownRemaining);
    }

    private string FormatCooldown(string label, float remaining)
    {
        return remaining > 0f ? $"{label}: {remaining:0.0}s" : $"{label}: Ready";
    }

    public void ShowDefeated()
    {
        phaseText.text = "DEFEATED";
    }
}
