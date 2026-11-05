using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The boss panel's aggro row: who the boss is after (role chip + ship name)
// and a TAUNTED tag when the Tank pulled it.
public class AggroRowView : MonoBehaviour
{
    public Image aggroIcon;
    public RoleChipView role;
    public TMP_Text shipName;
    public GameObject tauntedTag;

    public void Bind(BossTokens boss) => aggroIcon.color = NeonPalette.Get(boss.main);

    public void Show(PlayerRole target, bool taunted)
    {
        gameObject.SetActive(true);
        role.Show(target);
        shipName.text = RoleCardText.ShipName(target);
        tauntedTag.SetActive(taunted);
    }

    public void Hide() => gameObject.SetActive(false);
}
