using UnityEngine;

// Root of a boss background prefab (its ParallaxLayer children do the
// scrolling). Mirrors the whole background left-to-right on a coin flip,
// so the city varies between runs. All layers flip together, so the
// rooftop shadows keep falling the same way. Flipping in X keeps every
// layer's vertical wrap seamless.
public class ParallaxBackground : MonoBehaviour
{
    void Awake()
    {
        if (Random.value < 0.5f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -scale.x;
            transform.localScale = scale;
        }
    }
}
