using TMPro;
using UnityEngine;

// The top line of a party frame: who-tag (◤ P1 / P2 / ◇ CPU 1), ship name,
// role word, the LOW HP tag and the Speed Boost buff chip.
public class FrameIdentityView : MonoBehaviour
{
    public TagChip who;
    public TMP_Text shipName;
    public TMP_Text roleName;
    public GameObject lowHpTag;
    public GameObject speedBuffChip;

    public void Show(PlayerRole role, FrameSeat seat)
    {
        who.Show(PartyFrameLooks.WhoTag(role, seat));
        shipName.text = RoleCardText.ShipName(role);
        shipName.color = NeonPalette.Get(seat.isHuman ? PaletteColor.Text : PaletteColor.TextSoft);
        roleName.text = RoleCardText.RoleName(role);
        roleName.color = NeonPalette.Get(RolePalette.TokenFor(role));
    }

    public void SetConditions(bool lowHp, bool speedBoosted)
    {
        lowHpTag.SetActive(lowHp);
        speedBuffChip.SetActive(speedBoosted);
    }

    public void ShowDown()
    {
        shipName.color = NeonPalette.Get(PaletteColor.TextSoft);
        roleName.color = NeonPalette.Get(PaletteColor.Text3);
        SetConditions(false, false);
    }
}
