using System.Collections.Generic;
using UnityEngine;

// Raw cumulative damage each ship has dealt to one boss, plus the fight
// clock - the data behind the damage meter and each party frame's DPS line.
// Every boss owns one (IBoss.DamageLedger). Deliberately kept apart from
// Marauder's aggro table: a taunt overwrites aggro, which must never inflate
// a damage stat. A plain helper class, same shape as MarauderBossAggro, and
// it outlives its boss: the HUD keeps reading the frozen totals after the
// boss GameObject is destroyed.
public class BossDamageLedger
{
    private readonly Dictionary<GameObject, float> damageDealt = new Dictionary<GameObject, float>();
    private float combatStartTime = -1f;
    private float combatEndTime = -1f;

    public bool CombatStarted => combatStartTime >= 0f;
    public bool CombatEnded => combatEndTime >= 0f;
    public float CombatElapsed => ElapsedAt(Time.time);

    public void BeginCombat() => BeginCombatAt(Time.time);

    public void EndCombat() => EndCombatAt(Time.time);

    public void BeginCombatAt(float now)
    {
        if (!CombatStarted) combatStartTime = now;
    }

    public void EndCombatAt(float now)
    {
        if (CombatStarted && !CombatEnded) combatEndTime = now;
    }

    public float ElapsedAt(float now)
    {
        if (!CombatStarted) return 0f;
        float end = CombatEnded ? combatEndTime : now;
        return Mathf.Max(0f, end - combatStartTime);
    }

    public void Record(GameObject source, float amount)
    {
        if (source == null) return;
        damageDealt.TryGetValue(source, out float dealtSoFar);
        damageDealt[source] = dealtSoFar + amount;
    }

    public float GetDamageDealt(GameObject source)
    {
        if (source == null) return 0f;
        damageDealt.TryGetValue(source, out float dealt);
        return dealt;
    }

    public float Dps(GameObject source) => DpsAt(source, Time.time);

    public float DpsAt(GameObject source, float now)
    {
        float elapsed = ElapsedAt(now);
        return elapsed > 0f ? GetDamageDealt(source) / elapsed : 0f;
    }
}
