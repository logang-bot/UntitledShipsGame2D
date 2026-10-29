using UnityEngine;

// Sibling component on the Boss GameObject: full-arena waypoint-to-waypoint
// roam, unlike MarauderBoss's near-home M-pattern. Speed is randomized per
// leg and mid-glide (see HalcyonRoamPhaseSettings), faster in phase 2.
// Freezes whenever HalcyonSurge is telegraphing or active. See
// docs/superpowers/specs/2026-09-04-halcyon-boss-design.md.
public class HalcyonRoam : MonoBehaviour
{
    [Header("Speed / pauses per phase")]
    public HalcyonRoamPhaseSettings phase1 = new HalcyonRoamPhaseSettings();
    public HalcyonRoamPhaseSettings phase2 = new HalcyonRoamPhaseSettings
    {
        speedMin = 5f,
        speedMax = 7f,
        pauseMin = 0.05f,
        pauseMax = 0.2f,
        speedChangeIntervalMin = 0.4f,
        speedChangeIntervalMax = 0.9f,
    };

    [Header("Waypoints")]
    public float minWaypointDistance = 2f;
    public Vector2 screenPadding = new Vector2(0.8f, 0.5f);

    private HalcyonBoss boss;
    private HalcyonSurge surge;
    private Camera cam;
    private Vector3 target;
    private float pauseUntil;
    private float currentSpeed;
    private float nextSpeedChangeTime;

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
        RerollSpeed();
    }

    void Update()
    {
        if (surge != null && (surge.IsTelegraphing || surge.IsActive)) return;
        if (Time.time < pauseUntil) return;

        if (Time.time >= nextSpeedChangeTime) RerollSpeed();
        MoveTowardTarget();
    }

    private HalcyonRoamPhaseSettings CurrentSettings()
    {
        return boss != null && boss.IsPhase2 ? phase2 : phase1;
    }

    private void RerollSpeed()
    {
        HalcyonRoamPhaseSettings settings = CurrentSettings();
        currentSpeed = Random.Range(settings.speedMin, settings.speedMax);
        nextSpeedChangeTime = Time.time + Random.Range(settings.speedChangeIntervalMin, settings.speedChangeIntervalMax);
    }

    private void MoveTowardTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.05f) ArriveAtTarget();
    }

    private void ArriveAtTarget()
    {
        HalcyonRoamPhaseSettings settings = CurrentSettings();
        pauseUntil = Time.time + Random.Range(settings.pauseMin, settings.pauseMax);
        target = PickWaypoint();
        RerollSpeed();
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
