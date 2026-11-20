using UnityEngine;

// One sound effect as set in the Inspector: its variants (one is picked at
// random each time it plays) and its volume. Played through SfxPlayback.
[System.Serializable]
public class SfxCue
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;

    public bool IsEmpty => clips == null || clips.Length == 0;

    public AudioClip Pick() => IsEmpty ? null : clips[Random.Range(0, clips.Length)];
}
