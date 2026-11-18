// The Tank's two abilities: Super Shield on E (Trigger, through
// PlayerAbility's shared cooldown gate) and Taunt on key 1 (its own
// cooldown), plus the always-on shield arc the Super Shield grows.
public class PlayerAbilityTank
{
    private readonly PlayerAbility ability;

    public PlayerAbilityTank(PlayerAbility ability)
    {
        this.ability = ability;
        Taunt = new TankTaunt(ability);
    }

    public TankTaunt Taunt { get; }
    public TankSuperShield SuperShield { get; private set; }

    /// <summary>Seconds into the Super Shield (the cut-in card's clock), or -1 when it's down.</summary>
    public float SuperShieldElapsed => SuperShield != null ? SuperShield.Elapsed : -1f;
    public float SuperShieldRemaining => SuperShield != null ? SuperShield.Remaining : 0f;

    public void Trigger() => SuperShield?.Begin();

    /// <summary>Called from PlayerAbility.Start, once the ship knows it's the Tank.</summary>
    public void Build()
    {
        SuperShield = new TankSuperShield(ability, TankShieldArc.Create(ability));
        Taunt.CreateMagnet();
    }
}
