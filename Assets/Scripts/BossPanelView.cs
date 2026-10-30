using System.Collections.Generic;
using UnityEngine;

// The right rail's one shared boss panel (design: HUD Boss Panel): header,
// up to two warning bars, the OPENING! row, aggro, Warden's arm cards and
// the cooldown tiles. It only draws: each boss has a feed
// (MarauderPanelFeed / HalcyonPanelFeed / WardenPanelFeed) that reads its
// boss and pushes states in. Hidden until the boss-entry callout leaves
// (Reveal), when hideUntilRevealed is on.
public class BossPanelView : MonoBehaviour
{
    public CanvasGroup group;
    public BossHeaderView header;
    public WarningBarView[] warnings;
    public GameObject openingRow;
    public AggroRowView aggro;
    public GameObject armsSection;
    public ArmCardView[] arms;
    public CooldownTileView[] tiles;
    public bool hideUntilRevealed = true;
    public float revealSeconds = 0.3f;

    private float targetAlpha = 1f;

    public void Bind(Level level)
    {
        BossTokens boss = BossPalette.For(level);
        header.Bind(level);
        foreach (WarningBarView warning in warnings) warning.Bind(boss);
        foreach (CooldownTileView tile in tiles) tile.Bind(boss);
        foreach (ArmCardView arm in arms) arm.Bind(boss);
        aggro.Bind(boss);
        ClearRows();
        targetAlpha = hideUntilRevealed ? 0f : 1f;
        group.alpha = targetAlpha;
    }

    public void Reveal() => targetAlpha = 1f;

    void Update()
    {
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.unscaledDeltaTime / revealSeconds);
    }

    public void ShowWarnings(List<WarningRowState> states)
    {
        for (int i = 0; i < warnings.Length; i++)
        {
            if (i < states.Count) warnings[i].Show(states[i]);
            else warnings[i].Hide();
        }
    }

    public void ShowTiles(List<CooldownTileState> states)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            if (i < states.Count) tiles[i].Show(states[i]);
            else tiles[i].Hide();
        }
    }

    public void ShowArms(List<ArmCardState> states)
    {
        armsSection.SetActive(states.Count > 0);
        for (int i = 0; i < arms.Length; i++)
        {
            arms[i].gameObject.SetActive(i < states.Count);
            if (i < states.Count) arms[i].Show(states[i]);
        }
    }

    public void ShowOpening(bool open) => openingRow.SetActive(open);

    public void ShowAggro(PlayerRole? target, bool taunted)
    {
        if (target.HasValue) aggro.Show(target.Value, taunted);
        else aggro.Hide();
    }

    public void ShowDefeated()
    {
        header.ShowDefeated();
        ClearRows();
        targetAlpha = 1f;
    }

    private void ClearRows()
    {
        ShowWarnings(new List<WarningRowState>());
        ShowTiles(new List<CooldownTileState>());
        ShowArms(new List<ArmCardState>());
        ShowOpening(false);
        ShowAggro(null, false);
    }
}
