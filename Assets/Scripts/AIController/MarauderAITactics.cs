using UnityEngine;

// Marauder has a single-target aggro table, so an AI Tank only taunts when
// it isn't already the one holding aggro.
public class MarauderAITactics : AIBossTactics
{
    private readonly MarauderBoss marauder;

    public MarauderAITactics(MarauderBoss marauder) : base(marauder)
    {
        this.marauder = marauder;
    }

    public override bool ShouldTaunt(AIController ai)
    {
        return marauder.CurrentTarget != ai.gameObject;
    }
}
