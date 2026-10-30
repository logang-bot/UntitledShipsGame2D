using UnityEngine;

// On the Tank's ShieldArc (next to its trigger collider): swaps the arc's
// sprite to the brighter "block" frame for a moment whenever an enemy
// bullet hits the arc.
public class ShieldArcBlockFlash : MonoBehaviour
{
    public SpriteRenderer arc;
    public Sprite idle;
    public Sprite block;
    public float blockDuration = 0.12f;

    private float blockUntil;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Bullet bullet) && bullet.Owner == "Enemy") blockUntil = Time.time + blockDuration;
    }

    void Update()
    {
        if (arc != null) arc.sprite = Time.time < blockUntil && block != null ? block : idle;
    }
}
