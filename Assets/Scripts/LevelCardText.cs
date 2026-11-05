// The words on Level Select's boss cards, from the Pre-Fight Menus design.
// Threat levels are the design's placeholders until the fights are tuned.
public static class LevelCardText
{
    public static string Number(Level level) => $"0{(int)level + 1}";

    public static string BossName(Level level)
    {
        switch (level)
        {
            case Level.Level1: return "MARAUDER";
            case Level.Level2: return "HALCYON";
            default: return "WARDEN";
        }
    }

    public static string FightType(Level level)
    {
        switch (level)
        {
            case Level.Level1: return "RAID";
            case Level.Level2: return "POSITIONING";
            default: return "COVERAGE";
        }
    }

    public static string Description(Level level)
    {
        switch (level)
        {
            case Level.Level1: return "The classic raid fight. Bullet-heavy, two phases. It chases whoever it’s angriest at; the Tank’s Taunt pulls it.";
            case Level.Level2: return "A positioning fight with no bullets. Damage comes from touching the boss and from its Static Field pulse.";
            default: return "A coverage fight. Two, later three, turret arms lock onto different ships at once.";
        }
    }

    public static int Threat(Level level) => (int)level + 2;
}
