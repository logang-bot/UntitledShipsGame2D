using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One role in the squad strip (Role Select's final line-up, Level Select's
// chips): role icon and name, and who flies it - a player number in the
// role color, or ◇ CPU in the dashed CPU frame.
public class SquadChipView : MonoBehaviour
{
    private const float CpuIconAlpha = 0.5f;

    public PlayerRole role;
    public NeonFrame frame;
    public Image icon;
    public TMP_Text roleName;
    public TagChip who;

    public void Show(int? player)
    {
        bool human = player.HasValue;
        frame.SetLook(RoleFrameLooks.Chip(role, human));
        icon.color = NeonPalette.Get(PaletteColor.White, human ? 1f : CpuIconAlpha);
        if (roleName != null) ShowRoleName(human);
        who.Show(human ? TagChipLooks.Player(role, player.Value) : TagChipLooks.Cpu);
    }

    private void ShowRoleName(bool human)
    {
        roleName.text = RoleCardText.RoleName(role);
        roleName.color = NeonPalette.Get(human ? PaletteColor.Text : PaletteColor.Text2);
    }
}
