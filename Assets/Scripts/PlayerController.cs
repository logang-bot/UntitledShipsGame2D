using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f; // fixed per-role base, see PlayerRoleStats
    // Caps how fast actual velocity can change, so movement ramps up/down
    // instead of snapping to full speed instantly - most noticeable right
    // after LevelSequencer unfreezes a ship (see ResetVelocity()).
    public float acceleration = 12f;
    // Extra buffer beyond the ship's own collider size (see HandleMovement's
    // clamp) - 0 lets the ship's edge reach all the way to the true screen
    // edge, same as enemies (which have no screen clamp at all).
    public Vector2 screenPadding = Vector2.zero;
    // Non-destructive buff multiplier - set/cleared by PlayerAbility's
    // party-wide speed boost, never mutated into moveSpeed itself, so
    // there's nothing to divide back out (and nothing to double-apply).
    public float speedBuffMultiplier = 1f;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shotsPerSecond = 2.857f; // fixed per-role base (higher = faster), see PlayerRoleStats
    public float bulletSpeed = 12f;
    public float fireDamage = 0.6f; // fixed per-role base, see PlayerRoleStats
    // Same non-destructive buff pattern as speedBuffMultiplier above.
    public float fireRateBuffMultiplier = 1f;

    [Header("Recoil")]
    public float recoilDamping = 8f;

    [Header("Collision")]
    public Level1Boss boss;

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private bool isFiring;
    private float nextFireTime;
    private Vector2 recoilVelocity;
    private BoxCollider2D selfCollider;
    private Vector2 selfHalfExtents;
    private PlayerAbility ability;
    private BoxCollider2D bossCollider;
    private Vector2 bossHalfExtents;
    private BoxCollider2D[] allyColliders;

    // Seconds until the next shot, derived from the live (possibly buffed)
    // shots/second - computed at point of use rather than stored, so
    // buffs never need to mutate a cached interval.
    private float FireInterval => 1f / (shotsPerSecond * fireRateBuffMultiplier);

void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        selfCollider = GetComponent<BoxCollider2D>();
        selfHalfExtents = selfCollider.bounds.extents;
        ability = GetComponent<PlayerAbility>();

        if (boss != null)
        {
            bossCollider = boss.GetComponent<BoxCollider2D>();
            if (bossCollider != null) bossHalfExtents = bossCollider.bounds.extents;
        }

        if (ability != null && ability.allies != null)
        {
            allyColliders = new BoxCollider2D[ability.allies.Length];
            for (int i = 0; i < ability.allies.Length; i++)
            {
                if (ability.allies[i] != null)
                    allyColliders[i] = ability.allies[i].GetComponent<BoxCollider2D>();
            }
        }

        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent != null)
        {
            moveSpeed = roleComponent.Stats.moveSpeed;
            shotsPerSecond = roleComponent.Stats.shotsPerSecond;
            fireDamage = roleComponent.Stats.fireDamage;
        }
    }

    void Update()
    {
        // Handles held-down fire (auto-fire while button is held)
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + FireInterval;
            Fire();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

void HandleMovement()
    {
        recoilVelocity = Vector2.Lerp(recoilVelocity, Vector2.zero, recoilDamping * Time.fixedDeltaTime);

        Vector2 targetVelocity = moveInput.normalized * (moveSpeed * speedBuffMultiplier);
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        Vector2 move = currentVelocity + recoilVelocity;
        Vector2 newPos = rb.position + move * Time.fixedDeltaTime;

        newPos = ResolveShipCollisions(newPos);

        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        // Clamp by the ship's own half-extent (not a flat guess) so its edge
        // - not its center - is what reaches the screen boundary.
        float marginX = selfHalfExtents.x + screenPadding.x;
        float marginY = selfHalfExtents.y + screenPadding.y;
        newPos.x = Mathf.Clamp(newPos.x, min.x + marginX, max.x - marginX);
        newPos.y = Mathf.Clamp(newPos.y, min.y + marginY, max.y - marginY);

        rb.MovePosition(newPos);
    }

// Resolves newPos against every other living ship (push-apart only - no
    // damage) and against the boss (push-apart AND contact damage), using the
    // same AABB math for both - ships/boss move in small discrete steps each
    // frame rather than teleporting, so a momentary overlap here is exactly
    // the signal a physical collision actually happened.
    Vector2 ResolveShipCollisions(Vector2 candidatePos)
    {
        if (selfCollider == null) return candidatePos;

        if (ability != null && ability.allies != null)
        {
            for (int i = 0; i < ability.allies.Length; i++)
            {
                Transform ally = ability.allies[i];
                if (ally == null || ally == transform || !ally.gameObject.activeInHierarchy) continue;
                BoxCollider2D allyCollider = allyColliders != null && i < allyColliders.Length ? allyColliders[i] : null;
                if (allyCollider == null) continue;

                candidatePos = ShipCollisionUtil.ResolveBoxOverlap(
                    candidatePos, selfHalfExtents,
                    ally.position, allyCollider.bounds.extents,
                    out _);
            }
        }

        if (boss != null && bossCollider != null)
        {
            candidatePos = ShipCollisionUtil.ResolveBoxOverlap(
                candidatePos, selfHalfExtents,
                boss.transform.position, bossHalfExtents,
                out bool overlappingBoss);
            if (overlappingBoss) boss.ApplyContactDamage(gameObject);
        }

        // Minions are spawned/destroyed at runtime by MinionSpawner, so
        // (unlike allies/boss above) their colliders can't be cached once in
        // Start() - each Minion caches its own HalfExtents instead.
        foreach (Minion minion in Minion.Active)
        {
            if (minion == null) continue;
            candidatePos = ShipCollisionUtil.ResolveBoxOverlap(
                candidatePos, selfHalfExtents,
                minion.transform.position, minion.HalfExtents,
                out bool overlappingMinion);
            if (overlappingMinion) minion.ApplyContactDamage(gameObject);
        }

        // Same kamikaze contact damage as Minion.Active above, for the
        // pre-boss/phase-2 wave enemies (EnemySpawner) - same runtime-spawned
        // reasoning, each Enemy caches its own HalfExtents.
        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy == null) continue;
            candidatePos = ShipCollisionUtil.ResolveBoxOverlap(
                candidatePos, selfHalfExtents,
                enemy.transform.position, enemy.HalfExtents,
                out bool overlappingEnemy);
            if (overlappingEnemy) enemy.ApplyContactDamage(gameObject);
        }

        return candidatePos;
    }


    // Auto-called by PlayerInput ("Send Messages" behavior) whenever
    // the "Move" action changes value (WASD, arrows, or gamepad stick).
// Auto-called by PlayerInput ("Send Messages" behavior) whenever
    // the "Move" action changes value (WASD, arrows, or gamepad stick).
    public void OnMove(InputValue value)
    {
        SetMoveDirection(value.Get<Vector2>());
    }

    // Non-input entry point so AIController can drive movement directly.
    public void SetMoveDirection(Vector2 direction)
    {
        moveInput = direction;
    }

    // Called by LevelSequencer right before freezing a ship, so the next
    // unfreeze ramps up from a standing start instead of resuming whatever
    // velocity carried over from before the freeze.
    public void ResetVelocity()
    {
        currentVelocity = Vector2.zero;
    }

    // Auto-called by PlayerInput whenever the "Fire" action is pressed/released.
// Auto-called by PlayerInput whenever the "Fire" action is pressed/released.
    public void OnFire(InputValue value)
    {
        SetFiring(value.isPressed);
    }

    // Non-input entry point so AIController can drive firing directly.
    public void SetFiring(bool firing)
    {
        isFiring = firing;

        // Fire immediately on press, then Update() handles repeat-fire while held
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + FireInterval;
            Fire();
        }
    }

    public void AddRecoil(Vector2 impulse)
    {
        recoilVelocity += impulse;
    }

    void Fire()
    {
        SpawnBullet(1f, fireDamage);
    }

    public void FireBigShot(float widthMultiplier, float damageAmount)
    {
        SpawnBullet(widthMultiplier, damageAmount);
    }

void SpawnBullet(float widthMultiplier, float damageAmount)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        if (widthMultiplier != 1f)
        {
            Vector3 scale = bulletObj.transform.localScale;
            scale.x *= widthMultiplier;
            bulletObj.transform.localScale = scale;
        }
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = damageAmount;
        b.Init(Vector2.up, bulletSpeed, "Player", gameObject);
    }

}
