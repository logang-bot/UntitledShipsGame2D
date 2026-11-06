// What the Attacker's picked cards add up to. Every card can be picked
// repeatedly, so each stat is derived from its pick count rather than stored.
public class AttackerUpgrades
{
    // Whole numbers only: Enemy/Minion.TakeDamage round to int, so a fractional
    // bonus could round away to nothing against mobs.
    public const int DamagePerPick = 1;
    private const float CritBonusPerPick = 1f; // +100% of attack damage
    private const int FirstMultishotShots = 3;
    private const int ShotsPerExtraPick = 2;

    private int damagePicks;
    private int critPicks;
    private int multishotPicks;

    public int DamageBonus => damagePicks * DamagePerPick;
    public bool CritUnlocked => critPicks > 0;
    public float CritBonusFraction => CritBonusFor(critPicks);
    public int ShotCount => ShotsFor(multishotPicks);

    public static float CritBonusFor(int picks) => picks * CritBonusPerPick;

    public static int ShotsFor(int picks) => picks == 0 ? 1 : FirstMultishotShots + (picks - 1) * ShotsPerExtraPick;

    public int PicksOf(UpgradeCard card)
    {
        switch (card)
        {
            case UpgradeCard.AttackDamage: return damagePicks;
            case UpgradeCard.CritStrike: return critPicks;
            default: return multishotPicks;
        }
    }

    public void Apply(UpgradeCard card)
    {
        switch (card)
        {
            case UpgradeCard.AttackDamage: damagePicks++; break;
            case UpgradeCard.CritStrike: critPicks++; break;
            case UpgradeCard.Multishot: multishotPicks++; break;
        }
    }

    public float CritDamage(float attackDamage) => attackDamage * (1f + CritBonusFraction);
}
