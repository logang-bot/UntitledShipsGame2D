using UnityEngine;

public class PlayerAbilityTank
{
    private const int ShieldArcSegments = 16;

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
    /// Wide, curved shield in front of Tank - both a visual (LineRenderer)
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
        AddArcLineRenderer(arcObj, points);
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

    private void AddArcLineRenderer(GameObject arcObj, Vector2[] points)
    {
        LineRenderer arcLine = arcObj.AddComponent<LineRenderer>();
        arcLine.useWorldSpace = false;
        arcLine.loop = false;
        arcLine.positionCount = ShieldArcSegments;
        arcLine.material = new Material(Shader.Find("Sprites/Default"));
        arcLine.sortingLayerName = "Default";
        arcLine.sortingOrder = -1;
        arcLine.startColor = ability.shieldArcColor;
        arcLine.endColor = ability.shieldArcColor;
        arcLine.startWidth = ability.shieldArcLineWidth;
        arcLine.endWidth = ability.shieldArcLineWidth;
        for (int i = 0; i < ShieldArcSegments; i++)
        {
            arcLine.SetPosition(i, points[i]);
        }
    }
}
