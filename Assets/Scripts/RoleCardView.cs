using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One of the single-player Role Select's four cards: the role's live ship
// (a RawImage on its ShipPortraitStudio render texture), stats on the shared
// scale, and its ability lines. RoleSelectUI sets Normal / You / Cpu; while
// nothing is picked, keyboard/gamepad focus or mouse hover shows Focus.
public class RoleCardView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float PulseSeconds = 0.2f;
    private const float PulseScale = 1.03f;
    private const float CpuShipAlpha = 0.5f;

    public PlayerRole role;
    public NeonFrame frame;
    public Image focusOutline;
    public Image halo;
    public RawImage ship;
    public TagChip roleTag;
    public TagChip badge;
    public TMP_Text roleName;
    public TMP_Text shipName;
    public TMP_Text pilot;
    public StatBarView[] stats;
    public AbilityLineView[] abilities;

    private RoleCardState baseState = RoleCardState.Normal;
    private bool selected;
    private bool hovered;

    void Start() => Refresh();

    public void OnSelect(BaseEventData eventData) => SetFocus(ref selected, true);
    public void OnDeselect(BaseEventData eventData) => SetFocus(ref selected, false);
    public void OnPointerEnter(PointerEventData eventData) => SetFocus(ref hovered, true);
    public void OnPointerExit(PointerEventData eventData) => SetFocus(ref hovered, false);

    public void SetState(RoleCardState state)
    {
        baseState = state;
        Refresh();
    }

    public void Pulse()
    {
        StopAllCoroutines();
        StartCoroutine(PulseRoutine());
    }

    private void SetFocus(ref bool flag, bool value)
    {
        flag = value;
        Refresh();
    }

    private RoleCardState Shown => baseState == RoleCardState.Normal && (selected || hovered) ? RoleCardState.Focus : baseState;

    private void Refresh()
    {
        RoleCardState shown = Shown;
        bool cpu = shown == RoleCardState.Cpu;
        frame.SetLook(RoleFrameLooks.Card(role, shown));
        focusOutline.enabled = selected;
        ShowHeader(shown);
        ShowShip(cpu);
        ShowDetails(cpu);
    }

    private void ShowHeader(RoleCardState shown)
    {
        roleTag.Show(TagChipLooks.RoleTag(role));
        roleName.text = RoleCardText.RoleName(role);
        badge.gameObject.SetActive(shown != RoleCardState.Normal);
        if (shown == RoleCardState.Focus) badge.Show(TagChipLooks.Focus);
        else if (shown == RoleCardState.You) badge.Show(TagChipLooks.You(role, 1));
        else if (shown == RoleCardState.Cpu) badge.Show(TagChipLooks.Cpu);
    }

    private void ShowShip(bool cpu)
    {
        ship.color = NeonPalette.Get(PaletteColor.White, cpu ? CpuShipAlpha : 1f);
        halo.enabled = !cpu;
        halo.color = RolePalette.For(role).fill;
    }

    private void ShowDetails(bool cpu)
    {
        Color name = NeonPalette.Get(cpu ? PaletteColor.TextSoft : PaletteColor.Text);
        roleName.color = name;
        shipName.text = RoleCardText.ShipName(role);
        shipName.color = name;
        pilot.text = PilotLabel();
        for (int i = 0; i < stats.Length; i++) stats[i].Show(role, (RoleStat)i, cpu);
        RoleAbilityLine[] lines = RoleCardText.Abilities(role);
        for (int i = 0; i < abilities.Length; i++) ShowAbility(i, lines, cpu);
    }

    private void ShowAbility(int index, RoleAbilityLine[] lines, bool cpu)
    {
        abilities[index].gameObject.SetActive(index < lines.Length);
        if (index < lines.Length) abilities[index].Show(lines[index], role, cpu);
    }

    private string PilotLabel()
    {
        if (baseState == RoleCardState.You) return "PILOT · P1";
        return baseState == RoleCardState.Cpu ? "CPU TEAMMATE" : "AVAILABLE";
    }

    private IEnumerator PulseRoutine()
    {
        for (float t = 0f; t < PulseSeconds; t += Time.unscaledDeltaTime)
        {
            float bump = Mathf.Sin(Mathf.PI * t / PulseSeconds);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, PulseScale, bump);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
