using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One stat row on a role card: label, a bar on the shared per-stat scale
// (RoleStatScale), and the value - with an amber asterisk while the number
// is still a placeholder. CPU-flown roles show the bar in slate.
public class StatBarView : MonoBehaviour
{
    public TMP_Text label;
    public RectTransform barFill;
    public Image barFillImage;
    public TMP_Text value;

    public void Show(PlayerRole role, RoleStat stat, bool cpu)
    {
        label.text = RoleStatScale.Label(stat);
        barFill.anchorMax = new Vector2(RoleStatScale.Fraction(role, stat), 1f);
        barFillImage.color = NeonPalette.Get(cpu ? PaletteColor.Muted : RolePalette.TokenFor(role));
        value.text = RoleStatScale.Format(role, stat) + TbdMark(role, stat);
        value.color = NeonPalette.Get(cpu ? PaletteColor.Text2 : PaletteColor.Text);
    }

    private static string TbdMark(PlayerRole role, RoleStat stat) =>
        RoleCardText.IsTbd(role, stat) ? $"<color={NeonPalette.RichTextHex(PaletteColor.Notice)}>*</color>" : "";
}
