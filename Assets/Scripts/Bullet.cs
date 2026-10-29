
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
        ScheduleLifetimeCleanup();
    }

    /// <summary>
    /// Safety cleanup if the bullet never hits anything. Play mode only:
    /// a delayed Destroy() is rejected (and logged as an error) in edit
    /// mode, where EditMode tests spawn bullets.
    /// </summary>
    private void ScheduleLifetimeCleanup()
    {
        if (Application.isPlaying) Destroy(gameObject, lifeTime);
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
        ScheduleLifetimeCleanup();
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
        if (owner == "Player" && other.CompareTag("Enemy")) HitEnemy(other);
        else if (owner == "Enemy" && other.CompareTag("Player")) HitPlayer(other);
    }

    private void HitEnemy(Collider2D other)
    {
        DamageBoss(other);
        DamageMinorEnemy(other);
        Destroy(gameObject);
    }

    private void DamageBoss(Collider2D other)
    {
        if (other.TryGetComponent(out MarauderBoss boss)) boss.TakeDamage(damage, ownerObject);
        if (other.TryGetComponent(out HalcyonBoss halcyonBoss)) halcyonBoss.TakeDamage(damage);
        if (other.TryGetComponent(out WardenBoss wardenBoss)) wardenBoss.TakeDamage(damage);
        if (other.TryGetComponent(out WardenArmHealth wardenArm)) wardenArm.TakeDamage(damage);
    }

    private void DamageMinorEnemy(Collider2D other)
    {
        if (other.TryGetComponent(out Enemy enemy)) enemy.TakeDamage(damage);
        if (other.TryGetComponent(out Minion minion)) minion.TakeDamage(damage);
    }

    /// <summary>
    /// GetComponentInParent (not GetComponent) so a child collider - e.g.
    /// Tank's wide shield-arc trigger - can route the hit to its ship's own
    /// PlayerHealth, not just a ship's own body collider.
    /// </summary>
    private void HitPlayer(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.TakeDamage(Mathf.RoundToInt(damage));
        Destroy(gameObject);
    }

}
