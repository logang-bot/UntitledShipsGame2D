using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossPanelUI : MonoBehaviour
{
    public Boss boss; // direct scene reference - BossPanel is scene-bound, not a reusable prefab
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI targetText;

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
    }

    public void ShowDefeated()
    {
        phaseText.text = "DEFEATED";
    }
}
