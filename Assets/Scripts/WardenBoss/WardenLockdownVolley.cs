using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sibling component: Warden's signature mechanic. Every lockdownCooldown, a
// wide wall of parallel bullets sweeps in from a random arena edge - built to
// be blocked by Tank's Shield Arc width, not dodged. See
// docs/superpowers/specs/2026-09-04-warden-boss-design.md.
public class WardenLockdownVolley : MonoBehaviour
{
    public enum Edge { Top, Left, Right }

    [Header("Timing")]
    public float lockdownCooldown = 9f;
    public float lockdownCooldownPhase2 = 6f;
    public float telegraphTime = 1f;

    [Header("Wall")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    public int wallBulletCount = 12;
    public int wallGapCount = 2;
    public float spawnMargin = 1f; // how far outside the viewport edge bullets spawn

    public float CooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    public bool IsTelegraphing { get; private set; }
    public Edge? IncomingEdge { get; private set; }

    private WardenBoss boss;
    private Camera cam;
    private float nextFireTime;

    void Awake()
    {
        boss = GetComponent<WardenBoss>();
        cam = Camera.main;
    }

    void OnEnable()
    {
        nextFireTime = Time.time + CurrentCooldown();
    }

    void Update()
    {
        if (Time.time >= nextFireTime) StartCoroutine(VolleyRoutine());
    }

    private float CurrentCooldown()
    {
        return boss != null && boss.IsPhase2 ? lockdownCooldownPhase2 : lockdownCooldown;
    }

    private IEnumerator VolleyRoutine()
    {
        nextFireTime = Time.time + CurrentCooldown();
        Edge edge = (Edge)Random.Range(0, 3);
        IncomingEdge = edge;
        IsTelegraphing = true;
        yield return new WaitForSeconds(telegraphTime);
        IsTelegraphing = false;
        FireWall(edge);
        IncomingEdge = null;
    }

    private void FireWall(Edge edge)
    {
        List<int> gapIndices = PickGapIndices(wallBulletCount, wallGapCount);
        Vector2 direction = DirectionFor(edge);
        for (int i = 0; i < wallBulletCount; i++)
        {
            if (gapIndices.Contains(i)) continue;
            SpawnBullet(PositionAlongEdge(edge, i), direction);
        }
    }

    /// <summary>Left/Right walls travel horizontally inward; Top travels down - all reuse Bullet.Init's existing arbitrary-direction path.</summary>
    public static Vector2 DirectionFor(Edge edge)
    {
        switch (edge)
        {
            case Edge.Left: return Vector2.right;
            case Edge.Right: return Vector2.left;
            default: return Vector2.down;
        }
    }

    /// <summary>Evenly distributes gapCount single-bullet-width gaps across the row so the wall isn't fully unbroken.</summary>
    public static List<int> PickGapIndices(int bulletCount, int gapCount)
    {
        List<int> gaps = new List<int>();
        if (gapCount <= 0 || bulletCount <= 0) return gaps;
        float step = bulletCount / (float)(gapCount + 1);
        for (int i = 1; i <= gapCount; i++)
            gaps.Add(Mathf.Clamp(Mathf.RoundToInt(step * i), 0, bulletCount - 1));
        return gaps;
    }

    private Vector3 PositionAlongEdge(Edge edge, int index)
    {
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        float t = (index + 0.5f) / wallBulletCount;
        if (edge == Edge.Top) return new Vector3(Mathf.Lerp(min.x, max.x, t), max.y + spawnMargin, 0f);
        float y = Mathf.Lerp(min.y, max.y, t);
        return edge == Edge.Left ? new Vector3(min.x - spawnMargin, y, 0f) : new Vector3(max.x + spawnMargin, y, 0f);
    }

    private void SpawnBullet(Vector3 pos, Vector2 dir)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, pos, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.damage = boss.bulletDamage;
        b.Init(dir, bulletSpeed, "Enemy");
    }
}
