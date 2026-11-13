using System.Collections.Generic;
using UnityEngine;

// One damage tick of the Nova Beam: every enemy collider overlapping the
// column is struck once (an enemy with several colliders still takes one
// hit), through the same EnemyDamage dispatch bullets use.
public class BeamColumnDamage
{
    private readonly HashSet<GameObject> struck = new HashSet<GameObject>();
    private readonly List<Vector2> impacts = new List<Vector2>();

    /// <summary>Strikes the column and returns where each target was hit (a reused list).</summary>
    public List<Vector2> Strike(Rect column, float damage, GameObject source)
    {
        struck.Clear();
        impacts.Clear();
        foreach (Collider2D target in Physics2D.OverlapBoxAll(column.center, column.size, 0f))
        {
            if (!target.CompareTag("Enemy") || !struck.Add(target.gameObject)) continue;
            if (EnemyDamage.TryDamage(target.gameObject, damage, source)) impacts.Add(ImpactPoint(target.bounds, column.center.x));
        }
        return impacts;
    }

    /// <summary>The target's underside at the beam's x, where the beam meets it.</summary>
    private static Vector2 ImpactPoint(Bounds bounds, float beamX)
    {
        return new Vector2(Mathf.Clamp(beamX, bounds.min.x, bounds.max.x), bounds.min.y);
    }
}
