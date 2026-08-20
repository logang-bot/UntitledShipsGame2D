using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    
    private GameObject ownerObject; // shooter reference, for boss aggro attribution
private string owner; // "Player" or "Enemy" - determines what it can hit

    public float lifeTime = 3f;
    public float damage = 1f;

public void Init(Vector2 dir, float spd, string ownerTag, GameObject ownerObj = null)
    {
        direction = dir.normalized;
        speed = spd;
        owner = ownerTag;
        ownerObject = ownerObj;
        Destroy(gameObject, lifeTime); // safety cleanup if it never hits anything
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == "Player" && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);
            Boss boss = other.GetComponent<Boss>();
            if (boss != null) boss.TakeDamage(damage, ownerObject);
            Destroy(gameObject);
        }
        else if (owner == "Enemy" && other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(Mathf.RoundToInt(damage));
            Destroy(gameObject);
        }
    }

}
