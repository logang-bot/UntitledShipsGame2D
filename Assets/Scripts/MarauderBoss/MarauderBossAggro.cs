using System.Collections.Generic;
using UnityEngine;

// Threat table, split out of MarauderBoss.cs to keep that file under this
// project's file-size cap. A plain helper class (not a MonoBehaviour), same
// shape as MarauderBossMovement/Shockwave/Attacks. Raw damage per ship lives
// in the boss's BossDamageLedger instead: TauntedBy() overwrites a taunter's
// aggro, which must never corrupt a damage stat.
public class MarauderBossAggro
{
    private readonly MarauderBoss boss;
    private readonly Dictionary<GameObject, float> aggro = new Dictionary<GameObject, float>();
    private GameObject heldTarget;
    private float heldUntil = float.NegativeInfinity;

    public GameObject CurrentTarget { get; private set; }

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
        }
        CurrentTarget = boss.targets.Length > 0 ? boss.targets[0] : null;
    }

    public void PickTarget()
    {
        CurrentTarget = IsHeld ? heldTarget : FindHighestAggroTarget();
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

    public void RegisterDamage(float amount, GameObject source)
    {
        if (source != null && aggro.ContainsKey(source)) aggro[source] += amount;
    }

    /// <summary>
    /// Tops the taunter's aggro, and holds the boss on it for the taunt's
    /// seconds even if someone out-damages it meanwhile - the Tank's magnet
    /// shows that long, so the boss must not be shooting someone else.
    /// </summary>
    public void TauntedBy(GameObject taunter, float seconds)
    {
        if (!aggro.ContainsKey(taunter)) return;

        float highest = 0f;
        foreach (KeyValuePair<GameObject, float> kv in aggro) highest = Mathf.Max(highest, kv.Value);
        aggro[taunter] = highest + boss.tauntBonus;
        heldTarget = taunter;
        heldUntil = Time.time + seconds;
    }

    private bool IsHeld => heldTarget != null && heldTarget.activeInHierarchy && Time.time < heldUntil;
}
