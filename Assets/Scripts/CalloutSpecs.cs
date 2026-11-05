// The three playfield callouts from the design, with its timings: the
// pre-boss wave, the boss entry warning, and the phase change.
public static class CalloutSpecs
{
    public static CalloutSpec Wave => new CalloutSpec
    {
        kicker = "STAGE 01", title = "WAVE", sub = "CLEAR THE ESCORT", color = PaletteColor.Tank,
        timing = new CalloutTiming(0.16f, 1.2f, 0.24f),
    };

    public static CalloutSpec BossEntry(Level level) => new CalloutSpec
    {
        kicker = "WARNING", title = LevelCardText.BossName(level), sub = BossPalette.Kind(level),
        color = BossPalette.For(level).main, hazardBands = true, timing = new CalloutTiming(0.2f, 1.8f, 0.3f),
    };

    public static CalloutSpec Phase2(Level level) => new CalloutSpec
    {
        kicker = LevelCardText.BossName(level), title = "PHASE 2", sub = level == Level.Level3 ? "SHIELDED" : "",
        color = BossPalette.For(level).main, timing = new CalloutTiming(0.12f, 0.9f, 0.2f),
    };
}
