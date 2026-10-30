using System.Collections.Generic;
using UnityEngine;

// Victory's per-ship summary (design: 08 / Overlays): the frozen damage
// meter reused large - ship, pilot, total, DPS and share, sorted by damage.
// Filled each time the overlay opens.
public class VictorySummaryView : MonoBehaviour
{
    public DamageMeterView meter;
    public PartyFrameManager party;
    public DamageMeterRowView[] rows;

    void OnEnable()
    {
        List<DamageMeterRow> final = meter.FinalRows();
        for (int i = 0; i < rows.Length; i++)
        {
            if (i < final.Count) rows[i].Show(final[i], party.SeatOf(final[i].ship).label);
            else rows[i].Hide();
        }
    }
}
