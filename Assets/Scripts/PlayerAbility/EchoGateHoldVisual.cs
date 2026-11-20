using UnityEngine;

// What shows the Support is holding its Echo Gate: a tether from the ship
// to the gate's centre, and a channel ring around the ship. Drawn with the
// Combat FX sprites when they're in ShipCombatFx; until then a plain line in
// the Support's color and the old party speed ring stand in.
public class EchoGateHoldVisual
{
    private const int SortingOrder = -4;
    private const float PlaceholderTetherWidth = 0.05f;
    private const float PlaceholderTetherAlpha = 0.6f;
    private const float RingFrameSeconds = 0.25f;

    private readonly PlayerAbility support;
    private SpriteRenderer tether;
    private SpriteRenderer ring;

    public EchoGateHoldVisual(PlayerAbility support)
    {
        this.support = support;
    }

    private ShipCombatFx Fx => support.Fx;

    public void Show()
    {
        if (Fx == null) return;
        if (tether == null) Build();
        SetVisible(true);
    }

    public void Hide()
    {
        if (tether != null) SetVisible(false);
    }

    public void Follow(Vector2 gateCentre)
    {
        if (tether == null) return;
        Stretch(support.transform.position, gateCentre);
        ring.sprite = RingSprite();
    }

    private void Build()
    {
        tether = FxSprites.CreateChild(null, "EchoGateTether", SortingOrder);
        tether.sprite = Fx.echoTether != null ? Fx.echoTether : FxSprites.WhiteUnitSprite;
        if (Fx.echoTether == null) tether.color = TetherPlaceholderColor();
        ring = FxSprites.CreateChild(support.transform, "EchoGateChannelRing", SortingOrder);
        FxSprites.UndoParentScale(ring.transform);
    }

    private void SetVisible(bool visible)
    {
        tether.enabled = visible;
        ring.enabled = visible;
    }

    private void Stretch(Vector2 from, Vector2 to)
    {
        Vector2 span = to - from;
        float angle = Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg - 90f;
        tether.transform.SetPositionAndRotation((from + to) * 0.5f, Quaternion.Euler(0f, 0f, angle));
        Vector2 spriteSize = tether.sprite.bounds.size;
        float width = Fx.echoTether != null ? spriteSize.x : PlaceholderTetherWidth;
        tether.transform.localScale = new Vector3(width / spriteSize.x, span.magnitude / spriteSize.y, 1f);
    }

    private Sprite RingSprite()
    {
        Sprite[] frames = Fx.echoChannelFrames;
        if (frames == null || frames.Length == 0) return Fx.speedRing;
        return frames[Mathf.FloorToInt(Time.time / RingFrameSeconds) % frames.Length];
    }

    private Color TetherPlaceholderColor()
    {
        Color color = support.TintColor;
        color.a = PlaceholderTetherAlpha;
        return color;
    }
}
