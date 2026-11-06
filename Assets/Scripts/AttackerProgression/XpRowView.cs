using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The XP row on the roguelike Attacker frame (design: HUD Party Frame): LV n,
// one cell per XP to the next level, the n/next readout and a chip per picked
// upgrade. The cell an XP just filled flashes white for 200 ms with a +1 tag.
public class XpRowView : MonoBehaviour
{
    private const float GainFlashSeconds = 0.2f;

    public TMP_Text level;
    public PipRowView cells;
    public GameObject gainTag;
    public TMP_Text readout;
    public GameObject chipsGroup;
    public UpgradeChipView chipTemplate;

    private readonly List<UpgradeChipView> chips = new List<UpgradeChipView>();
    private int lastProgress = -1;
    private float gainUntil;

    private bool Gaining => Time.unscaledTime < gainUntil;

    public void Show(XpRowState state, List<UpgradeChip> picked)
    {
        TrackGain(state);
        level.text = state.level.ToString();
        cells.Show(state.filled, state.cells, false);
        if (Gaining && state.filled > 0 && !state.maxed) cells.Tint(state.filled - 1, NeonPalette.Get(PaletteColor.Text));
        gainTag.SetActive(Gaining && !state.maxed);
        readout.text = state.readout;
        readout.color = NeonPalette.Get(state.maxed ? PaletteColor.Attacker : PaletteColor.Text);
        ShowChips(picked);
    }

    private void TrackGain(XpRowState state)
    {
        int progress = state.level * 10 + state.filled;
        if (lastProgress >= 0 && progress > lastProgress) gainUntil = Time.unscaledTime + GainFlashSeconds;
        lastProgress = progress;
    }

    private void ShowChips(List<UpgradeChip> picked)
    {
        chipTemplate.gameObject.SetActive(false);
        chipsGroup.SetActive(picked.Count > 0);
        while (chips.Count < picked.Count) chips.Add(Instantiate(chipTemplate, chipTemplate.transform.parent));
        for (int i = 0; i < chips.Count; i++) ShowChip(i, picked);
    }

    private void ShowChip(int index, List<UpgradeChip> picked)
    {
        bool shown = index < picked.Count;
        chips[index].gameObject.SetActive(shown);
        if (shown) chips[index].Show(picked[index]);
    }
}
