using TMPro;
using UnityEngine;
using UnityEngine.UI;

// While the level-up screen has the fight frozen (design: Neon Fleet HUD v2,
// section 10): a full-screen scrim darkens the playfield and both rails, the
// Attacker's frame is lifted above it so current upgrades stay readable,
// every other party frame, the damage meter and the boss rail drop to 35%,
// and the rail's pause hint reads PAUSE · LOCKED with a lock icon.
public class HudFreezeDimmer : MonoBehaviour
{
    private const float FrozenAlpha = 0.35f;
    private const float ScrimDesignAlpha = 0.86f;
    private const int LiftedSortingOrder = 1;
    private const string PauseText = "PAUSE";
    private const string LockedPauseText = "PAUSE · LOCKED";

    public PartyFrameManager partyFrameManager;
    public CanvasGroup[] railGroups;
    public GameObject pauseLockIcon;
    public TMP_Text pauseLabel;
    public Graphic scrim;

    public void SetFrozen(bool frozen)
    {
        float alpha = frozen ? FrozenAlpha : 1f;
        foreach (CanvasGroup group in railGroups) group.alpha = alpha;
        DimOtherFrames(alpha);
        ShowScrim(frozen);
        LiftAttackerFrame(frozen);
        pauseLockIcon.SetActive(frozen);
        pauseLabel.text = frozen ? LockedPauseText : PauseText;
    }

    private void DimOtherFrames(float alpha)
    {
        foreach (PartyFrameUI frame in partyFrameManager.partyFrames)
            if (frame != null && frame.Role != PlayerRole.Attacker && frame.TryGetComponent(out CanvasGroup group)) group.alpha = alpha;
    }

    private void ShowScrim(bool shown)
    {
        scrim.gameObject.SetActive(shown);
        Color color = scrim.color;
        color.a = LinearSpaceAlpha.ForBlackOverlay(ScrimDesignAlpha);
        scrim.color = color;
    }

    private void LiftAttackerFrame(bool lifted)
    {
        PartyFrameUI frame = partyFrameManager.FrameFor(PlayerRole.Attacker);
        if (frame == null) return;
        if (!frame.TryGetComponent(out Canvas canvas)) canvas = frame.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = lifted;
        canvas.sortingOrder = lifted ? LiftedSortingOrder : 0;
    }
}
