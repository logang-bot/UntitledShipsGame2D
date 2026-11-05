using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A party frame's ability cell (design: HUD Party Frame): the square sweep
// around the icon (solid = ready, filling = active time left, grey track
// filling back up = cooldown), name, status, seconds and the 3 px active
// bar. On a CPU's frame it is also the click-to-trigger button: CLICK
// keycap, white TRIGGER on hover, WAIT + lock while cooling. The click
// listener is bound at runtime - each frame is generic until it knows its
// ship, the same exception the old PartyFrameUI made.
public class AbilityCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public Image boxFill;
    public Image boxStroke;
    public Image ringTrack;
    public Image ringFill;
    public Image ringInner;
    public Image icon;
    public Sprite[] roleIcons;
    public TMP_Text abilityName;
    public TMP_Text status;
    public TMP_Text timer;
    public Image activeBar;

    [Header("CPU keycap")]
    public GameObject keyRoot;
    public Image keyFill;
    public Image keyStroke;
    public Image keyIcon;
    public TMP_Text keyText;
    public Sprite clickSprite;
    public Sprite lockSprite;

    private RoleColors colors;
    private bool cpu;
    private bool hovered;
    private AbilityCellState state;

    private bool Clickable => cpu && state.phase == AbilityPhase.Ready;
    private bool Dimmed => state.phase == AbilityPhase.Cooldown || state.phase == AbilityPhase.Offline;

    public void Bind(PlayerRole role, bool isCpu, PlayerAbility ability)
    {
        colors = RolePalette.For(role);
        cpu = isCpu;
        icon.sprite = roleIcons[(int)role];
        abilityName.text = PlayerAbilityLabels.NameFor(role).ToUpperInvariant();
        button.onClick.RemoveAllListeners();
        if (cpu) button.onClick.AddListener(ability.TryUseAbility);
    }

    public void Show(AbilityCellState newState)
    {
        state = newState;
        button.interactable = Clickable;
        PaintBox();
        PaintRing();
        PaintTexts();
        PaintKey();
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void PaintBox()
    {
        bool hover = hovered && Clickable;
        boxStroke.color = hover ? NeonPalette.Get(PaletteColor.Text) : Dimmed ? NeonPalette.Get(PaletteColor.Line2) : colors.main;
        boxFill.color = hover || state.phase == AbilityPhase.Active ? colors.fill : BoxRestFill();
        activeBar.enabled = state.phase == AbilityPhase.Active;
        activeBar.fillAmount = state.sweep;
        activeBar.color = colors.main;
    }

    private Color BoxRestFill() =>
        Dimmed ? NeonPalette.Get(PaletteColor.HatchLight) : NeonPalette.Get(PaletteColor.HatchDark, 0.6f);

    private void PaintRing()
    {
        ringFill.fillAmount = state.sweep;
        ringFill.color = colors.main;
        ringTrack.color = Dimmed ? NeonPalette.Get(PaletteColor.Line2) : state.phase == AbilityPhase.Active ? colors.dim : colors.main;
        ringInner.color = NeonPalette.Get(Dimmed ? PaletteColor.HatchLight : PaletteColor.HatchDark);
        icon.color = Dimmed ? NeonPalette.Get(PaletteColor.Text3) : colors.main;
    }

    private void PaintTexts()
    {
        abilityName.color = NeonPalette.Get(Dimmed ? PaletteColor.Text2 : PaletteColor.Text);
        status.text = StatusText(state.phase);
        status.color = Dimmed ? NeonPalette.Get(PaletteColor.Text3) : colors.main;
        bool timed = state.phase == AbilityPhase.Active || state.phase == AbilityPhase.Cooldown;
        timer.gameObject.SetActive(timed);
        timer.text = state.phase == AbilityPhase.Active ? $"{state.seconds:0.0}s" : $"{Mathf.CeilToInt(state.seconds)}s";
        timer.color = state.phase == AbilityPhase.Active ? colors.main : NeonPalette.Get(PaletteColor.Text);
    }

    private static string StatusText(AbilityPhase phase) => phase switch
    {
        AbilityPhase.Active => "ACTIVE",
        AbilityPhase.Cooldown => "COOLDOWN",
        AbilityPhase.Offline => "OFFLINE",
        _ => "READY",
    };

    private void PaintKey()
    {
        keyRoot.SetActive(cpu && (state.phase == AbilityPhase.Ready || state.phase == AbilityPhase.Cooldown));
        if (!keyRoot.activeSelf) return;
        if (hovered && Clickable) PaintKeyAs("TRIGGER", PaletteColor.Ink, PaletteColor.Text, true);
        else if (Clickable) PaintKeyAs("CLICK", PaletteColor.Text, PaletteColor.Text2, false);
        else PaintKeyAs("WAIT", PaletteColor.Muted, PaletteColor.Line2, false);
        keyIcon.sprite = Clickable ? clickSprite : lockSprite;
    }

    private void PaintKeyAs(string text, PaletteColor ink, PaletteColor edge, bool filled)
    {
        keyText.text = text;
        keyText.color = NeonPalette.Get(ink);
        keyIcon.color = NeonPalette.Get(ink);
        keyStroke.color = NeonPalette.Get(edge);
        keyFill.enabled = filled;
        keyFill.color = NeonPalette.Get(edge);
    }
}
