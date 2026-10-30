using UnityEngine;

// The four squad chips, one per role in PlayerRole order.
public class SquadStripView : MonoBehaviour
{
    public SquadChipView[] chips;

    /// <param name="playerByRole">Player number flying each role, indexed by (int)PlayerRole; null = CPU.</param>
    public void Show(int?[] playerByRole)
    {
        for (int i = 0; i < chips.Length; i++) chips[i].Show(playerByRole[i]);
    }
}
