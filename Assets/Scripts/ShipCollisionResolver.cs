using UnityEngine;

// Pushes a ship's next position out of the other living ships (push-apart
// only, no damage), the boss, the minions and the wave enemies (push-apart
// AND contact damage), all with ShipCollisionUtil's AABB math. Ships, the
// boss and enemies move in small discrete steps each frame rather than
// teleporting, so a momentary overlap is exactly the signal that a physical
// collision happened. Split out of PlayerController.
public class ShipCollisionResolver
{
    private readonly GameObject self;
    private readonly Vector2 selfHalfExtents;
    private readonly Transform[] allies;
    private readonly BoxCollider2D[] allyColliders;
    private readonly MonoBehaviour bossObject;
    private readonly Vector2 bossHalfExtents;
    private readonly bool hasBossCollider;

    public ShipCollisionResolver(GameObject self, Vector2 selfHalfExtents, MonoBehaviour bossObject)
    {
        this.self = self;
        this.selfHalfExtents = selfHalfExtents;
        this.bossObject = bossObject;
        allies = self.GetComponent<PlayerAbility>()?.allies;
        allyColliders = CollidersOf(allies);
        BoxCollider2D bossCollider = bossObject != null ? bossObject.GetComponent<BoxCollider2D>() : null;
        hasBossCollider = bossCollider != null;
        if (hasBossCollider) bossHalfExtents = bossCollider.bounds.extents;
    }

    public Vector2 Resolve(Vector2 candidatePos)
    {
        candidatePos = ResolveAllies(candidatePos);
        candidatePos = ResolveBoss(candidatePos);
        candidatePos = ResolveMinions(candidatePos);
        return ResolveWaveEnemies(candidatePos);
    }

    private static BoxCollider2D[] CollidersOf(Transform[] ships)
    {
        if (ships == null) return null;
        BoxCollider2D[] colliders = new BoxCollider2D[ships.Length];
        for (int i = 0; i < ships.Length; i++)
            if (ships[i] != null) colliders[i] = ships[i].GetComponent<BoxCollider2D>();
        return colliders;
    }

    private Vector2 ResolveAllies(Vector2 pos)
    {
        if (allies == null) return pos;
        for (int i = 0; i < allies.Length; i++)
            if (IsSolidAlly(i)) pos = PushOut(pos, allies[i].position, allyColliders[i].bounds.extents).pos;
        return pos;
    }

    private bool IsSolidAlly(int index)
    {
        Transform ally = allies[index];
        if (ally == null || ally == self.transform || !ally.gameObject.activeInHierarchy) return false;
        return allyColliders[index] != null;
    }

    private Vector2 ResolveBoss(Vector2 pos)
    {
        if (bossObject == null || !hasBossCollider) return pos;
        (Vector2 pushed, bool touching) = PushOut(pos, bossObject.transform.position, bossHalfExtents);
        if (touching) ((IBoss)bossObject).ApplyContactDamage(self);
        return pushed;
    }

    /// <summary>
    /// Minions spawn and die at runtime, so their colliders can't be cached
    /// once; each Minion caches its own HalfExtents instead.
    /// </summary>
    private Vector2 ResolveMinions(Vector2 pos)
    {
        foreach (Minion minion in Minion.Active)
        {
            if (minion == null) continue;
            (Vector2 pushed, bool touching) = PushOut(pos, minion.transform.position, minion.HalfExtents);
            if (touching) minion.ApplyContactDamage(self);
            pos = pushed;
        }
        return pos;
    }

    /// <summary>Same kamikaze contact as the minions, for EnemySpawner's wave enemies.</summary>
    private Vector2 ResolveWaveEnemies(Vector2 pos)
    {
        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy == null) continue;
            (Vector2 pushed, bool touching) = PushOut(pos, enemy.transform.position, enemy.HalfExtents);
            if (touching) enemy.ApplyContactDamage(self);
            pos = pushed;
        }
        return pos;
    }

    private (Vector2 pos, bool touching) PushOut(Vector2 pos, Vector2 otherCenter, Vector2 otherHalfExtents)
    {
        Vector2 pushed = ShipCollisionUtil.ResolveBoxOverlap(pos, selfHalfExtents, otherCenter, otherHalfExtents, out bool touching);
        return (pushed, touching);
    }
}
