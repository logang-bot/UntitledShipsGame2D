using UnityEngine;

public class PlayerAbilityTank
{
    private const int ShieldArcSegments = 16;
    private const int ArcSortingOrder = -5;
    private const float SpriteArcWidthPx = 360f;
    private const float SpriteArcEndsBelowCenterPx = 37f;

    private readonly PlayerAbility ability;

    public PlayerAbilityTank(PlayerAbility ability)
    {
        this.ability = ability;
    }

    public void Trigger()
    {
        ability.OnTaunt?.Invoke();
    }

    /// <summary>
    /// Wide, curved shield in front of Tank - both a visual (Combat FX arc sprite)
    /// and a real trigger (EdgeCollider2D, tagged Player, no PlayerHealth of
    /// its own) that blocks enemy bullets across a width wider than Tank's
    /// own body, per Bullet.cs's GetComponentInParent fix routing the hit to
    /// this ship's own PlayerHealth. Local-space and built once - unlike
    /// Medic's ring it never resizes, so it needs no per-frame Update().
    /// Always on, independent of Taunt.
    /// </summary>
    public void CreateShieldArc()
    {
        Vector2[] points = BuildArcPoints();
        GameObject arcObj = CreateArcGameObject();
        AddArcCollider(arcObj, points);
        AddArcSprite(arcObj, points);
    }

    private Vector2[] BuildArcPoints()
    {
        BoxCollider2D bodyCollider = ability.GetComponent<BoxCollider2D>();
        float tankWidth = bodyCollider != null ? bodyCollider.bounds.size.x : 1f;
        float halfWidth = tankWidth * ability.shieldArcWidthMultiplier / 2f;

        Vector2[] points = new Vector2[ShieldArcSegments];
        for (int i = 0; i < ShieldArcSegments; i++)
        {
            float normalizedPosition = i / (float)(ShieldArcSegments - 1);
            float x = Mathf.Lerp(-halfWidth, halfWidth, normalizedPosition);
            float y = ability.shieldArcYOffset + ability.shieldArcHeight * (1f - (x / halfWidth) * (x / halfWidth));
            points[i] = new Vector2(x, y);
        }
        return points;
    }

    private GameObject CreateArcGameObject()
    {
        GameObject arcObj = new GameObject("ShieldArc");
        arcObj.transform.SetParent(ability.transform, false);
        arcObj.tag = "Player";
        return arcObj;
    }

    private void AddArcCollider(GameObject arcObj, Vector2[] points)
    {
        EdgeCollider2D arcCollider = arcObj.AddComponent<EdgeCollider2D>();
        arcCollider.isTrigger = true;
        arcCollider.points = points;
    }

    /// <summary>
    /// The design's arc sprite spans SpriteArcWidthPx between its two end
    /// caps, which sit SpriteArcEndsBelowCenterPx below the sprite's centre.
    /// It's scaled so the caps land on the collider's first and last point.
    /// </summary>
    private void AddArcSprite(GameObject arcObj, Vector2[] points)
    {
        if (ability.Fx == null) return;
        SpriteRenderer sprite = FxSprites.CreateChild(arcObj.transform, "ShieldArcSprite", ArcSortingOrder);
        float scale = (points[points.Length - 1].x - points[0].x) / (SpriteArcWidthPx / FxSprites.PixelsPerUnit);
        sprite.transform.localScale = Vector3.one * scale;
        sprite.transform.localPosition = new Vector3(0f, points[0].y + SpriteArcEndsBelowCenterPx / FxSprites.PixelsPerUnit * scale, 0f);
        AddBlockFlash(arcObj, sprite);
    }

    private void AddBlockFlash(GameObject arcObj, SpriteRenderer sprite)
    {
        ShieldArcBlockFlash flash = arcObj.AddComponent<ShieldArcBlockFlash>();
        flash.arc = sprite;
        flash.idle = ability.Fx.shieldIdle;
        flash.block = ability.Fx.shieldBlock;
    }
}
