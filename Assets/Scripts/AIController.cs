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

    [Header("Tank positioning")]
    // The 3 Teammate_* transforms (self included - filtered out at runtime).
    // Deliberately never includes Player: Tank's guard point only accounts
    // for AI-controlled allies, not the human, per the AI-teammate design
    // (see docs/systems/boss.md's "AI teammate behavior").
    public Transform[] teammates;
    public float guardBias = 0.65f; // 0 = at ally center, 1 = at the boss
    public float guardDeadzone = 0.2f;

    [Header("Medic positioning")]
    public float medicBias = -0.3f; // negative = extrapolates past ally center, away from the boss
    // Below this fraction of maxHealth OR maxShield, an ally is "hurt" and
    // pulls the Medic toward it instead of its default hang-back position.
    // Uses PlayerAbility.allies (all 4 ships, unlike teammates[] above)
    // since the Medic should react to the human Player being hurt too.
    public float medicApproachThreshold = 0.55f;

    [Header("Support positioning")]
    public float roamDeadzone = 0.3f;
    // Max seconds before picking a new roam point even if the current one
    // hasn't been reached - keeps it moving instead of stalling if it's
    // ever unable to close the last stretch of distance.
    public float roamInterval = 3f;

    [Header("Attacker positioning")]
    // Vertical balance between the allies and the boss - between Medic's
    // -0.3 (hangs back) and Tank's 0.65 (leans hard toward the boss), giving
    // Attacker a mid-distance stand-off rather than front- or back-line.
    public float attackerBias = 0.45f;
    // How far the patrol swings from the boss's live X, not a fixed center -
    // see AttackerPositionDirection() for why.
    public float attackerPatrolAmplitude = 1.5f;
    public float attackerDeadzone = 0.2f;

    private PlayerController controller;
    private PlayerAbility ability;
    private PlayerRoleComponent role;
    private Camera cam;
    private Vector2 roamTarget;
    private float nextRoamPickTime;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        ability = GetComponent<PlayerAbility>();
        role = GetComponent<PlayerRoleComponent>();
        cam = Camera.main;
    }

    void Update()
    {
        Vector2 moveDirection;
        switch (role.role)
        {
            case PlayerRole.Tank:
                moveDirection = BiasedPositionDirection(guardBias, guardDeadzone);
                break;
            case PlayerRole.Medic:
                {
                    Transform hurtAlly = FindHurtAlly();
                    moveDirection = hurtAlly != null
                        ? ApproachDirection(hurtAlly)
                        : BiasedPositionDirection(medicBias, guardDeadzone);
                }
                break;
            case PlayerRole.Support:
                moveDirection = WanderDirection();
                break;
            case PlayerRole.Attacker:
                moveDirection = AttackerPositionDirection();
                break;
            default:
                // Dead fallback for any future unhandled role.
                moveDirection = new Vector2(Mathf.Sin(Time.time * weaveFrequency), 0f) * weaveSpeed;
                break;
        }
        controller.SetMoveDirection(moveDirection);
        controller.SetFiring(true);

        switch (role.role)
        {
            case PlayerRole.Tank:
                if (boss != null && boss.CurrentTarget != gameObject) ability.TryUseAbility();
                break;
            case PlayerRole.Medic:
                // TEMPORARY heuristic: fire the aura boost the instant it's
                // off cooldown, regardless of whether anyone actually needs
                // it. Flagged for rework once the aura AI is revisited - see
                // docs/systems/boss.md's "AI teammate behavior".
                ability.TryUseAbility();
                break;
            default:
                // Support/Attacker: the ability's own cooldown gate makes a retry-every-frame safe.
                ability.TryUseAbility();
                break;
        }
    }

    // The ally (from PlayerAbility.allies - all 4 ships, unlike teammates[]
    // above) with the lowest health-or-shield fraction, but only if that
    // fraction is at/below medicApproachThreshold; null if everyone's fine.
    // "Fine" requires both health AND shield above the threshold, so a
    // single depleted pool (e.g. shield gone, health still full) still
    // counts as hurt - mirrors TickAura()'s own health-or-shield check.
    private Transform FindHurtAlly()
    {
        if (ability.allies == null) return null;

        Transform hurtAlly = null;
        float worstFraction = float.MaxValue;
        foreach (Transform ally in ability.allies)
        {
            if (ally == null || ally == transform || !ally.gameObject.activeInHierarchy) continue;
            PlayerHealth allyHealth = ally.GetComponent<PlayerHealth>();
            if (allyHealth == null) continue;

            float healthFraction = (float)allyHealth.CurrentHealth / allyHealth.maxHealth;
            float shieldFraction = allyHealth.maxShield > 0 ? (float)allyHealth.CurrentShield / allyHealth.maxShield : 1f;
            if (healthFraction > medicApproachThreshold && shieldFraction > medicApproachThreshold) continue;

            float allyWorstFraction = Mathf.Min(healthFraction, shieldFraction);
            if (allyWorstFraction < worstFraction)
            {
                worstFraction = allyWorstFraction;
                hurtAlly = ally;
            }
        }
        return hurtAlly;
    }

    // Roams the playable viewport freely rather than holding a zone -
    // steers toward a random point, picking a new one on arrival (or after
    // roamInterval, in case the current one is never quite reached) so
    // Support keeps moving continuously instead of settling like Tank/
    // Medic's biased positions do.
    private Vector2 WanderDirection()
    {
        if (Time.time >= nextRoamPickTime || Vector2.Distance(transform.position, roamTarget) < roamDeadzone)
        {
            roamTarget = RandomRoamPoint();
            nextRoamPickTime = Time.time + roamInterval;
        }
        return (roamTarget - (Vector2)transform.position).normalized;
    }

    // Same viewport bounds PlayerController.HandleMovement() clamps
    // movement to, reusing its screenPadding as the single source of truth
    // for the inset so this can't drift out of sync with the actual clamp.
    private Vector2 RandomRoamPoint()
    {
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector2 padding = controller.screenPadding;
        return new Vector2(
            Random.Range(min.x + padding.x, max.x - padding.x),
            Random.Range(min.y + padding.y, max.y - padding.y));
    }

    private Vector2 ApproachDirection(Transform target)
    {
        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        return toTarget.magnitude < guardDeadzone ? Vector2.zero : toTarget.normalized;
    }

    // Average position of the AI-controlled allies (teammates[], self
    // excluded) - shared by BiasedPositionDirection() and
    // AttackerPositionDirection() so this liveness-filtered average isn't
    // computed twice.
    private Vector2 GetAllyCenter()
    {
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
        return allyCount > 0 ? allySum / allyCount : (Vector2)transform.position;
    }

    // Steers toward a point between the AI-controlled allies and the boss,
    // biased by `bias` (0 = at ally center, 1 = at the boss, negative =
    // extrapolates past ally center away from the boss). Tank uses a
    // positive bias to physically stand in incoming bullets' paths
    // (Bullet.cs doesn't home - it just hits whichever Player-tagged
    // collider is in its straight-line path first, so standing in the way
    // is enough); Medic uses a negative bias to hang back from the boss.
    private Vector2 BiasedPositionDirection(float bias, float deadzone)
    {
        if (boss == null) return Vector2.zero;

        // LerpUnclamped (not Lerp, which clamps t to [0,1]) so a negative
        // bias (Medic) can extrapolate past allyCenter, away from the boss.
        Vector2 targetPoint = Vector2.LerpUnclamped(GetAllyCenter(), boss.transform.position, bias);

        Vector2 toTarget = targetPoint - (Vector2)transform.position;
        return toTarget.magnitude < deadzone ? Vector2.zero : toTarget.normalized;
    }

    // Hybrid patrol + boss-tracking. Ships never rotate and bullets only
    // ever fire straight up (Vector2.up, no homing - see Bullet.cs), so an
    // Attacker patrolling around a fixed, boss-independent center would
    // frequently drift out of the boss's lane and just miss. Instead the
    // patrol's center follows the boss's live X, keeping shots landing,
    // while still swinging side-to-side by attackerPatrolAmplitude for
    // DPS coverage/visual variety rather than sitting dead still under it.
    // Y uses the same ally-center/boss blend as BiasedPositionDirection()
    // (attackerBias, between Medic's and Tank's) for a mid-distance
    // stand-off - this also keeps it clear of the top edge for free, since
    // the boss sits near the top of the screen and ally center is lower.
    private Vector2 AttackerPositionDirection()
    {
        if (boss == null) return Vector2.zero;

        float targetY = Mathf.LerpUnclamped(GetAllyCenter().y, boss.transform.position.y, attackerBias);
        float targetX = boss.transform.position.x + Mathf.Sin(Time.time * weaveFrequency) * attackerPatrolAmplitude;
        Vector2 targetPoint = new Vector2(targetX, targetY);

        Vector2 toTarget = targetPoint - (Vector2)transform.position;
        return toTarget.magnitude < attackerDeadzone ? Vector2.zero : toTarget.normalized;
    }
}
