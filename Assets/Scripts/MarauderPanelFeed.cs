using System.Collections.Generic;
using UnityEngine;

// Reads MarauderBoss each frame and fills the shared BossPanelView: the
// guided-missile lock-on (naming its target, then FIRED while the warning
// lingers), the pattern barrage's wind-up, aggro, and the shockwave /
// missile / barrage cooldown tiles. Bound by LevelSetup's onSpawned event.
public class MarauderPanelFeed : MonoBehaviour
{
    public BossPanelView panel;
    public Sprite missileIcon;
    public Sprite fanIcon;
    public Sprite ringIcon;
    public Sprite spiralIcon;
    public Sprite shockwaveIcon;
    public Sprite barrageIcon;

    private MarauderBoss boss;
    private readonly WarningClock missileClock = new WarningClock();
    private readonly WarningClock barrageClock = new WarningClock();
    private readonly List<WarningRowState> warnings = new List<WarningRowState>();
    private readonly List<CooldownTileState> tiles = new List<CooldownTileState>();
    private CooldownTileFeed shockwaveTile;
    private CooldownTileFeed missileTile;
    private CooldownTileFeed barrageTile;

    public void Bind(GameObject bossObject)
    {
        boss = bossObject.GetComponent<MarauderBoss>();
        shockwaveTile = new CooldownTileFeed(shockwaveIcon, "SHOCKWAVE");
        missileTile = new CooldownTileFeed(missileIcon, "MISSILE");
        barrageTile = new CooldownTileFeed(barrageIcon, "BARRAGE");
        panel.Bind(Level.Level1);
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
        panel.ShowAggro(TargetRole(), TargetRole() == PlayerRole.Tank);
        panel.ShowTiles(Tiles());
    }

    private List<WarningRowState> Warnings()
    {
        warnings.Clear();
        if (missileClock.Track(boss.GuidedMissileTargetRole.HasValue, Time.time)) warnings.Add(MissileWarning());
        bool barrage = barrageClock.Track(boss.PatternBarrageActivePattern.HasValue, Time.time);
        if (barrage && barrageClock.Elapsed < boss.patternBarrageTelegraphTime) warnings.Add(BarrageWarning());
        return warnings;
    }

    private WarningRowState MissileWarning()
    {
        float lockOn = boss.guidedMissile.telegraphTime;
        float linger = boss.guidedMissile.warningLingerTime;
        bool fired = missileClock.Elapsed >= lockOn;
        float left = fired ? lockOn + linger - missileClock.Elapsed : lockOn - missileClock.Elapsed;
        return new WarningRowState
        {
            icon = missileIcon, title = fired ? "MISSILE FIRED" : "MISSILE LOCK-ON", sub = "GUIDED MISSILE",
            target = boss.GuidedMissileTargetRole, seconds = Mathf.Max(0f, left), drain = left / (fired ? linger : lockOn), fired = fired,
        };
    }

    private WarningRowState BarrageWarning()
    {
        MarauderBossAttacks.BulletPattern pattern = boss.PatternBarrageActivePattern.Value;
        float left = boss.patternBarrageTelegraphTime - barrageClock.Elapsed;
        return new WarningRowState
        {
            icon = PatternIcon(pattern), title = $"BARRAGE · {pattern.ToString().ToUpperInvariant()}", sub = "PATTERN BARRAGE · WIND-UP",
            seconds = Mathf.Max(0f, left), drain = left / boss.patternBarrageTelegraphTime,
        };
    }

    private Sprite PatternIcon(MarauderBossAttacks.BulletPattern pattern) => pattern switch
    {
        MarauderBossAttacks.BulletPattern.Fan => fanIcon,
        MarauderBossAttacks.BulletPattern.Ring => ringIcon,
        _ => spiralIcon,
    };

    private PlayerRole? TargetRole()
    {
        GameObject target = boss.CurrentTarget;
        return target != null && target.TryGetComponent(out PlayerRoleComponent role) ? role.role : (PlayerRole?)null;
    }

    private List<CooldownTileState> Tiles()
    {
        tiles.Clear();
        tiles.Add(shockwaveTile.Read(boss.ShockwaveCooldownRemaining, false));
        tiles.Add(missileTile.Read(boss.GuidedMissileCooldownRemaining, boss.GuidedMissileTargetRole.HasValue));
        tiles.Add(barrageTile.Read(boss.PatternBarrageCooldownRemaining, boss.PatternBarrageActivePattern.HasValue));
        return tiles;
    }
}
