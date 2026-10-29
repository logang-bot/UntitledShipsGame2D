using UnityEngine;

// Strategy for how AI teammates play against a specific boss. This base
// class is the default behaviour (Halcyon, and any boss without special
// needs, use it as-is); a boss that needs different AI gets a subclass that
// overrides only what differs, plus one line in For(). See
// docs/architecture.md's "Per-boss AI behavior: Strategy (AIBossTactics)".
public class AIBossTactics
{
    protected readonly MonoBehaviour boss;

    public AIBossTactics(MonoBehaviour boss)
    {
        this.boss = boss;
    }

    public static AIBossTactics For(MonoBehaviour boss)
    {
        if (boss is MarauderBoss marauder) return new MarauderAITactics(marauder);
        if (boss is WardenBoss warden) return new WardenAITactics(warden);
        return new AIBossTactics(boss);
    }

    /// <summary>
    /// The point AI ships line their (straight-up) fire up under. Positioning
    /// aims here; boss-distance/avoidance math still uses the real boss.
    /// </summary>
    public virtual Vector2 FocusPoint()
    {
        return boss != null ? (Vector2)boss.transform.position : Vector2.zero;
    }

    /// <summary>
    /// True when every role should drop its usual spot (e.g. Support's roam)
    /// and line up under FocusPoint() instead.
    /// </summary>
    public virtual bool HasPriorityFocus()
    {
        return false;
    }

    public virtual bool ShouldTaunt(AIController ai)
    {
        return false;
    }
}
