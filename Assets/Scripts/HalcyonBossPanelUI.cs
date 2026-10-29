using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level 2's BossPanel script - mirrors BossPanelUI.cs's "HUD only reads,
// never owns game state" pattern against HalcyonBoss's different API: no
// target/guided-missile/pattern-barrage text, since none of that exists for
// this boss. See docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public class HalcyonBossPanelUI : MonoBehaviour
{
    public HalcyonBoss boss; // set by Bind() when LevelSetup spawns the boss
    public HalcyonSurge surge;
    public HalcyonStaticField staticField;
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI surgeWarningText;
    public TextMeshProUGUI surgeCooldownText;
    public TextMeshProUGUI staticFieldCooldownText;
    public PaletteSwatch telegraphTextColor = new PaletteSwatch(PaletteColor.SurgeTelegraphText);
    public PaletteSwatch openingTextColor = new PaletteSwatch(PaletteColor.SurgeOpening);

    public void Bind(GameObject bossObject)
    {
        boss = bossObject.GetComponent<HalcyonBoss>();
        surge = bossObject.GetComponent<HalcyonSurge>();
        staticField = bossObject.GetComponent<HalcyonStaticField>();
    }

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
        if (surgeWarningText != null)
        {
            surgeWarningText.text = SurgeWarningLabel();
            surgeWarningText.color = surge.IsActive ? openingTextColor : telegraphTextColor;
        }
        if (surgeCooldownText != null) surgeCooldownText.text = FormatCooldown("Opening", surge.CooldownRemaining);
    }

    private string SurgeWarningLabel()
    {
        if (surge.IsActive) return "OPENING — FIRE!";
        return surge.IsTelegraphing ? "Opening!" : "";
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
