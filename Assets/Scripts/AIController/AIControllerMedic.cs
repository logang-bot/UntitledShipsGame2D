using UnityEngine;

public class AIControllerMedic
{
    private readonly AIController owner;

    public AIControllerMedic(AIController owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// The ally (from PlayerAbility.allies - all 4 ships, unlike
    /// teammates[]) with the lowest health-or-shield fraction, but only if
    /// that fraction is at/below medicApproachThreshold; null if everyone's
    /// fine. "Fine" requires both health AND shield above the threshold, so
    /// a single depleted pool (e.g. shield gone, health still full) still
    /// counts as hurt - mirrors PlayerAbilityMedic.TickAura()'s own
    /// health-or-shield check.
    /// </summary>
    public Transform FindHurtAlly()
    {
        if (owner.Ability.allies == null) return null;

        Transform hurtAlly = null;
        float worstFraction = float.MaxValue;
        foreach (Transform ally in owner.Ability.allies)
        {
            if (!IsHurtCandidate(ally, out float fraction) || fraction >= worstFraction) continue;
            worstFraction = fraction;
            hurtAlly = ally;
        }
        return hurtAlly;
    }

    private bool IsHurtCandidate(Transform ally, out float fraction)
    {
        fraction = 0f;
        if (ally == null || ally == owner.transform || !ally.gameObject.activeInHierarchy) return false;

        PlayerHealth allyHealth = ally.GetComponent<PlayerHealth>();
        if (allyHealth == null) return false;

        float healthFraction = (float)allyHealth.CurrentHealth / allyHealth.maxHealth;
        float shieldFraction = allyHealth.maxShield > 0 ? (float)allyHealth.CurrentShield / allyHealth.maxShield : 1f;
        if (healthFraction > owner.medicApproachThreshold && shieldFraction > owner.medicApproachThreshold) return false;

        fraction = Mathf.Min(healthFraction, shieldFraction);
        return true;
    }

    /// <summary>
    /// Steers toward a point offset from `target` on the side away from the
    /// boss (medicStandoffDistance), rather than the ally's exact position -
    /// lands the Medic near but not on top of the ally it's healing, instead
    /// of nose-to-nose blocking its path. Falls back to the ally's exact
    /// position if there's no boss reference to compute "away" from.
    /// </summary>
    public Vector2 ApproachDirection(Transform target)
    {
        Vector2 targetPos = target.position;
        if (owner.bossObject != null)
        {
            Vector2 awayFromBoss = (Vector2)target.position - (Vector2)owner.bossObject.transform.position;
            if (awayFromBoss.sqrMagnitude > 0.0001f)
                targetPos += awayFromBoss.normalized * owner.medicStandoffDistance;
        }

        Vector2 toTarget = targetPos - (Vector2)owner.transform.position;
        return toTarget.magnitude < owner.guardDeadzone ? Vector2.zero : toTarget.normalized;
    }
}
