using UnityEngine;

// Shows Marauder's phase and core on its model (Claude Design "Boss State
// Wiring"): phase 2 blows off the right shoulder plate, shows the breach,
// cracks and fire, and splays the aim turret's barrels for the 3-bullet
// spread; the charge core pulses and its ring turns, both faster in phase 2,
// and the core goes white and swells while the shockwave winds up. The
// shockwave radius itself stays the ShockwaveRing's job. Lives on the boss
// root beside MarauderBoss and only reads it; MarauderModelWeapons shows
// the missile, barrage and minion-bay states.
public class MarauderModelStates : MonoBehaviour
{
    private const float RingDegreesPerSecond = 68.75f;
    private const float BarrelSplayDegrees = 9.17f;

    public BossModel model;

    private MarauderBoss boss;
    private bool? shownPhase2;
    private float ringAngle;

    void Awake()
    {
        boss = GetComponent<MarauderBoss>();
    }

    void LateUpdate()
    {
        if (shownPhase2 != boss.IsPhase2) ShowPhase(boss.IsPhase2);
        ShowCore(boss.IsShockwaveTelegraphing, boss.IsPhase2 ? 2.4f : 1f);
    }

    private void ShowPhase(bool phase2)
    {
        shownPhase2 = phase2;
        model.Show("marauder_phase2_damage", phase2);
        model.Show("marauder_armor_plate_FR", !phase2);
        model.Glow("marauder_white", PaletteColor.MarauderPale, phase2 ? 2.4f : 1.6f);
        float splay = phase2 ? BarrelSplayDegrees : 0f;
        model.Node("marauder_aim_barrel_0").localRotation = Quaternion.Euler(0f, splay, 0f);
        model.Node("marauder_aim_barrel_2").localRotation = Quaternion.Euler(0f, -splay, 0f);
    }

    private void ShowCore(bool charging, float speed)
    {
        float idleGlow = (boss.IsPhase2 ? 1.8f : 1.1f) + Mathf.Sin(Time.time * 4f * speed) * 0.5f;
        if (charging) model.Glow("marauder_core_glow", PaletteColor.MarauderPale, 3f + Mathf.Sin(Time.time * 20f) * 0.6f);
        else model.Glow("marauder_core_glow", PaletteColor.Marauder, idleGlow);
        model.Node("marauder_charge_core_orb").localScale = Vector3.one * (charging ? 1.3f : 1f);
        ringAngle += RingDegreesPerSecond * speed * (charging ? 4f : 1f) * Time.deltaTime;
        model.Node("marauder_charge_ring").localRotation = Quaternion.Euler(0f, ringAngle, 0f);
    }
}
