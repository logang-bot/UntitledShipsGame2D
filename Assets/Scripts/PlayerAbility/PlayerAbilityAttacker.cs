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
        ability.PlayerController.FireBigShot(ability.bigShotWidthMultiplier, damage);
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

        int slotIndex = slot - 1;
        if (Time.time < nextAttackTime[slotIndex]) return;
        nextAttackTime[slotIndex] = Time.time + ability.comboAttackCooldown;

        float damageMultiplier;
        float widthMultiplier;
        if (slot == expectedSlot)
        {
            int stepIndex = expectedSlot - 1;
            damageMultiplier = ability.comboStepDamageMultipliers[stepIndex];
            widthMultiplier = ability.comboStepWidthMultipliers[stepIndex];
            expectedSlot = expectedSlot == 3 ? 1 : expectedSlot + 1;
        }
        else
        {
            damageMultiplier = ability.comboBreakDamageMultiplier;
            widthMultiplier = 1f;
            expectedSlot = 1;
        }

        float damage = ability.PlayerController.fireDamage * damageMultiplier;
        ability.PlayerController.FireBigShot(widthMultiplier, damage);
    }
}
