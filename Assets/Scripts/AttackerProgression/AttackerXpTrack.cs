// The level curve: how much XP each level-up costs, and where the Attacker is
// on it. Plain class so the curve can be tested without a scene. Test values -
// see docs/systems/roguelike-progression.md.
public class AttackerXpTrack
{
    private static readonly int[] XpPerLevelUp = { 1, 2, 3, 4, 5 };

    public int Level { get; private set; }
    public int XpIntoLevel { get; private set; }

    public int MaxLevel => XpPerLevelUp.Length;
    public bool IsMaxed => Level >= MaxLevel;
    public int XpToNextLevel => IsMaxed ? 0 : XpPerLevelUp[Level];

    /// <summary>Adds one XP. Returns true if that earned a level-up.</summary>
    public bool AddXp()
    {
        if (IsMaxed) return false;
        XpIntoLevel++;
        if (XpIntoLevel < XpToNextLevel) return false;
        XpIntoLevel = 0;
        Level++;
        return true;
    }
}
