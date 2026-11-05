using System.Globalization;
using UnityEngine;

// The roguelike Attacker test's rows on the Attacker's party frame (design:
// HUD Party Frame), replacing the ability cell and combo box: the Attack and
// Crit cooldown cells and the XP row for a human Attacker, or one dashed
// AUTO-FIRE · NO UPGRADES line for a CPU one.
public class RogueFrameView : MonoBehaviour
{
    public GameObject cellsRow;
    public CooldownCellView attack;
    public CooldownCellView crit;
    public XpRowView xp;
    public GameObject cpuNote;

    private AttackerProgression progression;

    public bool LevelUpPending => progression != null && progression.PendingPicks > 0;

    public void Bind(AttackerProgression attackerProgression, bool human)
    {
        progression = human ? attackerProgression : null;
        gameObject.SetActive(true);
        cellsRow.SetActive(human);
        xp.gameObject.SetActive(human);
        cpuNote.SetActive(!human);
    }

    public void Show()
    {
        if (progression == null || progression.Ship == null) return;
        ShowAttack(progression.Ship);
        ShowCrit();
        xp.Show(XpRowState.From(progression.Xp), UpgradeChips.From(progression.Upgrades));
    }

    private void ShowAttack(PlayerController ship)
    {
        CooldownReading reading = new CooldownReading
        {
            remaining = ship.AttackCooldownRemaining, total = ship.FireInterval, sinceFired = Time.time - ship.LastAttackTime,
        };
        string shots = ship.shotsPerAttack > 1 ? $"×{ship.shotsPerAttack}" : "";
        attack.Show(CooldownCellState.From(reading, CooldownCellSpec.Attack), "DMG " + Number(ship.CurrentDamage), shots);
    }

    private void ShowCrit()
    {
        bool unlocked = progression.Upgrades.CritUnlocked;
        crit.gameObject.SetActive(unlocked);
        if (unlocked) crit.Show(CooldownCellState.From(CritReading(), CooldownCellSpec.Crit), CritBonusLine(), "");
    }

    private CooldownReading CritReading() => new CooldownReading
    {
        remaining = progression.CritCooldownRemaining, total = progression.critInterval, sinceFired = Time.time - progression.LastCritTime,
    };

    private string CritBonusLine() => $"+{Mathf.RoundToInt(progression.Upgrades.CritBonusFraction * 100f)}% DMG";

    private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
