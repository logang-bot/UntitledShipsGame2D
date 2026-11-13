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
    // Extra buffer beyond the ship's own collider size (see ClampToScreen)
    // - 0 lets the ship's edge reach all the way to the true screen edge,
    // same as enemies (which have no screen clamp at all).
    public Vector2 screenPadding = Vector2.zero;
    // Non-destructive buff multiplier - set/cleared by PlayerAbility's
    // party-wide speed boost, never mutated into moveSpeed itself, so
    // there's nothing to divide back out (and nothing to double-apply).
    public float speedBuffMultiplier = 1f;
    // Set while the Attacker casts its Nova Beam: the ship holds still and
    // stops its normal fire until the beam ends (AttackerBeamCast).
    public bool castLocked;

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
    public MonoBehaviour bossObject; // must implement IBoss - MarauderBoss or HalcyonBoss

    private Rigidbody2D rb;
    private Camera cam;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private bool isFiring;
    private float nextFireTime;
    private Vector2 recoilVelocity;
    private Vector2 selfHalfExtents;
    private ShipCollisionResolver collisions;

    // Seconds until the next shot, derived from the live (possibly buffed)
    // shots/second - computed at point of use rather than stored, so
    // buffs never need to mutate a cached interval.
    private float FireInterval => 1f / (shotsPerSecond * fireRateBuffMultiplier);

    // This ship's live damage-per-second if every shot lands, including any
    // active fire-rate buff (Support's party-wide boost). Computed at the
    // point of use like FireInterval above, never cached, so a buff starting
    // or ending needs no invalidation. Distinct from RoleStats.Dps, which is
    // the role's *designed* number from the static table - these differ
    // whenever a buff is up, or if a field was hand-tweaked in the Inspector.
    public float CurrentDps => fireDamage * shotsPerSecond * fireRateBuffMultiplier;

    /// <summary>The ship's collider width in world units (about 0.6).</summary>
    public float ShipWidth => selfHalfExtents.x * 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
        selfHalfExtents = GetComponent<BoxCollider2D>().bounds.extents;
        collisions = new ShipCollisionResolver(gameObject, selfHalfExtents, bossObject);
        ApplyRoleStats();
    }

    private void ApplyRoleStats()
    {
        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent == null) return;
        moveSpeed = roleComponent.Stats.moveSpeed;
        shotsPerSecond = roleComponent.Stats.shotsPerSecond;
        fireDamage = roleComponent.Stats.fireDamage;
    }

    void Update()
    {
        TryAutoFire();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (castLocked) { HoldStill(); return; }
        recoilVelocity = Vector2.Lerp(recoilVelocity, Vector2.zero, recoilDamping * Time.fixedDeltaTime);
        Vector2 targetVelocity = moveInput.normalized * (moveSpeed * speedBuffMultiplier);
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        Vector2 newPos = rb.position + (currentVelocity + recoilVelocity) * Time.fixedDeltaTime;
        rb.MovePosition(ClampToScreen(collisions.Resolve(newPos)));
    }

    private void HoldStill()
    {
        currentVelocity = Vector2.zero;
        recoilVelocity = Vector2.zero;
        rb.MovePosition(rb.position);
    }

    /// <summary>
    /// Clamps by the ship's own half-extent (not a flat guess) so its edge,
    /// not its center, is what reaches the screen boundary.
    /// </summary>
    private Vector2 ClampToScreen(Vector2 pos)
    {
        Vector3 min = cam.ViewportToWorldPoint(Vector3.zero);
        Vector3 max = cam.ViewportToWorldPoint(Vector3.one);
        Vector2 margin = selfHalfExtents + screenPadding;
        pos.x = Mathf.Clamp(pos.x, min.x + margin.x, max.x - margin.x);
        pos.y = Mathf.Clamp(pos.y, min.y + margin.y, max.y - margin.y);
        return pos;
    }

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
    public void OnFire(InputValue value)
    {
        SetFiring(value.isPressed);
    }

    // Non-input entry point so AIController can drive firing directly. Fires
    // immediately on press; Update() then repeats while it's held.
    public void SetFiring(bool firing)
    {
        isFiring = firing;
        TryAutoFire();
    }

    private void TryAutoFire()
    {
        if (!isFiring || castLocked || Time.time < nextFireTime) return;
        nextFireTime = Time.time + FireInterval;
        Fire();
    }

    public void AddRecoil(Vector2 impulse)
    {
        recoilVelocity += impulse;
    }

    void Fire()
    {
        SpawnBullet(1f, fireDamage, PlayerShotKind.Regular);
    }

    public void FireBigShot(float widthMultiplier, float damageAmount, PlayerShotKind kind)
    {
        SpawnBullet(widthMultiplier, damageAmount, kind);
    }

    void SpawnBullet(float widthMultiplier, float damageAmount, PlayerShotKind kind)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        if (TryGetComponent(out PlayerShotLook look)) look.Dress(bulletObj, kind, widthMultiplier);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = damageAmount;
        b.Init(Vector2.up, bulletSpeed, "Player", gameObject);
    }
}
