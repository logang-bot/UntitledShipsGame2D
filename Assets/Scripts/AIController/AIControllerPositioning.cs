using UnityEngine;

public class AIControllerPositioning
{
    private readonly AIController owner;

    public AIControllerPositioning(AIController owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Average position of the AI-controlled allies (teammates[], self
    /// excluded) - shared by BiasedPositionDirection() and
    /// AIControllerAttacker.PositionDirection() so this liveness-filtered
    /// average isn't computed twice.
    /// </summary>
    public Vector2 GetAllyCenter()
    {
        Vector2 allySum = Vector2.zero;
        int allyCount = 0;
        if (owner.teammates != null)
        {
            foreach (Transform t in owner.teammates)
            {
                if (t == null || t == owner.transform || !t.gameObject.activeInHierarchy) continue;
                allySum += (Vector2)t.position;
                allyCount++;
            }
        }
        return allyCount > 0 ? allySum / allyCount : (Vector2)owner.transform.position;
    }

    /// <summary>
    /// Pushes a candidate target point out to at least minDistanceFromBoss
    /// from the boss's position, if it's currently closer. Falls back to
    /// Vector2.up if the point lands exactly on the boss (degenerate case,
    /// avoids a zero-length normalize).
    /// </summary>
    public Vector2 EnforceBossDistance(Vector2 point)
    {
        if (owner.boss == null) return point;

        Vector2 fromBoss = point - (Vector2)owner.boss.transform.position;
        if (fromBoss.magnitude >= owner.minDistanceFromBoss) return point;

        Vector2 pushDir = fromBoss.sqrMagnitude > 0.0001f ? fromBoss.normalized : Vector2.up;
        return (Vector2)owner.boss.transform.position + pushDir * owner.minDistanceFromBoss;
    }

    /// <summary>
    /// Steers toward a point between the AI-controlled allies and the boss,
    /// biased by `bias` (0 = at ally center, 1 = at the boss, negative =
    /// extrapolates past ally center away from the boss, via LerpUnclamped
    /// rather than Lerp). Tank uses a positive bias to physically stand in
    /// incoming bullets' paths (Bullet.cs doesn't home - it just hits
    /// whichever Player-tagged collider is in its straight-line path first,
    /// so standing in the way is enough); Medic uses a negative bias to
    /// hang back from the boss.
    /// </summary>
    public Vector2 BiasedPositionDirection(float bias, float deadzone)
    {
        if (owner.boss == null) return Vector2.zero;

        Vector2 targetPoint = Vector2.LerpUnclamped(GetAllyCenter(), owner.boss.transform.position, bias);
        targetPoint = EnforceBossDistance(targetPoint);

        Vector2 toTarget = targetPoint - (Vector2)owner.transform.position;
        return toTarget.magnitude < deadzone ? Vector2.zero : toTarget.normalized;
    }
}
