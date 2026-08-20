using UnityEngine;

// Drives a CPU-controlled teammate: continuous auto-fire, a simple strafe
// weave, and per-role heuristics for ability use. Attached alongside the
// same component set as a human Player (PlayerController/PlayerHealth/
// PlayerRoleComponent/PlayerAbility) with PlayerInput removed.
public class AIController : MonoBehaviour
{
    public Boss boss;
    public float weaveFrequency = 0.8f;
    public float weaveSpeed = 1f;
    public float medicHealThreshold = 0.6f; // fraction of maxHealth

    private PlayerController controller;
    private PlayerAbility ability;
    private PlayerHealth health;
    private PlayerRoleComponent role;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        ability = GetComponent<PlayerAbility>();
        health = GetComponent<PlayerHealth>();
        role = GetComponent<PlayerRoleComponent>();
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * weaveFrequency);
        controller.SetMoveDirection(new Vector2(x, 0f) * weaveSpeed);
        controller.SetFiring(true);

        switch (role.role)
        {
            case PlayerRole.Tank:
                if (boss != null && boss.CurrentTarget != gameObject) ability.TryUseAbility();
                break;
            case PlayerRole.Medic:
                if (health.CurrentHealth < health.maxHealth * medicHealThreshold) ability.TryUseAbility();
                break;
            default:
                // Support/Attacker: the ability's own cooldown gate makes a retry-every-frame safe.
                ability.TryUseAbility();
                break;
        }
    }
}
