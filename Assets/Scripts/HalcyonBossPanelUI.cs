using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level 2's BossPanel script - mirrors BossPanelUI.cs's "HUD only reads,
// never owns game state" pattern against HalcyonBoss's different API: no
// target/guided-missile/pattern-barrage text, since none of that exists for
// this boss. See docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public class HalcyonBossPanelUI : MonoBehaviour
{
    public HalcyonBoss boss; // direct scene reference - scene-bound, not a reusable prefab
    public HalcyonSurge surge;
    public HalcyonStaticField staticField;
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI surgeWarningText;
    public TextMeshProUGUI surgeCooldownText;
    public TextMeshProUGUI staticFieldCooldownText;

    void Update()
    {
        if (boss == null) return;

        UpdateHealth();
        UpdateSurgeTexts();
        UpdateStaticFieldText();
    }

    private void UpdateHealth()
    {
        healthBarFill.fillAmount = (float)boss.CurrentHealth / boss.maxHealth;
        healthText.text = $"HP: {boss.CurrentHealth}/{boss.maxHealth}";
        phaseText.text = boss.IsPhase2 ? "Phase 2" : "Phase 1";
    }

    private void UpdateSurgeTexts()
    {
        if (surge == null) return;
        bool warning = surge.IsTelegraphing || surge.IsActive;
        if (surgeWarningText != null) surgeWarningText.text = warning ? "Surge!" : "";
        if (surgeCooldownText != null) surgeCooldownText.text = FormatCooldown("Surge", surge.CooldownRemaining);
    }

    private void UpdateStaticFieldText()
    {
        if (staticField == null || staticFieldCooldownText == null) return;
        staticFieldCooldownText.text = FormatCooldown("Static Field", staticField.CooldownRemaining);
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
