using System.Collections.Generic;
using UnityEngine;

// Aggro/threat-table, contact-damage cooldowns, and damage-dealt bookkeeping
// for Level1Boss - split out purely to keep Level1Boss.cs under this
// project's per-file line budget. Same composition pattern as
// Level1BossMovement/Shockwave/Attacks: a plain instantiated helper, not a
// MonoBehaviour of its own.
public class Level1BossAggro
{
    readonly Level1Boss boss;
    readonly Dictionary<GameObject, float> aggro = new Dictionary<GameObject, float>();
    readonly Dictionary<GameObject, float> lastContactDamageTime = new Dictionary<GameObject, float>();
    // Raw cumulative damage per source, deliberately kept separate from
    // `aggro`: TauntedBy() overwrites a taunter's aggro with
    // (highest + tauntBonus), which would corrupt these numbers the moment
    // Tank pressed E. Aggro is a threat value; this is a damage stat.
    readonly Dictionary<GameObject, float> damageDealt = new Dictionary<GameObject, float>();
    float combatStartTime;

    public Level1BossAggro(Level1Boss boss)
    {
        this.boss = boss;
    }

    public GameObject InitialTarget()
    {
        foreach (GameObject t in boss.targets)
        {
            if (t == null) continue;
            aggro[t] = 0f;
            damageDealt[t] = 0f;
        }
        return boss.targets.Length > 0 ? boss.targets[0] : null;
    }

    public GameObject PickTarget(GameObject currentTarget)
    {
        if (currentTarget == null || !aggro.TryGetValue(currentTarget, out float bestAggro)) bestAggro = -1f;

        GameObject best = currentTarget;
        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            if (!aggro.TryGetValue(t, out float candidateAggro)) continue;
            if (candidateAggro > bestAggro) { bestAggro = candidateAggro; best = t; }
        }
        return best;
    }

    public void RegisterDamage(float amount, GameObject source)
    {
        if (source == null) return;
        if (aggro.ContainsKey(source)) aggro[source] += amount;
        // TryGetValue rather than ContainsKey-gating, so damage from a
        // source that never made it into targets[] still shows up on the
        // meter instead of silently vanishing.
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

    public void ApplyContactDamage(GameObject ship, float damageAmount, float cooldown)
    {
        if (ship == null) return;
        PlayerHealth health = ship.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (lastContactDamageTime.TryGetValue(ship, out float t) && Time.time - t < cooldown) return;

        lastContactDamageTime[ship] = Time.time;
        health.TakeDamage(Mathf.RoundToInt(damageAmount));
    }

    // LevelSequencer enables Level1Boss exactly when BossCombat starts,
    // which is the only moment ships can both act and damage the boss - so
    // it's the correct zero for the DPS denominator. Guarded so a re-enable
    // can't restart the fight clock mid-run.
    public void MarkCombatStarted()
    {
        if (combatStartTime <= 0f) combatStartTime = Time.time;
    }

    // Seconds of real boss combat so far - the DPS denominator. Stays 0
    // until the fight actually starts, so DpsMeterUI can avoid dividing by
    // an elapsed time that hasn't begun.
    public float CombatElapsed => combatStartTime > 0f ? Time.time - combatStartTime : 0f;

    // Total damage this source has dealt to the boss. Read by DpsMeterUI
    // per-ship rather than exposing the dictionary, so nothing outside can
    // mutate it and iterating costs no allocation.
    public float GetDamageDealt(GameObject source)
    {
        if (source == null) return 0f;
        damageDealt.TryGetValue(source, out float dealt);
        return dealt;
    }
}
