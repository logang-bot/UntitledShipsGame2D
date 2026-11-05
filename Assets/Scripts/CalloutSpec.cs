// One playfield callout (design: Neon Fleet HUD v2, "06 / Playfield
// callouts"): its three lines, tint, whether it uses the scrolling hazard
// bands (boss entry) or two thin lines, and its in / hold / out timings.
public struct CalloutSpec
{
    public string kicker;
    public string title;
    public string sub;
    public PaletteColor color;
    public bool hazardBands;
    public CalloutTiming timing;
}
