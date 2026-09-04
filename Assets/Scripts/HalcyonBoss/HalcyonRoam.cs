using UnityEngine;

// Sibling component on the Boss GameObject: full-arena waypoint-to-waypoint
// roam, unlike MarauderBoss's near-home M-pattern. Freezes whenever
// HalcyonSurge is telegraphing or active. See
// docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public class HalcyonRoam : MonoBehaviour
{
    [Header("Speed")]
    public float roamSpeed = 2.5f;
    public float roamSpeedPhase2 = 3.5f;

    [Header("Waypoints")]
    public float pauseMin = 0.3f;
    public float pauseMax = 0.8f;
    public float minWaypointDistance = 2f;
    public Vector2 screenPadding = new Vector2(0.8f, 0.5f);

    private HalcyonBoss boss;
    private HalcyonSurge surge;
    private Camera cam;
    private Vector3 target;
    private float pauseUntil;

    void Awake()
    {
        boss = GetComponent<HalcyonBoss>();
        surge = GetComponent<HalcyonSurge>();
        cam = Camera.main;
    }

    void OnEnable()
    {
        target = transform.position;
        pauseUntil = Time.time;
    }

    void Update()
    {
        if (surge != null && (surge.IsTelegraphing || surge.IsActive)) return;
        if (Time.time < pauseUntil) return;

        MoveTowardTarget();
    }

    private void MoveTowardTarget()
    {
        float speed = boss != null && boss.IsPhase2 ? roamSpeedPhase2 : roamSpeed;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.05f) ArriveAtTarget();
    }

    private void ArriveAtTarget()
    {
        pauseUntil = Time.time + Random.Range(pauseMin, pauseMax);
        target = PickWaypoint();
    }

    private Vector3 PickWaypoint()
    {
        Vector3 candidate = RandomViewportPoint();
        int guard = 0;
        while (Vector3.Distance(candidate, transform.position) < minWaypointDistance && guard < 10)
        {
            candidate = RandomViewportPoint();
            guard++;
        }
        return candidate;
    }

    private Vector3 RandomViewportPoint()
    {
        if (cam == null) return transform.position;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float x = Random.Range(min.x + screenPadding.x, max.x - screenPadding.x);
        float y = Random.Range(min.y + screenPadding.y, max.y - screenPadding.y);
        return new Vector3(x, y, transform.position.z);
    }
}
