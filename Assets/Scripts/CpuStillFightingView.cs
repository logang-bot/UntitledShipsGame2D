using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game Over's "CPU STILL FIGHTING" list (design: 08 / Overlays): every
// human is down but the CPUs keep fighting behind the lighter backdrop, so
// their ships are listed as a reminder. Filled each time the overlay opens.
public class CpuStillFightingView : MonoBehaviour
{
    public PartyFrameManager party;
    public GameObject box;
    public GameObject chipTemplate;
    public Sprite[] roleIcons;

    private readonly List<GameObject> chips = new List<GameObject>();

    void OnEnable()
    {
        foreach (GameObject chip in chips) Destroy(chip);
        chips.Clear();
        chipTemplate.SetActive(false);
        foreach (GameObject ship in party.players)
            if (IsLivingCpu(ship)) chips.Add(ChipFor(ship.GetComponent<PlayerRoleComponent>().role));
        box.SetActive(chips.Count > 0);
    }

    private bool IsLivingCpu(GameObject ship) =>
        ship != null && ship.activeInHierarchy && !party.SeatOf(ship).isHuman && ship.GetComponent<PlayerRoleComponent>() != null;

    private GameObject ChipFor(PlayerRole role)
    {
        GameObject chip = Instantiate(chipTemplate, chipTemplate.transform.parent);
        chip.SetActive(true);
        chip.GetComponentInChildren<Image>().sprite = roleIcons[(int)role];
        chip.GetComponentInChildren<TMP_Text>().text = RoleCardText.ShipName(role);
        return chip;
    }
}
