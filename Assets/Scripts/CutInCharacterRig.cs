using UnityEngine;
using UnityEngine.UI;

// Plays the cut-in character's drawn frames from CutInPoseTimeline: swaps
// the portrait's sprite, flashes the card white at the snap, and pushes in
// and shakes the character. The frames are full-card 1216x362 images
// (Assets/Art/CutIns/<Role>/), listed in CutInFrame order.
public class CutInCharacterRig : MonoBehaviour
{
    public RectTransform character;
    public Image portrait;
    public Sprite[] frames;
    public Image flash;
    public CutInSpeedLines speedLines;
    /// <summary>How far the character lags behind the sliding card, for a little depth.</summary>
    public float parallaxPixels = 60f;
    public float shakePixels = 6f;

    public void Play(CutInTiming timing, float elapsed, float coverage)
    {
        portrait.sprite = frames[(int)CutInPoseTimeline.FrameAt(timing, elapsed)];
        flash.color = NeonPalette.Get(PaletteColor.White, CutInPoseTimeline.FlashAlpha(timing, elapsed));
        PlaceCharacter(timing, elapsed, coverage);
        speedLines.Tick(elapsed);
    }

    private void PlaceCharacter(CutInTiming timing, float elapsed, float coverage)
    {
        Vector2 shake = Random.insideUnitCircle * shakePixels * CutInPoseTimeline.ShakeStrength(timing, elapsed);
        character.anchoredPosition = new Vector2(-(1f - coverage) * parallaxPixels, 0f) + shake;
        character.localScale = Vector3.one * CutInPoseTimeline.PushScale(timing, elapsed);
    }
}
