using TMPro;
using UnityEngine;

// A party frame's stat line: SPD, FIRE and real DPS against the boss.
// Values boosted by Support's Speed Boost read in light purple with a "+"
// (the design's ▲ isn't in the HUD fonts).
public class FrameStatsView : MonoBehaviour
{
    public TMP_Text speedValue;
    public TMP_Text fireValue;
    public TMP_Text dpsValue;

    public void Show(PlayerController ship, float dps, bool cpu)
    {
        bool boosted = ship.speedBuffMultiplier > 1f || ship.fireRateBuffMultiplier > 1f;
        Paint(speedValue, $"{ship.moveSpeed * ship.speedBuffMultiplier:0.0}", boosted, cpu);
        Paint(fireValue, $"{ship.shotsPerSecond * ship.fireRateBuffMultiplier:0.0}/s", boosted, cpu);
        Paint(dpsValue, $"{dps:0.0}", false, cpu);
    }

    public void ShowDown()
    {
        foreach (TMP_Text value in new[] { speedValue, fireValue, dpsValue })
            value.color = NeonPalette.Get(PaletteColor.Text3);
    }

    private static void Paint(TMP_Text value, string text, bool boosted, bool cpu)
    {
        value.text = boosted ? text + "+" : text;
        value.color = NeonPalette.Get(boosted ? PaletteColor.SupportLight : cpu ? PaletteColor.TextSoft : PaletteColor.Text);
    }
}
