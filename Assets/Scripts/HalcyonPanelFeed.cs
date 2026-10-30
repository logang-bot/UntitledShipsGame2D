using System.Collections.Generic;
using UnityEngine;

// Reads HalcyonBoss each frame and fills the shared BossPanelView: the SURGE
// warning while it telegraphs, the white OPENING! row while the boss is
// exposed, and the surge / static field cooldown tiles. Halcyon has no aggro.
public class HalcyonPanelFeed : MonoBehaviour
{
    public BossPanelView panel;
    public Sprite surgeIcon;
    public Sprite staticFieldIcon;

    private HalcyonBoss boss;
    private HalcyonSurge surge;
    private HalcyonStaticField staticField;
    private readonly WarningClock surgeClock = new WarningClock();
    private readonly List<WarningRowState> warnings = new List<WarningRowState>();
    private readonly List<CooldownTileState> tiles = new List<CooldownTileState>();
    private CooldownTileFeed surgeTile;
    private CooldownTileFeed staticFieldTile;

    public void Bind(GameObject bossObject)
    {
        boss = bossObject.GetComponent<HalcyonBoss>();
        surge = bossObject.GetComponent<HalcyonSurge>();
        staticField = bossObject.GetComponent<HalcyonStaticField>();
        surgeTile = new CooldownTileFeed(surgeIcon, "SURGE");
        staticFieldTile = new CooldownTileFeed(staticFieldIcon, "STATIC FIELD");
        panel.Bind(Level.Level2);
    }

    public void ShowDefeated()
    {
        boss = null;
        panel.ShowDefeated();
    }

    void Update()
    {
        if (boss == null) return;
        panel.header.ShowHealth(new BossVitals { hp = boss.CurrentHealth, maxHp = boss.maxHealth, phase = boss.IsPhase2 ? 2 : 1 });
        panel.ShowWarnings(Warnings());
        panel.ShowOpening(surge != null && surge.IsActive);
        panel.ShowTiles(Tiles());
    }

    private List<WarningRowState> Warnings()
    {
        warnings.Clear();
        if (surge != null && surgeClock.Track(surge.IsTelegraphing, Time.time)) warnings.Add(SurgeWarning());
        return warnings;
    }

    private WarningRowState SurgeWarning()
    {
        float left = surge.telegraphTime - surgeClock.Elapsed;
        return new WarningRowState
        {
            icon = surgeIcon, title = "SURGE", sub = "GET CLEAR OF THE BOSS",
            seconds = Mathf.Max(0f, left), drain = left / surge.telegraphTime,
        };
    }

    private List<CooldownTileState> Tiles()
    {
        tiles.Clear();
        if (surge != null) tiles.Add(surgeTile.Read(surge.CooldownRemaining, surge.IsTelegraphing || surge.IsActive));
        if (staticField != null) tiles.Add(staticFieldTile.Read(staticField.CooldownRemaining, false));
        return tiles;
    }
}
