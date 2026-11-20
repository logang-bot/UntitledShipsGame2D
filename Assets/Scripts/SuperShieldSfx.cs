using UnityEngine;

// The Tank's Super Shield sounds, cued by TankSuperShield as its drawn frame
// changes, so they stay in sync with SuperShieldTimeline when it's retuned:
// expand and the idle loop, the ending warning, the collapse.
public class SuperShieldSfx : MonoBehaviour
{
    public AudioSource source;
    public AudioSource loop;
    public SfxCue expand;
    public SfxCue idleLoop;
    public SfxCue ending;
    public SfxCue collapse;

    private bool endingPlayed;

    public void OnFrame(SuperShieldFrame frame)
    {
        if (frame == SuperShieldFrame.Expand) PlayExpand();
        else if (frame == SuperShieldFrame.Ending && !endingPlayed) PlayEnding();
        else if (frame == SuperShieldFrame.Collapse) SfxPlayback.Play(source, collapse);
    }

    public void OnEnd() => SfxPlayback.StopLoop(loop);

    private void PlayExpand()
    {
        endingPlayed = false;
        SfxPlayback.Play(source, expand);
        SfxPlayback.StartLoop(loop, idleLoop);
    }

    /// <summary>Once per cast: the ending frame flickers on and off with Idle.</summary>
    private void PlayEnding()
    {
        endingPlayed = true;
        SfxPlayback.StopLoop(loop);
        SfxPlayback.Play(source, ending);
    }
}
