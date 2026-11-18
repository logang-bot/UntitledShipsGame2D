using UnityEngine;

// The Tank's wide, curved shield in front of the ship - both a visual
// (Combat FX arc sprite) and a real trigger (EdgeCollider2D, tagged Player,
// no PlayerHealth of its own) that blocks enemy bullets across a width wider
// than Tank's own body, per Bullet.cs's GetComponentInParent fix routing the
// hit to this ship's own PlayerHealth. Always on. The Super Shield grows it
// (SetScale), swaps its look, and makes its hits free (FreeBlock).
public class TankShieldArc : MonoBehaviour
{
    private const int ArcSortingOrder = -5;

    private ShieldArcShape shape;
    private EdgeCollider2D edge;
    private SpriteRenderer sprite;
    private ShieldArcBlockFlash flash;
    private ShipCombatFx fx;
    private float superScale;

    /// <summary>While true, a bullet hitting the arc is stopped without costing the Tank anything.</summary>
    public bool FreeBlock { get; set; }

    public static bool Absorbs(Collider2D hit) => hit.TryGetComponent(out TankShieldArc arc) && arc.FreeBlock;

    public static TankShieldArc Create(PlayerAbility ability)
    {
        GameObject arcObj = new GameObject("ShieldArc") { tag = "Player" };
        arcObj.transform.SetParent(ability.transform, false);
        TankShieldArc arc = arcObj.AddComponent<TankShieldArc>();
        arc.Build(ability);
        return arc;
    }

    public void SetScale(float scale) => edge.points = shape.Points(scale);

    public void ShowNormalLook()
    {
        if (sprite == null) return;
        ShowLook(fx.shieldIdle, fx.shieldBlock);
        ShieldArcSpriteFit.Normal.Place(sprite.transform, shape.Points(1f));
    }

    /// <summary>Without the Super Shield art, the normal arc is stretched to the Super Shield's size instead.</summary>
    public void ShowSuperFrame(SuperShieldFrame frame)
    {
        if (sprite == null) return;
        bool drawn = fx.HasSuperShieldArt;
        ShowLook(drawn ? fx.SuperShieldSprite(frame) : fx.shieldIdle, drawn ? fx.superShieldBlock : fx.shieldBlock);
        ShieldArcSpriteFit fit = drawn ? ShieldArcSpriteFit.Super : ShieldArcSpriteFit.Normal;
        fit.Place(sprite.transform, shape.Points(superScale));
    }

    private void Build(PlayerAbility ability)
    {
        shape = ShapeFor(ability);
        superScale = ability.tankPowers.superShieldScale;
        edge = gameObject.AddComponent<EdgeCollider2D>();
        edge.isTrigger = true;
        SetScale(1f);
        if (ability.Fx != null) AddSprite(ability.Fx);
    }

    private static ShieldArcShape ShapeFor(PlayerAbility ability)
    {
        BoxCollider2D body = ability.GetComponent<BoxCollider2D>();
        float tankWidth = body != null ? body.bounds.size.x : 1f;
        TankAbilitySettings s = ability.tankPowers;
        return new ShieldArcShape { halfWidth = tankWidth * s.shieldArcWidthMultiplier / 2f, height = s.shieldArcHeight, yOffset = s.shieldArcYOffset };
    }

    private void AddSprite(ShipCombatFx shipFx)
    {
        fx = shipFx;
        sprite = FxSprites.CreateChild(transform, "ShieldArcSprite", ArcSortingOrder);
        flash = gameObject.AddComponent<ShieldArcBlockFlash>();
        flash.arc = sprite;
        ShowNormalLook();
    }

    private void ShowLook(Sprite idle, Sprite block)
    {
        flash.idle = idle;
        flash.block = block;
    }
}
