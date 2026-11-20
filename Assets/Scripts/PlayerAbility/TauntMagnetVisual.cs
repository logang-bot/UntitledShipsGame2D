using UnityEngine;

// The magnet field around a taunting Tank: plays TauntMagnetTimeline's
// frames on its own sprite under the ship, hidden between taunts. In the
// ability-effect sorting layer, below both sides' bullets.
public class TauntMagnetVisual : MonoBehaviour
{
    private const int SortingOrder = -2;

    private SpriteRenderer sr;
    private Sprite[] frames;
    private TauntSfx sfx;
    private TauntMagnetFrame shownFrame = TauntMagnetFrame.None;
    private float startedAt = float.NegativeInfinity;
    private float tauntSeconds;

    public static TauntMagnetVisual Create(Transform ship, Sprite[] frames)
    {
        SpriteRenderer sr = FxSprites.CreateChild(ship, "TauntMagnet", SortingOrder);
        FxSprites.UndoParentScale(sr.transform);
        sr.enabled = false;
        TauntMagnetVisual visual = sr.gameObject.AddComponent<TauntMagnetVisual>();
        visual.sr = sr;
        visual.frames = frames;
        visual.sfx = ship.GetComponent<TauntSfx>();
        return visual;
    }

    public void Play(float seconds)
    {
        startedAt = Time.time;
        tauntSeconds = seconds;
    }

    void Update()
    {
        TauntMagnetFrame frame = TauntMagnetTimeline.FrameAt(Time.time - startedAt, tauntSeconds);
        sr.enabled = frame != TauntMagnetFrame.None;
        if (sr.enabled) sr.sprite = frames[(int)frame];
        CueSoundOnFrameChange(frame);
    }

    private void CueSoundOnFrameChange(TauntMagnetFrame frame)
    {
        if (frame == shownFrame) return;
        shownFrame = frame;
        if (sfx != null) sfx.OnFrame(frame);
    }
}
