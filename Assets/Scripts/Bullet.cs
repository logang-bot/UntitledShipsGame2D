
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // AI teammates read this to enumerate live enemy bullets for dodging
    // (AIController.ComputeDodgeVector()) without a per-frame scene scan.
    public static readonly List<Bullet> Active = new List<Bullet>();

    public Vector2 Direction => direction;
    public float Speed => speed;
    public string Owner => owner;

    void Awake() { Active.Add(this); }
    void OnDestroy() { Active.Remove(this); }

    private Vector2 direction;
    private float speed;
    
    private GameObject ownerObject; // shooter reference, for boss aggro attribution
private string owner; // "Player" or "Enemy" - determines what it can hit

    private Transform homingTarget;
    private float turnRateDegPerSec;

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

    // Re-aims toward target's current position every frame, at a capped turn
    // rate, instead of the fixed-at-spawn direction Init() uses - dodgeable,
    // not inescapable. If target dies/deactivates mid-flight, Update() simply
    // stops re-aiming and the bullet keeps its last heading; no cleanup needed.
    public void InitHoming(Transform target, float turnRate, float spd, string ownerTag, GameObject ownerObj = null)
    {
        homingTarget = target;
        turnRateDegPerSec = turnRate;
        speed = spd;
        owner = ownerTag;
        ownerObject = ownerObj;
        direction = target != null ? ((Vector2)target.position - (Vector2)transform.position).normalized : Vector2.down;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (homingTarget != null && homingTarget.gameObject.activeInHierarchy)
        {
            Vector2 desiredDir = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            direction = ((Vector2)Vector3.RotateTowards(direction, desiredDir, turnRateDegPerSec * Mathf.Deg2Rad * Time.deltaTime, 0f)).normalized;
        }
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == "Player" && other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);
            MarauderBoss boss = other.GetComponent<MarauderBoss>();
            if (boss != null) boss.TakeDamage(damage, ownerObject);
            HalcyonBoss halcyonBoss = other.GetComponent<HalcyonBoss>();
            if (halcyonBoss != null) halcyonBoss.TakeDamage(damage);
            Minion minion = other.GetComponent<Minion>();
            if (minion != null) minion.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (owner == "Enemy" && other.CompareTag("Player"))
        {
            // GetComponentInParent (not GetComponent) so a child collider -
            // e.g. Tank's wide shield-arc trigger - can route the hit to
            // its ship's own PlayerHealth, not just a ship's own body collider.
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(Mathf.RoundToInt(damage));
            Destroy(gameObject);
        }
    }

}
