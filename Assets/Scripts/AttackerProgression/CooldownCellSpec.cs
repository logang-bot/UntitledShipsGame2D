// What differs between the Attack and Crit cells (design: party frame,
// roguelike test): how long the fire flash lasts and the timer's unit.
public struct CooldownCellSpec
{
    public float flashSeconds;
    public string timerUnit;

    public static CooldownCellSpec Attack => new CooldownCellSpec { flashSeconds = 0.08f, timerUnit = "" };
    public static CooldownCellSpec Crit => new CooldownCellSpec { flashSeconds = 0.12f, timerUnit = "s" };
}
