using System.Globalization;
using UnityEngine;

// What an Attack or Crit cell draws: how full its square sweep is (refilling
// as the cooldown runs out), the seconds left, and whether it is flashing
// because it just fired. Pure, so it's unit-tested (CooldownCellStateTests).
public struct CooldownCellState
{
    public float sweep;
    public string timer;
    public bool flashing;

    public static CooldownCellState From(CooldownReading reading, CooldownCellSpec spec) => new CooldownCellState
    {
        sweep = Sweep(reading),
        timer = reading.remaining.ToString("0.0", CultureInfo.InvariantCulture) + spec.timerUnit,
        flashing = reading.sinceFired >= 0f && reading.sinceFired < spec.flashSeconds,
    };

    private static float Sweep(CooldownReading reading) =>
        reading.total > 0f ? Mathf.Clamp01(1f - reading.remaining / reading.total) : 1f;
}
