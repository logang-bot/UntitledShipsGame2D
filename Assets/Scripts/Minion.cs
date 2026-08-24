using System.Collections.Generic;
using UnityEngine;

public class Minion : MonoBehaviour
{
    // Lets PlayerController resolve ship/minion collisions without a
    // fixed-size array - minions are spawned/destroyed at runtime by
    // MinionSpawner, unlike the hand-placed Player/Teammate_*/Boss. Same
    // pattern as Bullet.Active.
    public static readonly List<Minion> Active = new List<Minion>();

    [Header("Movement")]
    // Positioning is boss-relative, not free drift like wave Enemy.cs - each
    // minion holds a fixed local offset from the boss (assigned by
    // MinionSpawner.Init()) plus a small independent wobble so it still
    // reads as alive. This tracks the boss's own erratic dash movement
    // automatically, with zero pathfinding.
    public float wobbleAmplitude = 0.2f;
    public float wobbleFrequency = 1.5f;

    [Header("Combat")]
    public int health = 2;
    public GameObject bulletPrefab;
    public float fireInterval = 2f;
    public float bulletSpeed = 6f;
    // Whole numbers only - PlayerHealth.TakeDamage(int) rounds via
    // Mathf.RoundToInt, which rounds anything below 0.5 down to 0 and
    // rounds exactly 0.5 to 0 too (round-half-to-even) - a fractional value
    // here would silently deal zero damage. Matches Enemy.cs's own bullet
    // damage (1) - a chip-damage add, not a second boss.
    public float bulletDamage = 1f;

    [Header("Contact")]
    // Kamikaze - a minion deals this once on the ship it touches, then dies
    // immediately (see Die()), so there's no repeat-hit cooldown to track.
    public float contactDamage = 1f; // see bulletDamage comment - must stay >= 1, fractions round to 0

    public enum MinionType { Standard, Explosive }

    [Header("Explosive Death")]
    public MinionType type = MinionType.Standard;
    // Falls back to bulletPrefab if left unassigned - a fragment is just
    // another enemy-owned Bullet, no dedicated prefab required.
    public GameObject fragmentPrefab;
    public int fragmentCount = 8;
    public float fragmentSpeed = 5f;
    public float fragmentDamage = 1f; // see bulletDamage comment - must stay >= 1, fractions round to 0
    public Color explosiveTintColor = new Color(1f, 0.45f, 0.1f);

    public Vector2 HalfExtents { get; private set; }

    private Boss boss;
    private Vector2 flankOffset;
    private float wobblePhase;
    private float nextFireTime;
    private bool isDead; // guards against a same-frame double-kill (bullet + ship contact both landing before Destroy() actually processes)

    void Awake()
    {
        Active.Add(this);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) HalfExtents = col.bounds.extents;
        wobblePhase = Random.Range(0f, Mathf.PI * 2f); // desync multiple minions' bob
    }

    void OnDestroy() { Active.Remove(this); }

    // Called once by MinionSpawner right after Instantiate - sets which boss
    // this minion flanks, its fixed offset from the boss's live position,
    // and its type. Type has to flow in here rather than being set directly
    // on the field post-Instantiate, since Awake() (which would need it for
    // the tint below) already ran by then.
    public void Init(Boss owner, Vector2 offset, MinionType minionType = MinionType.Standard)
    {
        boss = owner;
        flankOffset = offset;
        type = minionType;
        nextFireTime = Time.time + Random.Range(0f, fireInterval); // stagger, same idiom as Enemy.cs

        if (type == MinionType.Explosive)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = explosiveTintColor;
        }
    }

    void Update()
    {
        if (boss == null) { Destroy(gameObject); return; } // orphaned - no anchor to follow

        Vector2 wobble = new Vector2(0f, Mathf.Sin(Time.time * wobbleFrequency + wobblePhase) * wobbleAmplitude);
        transform.position = (Vector2)boss.transform.position + flankOffset + wobble;

        if (boss.CurrentTarget != null && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireInterval;
            Fire();
        }
    }

    // Always aims at the boss's own current aggro target rather than
    // tracking anything of its own - ties minions to the existing aggro
    // system for free (Tank taunt redirects minion fire too) with no
    // independent threat table to maintain.
    void Fire()
    {
        if (bulletPrefab == null || boss == null || boss.CurrentTarget == null) return;
        Vector2 dir = ((Vector2)boss.CurrentTarget.transform.position - (Vector2)transform.position).normalized;
        SpawnBullet(dir);
    }

    void SpawnBullet(Vector2 dir)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = bulletDamage;
        b.Init(dir, bulletSpeed, "Enemy");
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        health -= Mathf.RoundToInt(amount);
        if (health <= 0) Die();
    }

    // Called by a ship's own PlayerController.ResolveShipCollisions() the
    // moment its overlap-resolution math detects it overlapping this minion -
    // same shape as Boss.ApplyContactDamage, except this minion doesn't
    // survive the hit (kamikaze).
    public void ApplyContactDamage(GameObject ship)
    {
        if (isDead || ship == null) return;
        PlayerHealth playerHealth = ship.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(Mathf.RoundToInt(contactDamage));
        Die();
    }

    // Single funnel point for both death paths (bullet kill and kamikaze
    // contact) so Explosive fragments only ever spawn once per minion.
    void Die()
    {
        if (isDead) return;
        isDead = true;
        if (type == MinionType.Explosive) SpawnFragments();
        Destroy(gameObject);
    }

    // Same "evenly-spaced ring, random start offset" idiom as
    // Boss.FireRing() - a fragment is just another enemy-owned Bullet, so
    // Bullet.cs needs zero changes for these to damage ships correctly.
    void SpawnFragments()
    {
        GameObject prefab = fragmentPrefab != null ? fragmentPrefab : bulletPrefab;
        if (prefab == null || fragmentCount <= 0) return;

        float step = 360f / fragmentCount;
        float startOffset = Random.Range(0f, step);
        for (int i = 0; i < fragmentCount; i++)
        {
            float angle = startOffset + step * i;
            Vector2 dir = (Vector2)(Quaternion.Euler(0, 0, angle) * Vector2.up);

            GameObject fragObj = Instantiate(prefab, transform.position, Quaternion.identity);
            Bullet b = fragObj.GetComponent<Bullet>();
            b.damage = fragmentDamage;
            b.Init(dir, fragmentSpeed, "Enemy");
        }
    }
}
