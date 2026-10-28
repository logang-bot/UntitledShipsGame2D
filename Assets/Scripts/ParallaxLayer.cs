using UnityEngine;

// One layer of a boss background: scrolls its sprite downward at its own
// speed and wraps it seamlessly. The sprite is drawn tiled two tiles tall,
// so moving it down by up to one tile never shows a gap. Each run starts at
// a random point in the loop, so the layers line up differently every fight.
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    public float unitsPerSecond = 0.79f;

    private float tileHeight;
    private float topY;
    private float scrolled;

    void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Vector2 tileSize = spriteRenderer.sprite.bounds.size;
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;
        spriteRenderer.size = new Vector2(tileSize.x, tileSize.y * 2f);
        tileHeight = tileSize.y;
        topY = transform.localPosition.y + tileHeight / 2f;
        scrolled = Random.value * tileHeight;
    }

    void Update()
    {
        scrolled += unitsPerSecond * Time.deltaTime;
        Vector3 position = transform.localPosition;
        position.y = topY - WrappedOffset(scrolled, tileHeight);
        transform.localPosition = position;
    }

    public static float WrappedOffset(float scrolled, float tileHeight)
    {
        return Mathf.Repeat(scrolled, tileHeight);
    }
}
