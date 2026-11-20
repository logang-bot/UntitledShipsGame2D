using UnityEngine;

// The AI Tank's two abilities: Taunt whenever this boss's tactics ask for it
// (AIBossTactics.ShouldTaunt), and the Super Shield once its own shield is
// down to LowShieldFraction or less - the refill is when the wall pays off most.
public class AIControllerTank
{
    private const float LowShieldFraction = 0.5f;

    private readonly AIController owner;

    public AIControllerTank(AIController owner)
    {
        this.owner = owner;
    }

    public void UseAbilities()
    {
        if (owner.Tactics.ShouldTaunt(owner)) owner.Ability.TryTaunt();
        if (ShieldIsLow()) owner.Ability.TryUseAbility();
    }

    private bool ShieldIsLow()
    {
        PlayerHealth health = owner.Ability.Health;
        return health.CurrentShield <= health.maxShield * LowShieldFraction;
    }
}
