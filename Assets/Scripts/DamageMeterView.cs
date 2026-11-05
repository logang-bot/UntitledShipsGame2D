using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The left rail's Recount-style damage meter (design: Neon Fleet HUD v2):
// one row per ship, sorted by damage dealt to the boss, from the boss's
// BossDamageLedger. Boss damage only - minions and waves don't count. When
// the boss dies the ledger's clock stops, and the meter freezes as
// DAMAGE · BOSS DOWN with a lock icon instead of blanking. Bound by
// LevelSetup's onSpawned event, like the boss panel.
public class DamageMeterView : MonoBehaviour
{
    public PartyFrameManager party;
    public TMP_Text title;
    public Image lockIcon;
    public DamageMeterRowView[] rows;
    public float refreshInterval = 0.2f;

    private BossDamageLedger ledger;
    private float nextRefreshTime;
    private bool frozen;

    public List<DamageMeterRow> LastRows { get; private set; } = new List<DamageMeterRow>();

    public void Bind(GameObject bossObject)
    {
        IBoss boss = bossObject.GetComponent<IBoss>();
        ledger = boss?.DamageLedger;
        lockIcon.enabled = false;
        Refresh();
    }

    void Update()
    {
        if (ledger == null || frozen || Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshInterval;
        Refresh();
        if (ledger.CombatEnded) Freeze();
    }

    public List<DamageMeterRow> FinalRows() => ledger != null ? DamageMeterRows.Read(ledger, party.players) : LastRows;

    private void Refresh()
    {
        if (ledger == null) return;
        LastRows = DamageMeterRows.Read(ledger, party.players);
        for (int i = 0; i < rows.Length; i++)
        {
            if (i < LastRows.Count) rows[i].Show(LastRows[i]);
            else rows[i].Hide();
        }
    }

    private void Freeze()
    {
        frozen = true;
        title.text = "DAMAGE · BOSS DOWN";
        title.color = NeonPalette.Get(PaletteColor.Text);
        lockIcon.enabled = true;
    }
}
