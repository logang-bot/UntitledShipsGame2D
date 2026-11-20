using UnityEngine;

// Plays SfxCues from the emitter's own AudioSource - no shared audio
// manager. One-shots go through SfxVoiceLimit; loops (beam, shields)
// own a dedicated looping source.
public static class SfxPlayback
{
    public static void Play(AudioSource source, SfxCue cue)
    {
        AudioClip clip = cue?.Pick();
        if (source == null || !SfxVoiceLimit.TryClaim(clip, Time.time)) return;
        source.PlayOneShot(clip, cue.volume);
    }

    public static void StartLoop(AudioSource loop, SfxCue cue)
    {
        AudioClip clip = cue?.Pick();
        if (loop == null || clip == null) return;
        loop.clip = clip;
        loop.volume = cue.volume;
        loop.loop = true;
        loop.Play();
    }

    public static void StopLoop(AudioSource loop)
    {
        if (loop != null) loop.Stop();
    }
}
