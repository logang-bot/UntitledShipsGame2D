using System.Collections;
using UnityEngine;

// Plays one music track on a loop that restarts at startAtSeconds instead of
// the song's intro (so a song can start on its best part). The AudioSource
// keeps its own Loop off and Ignore Listener Pause on, so the music plays on
// under the pause menu; its Volume is the track's full volume. One sits in
// MenuBackground (all five menu screens) and one in Level, where
// BossMusicCue moves it. Provisional until the FMOD pass (docs/roadmap.md).
public class MusicTrack : MonoBehaviour
{
    public AudioSource source;
    public float startAtSeconds;

    private float fullVolume;

    void Awake() => fullVolume = source.volume;

    void Start() => PlayFromStart();

    void Update()
    {
        if (!source.isPlaying) PlayFromStart();
    }

    /// <summary>Dips the volume out, seeks to songSeconds, and brings it back in, each fade fadeSeconds long.</summary>
    public IEnumerator JumpTo(float songSeconds, float fadeSeconds)
    {
        yield return FadeVolume(fullVolume, 0f, fadeSeconds);
        source.time = songSeconds;
        yield return FadeVolume(0f, fullVolume, fadeSeconds);
    }

    private void PlayFromStart()
    {
        source.Play();
        source.time = Mathf.Clamp(startAtSeconds, 0f, source.clip.length - 1f);
    }

    private IEnumerator FadeVolume(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            source.volume = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        source.volume = to;
    }
}
