using UnityEngine;

// The sounds every placed superpower shares, cued by its cast as the drawn
// frame changes, so they stay in sync with ZonePowerTimeline when it's
// retuned: the open cue and the hold loop, the ending warning, the collapse.
// MedicSfx and EchoGateSfx add each role's own cues.
public abstract class ZonePowerSfx : MonoBehaviour
{
    public AudioSource source;
    public AudioSource loop;
    public SfxCue open;
    public SfxCue idleLoop;
    public SfxCue ending;
    public SfxCue collapse;

    private bool endingPlayed;

    public void OnFrame(ZoneFrame frame)
    {
        if (frame == ZoneFrame.Open) PlayOpen();
        else if (frame == ZoneFrame.Ending && !endingPlayed) PlayEnding();
        else if (frame == ZoneFrame.Collapse) SfxPlayback.Play(source, collapse);
    }

    public void OnEnd() => SfxPlayback.StopLoop(loop);

    private void PlayOpen()
    {
        endingPlayed = false;
        SfxPlayback.Play(source, open);
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
