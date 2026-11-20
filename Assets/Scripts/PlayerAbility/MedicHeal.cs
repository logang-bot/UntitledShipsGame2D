using UnityEngine;

// One heal pulse from the Medic: +auraHealPerTick health and
// +auraShieldPerTick shield to every live ally inside the area, with the
// green heal flash on the ones that needed it. Shared by the passive aura
// and the Sanctuary dome, and identical for a human or CPU Medic.
public static class MedicHeal
{
    /// <summary>True when at least one ally inside actually needed healing.</summary>
    public static bool Pulse(PlayerAbility medic, HealArea area)
    {
        if (medic.allies == null) return false;
        bool healedAny = false;
        foreach (Transform ally in medic.allies)
            if (IsInside(medic, ally, area)) healedAny |= HealAlly(medic, ally);
        return healedAny;
    }

    private static bool IsInside(PlayerAbility medic, Transform ally, HealArea area)
    {
        if (ally == null || !ally.gameObject.activeInHierarchy) return false;
        if (ally == medic.transform && !area.includesCaster) return false;
        return Vector2.Distance(area.centre, ally.position) <= area.radius;
    }

    private static bool HealAlly(PlayerAbility medic, Transform ally)
    {
        if (!ally.TryGetComponent(out PlayerHealth health)) return false;
        bool neededHeal = health.CurrentHealth < health.maxHealth || health.CurrentShield < health.maxShield;
        health.Heal(medic.auraHealPerTick);
        health.RestoreShield(medic.auraShieldPerTick);
        if (neededHeal) FlashHealed(medic, ally);
        return neededHeal;
    }

    private static void FlashHealed(PlayerAbility medic, Transform ally)
    {
        if (ally.TryGetComponent(out PlayerDamageFlash flash)) flash.Flash(medic.healFlashColor);
        if (ally.TryGetComponent(out ShipCombatFx fx)) fx.PlayHealFlash();
    }
}
