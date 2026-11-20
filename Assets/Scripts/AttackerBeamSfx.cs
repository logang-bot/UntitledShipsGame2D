using UnityEngine;

// The Nova Beam's sounds, cued by AttackerBeamCast: the wind-up on the E
// press, the activation and the sustain loop when the beam fires, the tail
// when it ends.
public class AttackerBeamSfx : MonoBehaviour
{
    public AudioSource source;
    public AudioSource loop;
    public SfxCue windup;
    public SfxCue activate;
    public SfxCue sustainLoop;
    public SfxCue end;

    public void PlayWindup() => SfxPlayback.Play(source, windup);

    public void PlayBeamStart()
    {
        SfxPlayback.Play(source, activate);
        SfxPlayback.StartLoop(loop, sustainLoop);
    }

    public void PlayBeamEnd()
    {
        SfxPlayback.StopLoop(loop);
        SfxPlayback.Play(source, end);
    }
}
