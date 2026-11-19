using UnityEngine;

// The Tank's Taunt magnet sounds, cued by TauntMagnetVisual as its drawn
// frame changes, so they stay in sync with TauntMagnetTimeline.
public class TauntSfx : MonoBehaviour
{
    public AudioSource source;
    public AudioSource loop;
    public SfxCue activate;
    public SfxCue sustainLoop;
    public SfxCue end;

    public void OnFrame(TauntMagnetFrame frame)
    {
        if (frame == TauntMagnetFrame.Activate) PlayActivate();
        else if (frame == TauntMagnetFrame.End) PlayEnd();
        else if (frame == TauntMagnetFrame.None) SfxPlayback.StopLoop(loop);
    }

    private void PlayActivate()
    {
        SfxPlayback.Play(source, activate);
        SfxPlayback.StartLoop(loop, sustainLoop);
    }

    private void PlayEnd()
    {
        SfxPlayback.StopLoop(loop);
        SfxPlayback.Play(source, end);
    }
}
