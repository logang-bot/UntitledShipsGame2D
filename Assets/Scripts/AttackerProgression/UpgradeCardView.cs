using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One level-up card (design: HUD Upgrade Card): keycap, icon box, the name
// with a NEW chip and five stack pips, what the card does and the stat it
// changes, before → after. Hover and keyboard / gamepad navigation share one
// focus; a click or Enter / A picks the card. LevelUpUI decides its state.
public class UpgradeCardView : Selectable, IPointerClickHandler, ISubmitHandler
{
    private const int MaxPips = 5;
    private const float PickedSoftAlpha = 0.7f;

    public NeonFrame frame;
    public Image accentEdge;
    public CanvasGroup group;
    public Image keyFill;
    public Image keyStroke;
    public TMP_Text keyText;
    public Image iconFill;
    public Image iconStroke;
    public Image icon;
    public TMP_Text cardName;
    public GameObject newChip;
    public Image newChipFill;
    public TMP_Text newChipText;
    public PipRowView pips;
    public TMP_Text taken;
    public TMP_Text effect;
    public TMP_Text statLabel;
    public TMP_Text before;
    public TMP_Text arrow;
    public TMP_Text after;
    public GameObject focusTag;
    public GameObject pickedTag;
    public UpgradeCardIcons icons;

    public event Action<int> Picked;
    public event Action<int> Focused;

    private int index;
    private UpgradeCard card;
    private UpgradeCardState shownState;

    private PaletteColor Accent => UpgradeCardPalette.MainFor(card);
    private bool IsPicked => shownState == UpgradeCardState.Picked;

    public void Bind(int cardIndex, UpgradeCard upgrade)
    {
        index = cardIndex;
        card = upgrade;
        keyText.text = (cardIndex + 1).ToString();
        icon.sprite = icons.For(upgrade);
    }

    public void Show(UpgradeCardCopy copy, UpgradeCardState state, int takenCount)
    {
        shownState = state;
        PaintFrame();
        PaintKey();
        PaintIcon();
        PaintTitle(copy, takenCount);
        PaintBody(copy);
        focusTag.SetActive(state == UpgradeCardState.Focus);
        pickedTag.SetActive(IsPicked);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        Focused?.Invoke(index);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (IsInteractable() && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerClick(PointerEventData eventData) => Picked?.Invoke(index);

    public void OnSubmit(BaseEventData eventData) => Picked?.Invoke(index);

    private void PaintFrame()
    {
        frame.SetLook(UpgradeCardLooks.Frame(shownState));
        accentEdge.enabled = shownState == UpgradeCardState.Focus;
        accentEdge.color = NeonPalette.Get(PaletteColor.Attacker);
        group.alpha = UpgradeCardLooks.Opacity(shownState);
    }

    private void PaintKey()
    {
        keyFill.color = NeonPalette.Get(UpgradeCardLooks.KeyFill(shownState));
        keyStroke.color = NeonPalette.Get(UpgradeCardLooks.KeyStroke(shownState));
        keyText.color = NeonPalette.Get(UpgradeCardLooks.KeyInk(shownState));
    }

    private void PaintIcon()
    {
        iconFill.color = NeonPalette.Get(IsPicked ? PaletteColor.Ink : PaletteColor.HatchDark);
        iconStroke.color = NeonPalette.Get(IconStroke());
        icon.color = NeonPalette.Get(Accent);
    }

    private PaletteColor IconStroke()
    {
        if (IsPicked) return PaletteColor.Ink;
        return shownState == UpgradeCardState.Focus ? Accent : UpgradeCardPalette.DimFor(card);
    }

    private void PaintTitle(UpgradeCardCopy copy, int takenCount)
    {
        cardName.text = copy.name;
        cardName.color = OnCard(PaletteColor.Text);
        newChip.SetActive(copy.isNew);
        newChipFill.color = NeonPalette.Get(IsPicked ? PaletteColor.Ink : PaletteColor.Text);
        newChipText.color = NeonPalette.Get(IsPicked ? PaletteColor.Attacker : PaletteColor.Ink);
        PaintPips(takenCount);
    }

    private void PaintPips(int takenCount)
    {
        pips.filledColor = new PaletteSwatch(IsPicked ? PaletteColor.Ink : PaletteColor.Text);
        pips.emptyColor = IsPicked ? new PaletteSwatch(PaletteColor.Ink, 0.45f) : new PaletteSwatch(PaletteColor.Slate);
        pips.Show(Mathf.Min(takenCount, MaxPips), MaxPips, false);
        taken.text = takenCount > 0 ? $"×{takenCount}" : "—";
        taken.color = IsPicked ? NeonPalette.Get(PaletteColor.Ink) : NeonPalette.Get(takenCount > 0 ? PaletteColor.Text : PaletteColor.Text3);
    }

    private void PaintBody(UpgradeCardCopy copy)
    {
        effect.text = copy.effect;
        effect.color = OnCard(PaletteColor.TextSoft);
        statLabel.text = copy.statLabel;
        statLabel.color = OnCard(PaletteColor.Text3, PickedSoftAlpha);
        before.text = copy.before;
        before.color = arrow.color = OnCard(PaletteColor.Text2, PickedSoftAlpha);
        after.text = copy.after;
        after.color = NeonPalette.Get(IsPicked ? PaletteColor.Ink : Accent);
    }

    private Color OnCard(PaletteColor normal, float pickedAlpha = 1f) =>
        IsPicked ? NeonPalette.Get(PaletteColor.Ink, pickedAlpha) : NeonPalette.Get(normal);
}
