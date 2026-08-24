using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Set externally by EnemySpawner right after Instantiate, before Start() runs next frame -
    // same safe assign-before-Start ordering Boss.SpawnBullet() relies on for b.damage.
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

    private float startX;
    private float nextFireTime;
    private float zigzagDir = 1f;
    private float nextZigzagFlip;

    void Start()
    {
        startX = transform.position.x;
        nextFireTime = Time.time + Random.Range(0f, fireInterval); // stagger enemy shots
        nextZigzagFlip = Time.time + zigzagInterval;
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
        if (transform.position.y < -10f) Destroy(gameObject);
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
        health -= Mathf.RoundToInt(amount);
        if (health <= 0) Destroy(gameObject);
    }
}
