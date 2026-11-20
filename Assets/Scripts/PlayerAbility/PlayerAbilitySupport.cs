// The Support's superpower on Q: the Echo Gate, through PlayerAbility's
// shared cooldown gate.
public class PlayerAbilitySupport
{
    public PlayerAbilitySupport(PlayerAbility ability)
    {
        Gate = new SupportEchoGate(ability);
    }

    public SupportEchoGate Gate { get; }

    public void Trigger() => Gate.Begin();
}
