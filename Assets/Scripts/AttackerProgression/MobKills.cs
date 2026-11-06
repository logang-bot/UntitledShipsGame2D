using System;

// Lets mobs report a kill to whoever is counting, without knowing who that is.
// A static event rather than a UnityEvent because mobs are spawned at runtime,
// so there's no Inspector slot to wire - same reasoning as Enemy.Active.
public static class MobKills
{
    public static event Action Killed;

    public static void Report() => Killed?.Invoke();
}
