using System.Collections;
using UnityEngine;

// Plays a short run of sprite frames on its own SpriteRenderer, then
// destroys itself: death bursts, the heal flash and the role superpowers
// from the Combat FX design. Spawned as a prefab and forgotten.
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
        Destroy(gameObject);
    }
}
