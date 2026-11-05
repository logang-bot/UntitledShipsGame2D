using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One role tile in a multiplayer Role Select row: the role's live ship,
// names and ability, in one of RoleTileState's looks. The tag names the
// player it refers to - the row's own player for Hover, the holder for Taken.
public class RoleTileView : MonoBehaviour
{
    private const float TakenShipAlpha = 0.22f;
    private const float MutedAlpha = 0.4f;

    public PlayerRole role;
    public NeonFrame frame;
    public Image focusOutline;
    public RawImage ship;
    public TMP_Text roleName;
    public TMP_Text shipName;
    public TMP_Text abilityName;
    public TagChip badge;
    public CanvasGroup group;

    void Awake()
    {
        roleName.text = RoleCardText.RoleName(role);
        shipName.text = RoleCardText.ShipName(role);
        abilityName.text = RoleCardText.Abilities(role)[0].name;
    }

    public void Show(RoleTileState state, int player)
    {
        bool taken = state == RoleTileState.Taken;
        frame.SetLook(RoleFrameLooks.Tile(role, state));
        focusOutline.enabled = state == RoleTileState.Hover;
        group.alpha = state == RoleTileState.Muted ? MutedAlpha : 1f;
        ship.color = NeonPalette.Get(PaletteColor.White, taken ? TakenShipAlpha : 1f);
        roleName.color = NeonPalette.Get(taken ? PaletteColor.Text3 : PaletteColor.Text);
        abilityName.color = NeonPalette.Get(taken ? PaletteColor.Muted : RolePalette.TokenFor(role));
        ShowTag(state, player);
    }

    private void ShowTag(RoleTileState state, int player)
    {
        bool hasTag = state == RoleTileState.Hover || state == RoleTileState.Locked || state == RoleTileState.Taken;
        badge.gameObject.SetActive(hasTag);
        if (state == RoleTileState.Hover) badge.Show(TagChipLooks.Hover(player));
        else if (state == RoleTileState.Locked) badge.Show(TagChipLooks.Locked(role));
        else if (state == RoleTileState.Taken) badge.Show(TagChipLooks.Taken(player));
    }
}
