using UnityEngine;

// Lives beside WardenArm on each arm. While any active arm is up, WardenBoss
// ignores damage (see WardenBoss.IsShielded). Enough hits knock the arm down:
// it stops firing, grays out, and loses its hitbox until downDuration passes.
public class WardenArmHealth : MonoBehaviour
{
    public float maxHealth = 20f;
    public float downDuration = 15f;
    public PaletteSwatch downColor = new PaletteSwatch(PaletteColor.WardenArmDown);

    public float CurrentHealth { get; private set; }
    public bool IsDown { get; private set; }
    public float DownRemaining => IsDown ? Mathf.Max(0f, downUntil - Time.time) : 0f;

    private WardenArm arm;
    private WardenBoss boss;
    private SpriteRenderer sr;
    private Collider2D col;
    private Color upColor;
    private float downUntil;

    void Awake()
    {
        arm = GetComponent<WardenArm>();
        boss = GetComponentInParent<WardenBoss>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr != null) upColor = sr.color;
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
        if (CurrentHealth <= 0f) KnockDown();
    }

    private void KnockDown()
    {
        IsDown = true;
        downUntil = Time.time + downDuration;
        SetArmUp(false);
        if (sr != null) sr.color = downColor;
    }

    private void Recover()
    {
        IsDown = false;
        CurrentHealth = maxHealth;
        SetArmUp(true);
        if (sr != null) sr.color = upColor;
    }

    private void SetArmUp(bool up)
    {
        if (arm != null) arm.enabled = up;
        if (col != null) col.enabled = up;
    }
}
