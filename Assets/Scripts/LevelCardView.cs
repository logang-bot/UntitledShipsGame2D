using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One of Level Select's boss cards: level number and fight type, the boss
// art slot (placeholder glyph until the boss renders exist), name,
// description and threat pips. Hover or focus lights the frame in the boss
// magenta and switches the footer to "▸ LAUNCH"; keyboard/gamepad focus also
// adds the white outline.
public class LevelCardView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Level level;
    public NeonFrame frame;
    public Image focusOutline;
    public TMP_Text levelLabel;
    public TMP_Text fightType;
    public TMP_Text bossName;
    public TMP_Text description;
    public Image[] threatPips;
    public TMP_Text launchHint;

    private bool selected;
    private bool hovered;

    void Start()
    {
        levelLabel.text = $"LEVEL {LevelCardText.Number(level)}";
        fightType.text = LevelCardText.FightType(level);
        bossName.text = LevelCardText.BossName(level);
        description.text = LevelCardText.Description(level);
        for (int i = 0; i < threatPips.Length; i++) ShowPip(i);
        Refresh();
    }

    public void OnSelect(BaseEventData eventData) => SetFocus(ref selected, true);
    public void OnDeselect(BaseEventData eventData) => SetFocus(ref selected, false);
    public void OnPointerEnter(PointerEventData eventData) => SetFocus(ref hovered, true);
    public void OnPointerExit(PointerEventData eventData) => SetFocus(ref hovered, false);

    private void SetFocus(ref bool flag, bool value)
    {
        flag = value;
        Refresh();
    }

    private void Refresh()
    {
        bool lit = selected || hovered;
        frame.SetLook(lit ? NeonFrameLooks.Boss : NeonFrameLooks.BossIdle);
        focusOutline.enabled = selected;
        launchHint.text = lit ? "▸ LAUNCH" : "A · LAUNCH";
        launchHint.color = NeonPalette.Get(lit ? PaletteColor.Text : PaletteColor.Text3);
    }

    private void ShowPip(int index)
    {
        bool on = index < LevelCardText.Threat(level);
        threatPips[index].color = NeonPalette.Get(on ? PaletteColor.Boss : PaletteColor.Line2);
    }
}
