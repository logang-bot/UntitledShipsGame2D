using UnityEngine;

// Reads one WardenArm into its boss-panel card: Off until its GameObject is
// in the fight (Arm C before phase 2), Down while knocked out, Lock while it
// has a target, Idle otherwise.
public static class WardenArmCard
{
    public static ArmCardState Read(WardenArm arm, string letter)
    {
        ArmCardState card = new ArmCardState { letter = letter, status = ArmStatus.Off };
        if (arm == null || !arm.gameObject.activeInHierarchy) return card;
        if (arm.TryGetComponent(out WardenArmHealth health) && health.IsDown) return Down(card, health);
        PlayerRole? target = arm.isActiveAndEnabled ? arm.CurrentTargetRole : null;
        card.status = target.HasValue ? ArmStatus.Lock : ArmStatus.Idle;
        card.target = target;
        return card;
    }

    private static ArmCardState Down(ArmCardState card, WardenArmHealth health)
    {
        card.status = ArmStatus.Down;
        card.downSeconds = health.DownRemaining;
        card.downLeft = health.downDuration > 0f ? Mathf.Clamp01(health.DownRemaining / health.downDuration) : 0f;
        return card;
    }
}
