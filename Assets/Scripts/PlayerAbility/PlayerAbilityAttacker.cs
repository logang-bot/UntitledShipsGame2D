using UnityEngine;

public class PlayerAbilityAttacker
{
    private readonly PlayerAbility ability;

    // The fixed 1->2->3 rotation the combo rewards. 1-based to match the
    // combo key numbers (Combo1/2/3) shown to the player, so there's no
    // off-by-one translating UI/input slot numbers to array indices.
    private int expectedSlot = 1;
    private readonly float[] nextAttackTime = new float[3];

    public PlayerAbilityAttacker(PlayerAbility ability)
    {
        this.ability = ability;
    }

    // What slot a correctly-played combo attacks next - read by the HUD
    // (PlayerAbility.StatusText) and by AIController to always feed the
    // Attacker bot the right slot, so bots execute a perfect rotation.
    public int ExpectedSlot => expectedSlot;

    public void Trigger()
    {
        float damage = ability.PlayerController.fireDamage * ability.bigShotDamageMultiplier;
        ability.PlayerController.FireBigShot(ability.bigShotWidthMultiplier, damage, PlayerShotKind.BigShot);
        ability.PlayerController.AddRecoil(Vector2.down * ability.recoilForce);
    }

    /// <summary>
    /// One step of the 3-attack combo. `slot` is whichever of the 3 combo
    /// keys the player (or AI) just pressed - see PlayerAbility.OnCombo1/2/3.
    /// Landing them in the fixed 1->2->3 order builds toward the finisher's
    /// bonus multiplier; landing the wrong slot is a "bad execution" that
    /// deals reduced damage and resets the rotation back to step 1, rather
    /// than failing outright.
    /// </summary>
    public void TryComboAttack(int slot)
    {
        if (slot < 1 || slot > 3) return;
        if (Time.time < nextAttackTime[slot - 1]) return;
        nextAttackTime[slot - 1] = Time.time + ability.comboAttackCooldown;

        AttackerComboShot shot = slot == expectedSlot ? AdvanceRotation() : BreakRotation();
        float damage = ability.PlayerController.fireDamage * shot.DamageMultiplier;
        ability.PlayerController.FireBigShot(shot.WidthMultiplier, damage, shot.Kind);
    }

    private AttackerComboShot AdvanceRotation()
    {
        int stepIndex = expectedSlot - 1;
        expectedSlot = expectedSlot == 3 ? 1 : expectedSlot + 1;
        return new AttackerComboShot(
            ability.comboStepDamageMultipliers[stepIndex],
            ability.comboStepWidthMultipliers[stepIndex],
            PlayerShotKind.Combo1 + stepIndex);
    }

    private AttackerComboShot BreakRotation()
    {
        expectedSlot = 1;
        return new AttackerComboShot(ability.comboBreakDamageMultiplier, 1f, PlayerShotKind.ComboBroken);
    }
}
