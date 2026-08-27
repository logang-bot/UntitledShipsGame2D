using System.Collections;
using UnityEngine;

public class PlayerAbilityMedic
{
    private const int AuraRingSegments = 32;
    private const int BehindSpriteSortingOrder = -1;

    private readonly PlayerAbility ability;
    private LineRenderer ring;
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
        if (neededHeal)
        {
            PlayerDamageFlash allyFlash = ally.GetComponent<PlayerDamageFlash>();
            if (allyFlash != null) allyFlash.Flash(ability.healFlashColor);
        }
    }

    /// <summary>
    /// World-space ring around the Medic showing the aura's current reach -
    /// tiny and dim by default, larger and brighter while boosted. Built
    /// procedurally (Sprites/Default shader, no art asset) matching the
    /// project's current placeholder-art phase.
    /// </summary>
    public void CreateAuraRing()
    {
        GameObject ringObj = new GameObject("AuraRing");
        ringObj.transform.SetParent(ability.transform, false);
        ring = ringObj.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = AuraRingSegments;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.sortingLayerName = "Default";
        ring.sortingOrder = BehindSpriteSortingOrder;
    }

    public void UpdateRing()
    {
        if (ring == null) return;
        ApplyRingStyle();
        UpdateRingPositions();
    }

    private void ApplyRingStyle()
    {
        Color color = boosted ? ability.auraRingBoostedColor : ability.auraRingColor;
        float width = boosted ? ability.auraRingBoostedWidth : ability.auraRingWidth;
        ring.startColor = color;
        ring.endColor = color;
        ring.startWidth = width;
        ring.endWidth = width;
    }

    private void UpdateRingPositions()
    {
        float radius = boosted ? ability.auraBoostRadius : ability.auraRadius;
        Vector3 center = ability.transform.position;
        for (int i = 0; i < AuraRingSegments; i++)
        {
            float angle = (i / (float)AuraRingSegments) * Mathf.PI * 2f;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }
}
