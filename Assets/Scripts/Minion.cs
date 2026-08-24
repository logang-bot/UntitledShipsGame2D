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
    public float contactDamage = 1f; // see bulletDamage comment - must stay >= 1, fractions round to 0
    public float contactDamageCooldown = 1f;

    public Vector2 HalfExtents { get; private set; }

    private Boss boss;
    private Vector2 flankOffset;
    private float wobblePhase;
    private float nextFireTime;
    private readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();

    void Awake()
    {
        Active.Add(this);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) HalfExtents = col.bounds.extents;
        wobblePhase = Random.Range(0f, Mathf.PI * 2f); // desync multiple minions' bob
    }

    void OnDestroy() { Active.Remove(this); }

    // Called once by MinionSpawner right after Instantiate - sets which boss
    // this minion flanks and its fixed offset from the boss's live position.
    public void Init(Boss owner, Vector2 offset)
    {
        boss = owner;
        flankOffset = offset;
        nextFireTime = Time.time + Random.Range(0f, fireInterval); // stagger, same idiom as Enemy.cs
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
        health -= Mathf.RoundToInt(amount);
        if (health <= 0) Destroy(gameObject);
    }

    // Called by a ship's own PlayerController.ResolveShipCollisions() the
    // moment its overlap-resolution math detects it overlapping this minion -
    // same shape as Boss.ApplyContactDamage.
    public void ApplyContactDamage(GameObject ship)
    {
        if (ship == null) return;
        PlayerHealth playerHealth = ship.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        if (lastContactDamageTime.TryGetValue(ship, out float t) && Time.time - t < contactDamageCooldown) return;

        lastContactDamageTime[ship] = Time.time;
        playerHealth.TakeDamage(Mathf.RoundToInt(contactDamage));
    }
}
