using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MarauderBoss : MonoBehaviour, IBoss
{
    [Header("Health / Phases")]
    public int maxHealth = 150; // was 90 - bumped again now the boss stands still and fires less, so the fight still takes real time
    public UnityEvent OnPhase2;
    public UnityEvent OnDefeated;

    [Header("Movement — pattern")]
    // Off for now while the attacker-combo rotation is being tuned - a
    // stationary boss is a stable target to test damage rotation against.
    // MarauderBossMovement itself is untouched, so this is a one-line revert.
    public bool enableMovementPattern = false;
    // Scripted movement, not AI: hops in random order between the fixed
    // vertices of an "M" shape via real eased travel (never a snap/teleport)
    // - see MarauderBossMovement. Loops unchanged across both phases.
    public float sideOffsetX = 2.2f;
    // Per-hop travel duration is randomized within this range every hop -
    // the low end can be fast enough to read as a sudden dart between points.
    public float patternMoveDurationMin = 0.4f;
    public float patternMoveDurationMax = 2f;
    // How long the boss sits still at a vertex before hopping to the next
    // one, randomized within this range each time - see MarauderBossMovement.
    public float cycleGapMin = 0.5f;
    public float cycleGapMax = 2.5f;
    public float maxAdvanceFraction = 0.4f; // "2/5 of the screen" - fraction of playable height below homeY the boss may advance into
    [Range(0f, 1f)] public float mPatternNotchDepth = 0.5f; // how deep the M's middle notch dips, 0 = stays at home row, 1 = as deep as the outer low points
    public Vector2 screenPadding = new Vector2(0.8f, 0.5f);

    [Header("Combat")]
    public GameObject bulletPrefab;
    // Delay after this component is enabled (right when ships regain
    // control post-entrance) before it's allowed to attack at all - see
    // OnEnable(). Without this, attack timers default to 0 and the boss can
    // land a hit on the very first Update() with zero reaction time.
    public float postEntranceGracePeriod = 1.5f;
    public float phase1FireInterval = 2.4f; // was 1.2 - halved fire rate to go with the boss standing still now
    public float phase2FireInterval = 1.2f; // was 0.6 - same halving as phase1FireInterval
    public float bulletSpeed = 6f;
    public float spreadAngle = 15f; // phase 2 side-bullet offset
    public float bulletDamage = 1f; // single source of truth - contact/shockwave damage are multiples of this

    [Header("Body contact")]
    public float bodyContactDamageMultiplier = 2f;
    public float contactDamageCooldown = 1f;

    [Header("Shockwave")]
    public MarauderBossShockwaveSettings shockwaveSettings = new MarauderBossShockwaveSettings();

    [Header("Guided missile")]
    public MarauderBossGuidedMissileSettings guidedMissile = new MarauderBossGuidedMissileSettings();

    [Header("Pattern Barrage")]
    // One attack, three shapes - randomly picked each activation, never the
    // same shape twice in a row (see MarauderBossAttacks.PickPattern()).
    public float patternBarrageCooldown = 7f;
    public float patternBarrageTelegraphTime = 0.7f;
    public int fanBulletCount = 5;
    public float fanSpreadAngle = 50f; // total angular width, centered on the aim direction
    public int ringBulletCount = 12;
    public int spiralBulletCount = 20;
    public float spiralAngleStep = 25f; // degrees added per shot
    public float spiralShotInterval = 0.05f; // seconds between shots - this is what actually reads as "rapid-fire"

    [Header("Aggro / Targets")]
    public GameObject[] targets; // drag Player + 3 Teammates
    public float tauntBonus = 100f;

    [Header("Sub-enemies")]
    // Off for now - hides the boss's kamikaze minion waves (MinionSpawner)
    // while the attacker-combo rotation is being tuned, so the fight is just
    // ships vs. boss. MinionSpawner itself is untouched, so this is a
    // one-line revert.
    public bool enableMinions = false;

    public int CurrentHealth { get; private set; }
    public bool IsPhase2 { get; private set; }
    public GameObject CurrentTarget => aggroSystem.CurrentTarget;
    public PlayerRole? GuidedMissileTargetRole => attacks.GuidedMissileTargetRole;
    public MarauderBossAttacks.BulletPattern? PatternBarrageActivePattern => attacks.PatternBarrageActivePattern;
    public float ShockwaveCooldownRemaining => shockwave.CooldownRemaining;
    public float GuidedMissileCooldownRemaining => attacks.GuidedMissileCooldownRemaining;
    public float PatternBarrageCooldownRemaining => attacks.PatternBarrageCooldownRemaining;

    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    private float nextFireTime;
    private SpriteRenderer sr;
    private Collider2D col;
    private MinionSpawner minionSpawner;
    private MarauderBossMovement movement;
    private MarauderBossShockwave shockwave;
    private MarauderBossAttacks attacks;
    private MarauderBossAggro aggroSystem;

    void Awake()
    {
        CurrentHealth = maxHealth;
        CacheComponents();
        CreateHelpers();
        aggroSystem.Init();
    }

    private void CacheComponents()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        minionSpawner = GetComponent<MinionSpawner>();
        if (minionSpawner != null) minionSpawner.enabled = false;
    }

    private void CreateHelpers()
    {
        movement = new MarauderBossMovement(this);
        shockwave = new MarauderBossShockwave(this);
        attacks = new MarauderBossAttacks(this);
        aggroSystem = new MarauderBossAggro(this);
        shockwave.CreateRing();
    }

    /// <summary>
    /// Hides/shows sprite + ring + collider without touching the GameObject's
    /// active state (SetActive(false) was tried and rejected - see
    /// docs/systems/level-sequencing.md). The collider must toggle too, since
    /// Bullet.cs's OnTriggerEnter2D needs it disabled to stop early hits.
    /// Deliberately does NOT touch MinionSpawner - see OnEnable() - since this
    /// fires while ships are still frozen and unable to react to minions.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
        if (col != null) col.enabled = visible;
        shockwave.SetVisible(visible);
    }

    /// <summary>
    /// Fires when LevelSequencer flips this on, right after the boss's own
    /// entrance glide completes. MinionSpawner starts here (not SetVisible)
    /// so kamikaze minions only appear once ships can react to them.
    /// </summary>
    void OnEnable()
    {
        if (minionSpawner != null) minionSpawner.enabled = enableMinions;
        if (enableMovementPattern) movement.OnEnable();

        aggroSystem.StartCombatClock();

        float graceUntil = Time.time + postEntranceGracePeriod;
        nextFireTime = graceUntil;
        shockwave.ResetCooldown(graceUntil);
        attacks.ResetCooldowns(graceUntil);
    }

    void Update()
    {
        if (LevelSequencer.ShipsFrozen) return;

        aggroSystem.PickTarget();
        HandleFiring();
        shockwave.CheckShockwave();
        attacks.CheckGuidedMissile();
        attacks.CheckPatternBarrage();
        shockwave.UpdateRing();
    }

    private void HandleFiring()
    {
        float interval = IsPhase2 ? phase2FireInterval : phase1FireInterval;
        if (CurrentTarget == null || Time.time < nextFireTime) return;

        nextFireTime = Time.time + interval;
        Fire();
    }

    void Fire()
    {
        if (bulletPrefab == null || CurrentTarget == null) return;

        Vector2 dir = (CurrentTarget.transform.position - transform.position).normalized;
        SpawnBullet(dir);

        if (IsPhase2)
        {
            SpawnBullet(Quaternion.Euler(0, 0, spreadAngle) * dir);
            SpawnBullet(Quaternion.Euler(0, 0, -spreadAngle) * dir);
        }
    }

    public void SpawnBullet(Vector2 dir)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = bulletDamage;
        b.Init(dir, bulletSpeed, "Enemy");
    }

    /// <summary>
    /// Called by each ship's own PlayerController.ResolveShipCollisions() when
    /// its overlap-resolution math detects it overlapping the boss. `ship` is
    /// always a ship's own root GameObject, so GetComponent is correct here.
    /// </summary>
    public void ApplyContactDamage(GameObject ship)
    {
        if (ship == null) return;
        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (lastContactDamageTime.TryGetValue(ship, out float t) && Time.time - t < contactDamageCooldown) return;

        lastContactDamageTime[ship] = Time.time;
        health.TakeDamage(Mathf.RoundToInt(bulletDamage * bodyContactDamageMultiplier));
    }

    public void TakeDamage(float amount, GameObject source)
    {
        CurrentHealth -= Mathf.RoundToInt(amount);
        aggroSystem.RegisterDamage(amount, source);

        if (!IsPhase2 && CurrentHealth <= maxHealth / 2)
        {
            IsPhase2 = true;
            OnPhase2?.Invoke();
        }

        if (CurrentHealth <= 0) Die();
    }

    public void TauntedBy(GameObject taunter)
    {
        aggroSystem.TauntedBy(taunter);
    }

    void Die()
    {
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }

    public float CombatElapsed => aggroSystem.CombatElapsed;

    public float GetDamageDealt(GameObject source) => aggroSystem.GetDamageDealt(source);
}
