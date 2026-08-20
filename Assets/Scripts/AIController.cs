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

    [Header("Tank positioning")]
    // The 3 Teammate_* transforms (self included - filtered out at runtime).
    // Deliberately never includes Player: Tank's guard point only accounts
    // for AI-controlled allies, not the human, per the AI-teammate design
    // (see docs/systems/boss.md's "AI teammate behavior").
    public Transform[] teammates;
    public float guardBias = 0.65f; // 0 = at ally center, 1 = at the boss
    public float guardDeadzone = 0.2f;

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
        Vector2 moveDirection = role.role == PlayerRole.Tank
            ? GuardPointDirection()
            : new Vector2(Mathf.Sin(Time.time * weaveFrequency), 0f) * weaveSpeed;
        controller.SetMoveDirection(moveDirection);
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

    // Steers toward a point between the AI-controlled allies and the boss,
    // so Tank physically stands in incoming bullets' paths (Bullet.cs
    // doesn't home - it just hits whichever Player-tagged collider is in
    // its straight-line path first, so standing in the way is enough).
    private Vector2 GuardPointDirection()
    {
        if (boss == null) return Vector2.zero;

        Vector2 allySum = Vector2.zero;
        int allyCount = 0;
        if (teammates != null)
        {
            foreach (Transform t in teammates)
            {
                if (t == null || t == transform || !t.gameObject.activeInHierarchy) continue;
                allySum += (Vector2)t.position;
                allyCount++;
            }
        }

        Vector2 allyCenter = allyCount > 0 ? allySum / allyCount : (Vector2)transform.position;
        Vector2 guardPoint = Vector2.Lerp(allyCenter, boss.transform.position, guardBias);

        Vector2 toGuardPoint = guardPoint - (Vector2)transform.position;
        return toGuardPoint.magnitude < guardDeadzone ? Vector2.zero : toGuardPoint.normalized;
    }
}
