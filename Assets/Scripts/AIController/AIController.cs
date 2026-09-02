using UnityEngine;

/// <summary>
/// Drives a CPU-controlled teammate: continuous auto-fire, a simple strafe
/// weave, and per-role heuristics for ability use. Attached alongside the
/// same component set as a human Player (PlayerController/PlayerHealth/
/// PlayerRoleComponent/PlayerAbility) with PlayerInput removed.
/// </summary>
public class AIController : MonoBehaviour
{
    public Level1Boss boss;
    public float weaveFrequency = 0.8f;
    public float weaveSpeed = 1f;

    [Header("Tank positioning")]
    /// <summary>
    /// The 3 Teammate_* transforms (self included - filtered out at
    /// runtime). Deliberately never includes Player: Tank's guard point
    /// only accounts for AI-controlled allies, not the human, per the
    /// AI-teammate design (see docs/systems/boss.md's "AI teammate behavior").
    /// </summary>
    public Transform[] teammates;
    /// <summary>0 = at ally center, 1 = at the boss.</summary>
    public float guardBias = 0.65f;
    public float guardDeadzone = 0.2f;

    [Header("Medic positioning")]
    /// <summary>Negative = extrapolates past ally center, away from the boss.</summary>
    public float medicBias = -0.3f;
    /// <summary>
    /// Below this fraction of maxHealth OR maxShield, an ally is "hurt" and
    /// pulls the Medic toward it instead of its default hang-back position.
    /// Uses PlayerAbility.allies (all 4 ships, unlike teammates[] above)
    /// since the Medic should react to the human Player being hurt too.
    /// </summary>
    public float medicApproachThreshold = 0.55f;
    /// <summary>
    /// How far behind the hurt ally (on the side away from the boss) the
    /// Medic aims to stand, instead of steering straight at the ally's exact
    /// position - keeps it out of the ally's path while still landing well
    /// within PlayerAbility.auraRadius once it closes in. See
    /// AIControllerMedic.ApproachDirection().
    /// </summary>
    public float medicStandoffDistance = 0.5f;

    [Header("Support positioning")]
    public float roamDeadzone = 0.3f;
    /// <summary>
    /// Max seconds before picking a new roam point even if the current one
    /// hasn't been reached - keeps it moving instead of stalling if it's
    /// ever unable to close the last stretch of distance.
    /// </summary>
    public float roamInterval = 3f;

    [Header("Attacker positioning")]
    /// <summary>
    /// Vertical balance between the allies and the boss - between Medic's
    /// -0.3 (hangs back) and Tank's 0.65 (leans hard toward the boss),
    /// giving Attacker a mid-distance stand-off rather than front- or
    /// back-line.
    /// </summary>
    public float attackerBias = 0.45f;
    /// <summary>
    /// How far the patrol swings from the boss's live X, not a fixed
    /// center - see AIControllerAttacker.PositionDirection() for why.
    /// </summary>
    public float attackerPatrolAmplitude = 1.5f;
    public float attackerDeadzone = 0.2f;

    [Header("Boss avoidance")]
    /// <summary>
    /// Applied to every role's computed target point (including Support's
    /// wander) so default positioning never sits inside the boss's body
    /// contact / shockwave range. Set just outside Level1Boss.shockwaveRadius
    /// so normal positioning doesn't self-trigger the shockwave. Tank's
    /// guardBias still leans hard toward the boss for physical blocking -
    /// blocking only requires standing between the boss and the ally it's
    /// shooting at, not touching the boss's body, so this floor doesn't
    /// defeat that design.
    /// </summary>
    public float minDistanceFromBoss = 1.9f;

    [Header("Ship separation")]
    /// <summary>
    /// Additive steering push away from nearby allies so two ships whose
    /// targets sit on opposite sides of each other don't just lock up
    /// face-to-face - PlayerController's own collision resolution only ever
    /// shoves a ship back to the overlap boundary, it never redirects
    /// sideways, so nothing else breaks that kind of standoff on its own.
    /// </summary>
    public float separationRadius = 1.1f;
    public float separationWeight = 1f;

    [Header("Bullet dodging")]
    /// <summary>
    /// Applied to every role uniformly, blended additively with (not
    /// overriding) whatever positioning direction the role switch already
    /// computed - see ComputeMoveDirection().
    /// </summary>
    public float dodgeDetectionRadius = 3f;
    public float dodgeLookaheadTime = 0.6f;
    public float dodgeMissDistance = 0.6f;
    public float dodgeWeight = 1f;

    private PlayerController controller;
    private PlayerAbility ability;
    private PlayerRoleComponent role;
    private Camera cam;
    private AIControllerPositioning positioning;
    private AIControllerMedic medic;
    private AIControllerSupport support;
    private AIControllerAttacker attacker;
    private AIControllerAvoidance avoidance;

    public PlayerAbility Ability => ability;
    public PlayerController Controller => controller;
    public Camera Cam => cam;
    public AIControllerPositioning Positioning => positioning;

    void Awake()
    {
        CacheComponents();
        CreateHelpers();
    }

    private void CacheComponents()
    {
        controller = GetComponent<PlayerController>();
        ability = GetComponent<PlayerAbility>();
        role = GetComponent<PlayerRoleComponent>();
        cam = Camera.main;
    }

    private void CreateHelpers()
    {
        positioning = new AIControllerPositioning(this);
        medic = new AIControllerMedic(this);
        support = new AIControllerSupport(this);
        attacker = new AIControllerAttacker(this);
        avoidance = new AIControllerAvoidance(this);
    }

    void Update()
    {
        Transform medicTarget = role.role == PlayerRole.Medic ? medic.FindHurtAlly() : null;
        Vector2 moveDirection = ComputeMoveDirection(medicTarget);
        controller.SetMoveDirection(moveDirection);
        controller.SetFiring(true);
        UpdateAbilityUsage();
    }

    private Vector2 ComputeMoveDirection(Transform medicTarget)
    {
        Vector2 roleDirection = RolePositionDirection(medicTarget);
        Vector2 separation = avoidance.ComputeSeparationVector(medicTarget);
        Vector2 dodge = avoidance.ComputeDodgeVector();
        Vector2 combined = roleDirection + separation * separationWeight + dodge * dodgeWeight;
        if (combined.sqrMagnitude > 0.0001f) return combined.normalized;
        return dodge != Vector2.zero ? dodge : roleDirection;
    }

    /// <summary>
    /// Resolved once per frame, from a medicTarget already resolved once in
    /// Update() (not recomputed here) so ComputeMoveDirection() can also
    /// exclude it from separation - otherwise the Medic would be pushed
    /// away from the very ally it's trying to close in on heal.
    /// </summary>
    private Vector2 RolePositionDirection(Transform medicTarget)
    {
        switch (role.role)
        {
            case PlayerRole.Tank:
                return positioning.BiasedPositionDirection(guardBias, guardDeadzone);
            case PlayerRole.Medic:
                return medicTarget != null
                    ? medic.ApproachDirection(medicTarget)
                    : positioning.BiasedPositionDirection(medicBias, guardDeadzone);
            case PlayerRole.Support:
                return support.WanderDirection();
            case PlayerRole.Attacker:
                return attacker.PositionDirection();
            default:
                return new Vector2(Mathf.Sin(Time.time * weaveFrequency), 0f) * weaveSpeed;
        }
    }

    /// <summary>
    /// Tank gates its ability behind a distance check (below); every other
    /// role retries every frame, which the ability's own cooldown gate
    /// makes safe. TEMPORARY for Medic specifically: fires the aura boost
    /// the instant it's off cooldown regardless of whether anyone needs it -
    /// flagged for rework once the aura AI is revisited (see
    /// docs/systems/boss.md's "AI teammate behavior").
    /// </summary>
    private void UpdateAbilityUsage()
    {
        if (role.role == PlayerRole.Tank)
        {
            if (boss != null && boss.CurrentTarget != gameObject) ability.TryUseAbility();
            return;
        }
        // Always requests whatever slot continues the rotation correctly, so
        // Attacker bots play a perfect combo - TryComboAttack's own per-slot
        // cooldown (comboAttackCooldown) is what paces it, not this call.
        if (role.role == PlayerRole.Attacker) ability.TryComboAttack(ability.AttackerComboExpectedSlot);
        ability.TryUseAbility();
    }
}
