using System.Collections.Generic;
using UnityEngine;

// Reusable turret-arm component - one instance per arm (2 active from
// BossCombat, a 3rd added permanently at Phase 2 via WardenBoss.EnterPhase2).
// Idle -> Telegraph -> Firing (continuous stream) -> re-pick -> Idle. See
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenArm : MonoBehaviour
{
    private enum ArmState { Idle, Telegraph, Firing }

    // Groups the taunt-related inputs to PickWeighted so the method stays
    // within this project's 3-parameter convention.
    public readonly struct TauntBias
    {
        public readonly GameObject TauntedShip;
        public readonly bool Active;
        public readonly float Multiplier;

        public TauntBias(GameObject tauntedShip, bool active, float multiplier)
        {
            TauntedShip = tauntedShip;
            Active = active;
            Multiplier = multiplier;
        }
    }

    private readonly struct WeightedCandidates
    {
        public readonly List<GameObject> Ships;
        public readonly List<float> Weights;
        public readonly float Total;

        public WeightedCandidates(List<GameObject> ships, List<float> weights, float total)
        {
            Ships = ships;
            Weights = weights;
            Total = total;
        }
    }

    [Header("Timing")]
    public float idleCooldown = 4f;
    public float idleJitter = 1f;
    public float telegraphTime = 0.5f;
    public float firingDuration = 3f;
    public float fireInterval = 0.15f;

    [Header("Taunt bias")]
    public float tauntWeightMultiplier = 3f;

    [Header("Firing")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 6f;

    public PlayerRole? CurrentTargetRole => TargetRole();

    private WardenBoss boss;
    private SpriteRenderer sr;
    private ArmState state;
    private float stateEndTime;
    private float nextShotTime;
    private GameObject currentTarget;

    void Awake()
    {
        boss = GetComponentInParent<WardenBoss>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        EnterIdle();
    }

    public void SetVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
    }

    void Update()
    {
        switch (state)
        {
            case ArmState.Idle: UpdateIdle(); break;
            case ArmState.Telegraph: UpdateTelegraph(); break;
            case ArmState.Firing: UpdateFiring(); break;
        }
    }

    private PlayerRole? TargetRole()
    {
        if (currentTarget == null) return null;
        PlayerRoleComponent rc = currentTarget.GetComponent<PlayerRoleComponent>();
        return rc != null ? rc.role : (PlayerRole?)null;
    }

    private void UpdateIdle()
    {
        if (Time.time < stateEndTime) return;
        currentTarget = PickWeightedTarget();
        EnterTelegraph();
    }

    private void UpdateTelegraph()
    {
        if (Time.time < stateEndTime) return;
        EnterFiring();
    }

    private void UpdateFiring()
    {
        if (Time.time >= stateEndTime) { EnterIdle(); return; }
        if (Time.time >= nextShotTime) FireAtCurrentTarget();
    }

    private void EnterIdle()
    {
        state = ArmState.Idle;
        stateEndTime = Time.time + idleCooldown + Random.Range(-idleJitter, idleJitter);
        currentTarget = null;
    }

    private void EnterTelegraph()
    {
        state = ArmState.Telegraph;
        stateEndTime = Time.time + telegraphTime;
    }

    private void EnterFiring()
    {
        state = ArmState.Firing;
        stateEndTime = Time.time + firingDuration;
        nextShotTime = Time.time;
    }

    private void FireAtCurrentTarget()
    {
        nextShotTime = Time.time + fireInterval;
        if (currentTarget == null || !currentTarget.activeInHierarchy) return;
        Vector2 dir = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
        SpawnBullet(dir);
    }

    private void SpawnBullet(Vector2 dir)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = boss.bulletDamage;
        b.Init(dir, bulletSpeed, "Enemy");
    }

    private GameObject PickWeightedTarget()
    {
        if (boss == null || boss.ships == null) return null;
        TauntBias bias = new TauntBias(boss.TauntedShip, Time.time < boss.TauntActiveUntil, tauntWeightMultiplier);
        return PickWeighted(boss.ships, bias, Random.value);
    }

    /// <summary>
    /// Builds a weight per living ship (1, or bias.Multiplier for
    /// bias.TauntedShip while bias.Active) and picks against the cumulative
    /// total using randomDraw01 - the draw is a parameter rather than read
    /// internally so this is independently testable without RNG flakiness.
    /// </summary>
    public static GameObject PickWeighted(GameObject[] ships, TauntBias bias, float randomDraw01)
    {
        WeightedCandidates weighted = BuildWeightedCandidates(ships, bias);
        if (weighted.Ships.Count == 0) return null;
        return SelectFromWeights(weighted, randomDraw01);
    }

    private static WeightedCandidates BuildWeightedCandidates(GameObject[] ships, TauntBias bias)
    {
        List<GameObject> candidates = new List<GameObject>();
        List<float> weights = new List<float>();
        float total = 0f;
        foreach (GameObject ship in ships)
        {
            if (ship == null || !ship.activeInHierarchy) continue;
            float weight = bias.Active && ship == bias.TauntedShip ? bias.Multiplier : 1f;
            candidates.Add(ship);
            weights.Add(weight);
            total += weight;
        }
        return new WeightedCandidates(candidates, weights, total);
    }

    private static GameObject SelectFromWeights(WeightedCandidates weighted, float randomDraw01)
    {
        float target = randomDraw01 * weighted.Total;
        float cumulative = 0f;
        for (int i = 0; i < weighted.Ships.Count; i++)
        {
            cumulative += weighted.Weights[i];
            if (target < cumulative) return weighted.Ships[i];
        }
        return weighted.Ships[weighted.Ships.Count - 1];
    }
}
