// The live numbers one level-up card describes: which card, how many times
// it was already taken, the Attacker's current damage and the crit interval.
public struct UpgradeCardContext
{
    public UpgradeCard card;
    public int taken;
    public float damage;
    public float critInterval;
}
