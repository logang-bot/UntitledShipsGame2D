using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int maxShield = 3; // placeholder base, scaled by role like maxHealth; no passive regen (see Heal/RestoreShield)
    public UnityEvent OnDeath = new UnityEvent();
    public UnityEvent OnDamaged = new UnityEvent();

    private int currentHealth;
    private int currentShield;
    private float invulnerableUntil = float.NegativeInfinity;

    public int CurrentHealth => currentHealth;
    public int CurrentShield => currentShield;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    // Start, not Awake: see PlayerRoleComponent.cs's Start() comment - a
    // dynamically-Instantiate()'d ship (co-op spawner) only has its role
    // assigned after Instantiate() returns, and Awake() already runs
    // synchronously inside Instantiate() itself.
    void Start()
    {
        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent != null)
        {
            maxHealth = roleComponent.Stats.maxHealth;
            maxShield = roleComponent.Stats.maxShield;
        }

        currentHealth = maxHealth;
        currentShield = maxShield;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // No passive regen - only ever called by Medic's proximity aura
    // (see PlayerAbility.cs's TickAura / docs/systems/boss.md's "AI teammate behavior").
    public void RestoreShield(int amount)
    {
        currentShield = Mathf.Min(currentShield + amount, maxShield);
    }

    /// <summary>
    /// Ignores every hit for the given seconds (the Attacker's Nova Beam cast).
    /// Extends, never shortens, a window that is already running.
    /// </summary>
    public void GrantInvulnerability(float seconds)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    public void TakeDamage(int amount)
    {
        if (IsInvulnerable) return;
        int overflow = AbsorbWithShield(amount);
        if (overflow > 0) currentHealth -= overflow;
        if (currentHealth <= 0) Die();
        else OnDamaged?.Invoke();
    }

    /// <summary>The shield soaks the hit first; returns what spills over to health.</summary>
    private int AbsorbWithShield(int amount)
    {
        int absorbed = Mathf.Min(currentShield, amount);
        currentShield -= absorbed;
        return amount - absorbed;
    }

    void Die()
    {
        gameObject.SetActive(false);
        OnDeath?.Invoke();
    }
}
