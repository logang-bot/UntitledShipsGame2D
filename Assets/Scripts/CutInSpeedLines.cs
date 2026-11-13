using UnityEngine;

// The diagonal speed lines streaming behind the cut-in character.
public class CutInSpeedLines : MonoBehaviour
{
    public RectTransform strip;
    public float speed = 900f;
    /// <summary>The spacing between lines; the strip jumps back by this much, so the loop is seamless.</summary>
    public float spacing = 90f;

    public void Tick(float elapsed)
    {
        strip.anchoredPosition = new Vector2(Mathf.Repeat(elapsed * speed, spacing), 0f);
    }
}
