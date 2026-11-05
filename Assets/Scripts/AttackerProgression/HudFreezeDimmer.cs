using TMPro;
using UnityEngine;

// While the level-up screen has the fight frozen (design: Neon Fleet HUD v2,
// section 10): every party frame but the Attacker's, the damage meter and the
// boss rail drop to 35% so the freeze is obvious, and the rail's pause hint
// reads PAUSE · LOCKED with a lock icon.
public class HudFreezeDimmer : MonoBehaviour
{
    private const float FrozenAlpha = 0.35f;
    private const string PauseText = "PAUSE";
    private const string LockedPauseText = "PAUSE · LOCKED";

    public PartyFrameManager partyFrameManager;
    public CanvasGroup[] railGroups;
    public GameObject pauseLockIcon;
    public TMP_Text pauseLabel;

    public void SetFrozen(bool frozen)
    {
        float alpha = frozen ? FrozenAlpha : 1f;
        foreach (CanvasGroup group in railGroups) group.alpha = alpha;
        DimOtherFrames(alpha);
        pauseLockIcon.SetActive(frozen);
        pauseLabel.text = frozen ? LockedPauseText : PauseText;
    }

    private void DimOtherFrames(float alpha)
    {
        foreach (PartyFrameUI frame in partyFrameManager.partyFrames)
            if (frame != null && frame.Role != PlayerRole.Attacker && frame.TryGetComponent(out CanvasGroup group)) group.alpha = alpha;
    }
}
