using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The top of the right rail's boss panel (design: HUD Boss Panel): mark,
// fight kind, name, PHASE n and SHIELDED tags, HP number / % / bar (hatched
// while shielded, with the one-line reason), and the DEFEATED banner.
public class BossHeaderView : MonoBehaviour
{
    private const float DefeatedHeaderAlpha = 0.45f;

    public NeonFrame panelFrame;
    public Image mark;
    public Image markStroke;
    public Sprite[] bossMarks;
    public TMP_Text kind;
    public TMP_Text bossName;
    public TMP_Text phase;
    public Image phaseStroke;
    public GameObject shieldedTag;
    public Image shieldedFill;
    public TMP_Text hpValue;
    public TMP_Text hpPercent;
    public Image hpFill;
    public GameObject hpHatch;
    public TMP_Text shieldNote;
    public GameObject defeatedBanner;
    public CanvasGroup headerGroup;

    public void Bind(Level level)
    {
        BossTokens boss = BossPalette.For(level);
        panelFrame.SetLook(BossPalette.PanelLook(boss));
        mark.sprite = bossMarks[(int)level];
        TintBossParts(NeonPalette.Get(boss.main));
        markStroke.color = NeonPalette.Get(boss.dim);
        kind.text = BossPalette.Kind(level);
        bossName.text = LevelCardText.BossName(level);
        defeatedBanner.SetActive(false);
        headerGroup.alpha = 1f;
    }

    private BossVitals lastVitals;

    public void ShowHealth(BossVitals vitals)
    {
        lastVitals = vitals;
        float fraction = vitals.maxHp > 0 ? Mathf.Clamp01((float)vitals.hp / vitals.maxHp) : 0f;
        phase.text = $"PHASE {vitals.phase}";
        hpValue.text = $"{Mathf.Max(0, vitals.hp)} / {vitals.maxHp}";
        hpPercent.text = $"{Mathf.RoundToInt(fraction * 100f)}%";
        hpFill.fillAmount = fraction;
        shieldedTag.SetActive(vitals.shielded);
        hpHatch.SetActive(vitals.shielded);
        shieldNote.gameObject.SetActive(vitals.shielded);
    }

    public void ShowDefeated()
    {
        ShowHealth(new BossVitals { hp = 0, maxHp = lastVitals.maxHp, phase = lastVitals.phase });
        panelFrame.SetLook(BossPalette.DefeatedLook);
        TintBossParts(NeonPalette.Get(PaletteColor.Text2));
        phaseStroke.color = NeonPalette.Get(PaletteColor.Muted);
        defeatedBanner.SetActive(true);
        headerGroup.alpha = DefeatedHeaderAlpha;
    }

    private void TintBossParts(Color main)
    {
        mark.color = main;
        kind.color = main;
        phaseStroke.color = main;
        shieldedFill.color = main;
        hpFill.color = main;
        shieldNote.color = main;
    }
}
