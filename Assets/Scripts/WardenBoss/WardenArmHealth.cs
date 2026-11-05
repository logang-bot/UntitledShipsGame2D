using UnityEngine;

// Lives beside WardenArm on each arm. While any active arm is up, WardenBoss
// ignores damage (see WardenBoss.IsShielded). Enough hits knock the arm down:
// it stops firing, shows its "down" look (WardenArmModel), and loses its
// hitbox until downDuration passes.
public class WardenArmHealth : MonoBehaviour
{
    public float maxHealth = 20f;
    public float downDuration = 15f;

    public float CurrentHealth { get; private set; }
    public bool IsDown { get; private set; }
    public float DownRemaining => IsDown ? Mathf.Max(0f, downUntil - Time.time) : 0f;

    private WardenArm arm;
    private WardenBoss boss;
    private Collider2D col;
    private BossHitFlash hitFlash;
    private float downUntil;

    void Awake()
    {
        arm = GetComponent<WardenArm>();
        boss = GetComponentInParent<WardenBoss>();
        col = GetComponent<Collider2D>();
        hitFlash = GetComponent<BossHitFlash>();
        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (IsDown && Time.time >= downUntil) Recover();
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (IsDown) return;
        CurrentHealth -= amount;
        if (boss != null) boss.DamageLedger.Record(source, amount);
        if (hitFlash != null) hitFlash.Flash();
        if (CurrentHealth <= 0f) KnockDown();
    }

    private void KnockDown()
    {
        IsDown = true;
        downUntil = Time.time + downDuration;
        SetArmUp(false);
    }

    private void Recover()
    {
        IsDown = false;
        CurrentHealth = maxHealth;
        SetArmUp(true);
    }

    private void SetArmUp(bool up)
    {
        if (arm != null) arm.enabled = up;
        if (col != null) col.enabled = up;
    }
}
