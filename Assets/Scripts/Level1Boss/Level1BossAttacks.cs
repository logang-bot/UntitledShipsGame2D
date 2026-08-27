using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level1BossAttacks
{
    public enum BulletPattern { Fan, Ring, Spiral }

    private readonly Level1Boss boss;
    private float nextGuidedMissileTime;
    private float nextPatternBarrageTime;
    private BulletPattern? lastPattern;

    public Level1BossAttacks(Level1Boss boss)
    {
        this.boss = boss;
    }

    public PlayerRole? GuidedMissileTargetRole { get; private set; }
    public BulletPattern? PatternBarrageActivePattern { get; private set; }
    public float GuidedMissileCooldownRemaining => Mathf.Max(0f, nextGuidedMissileTime - Time.time);
    public float PatternBarrageCooldownRemaining => Mathf.Max(0f, nextPatternBarrageTime - Time.time);

    public void ResetCooldowns(float until)
    {
        nextGuidedMissileTime = until;
        nextPatternBarrageTime = until;
    }

    public void CheckGuidedMissile()
    {
        if (Time.time < nextGuidedMissileTime) return;

        List<GameObject> eligible = GetGuidedMissileEligibleTargets();
        if (eligible.Count == 0) return;

        nextGuidedMissileTime = Time.time + boss.guidedMissileInterval;
        GameObject chosen = eligible[Random.Range(0, eligible.Count)];
        boss.StartCoroutine(GuidedMissileRoutine(chosen));
    }

    private List<GameObject> GetGuidedMissileEligibleTargets()
    {
        List<GameObject> eligible = new List<GameObject>();
        foreach (GameObject t in boss.targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            PlayerRoleComponent roleComponent = t.GetComponent<PlayerRoleComponent>();
            if (roleComponent == null) continue;
            foreach (PlayerRole candidateRole in boss.guidedMissileTargetRoles)
            {
                if (roleComponent.role == candidateRole)
                {
                    eligible.Add(t);
                    break;
                }
            }
        }
        return eligible;
    }

    private IEnumerator GuidedMissileRoutine(GameObject target)
    {
        PlayerRoleComponent targetRole = target != null ? target.GetComponent<PlayerRoleComponent>() : null;
        GuidedMissileTargetRole = targetRole != null ? (PlayerRole?)targetRole.role : null;

        yield return new WaitForSeconds(boss.guidedMissileTelegraphTime);

        if (target != null && target.activeInHierarchy && boss.bulletPrefab != null)
        {
            SpawnGuidedMissile(target.transform);
        }

        yield return new WaitForSeconds(boss.guidedMissileWarningLingerTime);
        GuidedMissileTargetRole = null;
    }

    private void SpawnGuidedMissile(Transform target)
    {
        GameObject bulletObj = Object.Instantiate(boss.bulletPrefab, boss.transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = boss.bulletDamage;
        b.InitHoming(target, boss.guidedMissileTurnRate, boss.guidedMissileSpeed, "Enemy");
    }

    public void CheckPatternBarrage()
    {
        if (Time.time < nextPatternBarrageTime) return;
        if (boss.CurrentTarget == null) return;

        nextPatternBarrageTime = Time.time + boss.patternBarrageCooldown;
        boss.StartCoroutine(PatternBarrageRoutine());
    }

    /// <summary>
    /// Random pick, excluding whichever shape fired last time, so the same
    /// shape can never fire twice in a row while still keeping the surprise
    /// of not knowing which of the other two is coming.
    /// </summary>
    private BulletPattern PickPattern()
    {
        BulletPattern[] all = { BulletPattern.Fan, BulletPattern.Ring, BulletPattern.Spiral };
        BulletPattern picked = all[Random.Range(0, all.Length)];
        if (lastPattern.HasValue && picked == lastPattern.Value)
            picked = all[(System.Array.IndexOf(all, picked) + 1) % all.Length];
        return picked;
    }

    private IEnumerator PatternBarrageRoutine()
    {
        BulletPattern pattern = PickPattern();
        lastPattern = pattern;
        PatternBarrageActivePattern = pattern;

        yield return new WaitForSeconds(boss.patternBarrageTelegraphTime);

        Vector2 aimDir = ComputeAimDirection();

        switch (pattern)
        {
            case BulletPattern.Fan:
                FireFan(aimDir);
                break;
            case BulletPattern.Ring:
                FireRing();
                break;
            case BulletPattern.Spiral:
                yield return boss.StartCoroutine(FireSpiralRoutine(aimDir));
                break;
        }

        PatternBarrageActivePattern = null;
    }

    /// <summary>
    /// Re-aims after the telegraph wait, not at activation time - the target
    /// may have moved (or died) during the wind-up, same re-check-after-
    /// telegraph idiom Level1BossShockwave's ShockwaveRoutine uses.
    /// </summary>
    private Vector2 ComputeAimDirection()
    {
        return boss.CurrentTarget != null
            ? ((Vector2)boss.CurrentTarget.transform.position - (Vector2)boss.transform.position).normalized
            : Vector2.down;
    }

    private void FireFan(Vector2 aimDir)
    {
        if (boss.bulletPrefab == null) return;
        if (boss.fanBulletCount <= 1) { boss.SpawnBullet(aimDir); return; }

        float step = boss.fanSpreadAngle / (boss.fanBulletCount - 1);
        float startAngle = -boss.fanSpreadAngle / 2f;
        for (int i = 0; i < boss.fanBulletCount; i++)
        {
            float angle = startAngle + step * i;
            boss.SpawnBullet((Vector2)(Quaternion.Euler(0, 0, angle) * aimDir));
        }
    }

    /// <summary>
    /// Omnidirectional by definition - the boss never rotates, so there's no
    /// "facing" to aim relative to. A randomized start-angle offset per burst
    /// keeps the gaps between bullets from always landing in the same screen
    /// position (would otherwise create a permanent memorized safe lane).
    /// </summary>
    private void FireRing()
    {
        if (boss.bulletPrefab == null || boss.ringBulletCount <= 0) return;

        float step = 360f / boss.ringBulletCount;
        float startOffset = Random.Range(0f, step);
        for (int i = 0; i < boss.ringBulletCount; i++)
        {
            float angle = startOffset + step * i;
            boss.SpawnBullet((Vector2)(Quaternion.Euler(0, 0, angle) * Vector2.up));
        }
    }

    /// <summary>
    /// Starts aimed at the target like Fan (first shot reads as "aimed at
    /// you"), then sweeps by spiralAngleStep per shot fired rapidly over
    /// time - the one shape that actually delivers "rapid-fire", since
    /// Fan/Ring resolve in a single frame.
    /// </summary>
    private IEnumerator FireSpiralRoutine(Vector2 aimDir)
    {
        if (boss.bulletPrefab == null) yield break;

        float angle = 0f;
        for (int i = 0; i < boss.spiralBulletCount; i++)
        {
            boss.SpawnBullet((Vector2)(Quaternion.Euler(0, 0, angle) * aimDir));
            angle += boss.spiralAngleStep;
            yield return new WaitForSeconds(boss.spiralShotInterval);
        }
    }
}
