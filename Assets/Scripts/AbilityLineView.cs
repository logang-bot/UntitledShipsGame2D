using TMPro;
using UnityEngine;

// One ability line at the bottom of a role card: kind (in the role color,
// or slate for a CPU-flown role), name and description.
public class AbilityLineView : MonoBehaviour
{
    public TMP_Text kind;
    public TMP_Text abilityName;
    public TMP_Text description;

    public void Show(RoleAbilityLine line, PlayerRole role, bool cpu)
    {
        kind.text = line.kind;
        kind.color = NeonPalette.Get(cpu ? PaletteColor.Text3 : RolePalette.TokenFor(role));
        abilityName.text = line.name;
        abilityName.color = NeonPalette.Get(cpu ? PaletteColor.TextSoft : PaletteColor.Text);
        description.text = line.description;
    }
}
