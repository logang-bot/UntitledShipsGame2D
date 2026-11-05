using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One line of the damage meter (left rail) or the Victory summary: role
// icon, ship name, a role-colored bar relative to the top dealer, TOTAL,
// DPS and share. `pilot` is the Victory variant's seat tag under the name.
public class DamageMeterRowView : MonoBehaviour
{
    public Image icon;
    public Sprite[] roleIcons;
    public TMP_Text shipName;
    public TMP_Text pilot;
    public Image barFill;
    public TMP_Text total;
    public TMP_Text dps;
    public TMP_Text share;

    public void Show(DamageMeterRow row, string pilotLabel = "")
    {
        gameObject.SetActive(true);
        icon.sprite = roleIcons[(int)row.role];
        shipName.text = RoleCardText.ShipName(row.role);
        if (pilot != null) pilot.text = pilotLabel;
        barFill.fillAmount = row.bar;
        barFill.color = NeonPalette.Get(RolePalette.TokenFor(row.role));
        total.text = $"{row.damage:0}";
        dps.text = $"{row.dps:0.0}";
        share.text = $"{Mathf.RoundToInt(row.share * 100f)}%";
    }

    public void Hide() => gameObject.SetActive(false);
}
