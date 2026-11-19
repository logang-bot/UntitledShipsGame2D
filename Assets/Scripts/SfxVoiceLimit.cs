using System.Collections.Generic;
using UnityEngine;

// Caps how many copies of one clip play at once, so a 12-bullet ring or a
// 10-rod wall sounds once instead of twelve times: a clip is refused while
// it started under MinGapSeconds ago or already has MaxVoices copies playing.
public static class SfxVoiceLimit
{
    public const float MinGapSeconds = 0.03f;
    public const int MaxVoices = 3;

    private static readonly Dictionary<AudioClip, List<float>> startTimes = new Dictionary<AudioClip, List<float>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset() => startTimes.Clear();

    public static bool TryClaim(AudioClip clip, float now)
    {
        if (clip == null) return false;
        List<float> playing = PlayingStarts(clip, now);
        if (playing.Count >= MaxVoices || StartedTooRecently(playing, now)) return false;
        playing.Add(now);
        return true;
    }

    private static List<float> PlayingStarts(AudioClip clip, float now)
    {
        if (!startTimes.TryGetValue(clip, out List<float> starts)) startTimes[clip] = starts = new List<float>();
        for (int i = starts.Count - 1; i >= 0; i--)
            if (now >= starts[i] + clip.length) starts.RemoveAt(i);
        return starts;
    }

    private static bool StartedTooRecently(List<float> playing, float now)
    {
        return playing.Count > 0 && now - playing[playing.Count - 1] < MinGapSeconds;
    }
}
