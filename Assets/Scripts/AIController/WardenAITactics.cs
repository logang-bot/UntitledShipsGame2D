using UnityEngine;

// While Warden's arms shield it, every AI ship lines up under the same arm
// to break the shield; once every arm is down they return to the boss.
// Taunt has no aggro to check against here, so an AI Tank taunts on cooldown.
public class WardenAITactics : AIBossTactics
{
    private readonly WardenBoss warden;

    public WardenAITactics(WardenBoss warden) : base(warden)
    {
        this.warden = warden;
    }

    public override bool HasPriorityFocus()
    {
        return warden.IsShielded;
    }

    public override Vector2 FocusPoint()
    {
        WardenArm arm = FocusArm();
        return arm != null ? (Vector2)arm.transform.position : base.FocusPoint();
    }

    public override bool ShouldTaunt(AIController ai)
    {
        return true;
    }

    /// <summary>
    /// The lowest-HP arm still shielding, ties broken in armA/armB/armC order,
    /// so every AI ship picks the same arm and focus-fires it.
    /// </summary>
    private WardenArm FocusArm()
    {
        WardenArm best = PickLower(null, warden.armA);
        best = PickLower(best, warden.armB);
        return PickLower(best, warden.armC);
    }

    private static WardenArm PickLower(WardenArm current, WardenArm candidate)
    {
        if (!WardenBoss.ArmShields(candidate)) return current;
        if (current == null) return candidate;
        return ArmHealth(candidate) < ArmHealth(current) ? candidate : current;
    }

    private static float ArmHealth(WardenArm arm)
    {
        return arm.GetComponent<WardenArmHealth>().CurrentHealth;
    }
}
