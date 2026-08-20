using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int maxShield = 3; // placeholder base, scaled by role like maxHealth; no passive regen (see Heal/RestoreShield)
    public UnityEvent OnDeath;
    public UnityEvent OnDamaged;

    private int currentHealth;
    private int currentShield;

    public int CurrentHealth => currentHealth;
    public int CurrentShield => currentShield;

    void Awake()
    {
        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent != null)
        {
            maxHealth = Mathf.RoundToInt(maxHealth * roleComponent.Stats.healthMultiplier);
            maxShield = Mathf.RoundToInt(maxShield * roleComponent.Stats.shieldMultiplier);
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

    public void TakeDamage(int amount)
    {
        // Shield absorbs first; only the overflow touches health.
        int remaining = amount;
        if (currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining > 0)
            currentHealth -= remaining;

        if (currentHealth <= 0)
            Die();
        else
            OnDamaged?.Invoke();
    }

    void Die()
    {
        gameObject.SetActive(false);
        OnDeath?.Invoke();
    }
}
