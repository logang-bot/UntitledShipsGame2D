using UnityEngine;

public class AIControllerAvoidance
{
    private readonly AIController owner;

    public AIControllerAvoidance(AIController owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Steers away from any other living ally within separationRadius,
    /// scaled by how deep into the radius they are (closer = stronger
    /// push). Excludes `ignore` so e.g. the Medic can still close the last
    /// stretch of distance on the ally it's actively approaching to heal.
    /// Blended additively in AIController.ComputeMoveDirection(), same
    /// pattern as ComputeDodgeVector().
    /// </summary>
    public Vector2 ComputeSeparationVector(Transform ignore)
    {
        Vector2 push = Vector2.zero;
        if (owner.Ability.allies == null) return push;

        foreach (Transform ally in owner.Ability.allies)
        {
            if (ally == null || ally == owner.transform || ally == ignore || !ally.gameObject.activeInHierarchy) continue;
            push += SeparationPush(ally);
        }
        return push;
    }

    private Vector2 SeparationPush(Transform ally)
    {
        Vector2 offset = (Vector2)owner.transform.position - (Vector2)ally.position;
        float dist = offset.magnitude;
        if (dist >= owner.separationRadius || dist < 0.0001f) return Vector2.zero;
        return offset.normalized * (1f - dist / owner.separationRadius);
    }

    /// <summary>
    /// Steers away from any enemy bullet on an imminent collision course.
    /// Iterates Bullet.Active (populated/depopulated via Bullet's own
    /// Awake/OnDestroy) rather than scanning the scene each frame. Blended
    /// additively with role positioning in
    /// AIController.ComputeMoveDirection(), not an override, so e.g. Tank
    /// doesn't abandon a block outright.
    /// </summary>
    public Vector2 ComputeDodgeVector()
    {
        Vector2 selfPos = owner.transform.position;
        Vector2 escape = Vector2.zero;
        foreach (Bullet bullet in Bullet.Active)
        {
            if (TryComputeBulletEscape(bullet, selfPos, out Vector2 bulletEscape))
                escape += bulletEscape;
        }
        return escape.sqrMagnitude > 0.0001f ? escape.normalized : Vector2.zero;
    }

    private bool TryComputeBulletEscape(Bullet bullet, Vector2 selfPos, out Vector2 escape)
    {
        escape = Vector2.zero;
        if (!IsThreateningBullet(bullet, selfPos, out Vector2 vel, out Vector2 toSelf)) return false;
        escape = EscapeDirection(vel, toSelf);
        return true;
    }

    /// <summary>
    /// For an enemy bullet within dodgeDetectionRadius, projects this
    /// teammate's position onto the bullet's current straight-line velocity
    /// to find the time/point of closest approach (re-evaluated fresh every
    /// frame, so a re-aiming homing bullet's current heading is still
    /// handled reasonably without full intercept prediction), and reports
    /// whether that closest approach is within dodgeMissDistance.
    /// </summary>
    private bool IsThreateningBullet(Bullet bullet, Vector2 selfPos, out Vector2 vel, out Vector2 toSelf)
    {
        vel = Vector2.zero;
        toSelf = Vector2.zero;
        if (bullet == null || bullet.Owner != "Enemy") return false;

        Vector2 bulletPos = bullet.transform.position;
        if (Vector2.Distance(selfPos, bulletPos) > owner.dodgeDetectionRadius) return false;

        vel = bullet.Direction * bullet.Speed;
        if (vel.sqrMagnitude < 0.0001f) return false;

        toSelf = selfPos - bulletPos;
        float t = Mathf.Clamp(Vector2.Dot(toSelf, vel) / vel.sqrMagnitude, 0f, owner.dodgeLookaheadTime);
        return Vector2.Distance(selfPos, bulletPos + vel * t) <= owner.dodgeMissDistance;
    }

    /// <summary>
    /// Perpendicular to the bullet's travel direction (a sideways step out
    /// of its lane, not a radial push away from its current position), on
    /// whichever side increases this teammate's distance from it.
    /// </summary>
    private static Vector2 EscapeDirection(Vector2 vel, Vector2 toSelf)
    {
        Vector2 perp = new Vector2(-vel.y, vel.x).normalized;
        return Vector2.Dot(perp, toSelf) < 0f ? -perp : perp;
    }
}
