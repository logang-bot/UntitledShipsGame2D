using System.Collections.Generic;
using UnityEngine;

// Threat table + cumulative damage tracking, split out of MarauderBoss.cs to
// keep that file under this project's file-size cap. A plain helper class
// (not a MonoBehaviour), same shape as MarauderBossMovement/Shockwave/Attacks.
public class MarauderBossAggro
{
    private readonly MarauderBoss boss;
    private readonly Dictionary<GameObject, float> aggro = new Dictionary<GameObject, float>();
    // Raw cumulative damage per source, deliberately kept separate from
    // `aggro`: TauntedBy() overwrites a taunter's aggro with
    // (highest + tauntBonus), which would corrupt these numbers the moment
    // Tank pressed E. Aggro is a threat value; this is a damage stat.
    private readonly Dictionary<GameObject, float> damageDealt = new Dictionary<GameObject, float>();
    private float combatStartTime;

    public GameObject CurrentTarget { get; private set; }

    // Seconds of real boss combat so far - the DPS denominator. Stays 0
    // until the fight actually starts, so DpsMeterUI can avoid dividing by
    // an elapsed time that hasn't begun.
    public float CombatElapsed => combatStartTime > 0f ? Time.time - combatStartTime : 0f;

    public MarauderBossAggro(MarauderBoss boss)
    {
        this.boss = boss;
    }

    public void Init()
    {
        foreach (GameObject t in boss.targets)
        {
            if (t == null) continue;
            aggro[t] = 0f;
            damageDealt[t] = 0f;
        }
        CurrentTarget = boss.targets.Length > 0 ? boss.targets[0] : null;
    }

    // LevelSequencer enables MarauderBoss exactly when BossCombat starts,
    // which is the only moment ships can both act and damage the boss - so
    // it's the correct zero for CombatElapsed. Guarded so a re-enable can't
    // restart the fight clock mid-run.
    public void StartCombatClock()
    {
        if (combatStartTime <= 0f) combatStartTime = Time.time;
    }

    public void PickTarget()
    {
        CurrentTarget = FindHighestAggroTarget();
    }

    private GameObject FindHighestAggroTarget()
    {
        float bestAggro;
        if (CurrentTarget == null || !aggro.TryGetValue(CurrentTarget, out bestAggro)) bestAggro = -1f;

        GameObject best = CurrentTarget;
        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            float candidateAggro;
            if (!aggro.TryGetValue(t, out candidateAggro)) continue;
            if (candidateAggro > bestAggro)
            {
                bestAggro = candidateAggro;
                best = t;
            }
        }
        return best;
    }

    // TryGetValue rather than ContainsKey-gating, so damage from a source
    // that never made it into targets[] still shows up on the meter instead
    // of silently vanishing.
    public void RegisterDamage(float amount, GameObject source)
    {
        if (source == null) return;
        if (aggro.ContainsKey(source)) aggro[source] += amount;
        damageDealt.TryGetValue(source, out float dealtSoFar);
        damageDealt[source] = dealtSoFar + amount;
    }

    public void TauntedBy(GameObject taunter)
    {
        if (!aggro.ContainsKey(taunter)) return;

        float highest = 0f;
        foreach (KeyValuePair<GameObject, float> kv in aggro) highest = Mathf.Max(highest, kv.Value);
        aggro[taunter] = highest + boss.tauntBonus;
    }

    // Total damage this source has dealt to the boss. A method rather than
    // an exposed dictionary, so nothing outside can mutate it and per-ship
    // iteration allocates no enumerator.
    public float GetDamageDealt(GameObject source)
    {
        if (source == null) return 0f;
        damageDealt.TryGetValue(source, out float dealt);
        return dealt;
    }
}
