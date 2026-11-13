using UnityEngine;

// The anime cut-in card over a party frame: it slides in from the frame's
// left edge when the ship's superpower is cast, plays the character, and
// slides back out. Like the rest of the HUD it only reads state: PartyFrameUI
// hands it the cast's elapsed time every frame, so pausing freezes it too.
public class CutInCardView : MonoBehaviour
{
    public RectTransform panel;
    public CanvasGroup group;
    public CutInCharacterRig rig;
    public CutInTiming timing = new CutInTiming();

    void Awake() => Place(0f);

    /// <summary>Lands the pose snap on the moment the ship's power actually fires.</summary>
    public void SyncSnap(float windupSeconds) => timing.snapAt = windupSeconds;

    /// <summary>Shows the card at this moment of the cast (-1 = not casting) and returns how much of the frame it covers (0..1).</summary>
    public float Show(float elapsed)
    {
        float coverage = CutInTimeline.Coverage(timing, elapsed);
        Place(coverage);
        if (coverage > 0f) rig.Play(timing, elapsed, coverage);
        return coverage;
    }

    private void Place(float coverage)
    {
        panel.anchoredPosition = new Vector2(-(1f - coverage) * panel.rect.width, 0f);
        group.alpha = coverage > 0f ? 1f : 0f;
    }
}
