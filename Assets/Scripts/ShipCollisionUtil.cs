using UnityEngine;

// Exact axis-aligned box-vs-box overlap resolution. Ships and the boss never
// rotate (fixed Galaga-style orientation - see docs/systems/movement.md), so
// an exact AABB check is correct here, not a circle approximation.
public static class ShipCollisionUtil
{
    // Given a candidate position for "self" (not yet committed - the caller
    // hasn't applied it to a Rigidbody2D/transform yet) and a fixed,
    // already-committed position for "other", pushes candidateSelfPos out
    // along the axis of least penetration (minimum translation vector) so
    // the two boxes no longer overlap. Returns candidateSelfPos unchanged if
    // there's no overlap. wasOverlapping lets the caller react (e.g. contact
    // damage) using the exact same math that computed the push-back - a
    // single source of truth for both "should I push back" and "should this
    // count as a hit", since ships/boss move in small discrete per-frame
    // steps rather than teleporting, so a momentary overlap is exactly the
    // signal a physical collision actually happened.
    public static Vector2 ResolveBoxOverlap(
        Vector2 candidateSelfPos, Vector2 selfHalfExtents,
        Vector2 otherPos, Vector2 otherHalfExtents,
        out bool wasOverlapping)
    {
        Vector2 delta = candidateSelfPos - otherPos;
        float overlapX = (selfHalfExtents.x + otherHalfExtents.x) - Mathf.Abs(delta.x);
        float overlapY = (selfHalfExtents.y + otherHalfExtents.y) - Mathf.Abs(delta.y);

        if (overlapX <= 0f || overlapY <= 0f)
        {
            wasOverlapping = false;
            return candidateSelfPos;
        }

        wasOverlapping = true;

        // Push out along the shallower axis only (minimum translation
        // vector) rather than a diagonal jump.
        if (overlapX < overlapY)
        {
            float sign = delta.x >= 0f ? 1f : -1f;
            candidateSelfPos.x = otherPos.x + sign * (selfHalfExtents.x + otherHalfExtents.x);
        }
        else
        {
            float sign = delta.y >= 0f ? 1f : -1f;
            candidateSelfPos.y = otherPos.y + sign * (selfHalfExtents.y + otherHalfExtents.y);
        }
        return candidateSelfPos;
    }
}
