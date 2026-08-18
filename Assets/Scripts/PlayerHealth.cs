using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;

    private int currentHealth;

    public int CurrentHealth => currentHealth;

    void Awake()
    {
        PlayerRoleComponent roleComponent = GetComponent<PlayerRoleComponent>();
        if (roleComponent != null)
            maxHealth = Mathf.RoundToInt(maxHealth * roleComponent.Stats.healthMultiplier);

        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        gameObject.SetActive(false);
    }
}
