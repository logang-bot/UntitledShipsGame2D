using UnityEngine;

// Connects the party's ships and the level's runtime-spawned boss both
// ways. The scene can't wire this in the Inspector any more, since the boss
// only exists once LevelSetup instantiates it, so the Tank's OnTaunt gets a
// runtime listener here - the same documented exception as PartyFrameUI.
public static class PartyBossWiring
{
    public static void Wire(GameObject[] ships, MonoBehaviour boss)
    {
        foreach (GameObject ship in ships) WireShip(ship, boss);
        BossEvents.AssignShips(boss, ships);
    }

    private static void WireShip(GameObject ship, MonoBehaviour boss)
    {
        ship.GetComponent<PlayerController>().bossObject = boss;
        AIController ai = ship.GetComponent<AIController>();
        if (ai.enabled) ai.bossObject = boss;
        ship.GetComponent<PlayerAbility>().OnTaunt.AddListener(() => BossEvents.Taunt(boss, ship));
    }
}
