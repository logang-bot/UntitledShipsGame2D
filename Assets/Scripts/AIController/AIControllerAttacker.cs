using UnityEngine;

public class AIControllerAttacker
{
    /// <summary>How far past the beam's edge the focus point may sit and still count as lined up.</summary>
    private const float BeamAimMargin = 0.3f;

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
    /// patrol's center follows the live X of the tactics' FocusPoint (the
    /// boss, or e.g. a shielding Warden arm), keeping shots landing,
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

        Vector2 focus = owner.Tactics.FocusPoint();
        float targetY = Mathf.LerpUnclamped(owner.Positioning.GetAllyCenter().y, focus.y, owner.attackerBias);
        float targetX = focus.x + Mathf.Sin(Time.time * owner.weaveFrequency) * owner.attackerPatrolAmplitude;
        Vector2 targetPoint = owner.Positioning.EnforceBossDistance(new Vector2(targetX, targetY));

        Vector2 toTarget = targetPoint - (Vector2)owner.transform.position;
        return toTarget.magnitude < owner.attackerDeadzone ? Vector2.zero : toTarget.normalized;
    }

    /// <summary>
    /// Always feeds the combo the slot that continues it (a perfect rotation),
    /// and only casts the Nova Beam when the focus point sits in the beam's
    /// column, since the cast anchors the ship in place for its whole length.
    /// </summary>
    public void UseAbilities()
    {
        PlayerAbility ability = owner.Ability;
        ability.TryComboAttack(ability.AttackerComboExpectedSlot);
        if (IsFocusInBeamColumn(ability)) ability.TryUseAbility();
    }

    private bool IsFocusInBeamColumn(PlayerAbility ability)
    {
        if (owner.bossObject == null) return false;
        float reach = owner.Controller.ShipWidth * ability.beam.widthOfShip * 0.5f + BeamAimMargin;
        return Mathf.Abs(owner.Tactics.FocusPoint().x - owner.transform.position.x) < reach;
    }
}
