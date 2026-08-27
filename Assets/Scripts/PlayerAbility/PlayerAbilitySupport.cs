using System.Collections;
using UnityEngine;

public class PlayerAbilitySupport
{
    private readonly PlayerAbility ability;
    private Coroutine boostCoroutine;
    private float boostEndTime;

    public PlayerAbilitySupport(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public bool IsActive => boostCoroutine != null;
    public float Remaining => Mathf.Max(0f, boostEndTime - Time.time);

    public void Trigger()
    {
        boostEndTime = Time.time + ability.speedBoostDuration;
        if (boostCoroutine != null) ability.StopCoroutine(boostCoroutine);
        boostCoroutine = ability.StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        ApplyPartyBuff(ability.speedBoostMultiplier, true);
        yield return new WaitForSeconds(ability.speedBoostDuration);
        ApplyPartyBuff(1f, false);
        boostCoroutine = null;
    }

    /// <summary>
    /// Non-destructive party-wide buff: sets (never multiplies-in-place)
    /// every ally's PlayerController buff multipliers and toggles their
    /// party-buff ring, tinted to this caster's (always Support's) color.
    /// Plain assignment on both ends, so there's no revert math and nothing
    /// to double-apply.
    /// </summary>
    private void ApplyPartyBuff(float multiplier, bool visualActive)
    {
        if (ability.allies == null) return;
        foreach (Transform ally in ability.allies)
        {
            if (ally == null || !ally.gameObject.activeInHierarchy) continue;
            ApplyBuffToAlly(ally, multiplier, visualActive);
        }
    }

    private void ApplyBuffToAlly(Transform ally, float multiplier, bool visualActive)
    {
        PlayerController pc = ally.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.speedBuffMultiplier = multiplier;
            pc.fireRateBuffMultiplier = multiplier;
        }
        PlayerAbility allyAbility = ally.GetComponent<PlayerAbility>();
        if (allyAbility != null) allyAbility.SetPartyBuffVisual(visualActive, ability.TintColor);
    }
}
