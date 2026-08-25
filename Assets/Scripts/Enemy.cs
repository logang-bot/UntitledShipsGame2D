using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Mirrors Minion.Active - lets LevelSequencer detect "zero enemies on
    // screen" without a separate manager tracking spawned/destroyed counts.
    public static readonly List<Enemy> Active = new List<Enemy>();

    // Set externally by EnemySpawner right after Instantiate, before Start() runs next frame -
    // same safe assign-before-Start ordering Level1Boss.SpawnBullet() relies on for b.damage.
    // Defaults to SineWave so a stray direct-prefab spawn (no spawner involved) behaves exactly
    // as it always has.
    public enum MovementPattern { SineWave, ZigZag, StraightDive }

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float sineAmplitude = 1.5f;
    public float sineFrequency = 1f;
    public MovementPattern movementPattern = MovementPattern.SineWave;
    public float zigzagInterval = 0.4f; // seconds between direction flips
    public float zigzagSpeed = 3f;      // horizontal speed while zigzagging
    public float diveSpeedMultiplier = 1.6f; // moveSpeed multiplier for StraightDive

    [Header("Combat")]
    public int health = 3;
    public GameObject bulletPrefab;
    public float fireInterval = 1.5f;
    public float bulletSpeed = 6f;

    [Header("Contact")]
    // Kamikaze - same shape as Minion.cs's contact damage: one hit, then it
    // dies, rather than a repeat-hit cooldown. Must stay >= 1 - fractions
    // round to 0 through PlayerHealth.TakeDamage(int)'s Mathf.RoundToInt
    // (see Minion.cs's bulletDamage comment for the same footgun).
    public float contactDamage = 1f;

    public enum EnemyType { Standard, Explosive }

    [Header("Explosive Death")]
    public EnemyType type = EnemyType.Standard;
    public GameObject fragmentPrefab;      // falls back to bulletPrefab if unassigned
    public int fragmentCount = 8;
    public float fragmentSpeed = 5f;
    public float fragmentDamage = 1f;      // must stay >= 1 - same rounding footgun as contactDamage
    public Color explosiveTintColor = new Color(1f, 0.45f, 0.1f);

    public Vector2 HalfExtents { get; private set; }

    private float startX;
    private float nextFireTime;
    private float zigzagDir = 1f;
    private float nextZigzagFlip;
    private bool isDead; // guards against a same-frame double-kill (bullet + ship contact), same as Minion.cs

    void Awake()
    {
        Active.Add(this);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) HalfExtents = col.bounds.extents;
    }

    void OnDestroy()
    {
        Active.Remove(this);
    }

    void Start()
    {
        startX = transform.position.x;
        nextFireTime = Time.time + Random.Range(0f, fireInterval); // stagger enemy shots
        nextZigzagFlip = Time.time + zigzagInterval;
    }

    // Called by EnemySpawner right after Instantiate (Awake has already run by then,
    // since Unity runs Awake synchronously during Instantiate), same reasoning as
    // Minion.Init() needing to run after Awake to tint the already-cached SpriteRenderer.
    public void Init(MovementPattern pattern, EnemyType enemyType)
    {
        movementPattern = pattern;
        type = enemyType;
        if (type == EnemyType.Explosive)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = explosiveTintColor;
        }
    }

    void Update()
    {
        float newY;
        float newX;
        switch (movementPattern)
        {
            case MovementPattern.ZigZag:
                // Real alternating step, not a smoother sine - reads distinctly more erratic.
                if (Time.time >= nextZigzagFlip)
                {
                    zigzagDir = -zigzagDir;
                    nextZigzagFlip = Time.time + zigzagInterval;
                }
                newY = transform.position.y - moveSpeed * Time.deltaTime;
                newX = transform.position.x + zigzagDir * zigzagSpeed * Time.deltaTime;
                break;
            case MovementPattern.StraightDive:
                // No horizontal movement at all - fast, dangerous, no dodging via horizontal reads.
                newY = transform.position.y - moveSpeed * diveSpeedMultiplier * Time.deltaTime;
                newX = startX;
                break;
            default: // SineWave - Galaga-style, unchanged from the original behavior
                newY = transform.position.y - moveSpeed * Time.deltaTime;
                newX = startX + Mathf.Sin(Time.time * sineFrequency) * sineAmplitude;
                break;
        }
        transform.position = new Vector3(newX, newY, 0);

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireInterval;
            Fire();
        }

        // Destroy if it drifts off the bottom of the screen
        if (transform.position.y < -10f) Die();
    }

    void Fire()
    {
        if (bulletPrefab == null) return;
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bullet.GetComponent<Bullet>();
        b.Init(Vector2.down, bulletSpeed, "Enemy");
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        health -= Mathf.RoundToInt(amount);
        if (health <= 0) Die();
    }

    // Called by a ship's own PlayerController.ResolveShipCollisions() the
    // moment its overlap-resolution math detects it overlapping this enemy -
    // same shape as Minion.ApplyContactDamage (kamikaze: doesn't survive the hit).
    public void ApplyContactDamage(GameObject ship)
    {
        if (isDead || ship == null) return;
        PlayerHealth playerHealth = ship.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(Mathf.RoundToInt(contactDamage));
        Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        if (type == EnemyType.Explosive) SpawnFragments();
        Destroy(gameObject);
    }

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

            SpriteRenderer fragSr = fragObj.GetComponent<SpriteRenderer>();
            if (fragSr != null) fragSr.color = explosiveTintColor;
        }
    }
}
