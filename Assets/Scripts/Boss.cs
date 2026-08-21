using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    [Header("Health / Phases")]
    public int maxHealth = 90; // was 60 - x1.5'd to give the fixed-stats/ability rework enough runway to observe
    public UnityEvent OnPhase2;
    public UnityEvent OnDefeated;

    [Header("Movement")]
    // Erratic dash-or-hold movement, re-decided every dashDecisionInterval,
    // instead of a continuous predictable sine drift. X roams within the
    // viewport (minus screenPadding, same clamp idiom as
    // PlayerController.HandleMovement()); Y is capped by maxAdvanceFraction
    // so the boss can push toward the ships without ever reaching them.
    public float dashDecisionInterval = 1.5f;
    public float dashProbability = 0.35f;
    public float dashDistanceX = 1.2f;
    public float dashDistanceY = 0.8f;
    public float dashSpeed = 8f; // faster than any ship's own move speed - reads as a rapid burst
    public float maxAdvanceFraction = 0.4f; // "2/5 of the screen" - fraction of playable height below homeY the boss may advance into
    public Vector2 screenPadding = new Vector2(0.8f, 0.5f);

    [Header("Combat")]
    public GameObject bulletPrefab;
    public float phase1FireInterval = 1.2f;
    public float phase2FireInterval = 0.6f;
    public float bulletSpeed = 6f;
    public float spreadAngle = 15f; // phase 2 side-bullet offset
    public float bulletDamage = 1f; // single source of truth - contact/shockwave damage are multiples of this

    [Header("Body contact")]
    public float bodyContactDamageMultiplier = 2f;
    public float contactDamageCooldown = 1f;

    [Header("Shockwave")]
    public float shockwaveRadius = 1.7f; // boss half-extent (0.8) + ~1.5 ship-widths (0.9) from its edge
    public float shockwaveDamageMultiplier = 3f;
    public float shockwaveKnockback = 33f; // ~3.5 units of total displacement, see AddRecoil's decay math in PlayerController.cs
    public float shockwaveCooldown = 3f;
    public float shockwaveTelegraphTime = 0.3f;

    [Header("Shockwave Visual")]
    // Always-visible dim ring at shockwaveRadius so the danger zone reads
    // before it ever triggers; pulses to a bright warning color during the
    // telegraph wind-up, then flashes on the frame it actually hits. Same
    // procedural LineRenderer pattern as PlayerAbility.cs's Medic aura ring.
    public Color shockwaveRingColor = new Color(1f, 0.4f, 0.1f, 0.25f);
    public Color shockwaveRingTelegraphColor = new Color(1f, 0.15f, 0.1f, 0.85f);
    public Color shockwaveRingImpactColor = new Color(1f, 0.9f, 0.3f, 1f);
    public float shockwaveRingWidth = 0.06f;
    public float shockwaveRingTelegraphWidth = 0.14f;
    public float shockwaveTelegraphPulseSpeed = 12f;
    public float shockwaveImpactFlashDuration = 0.15f;
    private const int ShockwaveRingSegments = 32;

    [Header("Guided missile")]
    public PlayerRole[] guidedMissileTargetRoles = { PlayerRole.Medic, PlayerRole.Attacker };
    public float guidedMissileInterval = 5f;
    public float guidedMissileTelegraphTime = 0.8f;
    public float guidedMissileTurnRate = 90f; // degrees/second
    public float guidedMissileSpeed = 5f;
    public float guidedMissileWarningLingerTime = 2f; // keep the HUD warning up briefly after firing

    [Header("Aggro / Targets")]
    public GameObject[] targets; // drag Player + 3 Teammates
    public float tauntBonus = 100f;

    [Header("Scene hookup")]
    public GameObject enemySpawner; // drag Spawner; auto-disabled on Awake so wave enemies don't confound the boss fight

    public int CurrentHealth { get; private set; }
    public bool IsPhase2 { get; private set; }
    public GameObject CurrentTarget { get; private set; }
    public PlayerRole? GuidedMissileTargetRole { get; private set; }
    // Proximity-triggered, not a fixed auto-cast - reads "Ready" whenever no
    // ship has gotten close enough to trigger it yet, not just after cooldown elapses.
    public float ShockwaveCooldownRemaining => Mathf.Max(0f, nextShockwaveCheckTime - Time.time);
    public float GuidedMissileCooldownRemaining => Mathf.Max(0f, nextGuidedMissileTime - Time.time);

    private readonly Dictionary<GameObject, float> aggro = new Dictionary<GameObject, float>();
    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    private float nextFireTime;
    private float nextDashDecisionTime;
    private float nextShockwaveCheckTime;
    private float nextGuidedMissileTime;
    private Vector3 moveTarget;
    private float homeY;
    private Camera cam;
    private LineRenderer shockwaveRing;
    private bool isTelegraphingShockwave;
    private float shockwaveImpactFlashUntil;

    void Awake()
    {
        CurrentHealth = maxHealth;
        cam = Camera.main;
        homeY = transform.position.y;
        moveTarget = transform.position;
        CreateShockwaveRing();

        foreach (GameObject t in targets)
        {
            if (t != null) aggro[t] = 0f;
        }
        CurrentTarget = targets.Length > 0 ? targets[0] : null;

        if (enemySpawner != null) enemySpawner.SetActive(false);
    }

    void Update()
    {
        HandleMovement();

        PickTarget();

        float interval = IsPhase2 ? phase2FireInterval : phase1FireInterval;
        if (CurrentTarget != null && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + interval;
            Fire();
        }

        CheckShockwave();
        CheckGuidedMissile();
        UpdateShockwaveRing();
    }

    // World-space ring around the boss showing the shockwave's danger radius
    // - dim and always visible, pulses brighter during the telegraph
    // wind-up, then flashes on the frame it actually hits. Built the same
    // way PlayerAbility.cs's Medic aura ring is (procedural LineRenderer,
    // Sprites/Default shader, no art asset).
    private void CreateShockwaveRing()
    {
        GameObject ringObj = new GameObject("ShockwaveRing");
        ringObj.transform.SetParent(transform, false);
        shockwaveRing = ringObj.AddComponent<LineRenderer>();
        shockwaveRing.useWorldSpace = true;
        shockwaveRing.loop = true;
        shockwaveRing.positionCount = ShockwaveRingSegments;
        shockwaveRing.material = new Material(Shader.Find("Sprites/Default"));
        shockwaveRing.sortingLayerName = "Default";
        shockwaveRing.sortingOrder = -1; // behind the boss sprite
    }

    private void UpdateShockwaveRing()
    {
        if (shockwaveRing == null) return;

        Color color;
        float width;
        if (isTelegraphingShockwave)
        {
            float pulse = (Mathf.Sin(Time.time * shockwaveTelegraphPulseSpeed) + 1f) * 0.5f;
            color = Color.Lerp(shockwaveRingColor, shockwaveRingTelegraphColor, pulse);
            width = Mathf.Lerp(shockwaveRingWidth, shockwaveRingTelegraphWidth, pulse);
        }
        else if (Time.time < shockwaveImpactFlashUntil)
        {
            color = shockwaveRingImpactColor;
            width = shockwaveRingTelegraphWidth;
        }
        else
        {
            color = shockwaveRingColor;
            width = shockwaveRingWidth;
        }
        shockwaveRing.startColor = color;
        shockwaveRing.endColor = color;
        shockwaveRing.startWidth = width;
        shockwaveRing.endWidth = width;

        Vector3 center = transform.position;
        for (int i = 0; i < ShockwaveRingSegments; i++)
        {
            float angle = (i / (float)ShockwaveRingSegments) * Mathf.PI * 2f;
            shockwaveRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * shockwaveRadius);
        }
    }

    void HandleMovement()
    {
        if (Time.time >= nextDashDecisionTime)
        {
            nextDashDecisionTime = Time.time + dashDecisionInterval;
            if (Random.value < dashProbability)
            {
                float dirX = Random.value < 0.5f ? -1f : 1f;
                float dirY = Random.value < 0.5f ? -1f : 1f;
                Vector3 offset = new Vector3(dirX * dashDistanceX, dirY * dashDistanceY, 0f);
                moveTarget = ClampToBounds(transform.position + offset);
            }
            else
            {
                moveTarget = transform.position; // hold still
            }
        }
        transform.position = Vector3.MoveTowards(transform.position, moveTarget, dashSpeed * Time.deltaTime);
    }

    Vector3 ClampToBounds(Vector3 pos)
    {
        if (cam == null) return pos;

        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float viewportHeight = max.y - min.y;

        float clampedX = Mathf.Clamp(pos.x, min.x + screenPadding.x, max.x - screenPadding.x);
        float minY = homeY - maxAdvanceFraction * viewportHeight;
        float clampedY = Mathf.Clamp(pos.y, minY, homeY);
        return new Vector3(clampedX, clampedY, 0f);
    }

    void PickTarget()
    {
        float bestAggro;
        if (CurrentTarget == null || !aggro.TryGetValue(CurrentTarget, out bestAggro)) bestAggro = -1f;

        foreach (GameObject t in targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            float candidateAggro;
            if (!aggro.TryGetValue(t, out candidateAggro)) continue;
            if (candidateAggro > bestAggro)
            {
                bestAggro = candidateAggro;
                CurrentTarget = t;
            }
        }
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

    void SpawnBullet(Vector2 dir)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = bulletDamage;
        b.Init(dir, bulletSpeed, "Enemy");
    }

    void CheckShockwave()
    {
        if (Time.time < nextShockwaveCheckTime) return;

        foreach (GameObject t in targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (Vector2.Distance(t.transform.position, transform.position) <= shockwaveRadius)
            {
                nextShockwaveCheckTime = Time.time + shockwaveCooldown;
                StartCoroutine(ShockwaveRoutine());
                return;
            }
        }
    }

    IEnumerator ShockwaveRoutine()
    {
        isTelegraphingShockwave = true;
        yield return new WaitForSeconds(shockwaveTelegraphTime);
        isTelegraphingShockwave = false;
        shockwaveImpactFlashUntil = Time.time + shockwaveImpactFlashDuration;

        foreach (GameObject t in targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            Vector2 toTarget = (Vector2)t.transform.position - (Vector2)transform.position;
            if (toTarget.magnitude > shockwaveRadius) continue;

            PlayerHealth health = t.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(Mathf.RoundToInt(bulletDamage * shockwaveDamageMultiplier));

            PlayerController pc = t.GetComponent<PlayerController>();
            if (pc != null)
            {
                Vector2 pushDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.up;
                pc.AddRecoil(pushDir * shockwaveKnockback);
            }
        }
    }

    void CheckGuidedMissile()
    {
        if (Time.time < nextGuidedMissileTime) return;

        List<GameObject> eligible = new List<GameObject>();
        foreach (GameObject t in targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            PlayerRoleComponent roleComponent = t.GetComponent<PlayerRoleComponent>();
            if (roleComponent == null) continue;
            foreach (PlayerRole candidateRole in guidedMissileTargetRoles)
            {
                if (roleComponent.role == candidateRole)
                {
                    eligible.Add(t);
                    break;
                }
            }
        }
        if (eligible.Count == 0) return;

        nextGuidedMissileTime = Time.time + guidedMissileInterval;
        GameObject chosen = eligible[Random.Range(0, eligible.Count)];
        StartCoroutine(GuidedMissileRoutine(chosen));
    }

    IEnumerator GuidedMissileRoutine(GameObject target)
    {
        PlayerRoleComponent targetRole = target != null ? target.GetComponent<PlayerRoleComponent>() : null;
        GuidedMissileTargetRole = targetRole != null ? (PlayerRole?)targetRole.role : null;

        yield return new WaitForSeconds(guidedMissileTelegraphTime);

        if (target != null && target.activeInHierarchy && bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Bullet b = bulletObj.GetComponent<Bullet>();
            b.damage = bulletDamage;
            b.InitHoming(target.transform, guidedMissileTurnRate, guidedMissileSpeed, "Enemy");
        }

        yield return new WaitForSeconds(guidedMissileWarningLingerTime);
        GuidedMissileTargetRole = null;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        GameObject go = health.gameObject;
        if (lastContactDamageTime.TryGetValue(go, out float t) && Time.time - t < contactDamageCooldown) return;

        lastContactDamageTime[go] = Time.time;
        health.TakeDamage(Mathf.RoundToInt(bulletDamage * bodyContactDamageMultiplier));
    }

    public void TakeDamage(float amount, GameObject source)
    {
        CurrentHealth -= Mathf.RoundToInt(amount);
        if (source != null && aggro.ContainsKey(source)) aggro[source] += amount;

        if (!IsPhase2 && CurrentHealth <= maxHealth / 2)
        {
            IsPhase2 = true;
            OnPhase2?.Invoke();
        }

        if (CurrentHealth <= 0) Die();
    }

    public void TauntedBy(GameObject taunter)
    {
        if (!aggro.ContainsKey(taunter)) return;

        float highest = 0f;
        foreach (KeyValuePair<GameObject, float> kv in aggro) highest = Mathf.Max(highest, kv.Value);
        aggro[taunter] = highest + tauntBonus;
    }

    void Die()
    {
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }
}
