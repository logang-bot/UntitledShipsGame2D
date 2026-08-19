using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public UnityEvent OnDeath;
    public UnityEvent OnDamaged;

    private int currentHealth;

    public int CurrentHealth => currentHealth;

    void Awake()
    {
        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent != null)
            maxHealth = Mathf.RoundToInt(maxHealth * roleComponent.Stats.healthMultiplier);

        currentHealth = maxHealth;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
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
