using UnityEngine;

// Shows Marauder's weapon states on its model (Claude Design "Boss State
// Wiring"): the missile pod hatches slide open while the guided missile is
// locked, the cannon heat strips and muzzles go white-hot during a Pattern
// Barrage, and the flank bay doors open for a moment whenever new minions
// launch. Lives on the boss root beside MarauderBoss and only reads it.
public class MarauderModelWeapons : MonoBehaviour
{
    private const float HatchOpenZ = 2.2f;
    private const float BayDoorOpenX = 3.2f;
    private const float BayOpenSeconds = 1.2f;
    private static readonly string[] BayDoors = { "marauder_bay_door_L0", "marauder_bay_door_L1", "marauder_bay_door_L2", "marauder_bay_door_R0", "marauder_bay_door_R1", "marauder_bay_door_R2" };

    public BossModel model;

    private MarauderBoss boss;
    private bool? missileShown;
    private bool? barrageShown;
    private bool? baysShown;
    private int knownMinions;
    private float baysOpenUntil;

    void Awake()
    {
        boss = GetComponent<MarauderBoss>();
    }

    void LateUpdate()
    {
        bool missileLocked = boss.GuidedMissileTargetRole.HasValue;
        bool barrage = boss.PatternBarrageActivePattern.HasValue;
        if (missileShown != missileLocked) ShowMissileLock(missileLocked);
        if (barrageShown != barrage) ShowBarrage(barrage);
        WatchMinionLaunches();
        bool baysOpen = Time.time < baysOpenUntil;
        if (baysShown != baysOpen) ShowBays(baysOpen);
    }

    private void ShowMissileLock(bool locked)
    {
        missileShown = locked;
        model.Node("marauder_missile_hatch_L").localPosition = new Vector3(0f, 1.3f, locked ? HatchOpenZ : 0f);
        model.Node("marauder_missile_hatch_R").localPosition = new Vector3(0f, 1.3f, locked ? HatchOpenZ : 0f);
        if (locked) model.Glow("marauder_missile_glow", PaletteColor.BossSoft, 2.6f);
        else model.ResetGlow("marauder_missile_glow");
    }

    private void ShowBarrage(bool firing)
    {
        barrageShown = firing;
        if (firing) GlowWhiteHot();
        else { model.ResetGlow("marauder_cannon_heat"); model.ResetGlow("marauder_cannon_muzzle"); }
    }

    private void GlowWhiteHot()
    {
        model.Glow("marauder_cannon_heat", PaletteColor.MarauderPale, 2.4f);
        model.Glow("marauder_cannon_muzzle", PaletteColor.MarauderPale, 2.2f);
    }

    private void WatchMinionLaunches()
    {
        int minions = Minion.Active.Count;
        if (minions > knownMinions) baysOpenUntil = Time.time + BayOpenSeconds;
        knownMinions = minions;
    }

    /// <summary>The doors slide outward; glTFast mirrors the design's X
    /// axis, so the right-hand doors open toward local -X.</summary>
    private void ShowBays(bool open)
    {
        baysShown = open;
        foreach (string door in BayDoors)
        {
            Transform part = model.Node(door);
            float outward = door[door.Length - 2] == 'R' ? -BayDoorOpenX : BayDoorOpenX;
            part.localPosition = new Vector3(open ? outward : 0f, part.localPosition.y, part.localPosition.z);
        }
        if (open) model.Glow("marauder_bay_glow", PaletteColor.BossSoft, 2.4f);
        else model.ResetGlow("marauder_bay_glow");
    }
}
