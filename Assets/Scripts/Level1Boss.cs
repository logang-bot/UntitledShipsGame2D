using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Level1Boss : MonoBehaviour
{
    [Header("Health / Phases")]
    public int maxHealth = 90; // was 60 - x1.5'd to give the fixed-stats/ability rework enough runway to observe
    public UnityEvent OnPhase2;
    public UnityEvent OnDefeated;

    [Header("Movement — pattern")]
    // Scripted movement, not AI: hops between the fixed vertices of an "M"
    // shape (home, the two outer top corners, their low points below, and
    // the middle notch between them) in random order - see
    // MovementPatternRoutine/GetPatternVertices. Every hop is a real eased
    // travel (MoveOverTime) from wherever the boss currently is, so nothing
    // ever snaps/teleports. Both the hop's travel duration and how long the
    // boss pauses at each vertex before the next hop are re-rolled every
    // time, so the shape stays recognizable (it only ever stops at one of
    // these six points) while the path and pacing are unpredictable. Loops
    // for the rest of the fight, unchanged across both phases. Reuses
    // ClampToBounds()'s existing viewport/maxAdvanceFraction clamp so the
    // descent limit stays governed by the same tuning knob the old
    // random-dash movement used.
    public float sideOffsetX = 2.2f;
    // Per-hop travel duration is randomized within this range every hop -
    // the low end can be fast enough to read as a sudden dart between points.
    public float patternMoveDurationMin = 0.4f;
    public float patternMoveDurationMax = 2f;
    // How long the boss sits still at a vertex before hopping to the next
    // one, randomized within this range each time - see MovementPatternRoutine.
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

    [Header("Pattern Barrage")]
    // One standalone attack, three possible shapes - randomly picked each
    // activation (never the same shape twice in a row, see PickPattern()),
    // same "build eligible options, Random.Range pick one" idiom as guided
    // missile's target selection, rather than three separate cooldown/
    // telegraph/HUD stacks.
    public float patternBarrageCooldown = 7f;
    public float patternBarrageTelegraphTime = 0.7f;
    public int fanBulletCount = 5;
    public float fanSpreadAngle = 50f; // total angular width, centered on the aim direction
    public int ringBulletCount = 12;
    public int spiralBulletCount = 20;
    public float spiralAngleStep = 25f; // degrees added per shot
    public float spiralShotInterval = 0.05f; // seconds between shots - this is what actually reads as "rapid-fire"

    public enum BulletPattern { Fan, Ring, Spiral }

    [Header("Aggro / Targets")]
    public GameObject[] targets; // drag Player + 3 Teammates
    public float tauntBonus = 100f;

    public int CurrentHealth { get; private set; }
    public bool IsPhase2 { get; private set; }
    public GameObject CurrentTarget { get; private set; }
    public PlayerRole? GuidedMissileTargetRole { get; private set; }
    public BulletPattern? PatternBarrageActivePattern { get; private set; }
    // Proximity-triggered, not a fixed auto-cast - reads "Ready" whenever no
    // ship has gotten close enough to trigger it yet, not just after cooldown elapses.
    public float ShockwaveCooldownRemaining => Mathf.Max(0f, nextShockwaveCheckTime - Time.time);
    public float GuidedMissileCooldownRemaining => Mathf.Max(0f, nextGuidedMissileTime - Time.time);
    public float PatternBarrageCooldownRemaining => Mathf.Max(0f, nextPatternBarrageTime - Time.time);

    private readonly Dictionary<GameObject, float> aggro = new Dictionary<GameObject, float>();
    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    private float nextFireTime;
    private float nextShockwaveCheckTime;
    private float nextGuidedMissileTime;
    private float nextPatternBarrageTime;
    private BulletPattern? lastPatternBarragePattern;
    private Vector3 home; // captured fresh in OnEnable - LevelSequencer always lands the boss here first
    private float homeY;
    private Camera cam;
    private LineRenderer shockwaveRing;
    private bool isTelegraphingShockwave;
    private float shockwaveImpactFlashUntil;
    private SpriteRenderer sr;
    private Collider2D col;
    private MinionSpawner minionSpawner;

    void Awake()
    {
        CurrentHealth = maxHealth;
        cam = Camera.main;
        homeY = transform.position.y;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        minionSpawner = GetComponent<MinionSpawner>();
        if (minionSpawner != null) minionSpawner.enabled = false; // matches this component's own starts-disabled state
        CreateShockwaveRing();

        foreach (GameObject t in targets)
        {
            if (t != null) aggro[t] = 0f;
        }
        CurrentTarget = targets.Length > 0 ? targets[0] : null;
    }

    // Hides/shows the sprite, shockwave ring, and collider without touching
    // the GameObject's active state (SetActive(false) was tried and
    // rejected - see docs/systems/level-sequencing.md). The collider has to
    // be toggled too, not just the sprite: it's what Bullet.cs's
    // OnTriggerEnter2D needs to detect a hit, so leaving it enabled would
    // let player bullets damage the boss - and rack up real
    // TakeDamage()/aggro side effects - well before its entrance.
    //
    // Deliberately does NOT touch MinionSpawner (see OnEnable() below for
    // why): this fires at the *start* of the entrance glide, while ships
    // are still frozen for the next few seconds - starting minions here too
    // would let them spawn and overlap frozen ships that can't react
    // (PlayerController.enabled is false, so FixedUpdate/ResolveShipCollisions
    // never runs), making kamikaze contact silently do nothing.
    public void SetVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
        if (col != null) col.enabled = visible;
        if (shockwaveRing != null) shockwaveRing.gameObject.SetActive(visible);
    }

    // Fires every time this component is enabled - LevelSequencer flips it
    // on right after the boss's own entrance glide completes (ships already
    // unfrozen by then), so "home" is always wherever the sequencer just
    // placed it, not a value baked in Awake. MinionSpawner starts here too,
    // not in SetVisible - see the comment there - so the boss-flanking
    // kamikaze minions only ever appear once ships can actually react to them.
    void OnEnable()
    {
        home = transform.position;
        if (minionSpawner != null) minionSpawner.enabled = true;
        StartCoroutine(MovementPatternRoutine());

        // Attack timers default to 0, which without this would let every
        // attack fire on literally the first Update() after enabling - the
        // instant ships regain control, with zero reaction time. Give
        // players a breath before the boss's first move.
        float graceUntil = Time.time + postEntranceGracePeriod;
        nextFireTime = graceUntil;
        nextShockwaveCheckTime = graceUntil;
        nextGuidedMissileTime = graceUntil;
        nextPatternBarrageTime = graceUntil;
    }

    void Update()
    {
        // Belt-and-suspenders: LevelSequencer already keeps this component
        // disabled while ships are frozen, but this guarantees no attack can
        // land while a ship can't move/dodge even if that timing ever changes.
        if (LevelSequencer.ShipsFrozen) return;

        PickTarget();

        float interval = IsPhase2 ? phase2FireInterval : phase1FireInterval;
        if (CurrentTarget != null && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + interval;
            Fire();
        }

        CheckShockwave();
        CheckGuidedMissile();
        CheckPatternBarrage();
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

    // Loops for the rest of the fight once started: sit still for a random
    // beat (so players can't time when it'll move), then hop to a random
    // vertex of the "M" at a random speed. Runs identically through phase 1
    // and phase 2 - no branching.
    IEnumerator MovementPatternRoutine()
    {
        while (true)
        {
            float stillTime = Random.Range(cycleGapMin, cycleGapMax);
            yield return new WaitForSeconds(stillTime);

            Vector3 target = PickRandomVertex();
            float legDuration = Random.Range(patternMoveDurationMin, patternMoveDurationMax);
            yield return MoveOverTime(transform.position, target, legDuration);
        }
    }

    // The fixed points an "M" is built from: home, its two outer top
    // corners, their low points below, and the middle notch between them.
    // Recomputed each hop (not cached) since it depends on the live camera
    // viewport via BottomY()/ClampToBounds().
    Vector3[] GetPatternVertices()
    {
        float bottomY = BottomY();
        float notchY = Mathf.Lerp(homeY, bottomY, mPatternNotchDepth);
        return new Vector3[]
        {
            home,
            ClampToBounds(new Vector3(home.x - sideOffsetX, home.y, 0f)), // top-left
            ClampToBounds(new Vector3(home.x - sideOffsetX, bottomY, 0f)), // bottom-left
            ClampToBounds(new Vector3(home.x, notchY, 0f)), // notch
            ClampToBounds(new Vector3(home.x + sideOffsetX, bottomY, 0f)), // bottom-right
            ClampToBounds(new Vector3(home.x + sideOffsetX, home.y, 0f)), // top-right
        };
    }

    // Picks a random vertex, re-rolling (bounded) if it happens to land on
    // wherever the boss already is, so a hop always actually goes somewhere.
    Vector3 PickRandomVertex()
    {
        Vector3[] vertices = GetPatternVertices();
        Vector3 current = transform.position;
        Vector3 target;
        int guard = 0;
        do
        {
            target = vertices[Random.Range(0, vertices.Length)];
            guard++;
        } while (Vector3.Distance(target, current) < 0.05f && guard < 10);
        return target;
    }

    // The lowest Y the M's outer corners descend to - same
    // maxAdvanceFraction-of-viewport floor ClampToBounds() enforces, just
    // computed directly instead of relying on clamping an already-far-below candidate.
    float BottomY()
    {
        if (cam == null) return homeY;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float viewportHeight = max.y - min.y;
        return homeY - maxAdvanceFraction * viewportHeight;
    }

    IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        transform.position = to;
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

// Called by each ship's own PlayerController.ResolveShipCollisions() the
    // moment its overlap-resolution math detects it overlapping the boss -
    // replaces the old OnTriggerStay2D handler, which stopped being reachable
    // once ship/boss overlap is actively prevented before Unity's physics
    // engine ever sees a genuine overlap. Same cooldown-gated damage as
    // before. `ship` is always a ship's own root GameObject (the resolver
    // only ever checks each ship's own body collider, never a child collider
    // like Tank's Shield Arc), so GetComponent is correct here.
    public void ApplyContactDamage(GameObject ship)
    {
        if (ship == null) return;
        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (lastContactDamageTime.TryGetValue(ship, out float t) && Time.time - t < contactDamageCooldown) return;

        lastContactDamageTime[ship] = Time.time;
        health.TakeDamage(Mathf.RoundToInt(bulletDamage * bodyContactDamageMultiplier));
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

    void CheckPatternBarrage()
    {
        if (Time.time < nextPatternBarrageTime) return;
        if (CurrentTarget == null) return; // Fan needs an aim direction

        nextPatternBarrageTime = Time.time + patternBarrageCooldown;
        StartCoroutine(PatternBarrageRoutine());
    }

    // Random pick, excluding whichever shape fired last time, so the same
    // shape can never fire twice in a row while still keeping the surprise
    // of not knowing which of the other two is coming.
    BulletPattern PickPattern()
    {
        BulletPattern[] all = { BulletPattern.Fan, BulletPattern.Ring, BulletPattern.Spiral };
        BulletPattern picked = all[Random.Range(0, all.Length)];
        if (lastPatternBarragePattern.HasValue && picked == lastPatternBarragePattern.Value)
            picked = all[(System.Array.IndexOf(all, picked) + 1) % all.Length];
        return picked;
    }

    IEnumerator PatternBarrageRoutine()
    {
        BulletPattern pattern = PickPattern();
        lastPatternBarragePattern = pattern;
        PatternBarrageActivePattern = pattern;

        yield return new WaitForSeconds(patternBarrageTelegraphTime);

        // Re-aim after the telegraph wait, not at activation time - the
        // target may have moved (or died) during the wind-up, same
        // re-check-after-telegraph idiom ShockwaveRoutine() already uses.
        Vector2 aimDir = CurrentTarget != null
            ? ((Vector2)CurrentTarget.transform.position - (Vector2)transform.position).normalized
            : Vector2.down;

        switch (pattern)
        {
            case BulletPattern.Fan:
                FireFan(aimDir);
                break;
            case BulletPattern.Ring:
                FireRing();
                break;
            case BulletPattern.Spiral:
                yield return StartCoroutine(FireSpiralRoutine(aimDir));
                break;
        }

        PatternBarrageActivePattern = null;
    }

    void FireFan(Vector2 aimDir)
    {
        if (bulletPrefab == null) return;
        if (fanBulletCount <= 1) { SpawnBullet(aimDir); return; }

        float step = fanSpreadAngle / (fanBulletCount - 1);
        float startAngle = -fanSpreadAngle / 2f;
        for (int i = 0; i < fanBulletCount; i++)
        {
            float angle = startAngle + step * i;
            SpawnBullet((Vector2)(Quaternion.Euler(0, 0, angle) * aimDir));
        }
    }

    // Omnidirectional by definition - the boss never rotates, so there's no
    // "facing" to aim relative to. A randomized start-angle offset per burst
    // keeps the gaps between bullets from always landing in the same screen
    // position (would otherwise create a permanent memorized safe lane).
    void FireRing()
    {
        if (bulletPrefab == null || ringBulletCount <= 0) return;

        float step = 360f / ringBulletCount;
        float startOffset = Random.Range(0f, step);
        for (int i = 0; i < ringBulletCount; i++)
        {
            float angle = startOffset + step * i;
            SpawnBullet((Vector2)(Quaternion.Euler(0, 0, angle) * Vector2.up));
        }
    }

    // Starts aimed at the target like Fan (first shot reads as "aimed at
    // you"), then sweeps by spiralAngleStep per shot fired rapidly over
    // time - the one shape that actually delivers "rapid-fire", since
    // Fan/Ring resolve in a single frame.
    IEnumerator FireSpiralRoutine(Vector2 aimDir)
    {
        if (bulletPrefab == null) yield break;

        float angle = 0f;
        for (int i = 0; i < spiralBulletCount; i++)
        {
            SpawnBullet((Vector2)(Quaternion.Euler(0, 0, angle) * aimDir));
            angle += spiralAngleStep;
            yield return new WaitForSeconds(spiralShotInterval);
        }
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
