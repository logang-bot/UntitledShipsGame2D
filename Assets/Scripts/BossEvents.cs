using UnityEngine;
using UnityEngine.Events;

// The one place that knows each boss's concrete type, so LevelSetup and
// PartyBossWiring can drive whichever boss the level spawned. Same
// concrete-cast idiom AIController uses for its per-boss taunt rules.
// Halcyon has no aggro, so Taunt is deliberately a no-op against it.
public static class BossEvents
{
    public static UnityEvent Phase2(MonoBehaviour boss) => boss switch
    {
        MarauderBoss marauder => marauder.OnPhase2,
        HalcyonBoss halcyon => halcyon.OnPhase2,
        WardenBoss warden => warden.OnPhase2,
        _ => null
    };

    public static UnityEvent Defeated(MonoBehaviour boss) => boss switch
    {
        MarauderBoss marauder => marauder.OnDefeated,
        HalcyonBoss halcyon => halcyon.OnDefeated,
        WardenBoss warden => warden.OnDefeated,
        _ => null
    };

    public static Level LevelOf(MonoBehaviour boss) => boss switch
    {
        HalcyonBoss => Level.Level2,
        WardenBoss => Level.Level3,
        _ => Level.Level1
    };

    public static void AssignShips(MonoBehaviour boss, GameObject[] ships)
    {
        if (boss is MarauderBoss marauder) marauder.targets = ships;
        if (boss is WardenBoss warden) warden.ships = ships;
        HalcyonStaticField staticField = boss.GetComponent<HalcyonStaticField>();
        if (staticField != null) staticField.ships = ships;
    }

    public static void Taunt(MonoBehaviour boss, GameObject taunter)
    {
        if (boss is MarauderBoss marauder) marauder.TauntedBy(taunter);
        if (boss is WardenBoss warden) warden.TauntedBy(taunter);
    }
}
