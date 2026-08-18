using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float sineAmplitude = 1.5f;
    public float sineFrequency = 1f;

    [Header("Combat")]
    public int health = 3;
    public GameObject bulletPrefab;
    public float fireInterval = 1.5f;
    public float bulletSpeed = 6f;

    private float startX;
    private float nextFireTime;

    void Start()
    {
        startX = transform.position.x;
        nextFireTime = Time.time + Random.Range(0f, fireInterval); // stagger enemy shots
    }

    void Update()
    {
        // Move down the screen in a Galaga-style sine wave
        float newY = transform.position.y - moveSpeed * Time.deltaTime;
        float newX = startX + Mathf.Sin(Time.time * sineFrequency) * sineAmplitude;
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

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Destroy(gameObject);
    }
}
