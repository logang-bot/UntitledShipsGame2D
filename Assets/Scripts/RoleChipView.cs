using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A role chip (role icon + ATTACKER/TANK/...) in the role's color, used by
// the boss panel's aggro row, warning targets and arm locks.
public class RoleChipView : MonoBehaviour
{
    public Image stroke;
    public Image icon;
    public Sprite[] roleIcons;
    public TMP_Text label;

    public void Show(PlayerRole role)
    {
        Color main = NeonPalette.Get(RolePalette.TokenFor(role));
        gameObject.SetActive(true);
        stroke.color = main;
        icon.sprite = roleIcons[(int)role];
        icon.color = main;
        label.text = RoleCardText.RoleName(role);
        label.color = main;
    }

    public void Hide() => gameObject.SetActive(false);
}
