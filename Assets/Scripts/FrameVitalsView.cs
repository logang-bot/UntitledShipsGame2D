using TMPro;
using UnityEngine;

// A party frame's HP and shield line: pips, the n/max numbers, and the
// BROKEN tag when the shield is empty. HP turns amber at a third or less. A
// long shield (the Tank's 20 points) moves to its own line, per the design.
public class FrameVitalsView : MonoBehaviour
{
    private const float LowFraction = 0.34f;

    public PipRowView hpPips;
    public PipRowView shieldPips;
    public TMP_Text hpValue;
    public TMP_Text shieldValue;
    public GameObject brokenTag;
    public RectTransform shieldGroup;
    public RectTransform wrapRow;
    public int wrapAboveSegments = 12;

    public static bool IsLow(PlayerHealth health) =>
        health.CurrentHealth > 0 && (float)health.CurrentHealth / health.maxHealth <= LowFraction;

    public void Show(PlayerHealth health)
    {
        int hp = Mathf.Max(0, health.CurrentHealth);
        int shield = Mathf.Max(0, health.CurrentShield);
        bool low = IsLow(health);
        hpPips.Show(hp, health.maxHealth, low);
        shieldPips.Show(shield, health.maxShield, false);
        hpValue.text = $"{hp}/{health.maxHealth}";
        hpValue.color = NeonPalette.Get(low ? PaletteColor.Notice : PaletteColor.Text);
        shieldValue.text = $"{shield}/{health.maxShield}";
        brokenTag.SetActive(shield == 0 && hp > 0);
        WrapLongShield(health.maxShield > wrapAboveSegments);
    }

    private void WrapLongShield(bool wrap)
    {
        if (wrapRow.gameObject.activeSelf == wrap) return;
        wrapRow.gameObject.SetActive(wrap);
        shieldGroup.SetParent(wrap ? wrapRow : transform, false);
    }
}
