using System.Collections;
using UnityEngine;

// Plays a short run of sprite frames on its own SpriteRenderer, then
// destroys itself once its sound (if it has an AudioSource) is done: death
// bursts, the heal flash and the role superpowers from the Combat FX
// design. Spawned as a prefab and forgotten.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSequence : MonoBehaviour
{
    public SpriteSequenceStep[] steps;
    public float holdSeconds = 1f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        StartCoroutine(PlayThenDestroy());
    }

    private IEnumerator PlayThenDestroy()
    {
        foreach (SpriteSequenceStep step in steps)
        {
            sr.sprite = step.sprite;
            yield return new WaitForSeconds(step.hold ? holdSeconds : step.duration);
        }
        yield return HideUntilSoundEnds();
        Destroy(gameObject);
    }

    /// <summary>A death burst's sound outlasts its frames; destroying it on the last frame would cut the sound.</summary>
    private IEnumerator HideUntilSoundEnds()
    {
        if (!TryGetComponent(out AudioSource sound)) yield break;
        sr.enabled = false;
        while (sound.isPlaying) yield return null;
    }
}
