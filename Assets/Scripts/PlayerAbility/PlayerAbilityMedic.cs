using System.Collections;
using UnityEngine;

public class PlayerAbilityMedic
{
    private const int AuraSortingOrder = -5;
    private const float IdleRingRadiusPx = 80f;
    private const float BoostRingRadiusPx = 240f;

    private readonly PlayerAbility ability;
    private SpriteRenderer ring;
    private float nextTickTime;
    private bool boosted;
    private Coroutine boostCoroutine;
    private float boostEndTime;

    public PlayerAbilityMedic(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public bool IsBoosted => boosted;
    public float BoostRemaining => Mathf.Max(0f, boostEndTime - Time.time);

    public void Trigger()
    {
        boostEndTime = Time.time + ability.auraBoostDuration;
        if (boostCoroutine != null) ability.StopCoroutine(boostCoroutine);
        boostCoroutine = ability.StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        boosted = true;
        yield return new WaitForSeconds(ability.auraBoostDuration);
        boosted = false;
        boostCoroutine = null;
    }

    public void CheckTick()
    {
        if (Time.time < nextTickTime) return;
        nextTickTime = Time.time + (boosted ? ability.auraBoostTickInterval : ability.auraTickInterval);
        TickAura();
    }

    /// <summary>
    /// Passive proximity heal/shield aura. Tiny radius by default (allies
    /// must nearly touch the Medic); Trigger() temporarily expands both the
    /// radius and tick rate. Lives here rather than on AIController since it
    /// must work identically whether Medic is human- or AI-controlled (see
    /// docs/systems/boss.md's "AI teammate behavior").
    /// </summary>
    private void TickAura()
    {
        if (ability.allies == null) return;
        foreach (Transform ally in ability.allies)
        {
            if (ally == null || ally == ability.transform || !ally.gameObject.activeInHierarchy) continue;
            HealAlly(ally);
        }
    }

    private void HealAlly(Transform ally)
    {
        float radius = boosted ? ability.auraBoostRadius : ability.auraRadius;
        if (Vector2.Distance(ability.transform.position, ally.position) > radius) return;

        PlayerHealth allyHealth = ally.GetComponent<PlayerHealth>();
        if (allyHealth == null) return;

        bool neededHeal = allyHealth.CurrentHealth < allyHealth.maxHealth || allyHealth.CurrentShield < allyHealth.maxShield;
        allyHealth.Heal(ability.auraHealPerTick);
        allyHealth.RestoreShield(ability.auraShieldPerTick);
        if (neededHeal) FlashHealed(ally);
    }

    private void FlashHealed(Transform ally)
    {
        if (ally.TryGetComponent(out PlayerDamageFlash allyFlash)) allyFlash.Flash(ability.healFlashColor);
        if (ally.TryGetComponent(out ShipCombatFx allyFx)) allyFx.PlayHealFlash();
    }

    /// <summary>
    /// Ring around the Medic showing the aura's current reach, from the
    /// Combat FX sprites: a dim dashed idle ring, and while boosted a bright
    /// ring pulsing between two frames. Scaled so the drawn ring sits exactly
    /// on the heal radius.
    /// </summary>
    public void CreateAuraRing()
    {
        if (ability.Fx == null) return;
        ring = FxSprites.CreateChild(ability.transform, "AuraRing", AuraSortingOrder);
    }

    public void UpdateRing()
    {
        if (ring == null) return;
        ring.sprite = boosted ? BoostSprite() : ability.Fx.auraIdle;
        float radius = boosted ? ability.auraBoostRadius : ability.auraRadius;
        FxSprites.FitRingRadius(ring.transform, boosted ? BoostRingRadiusPx : IdleRingRadiusPx, radius);
    }

    private Sprite BoostSprite()
    {
        bool peak = Mathf.Repeat(Time.time, ability.auraPulseInterval * 2f) > ability.auraPulseInterval;
        return peak ? ability.Fx.auraBoostPulse : ability.Fx.auraBoost;
    }
}
