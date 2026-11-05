// What the XP row on the Attacker's frame draws (design: party frame,
// roguelike test): the level, one cell per XP the next level costs, how many
// are filled, and the n/next readout. At max level it is one solid cell
// reading MAX. Pure, so it's unit-tested (XpRowStateTests).
public struct XpRowState
{
    public int level;
    public int cells;
    public int filled;
    public string readout;
    public bool maxed;

    public static XpRowState From(AttackerXpTrack xp) => xp.IsMaxed ? Maxed(xp) : Leveling(xp);

    private static XpRowState Maxed(AttackerXpTrack xp) => new XpRowState
    {
        level = xp.Level, cells = 1, filled = 1, readout = "MAX", maxed = true,
    };

    private static XpRowState Leveling(AttackerXpTrack xp) => new XpRowState
    {
        level = xp.Level, cells = xp.XpToNextLevel, filled = xp.XpIntoLevel,
        readout = $"{xp.XpIntoLevel}/{xp.XpToNextLevel} XP",
    };
}
