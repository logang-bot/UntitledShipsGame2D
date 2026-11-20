using TMPro;
using UnityEngine;

// The anime cut-in card over a party frame: it slides in from the frame's
// left edge when the ship's superpower is cast, plays the character, and
// slides back out. Like the rest of the HUD it only reads state: PartyFrameUI
// hands it the cast's elapsed time every frame, so pausing freezes it too.
// Bind picks the role's art and recolors the card in the role's colors. The
// role's voice line starts on the press, on the cast's first frame.
public class CutInCardView : MonoBehaviour
{
    public RectTransform panel;
    public CanvasGroup group;
    public CutInCharacterRig rig;
    public CutInTiming timing = new CutInTiming();
    public CutInRoleArt[] roles;
    public TMP_Text kicker;
    public TMP_Text title;
    public AudioSource voiceSource;
    /// <summary>Drawn in the role color (speed lines, kicker, edge); their alphas are kept.</summary>
    public PaletteTint[] roleTints;
    /// <summary>Drawn in the role's dark card color (the panel fill).</summary>
    public PaletteTint[] roleBgTints;

    private float lastElapsed = -1f;

    void Awake() => Place(0f);

    /// <summary>Sets the card up for this role, with the pose snap on the moment its power lands. False when the role has no card.</summary>
    public bool Bind(PlayerRole role, float snapSeconds)
    {
        CutInRoleArt art = System.Array.Find(roles, entry => entry.role == role);
        if (art == null) return false;
        rig.frames = art.frames;
        kicker.text = art.kicker;
        title.text = art.title;
        voiceSource.clip = art.voice;
        Tint(roleTints, RolePalette.TokenFor(role));
        Tint(roleBgTints, RolePalette.BgTokenFor(role));
        timing.snapAt = snapSeconds;
        return true;
    }

    /// <summary>Shows the card at this moment of the cast (-1 = not casting) and returns how much of the frame it covers (0..1).</summary>
    public float Show(float elapsed)
    {
        CueVoice(elapsed);
        float coverage = CutInTimeline.Coverage(timing, elapsed);
        Place(coverage);
        if (coverage > 0f) rig.Play(timing, elapsed, coverage);
        return coverage;
    }

    private void CueVoice(float elapsed)
    {
        if (CutInTimeline.CastStarted(lastElapsed, elapsed) && voiceSource.clip != null) voiceSource.Play();
        lastElapsed = elapsed;
    }

    private static void Tint(PaletteTint[] tints, PaletteColor key)
    {
        foreach (PaletteTint tint in tints) tint.Set(new PaletteSwatch(key, tint.swatch.alpha));
    }

    private void Place(float coverage)
    {
        panel.anchoredPosition = new Vector2(-(1f - coverage) * panel.rect.width, 0f);
        group.alpha = coverage > 0f ? 1f : 0f;
    }
}
