using System.Collections.Generic;
using UnityEngine;

// Reads WardenBoss each frame and fills the shared BossPanelView: SHIELDED
// in the header while an arm is up, the INCOMING lockdown and BIG SHOT!
// warnings, the taunt window as aggro, arms A/B/C, and the shockwave / big
// shot / lockdown cooldown tiles.
public class WardenPanelFeed : MonoBehaviour
{
    public BossPanelView panel;
    public Sprite incomingIcon;
    public Sprite bigShotIcon;
    public Sprite shockwaveIcon;

    private WardenBoss boss;
    private WardenShockwave shockwave;
    private WardenLockdownVolley lockdown;
    private WardenBigShot bigShot;
    private readonly WarningClock lockdownClock = new WarningClock();
    private readonly WarningClock bigShotClock = new WarningClock();
    private readonly List<WarningRowState> warnings = new List<WarningRowState>();
    private readonly List<CooldownTileState> tiles = new List<CooldownTileState>();
    private readonly List<ArmCardState> arms = new List<ArmCardState>();
    private CooldownTileFeed shockwaveTile;
    private CooldownTileFeed bigShotTile;
    private CooldownTileFeed lockdownTile;

    public void Bind(GameObject bossObject)
    {
        boss = bossObject.GetComponent<WardenBoss>();
        shockwave = bossObject.GetComponent<WardenShockwave>();
        lockdown = bossObject.GetComponent<WardenLockdownVolley>();
        bigShot = bossObject.GetComponent<WardenBigShot>();
        shockwaveTile = new CooldownTileFeed(shockwaveIcon, "SHOCKWAVE");
        bigShotTile = new CooldownTileFeed(bigShotIcon, "BIG SHOT");
        lockdownTile = new CooldownTileFeed(incomingIcon, "LOCKDOWN");
        panel.Bind(Level.Level3);
    }

    public void ShowDefeated()
    {
        boss = null;
        panel.ShowDefeated();
    }

    void Update()
    {
        if (boss == null) return;
        panel.header.ShowHealth(new BossVitals { hp = boss.CurrentHealth, maxHp = boss.maxHealth, phase = boss.IsPhase2 ? 2 : 1, shielded = boss.IsShielded });
        panel.ShowWarnings(Warnings());
        panel.ShowAggro(TauntedRole(), true);
        panel.ShowArms(Arms());
        panel.ShowTiles(Tiles());
    }

    private List<WarningRowState> Warnings()
    {
        warnings.Clear();
        if (lockdown != null && lockdownClock.Track(lockdown.IsTelegraphing && lockdown.IncomingEdge.HasValue, Time.time)) warnings.Add(LockdownWarning());
        if (bigShot != null && bigShotClock.Track(bigShot.IsTelegraphing, Time.time)) warnings.Add(BigShotWarning());
        return warnings;
    }

    private WarningRowState LockdownWarning()
    {
        string edge = lockdown.IncomingEdge.Value.ToString().ToUpperInvariant();
        float left = lockdown.telegraphTime - lockdownClock.Elapsed;
        return new WarningRowState
        {
            icon = incomingIcon, title = $"INCOMING: {edge}", sub = $"LOCK-ON · MOVE OFF THE {edge} EDGE",
            seconds = Mathf.Max(0f, left), drain = left / lockdown.telegraphTime,
        };
    }

    private WarningRowState BigShotWarning()
    {
        float left = bigShot.telegraphTime - bigShotClock.Elapsed;
        return new WarningRowState
        {
            icon = bigShotIcon, title = "BIG SHOT!", sub = "CHARGING", seconds = Mathf.Max(0f, left), drain = left / bigShot.telegraphTime,
        };
    }

    private PlayerRole? TauntedRole()
    {
        bool active = boss.TauntedShip != null && Time.time < boss.TauntActiveUntil;
        return active && boss.TauntedShip.TryGetComponent(out PlayerRoleComponent role) ? role.role : (PlayerRole?)null;
    }

    private List<ArmCardState> Arms()
    {
        arms.Clear();
        arms.Add(WardenArmCard.Read(boss.armA, "A"));
        arms.Add(WardenArmCard.Read(boss.armB, "B"));
        arms.Add(WardenArmCard.Read(boss.armC, "C"));
        return arms;
    }

    private List<CooldownTileState> Tiles()
    {
        tiles.Clear();
        if (shockwave != null) tiles.Add(shockwaveTile.Read(shockwave.CooldownRemaining, false));
        if (bigShot != null) tiles.Add(bigShotTile.Read(bigShot.CooldownRemaining, bigShot.IsTelegraphing));
        if (lockdown != null) tiles.Add(lockdownTile.Read(lockdown.CooldownRemaining, lockdown.IsTelegraphing));
        return tiles;
    }
}
