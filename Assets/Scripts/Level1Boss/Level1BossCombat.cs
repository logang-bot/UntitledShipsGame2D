using UnityEngine;

// The boss's own fire/bullet-spawn logic - split out purely to keep
// Level1Boss.cs under this project's per-file line budget. Same
// composition pattern as Level1BossMovement/Shockwave/Attacks/Aggro: a
// plain instantiated helper, not a MonoBehaviour of its own.
public class Level1BossCombat
{
    readonly Level1Boss boss;

    public Level1BossCombat(Level1Boss boss)
    {
        this.boss = boss;
    }

    public void Fire()
    {
        if (boss.bulletPrefab == null || boss.CurrentTarget == null) return;

        Vector2 dir = (boss.CurrentTarget.transform.position - boss.transform.position).normalized;
        SpawnBullet(dir);
        if (boss.IsPhase2) SpawnPhase2SpreadBullets(dir);
    }

    void SpawnPhase2SpreadBullets(Vector2 dir)
    {
        SpawnBullet(Quaternion.Euler(0, 0, boss.spreadAngle) * dir);
        SpawnBullet(Quaternion.Euler(0, 0, -boss.spreadAngle) * dir);
    }

    public void SpawnBullet(Vector2 dir)
    {
        GameObject bulletObj = Object.Instantiate(boss.bulletPrefab, boss.transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = boss.bulletDamage;
        b.Init(dir, boss.bulletSpeed, "Enemy");
    }
}
