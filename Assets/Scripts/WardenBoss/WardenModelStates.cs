using UnityEngine;

// Shows Warden's body states on its model (Claude Design "Boss State
// Wiring"): the phase 2 look (arm C deployed, mount seal blown, cracks,
// brighter trim), the Lockdown warning (chevrons pointing at the incoming
// edge, strobing armour band and bastion lamps) and the core, whose
// shutters open wider in phase 2 and fully for the Big Shot charge. Lives
// on the boss root beside WardenBoss and only reads it; the arms have
// their own WardenArmModel.
public class WardenModelStates : MonoBehaviour
{
    private static readonly string[] LockdownIndicators = { "warden_lockdown_ind_top", "warden_lockdown_ind_left", "warden_lockdown_ind_right" };
    private static readonly string[] Shutters = { "warden_core_shutter_0", "warden_core_shutter_1", "warden_core_shutter_2", "warden_core_shutter_3" };

    public BossModel model;

    private WardenBoss boss;
    private WardenLockdownVolley volley;
    private WardenBigShot bigShot;
    private bool? shownPhase2;
    private bool strobing;

    void Awake()
    {
        boss = GetComponent<WardenBoss>();
        volley = GetComponent<WardenLockdownVolley>();
        bigShot = GetComponent<WardenBigShot>();
    }

    void LateUpdate()
    {
        if (shownPhase2 != boss.IsPhase2) ShowPhase(boss.IsPhase2);
        ShowLockdown(volley.IsTelegraphing ? volley.IncomingEdge : null);
        ShowCore(bigShot.IsTelegraphing);
    }

    private void ShowPhase(bool phase2)
    {
        shownPhase2 = phase2;
        model.Show("warden_phase2_damage", phase2);
        model.Show("warden_arm_c", phase2);
        model.Show("warden_mount_c_seal", !phase2);
        if (phase2) GlowPhase2();
        else { model.ResetGlow("warden_mount_c_glow"); model.ResetGlow("warden_trim_edge"); }
    }

    private void GlowPhase2()
    {
        model.Glow("warden_mount_c_glow", PaletteColor.WardenIce, 2f);
        model.Glow("warden_trim_edge", PaletteColor.WardenIce, 1f);
    }

    private void ShowLockdown(WardenLockdownVolley.Edge? edge)
    {
        for (int i = 0; i < LockdownIndicators.Length; i++)
            model.Show(LockdownIndicators[i], edge.HasValue && (int)edge.Value == i);
        if (edge.HasValue) Strobe();
        else if (strobing) StopStrobe();
    }

    private void Strobe()
    {
        strobing = true;
        float strobe = Mathf.Sin(Time.time * 25f) > 0f ? 1f : 0.25f;
        model.Glow("warden_armor_band_glow", PaletteColor.Warden, 2.4f * strobe);
        model.Glow("warden_bastion_lamp", PaletteColor.WardenIce, 3f * strobe);
    }

    private void StopStrobe()
    {
        strobing = false;
        model.ResetGlow("warden_armor_band_glow");
        model.ResetGlow("warden_bastion_lamp");
    }

    private void ShowCore(bool charging)
    {
        float idleGlow = (boss.IsPhase2 ? 1.9f : 1.1f) + Mathf.Sin(Time.time * 3f) * 0.4f;
        if (charging) model.Glow("warden_core_glow", PaletteColor.WardenPale, 3.5f);
        else model.Glow("warden_core_glow", PaletteColor.Warden, idleGlow);
        model.Node("warden_core_orb").localScale = Vector3.one * (charging ? 1.35f : 1f);
        OpenShutters(charging ? 4.6f : boss.IsPhase2 ? 3.9f : 3.1f);
    }

    /// <summary>The four shutters sit at 90 degree steps around the core;
    /// x is negated because glTFast mirrors the design's X axis.</summary>
    private void OpenShutters(float radius)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI / 2f;
            model.Node(Shutters[i]).localPosition = new Vector3(-Mathf.Sin(angle) * radius, 0.6f, Mathf.Cos(angle) * radius);
        }
    }
}
