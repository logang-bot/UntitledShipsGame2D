// What the Attacker's picked cards add up to. Every card can be picked
// repeatedly, so each stat is derived from its pick count rather than stored.
public class AttackerUpgrades
{
    // Whole numbers only: Enemy/Minion.TakeDamage round to int, so a fractional
    // bonus could round away to nothing against mobs.
    private const int DamagePerPick = 1;
    private const float CritBonusPerPick = 1f; // +100% of attack damage
    private const int FirstMultishotShots = 3;
    private const int ShotsPerExtraPick = 2;

    private int damagePicks;
    private int critPicks;
    private int multishotPicks;

    public int DamageBonus => damagePicks * DamagePerPick;
    public bool CritUnlocked => critPicks > 0;
    public float CritBonusFraction => critPicks * CritBonusPerPick;
    public int ShotCount => multishotPicks == 0 ? 1 : FirstMultishotShots + (multishotPicks - 1) * ShotsPerExtraPick;

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
