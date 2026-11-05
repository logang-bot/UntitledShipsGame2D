using UnityEngine;

// Shows Halcyon's Opening (HalcyonSurge) and its core on the model (Claude
// Design "Boss State Wiring"). Telegraph: chevrons slide in toward the
// body, the coils overcharge white and the core brightens. Exposed: the
// core shell splits open, the core goes white-hot under a weak-point
// reticle and everything else dims, so it reads as "shoot now". Lives on
// the boss root beside HalcyonSurge and only reads it.
public class HalcyonModelOpening : MonoBehaviour
{
    private enum Look { Roaming, Telegraph, Exposed }

    private const int ChevronSides = 4;
    private const float ShellOpenX = 1.8f;

    public BossModel model;

    private HalcyonBoss boss;
    private HalcyonSurge surge;
    private Look? shownLook;
    private string[] chevrons;

    void Awake()
    {
        boss = GetComponent<HalcyonBoss>();
        surge = GetComponent<HalcyonSurge>();
        chevrons = new string[ChevronSides * 2];
        for (int i = 0; i < chevrons.Length; i++) chevrons[i] = "halcyon_opening_chevron_" + i / 2 + "_" + i % 2;
    }

    void LateUpdate()
    {
        Look look = surge.IsActive ? Look.Exposed : surge.IsTelegraphing ? Look.Telegraph : Look.Roaming;
        if (shownLook != look) ShowLook(look);
        GlowCore(look);
        if (look == Look.Telegraph) SlideChevrons(Time.time % 1f);
    }

    private void ShowLook(Look look)
    {
        shownLook = look;
        bool exposed = look == Look.Exposed;
        model.Show("halcyon_opening_chevrons", look == Look.Telegraph);
        model.Show("halcyon_weakpoint_reticle", exposed);
        model.Node("halcyon_core_shell_L").localPosition = new Vector3(exposed ? ShellOpenX : 0f, 0f, 0f);
        model.Node("halcyon_core_shell_R").localPosition = new Vector3(exposed ? -ShellOpenX : 0f, 0f, 0f);
        model.Node("halcyon_storm_core_orb").localScale = Vector3.one * (exposed ? 1.25f : 1f);
        if (look == Look.Roaming) ResetCoils();
        else GlowCoils(exposed);
    }

    private void GlowCoils(bool exposed)
    {
        model.Glow("halcyon_coil_glow", exposed ? PaletteColor.Halcyon : PaletteColor.HalcyonPale, exposed ? 0.06f : 2.2f);
        model.Glow("halcyon_coil_tip", exposed ? PaletteColor.HalcyonTrim : PaletteColor.White, exposed ? 0.1f : 3f);
        if (exposed) model.Glow("halcyon_volt", PaletteColor.Halcyon, 0.35f);
    }

    private void ResetCoils()
    {
        model.ResetGlow("halcyon_coil_glow");
        model.ResetGlow("halcyon_coil_tip");
        model.ResetGlow("halcyon_volt");
    }

    private void GlowCore(Look look)
    {
        float roaming = (boss.IsPhase2 ? 1.5f : 1f) + Mathf.Sin(Time.time * 6f) * 0.3f;
        if (look == Look.Exposed) model.Glow("halcyon_core_glow", PaletteColor.HalcyonPale, 3f + Mathf.Sin(Time.time * 10f) * 0.4f);
        else if (look == Look.Telegraph) model.Glow("halcyon_core_glow", PaletteColor.Halcyon, 1.8f + Time.time % 1f);
        else model.Glow("halcyon_core_glow", PaletteColor.Halcyon, roaming);
    }

    /// <summary>Two chevrons per side, the inner one 3.2 units closer,
    /// sliding 5 units inward over each second of the telegraph; x is
    /// negated because glTFast mirrors the design's X axis.</summary>
    private void SlideChevrons(float cycle)
    {
        for (int i = 0; i < chevrons.Length; i++)
        {
            float angle = i / 2 * Mathf.PI / 2f;
            float radius = 26f - i % 2 * 3.2f - cycle * 5f;
            model.Node(chevrons[i]).localPosition = new Vector3(-Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
        }
    }
}
