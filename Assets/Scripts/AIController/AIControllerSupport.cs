using UnityEngine;

public class AIControllerSupport
{
    private readonly AIController owner;
    private Vector2 roamTarget;
    private float nextRoamPickTime;

    public AIControllerSupport(AIController owner)
    {
        this.owner = owner;
    }

    /// <summary>
    /// Roams the playable viewport freely rather than holding a zone -
    /// steers toward a random point, picking a new one on arrival (or after
    /// roamInterval, in case the current one is never quite reached) so
    /// Support keeps moving continuously instead of settling like Tank/
    /// Medic's biased positions do.
    /// </summary>
    public Vector2 WanderDirection()
    {
        bool arrived = Vector2.Distance(owner.transform.position, roamTarget) < owner.roamDeadzone;
        if (Time.time >= nextRoamPickTime || arrived)
        {
            roamTarget = RandomRoamPoint();
            nextRoamPickTime = Time.time + owner.roamInterval;
        }
        return (roamTarget - (Vector2)owner.transform.position).normalized;
    }

    /// <summary>
    /// Same viewport bounds PlayerController.HandleMovement() clamps
    /// movement to, reusing its screenPadding as the single source of truth
    /// for the inset so this can't drift out of sync with the actual clamp.
    /// </summary>
    private Vector2 RandomRoamPoint()
    {
        Vector3 min = owner.Cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = owner.Cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector2 padding = owner.Controller.screenPadding;
        Vector2 point = new Vector2(
            Random.Range(min.x + padding.x, max.x - padding.x),
            Random.Range(min.y + padding.y, max.y - padding.y));
        return owner.Positioning.EnforceBossDistance(point);
    }
}
