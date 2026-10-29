using UnityEngine;

// Sibling component on the Boss GameObject: a boss-side mirror of the
// Attacker's Big Shot - one wide, heavy bullet fired straight down on a
// long, jittered cooldown, after a sprite-flash telegraph. Update-driven
// (no coroutine), same idle -> telegraph -> fire shape as HalcyonSurge.
public class WardenBigShot : MonoBehaviour
{
    [Header("Timing")]
    public float cooldown = 8f;
    public float cooldownPhase2 = 6f;
    public float cooldownJitter = 1.5f;
    public float telegraphTime = 0.6f;

    [Header("Shot")]
    public GameObject bulletPrefab;
    public float widthMultiplier = 3f;
    public float damageMultiplier = 2f;
    public float bulletSpeed = 5f;
    public float muzzleOffset = 0.9f;

    [Header("Telegraph")]
    public Color telegraphColor = new Color(1f, 0.35f, 0.15f);
    public float telegraphPulseSpeed = 14f;

    public bool IsTelegraphing { get; private set; }
    public float CooldownRemaining => IsTelegraphing ? 0f : Mathf.Max(0f, nextShotTime - Time.time);

    private WardenBoss boss;
    private SpriteRenderer sr;
    private Color baseColor;
    private float nextShotTime;
    private float telegraphEndTime;

    void Awake()
    {
        boss = GetComponent<WardenBoss>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    void OnEnable()
    {
        ScheduleNextShot();
    }

    void OnDisable()
    {
        EndTelegraph();
    }

    void Update()
    {
        if (LevelSequencer.ShipsFrozen) return;
        if (IsTelegraphing) { UpdateTelegraph(); return; }
        if (Time.time >= nextShotTime) BeginTelegraph();
    }

    public float NextCooldown()
    {
        float baseCooldown = boss != null && boss.IsPhase2 ? cooldownPhase2 : cooldown;
        return baseCooldown + Random.Range(-cooldownJitter, cooldownJitter);
    }

    private void ScheduleNextShot()
    {
        nextShotTime = Time.time + NextCooldown();
    }

    private void BeginTelegraph()
    {
        IsTelegraphing = true;
        telegraphEndTime = Time.time + telegraphTime;
    }

    private void UpdateTelegraph()
    {
        if (Time.time < telegraphEndTime) { PulseTint(); return; }
        EndTelegraph();
        SpawnWideBullet();
        ScheduleNextShot();
    }

    private void PulseTint()
    {
        if (sr == null) return;
        float pulse = (Mathf.Sin(Time.time * telegraphPulseSpeed) + 1f) * 0.5f;
        sr.color = Color.Lerp(baseColor, telegraphColor, pulse);
    }

    private void EndTelegraph()
    {
        IsTelegraphing = false;
        if (sr != null) sr.color = baseColor;
    }

    public Bullet SpawnWideBullet()
    {
        Vector3 muzzle = transform.position + Vector3.down * muzzleOffset;
        GameObject bulletObj = Instantiate(bulletPrefab, muzzle, Quaternion.identity);
        Vector3 scale = bulletObj.transform.localScale;
        bulletObj.transform.localScale = new Vector3(scale.x * widthMultiplier, scale.y, scale.z);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = boss.bulletDamage * damageMultiplier;
        b.Init(Vector2.down, bulletSpeed, "Enemy");
        return b;
    }
}
