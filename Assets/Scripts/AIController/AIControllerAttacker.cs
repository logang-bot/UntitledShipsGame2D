using UnityEngine;

public class AIControllerAttacker
{
    private readonly AIController owner;

    public AIControllerAttacker(AIController owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Hybrid patrol + boss-tracking. Ships never rotate and bullets only
    /// ever fire straight up (Vector2.up, no homing - see Bullet.cs), so an
    /// Attacker patrolling around a fixed, boss-independent center would
    /// frequently drift out of the boss's lane and just miss. Instead the
    /// patrol's center follows the boss's live X, keeping shots landing,
    /// while still swinging side-to-side by attackerPatrolAmplitude for DPS
    /// coverage/visual variety rather than sitting dead still under it. Y
    /// uses the same ally-center/boss blend as
    /// AIControllerPositioning.BiasedPositionDirection() (attackerBias,
    /// between Medic's and Tank's) for a mid-distance stand-off - this also
    /// keeps it clear of the top edge for free, since the boss sits near
    /// the top of the screen and ally center is lower.
    /// </summary>
    public Vector2 PositionDirection()
    {
        if (owner.bossObject == null) return Vector2.zero;

        float targetY = Mathf.LerpUnclamped(owner.Positioning.GetAllyCenter().y, owner.bossObject.transform.position.y, owner.attackerBias);
        float targetX = owner.bossObject.transform.position.x + Mathf.Sin(Time.time * owner.weaveFrequency) * owner.attackerPatrolAmplitude;
        Vector2 targetPoint = owner.Positioning.EnforceBossDistance(new Vector2(targetX, targetY));

        Vector2 toTarget = targetPoint - (Vector2)owner.transform.position;
        return toTarget.magnitude < owner.attackerDeadzone ? Vector2.zero : toTarget.normalized;
    }
}
