using System.Collections.Generic;
using UnityEngine;

// Lives on the Boss GameObject itself (not a separate referenced object like
// EnemySpawner) - gets a free GetComponent<Boss>() reference in Awake with
// no Inspector wiring, and is destroyed automatically the instant
// Boss.Die() destroys the GameObject, so no explicit spawner cleanup is
// needed either.
public class MinionSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject minionPrefab;
    public int maxConcurrentMinions = 2;
    public float spawnInterval = 6f;
    public float spawnRadius = 2f;

    private Boss boss;
    private float nextSpawnTime;
    private bool spawnedLeftLast; // alternate flank sides so minions read as symmetric

    void Awake()
    {
        boss = GetComponent<Boss>();
        if (boss != null) boss.OnDefeated.AddListener(DestroyAllMinions);
    }

    void Update()
    {
        if (minionPrefab == null || boss == null) return;
        if (Minion.Active.Count >= maxConcurrentMinions) return;
        if (Time.time < nextSpawnTime) return;

        nextSpawnTime = Time.time + spawnInterval;
        SpawnMinion();
    }

    void SpawnMinion()
    {
        spawnedLeftLast = !spawnedLeftLast;
        Vector2 offset = new Vector2(spawnedLeftLast ? -spawnRadius : spawnRadius, 0f);

        GameObject minionObj = Instantiate(minionPrefab, (Vector2)boss.transform.position + offset, Quaternion.identity);
        Minion minion = minionObj.GetComponent<Minion>();
        if (minion != null) minion.Init(boss, offset);
    }

    // Wired to Boss.OnDefeated in Awake - no stray minions survive into the
    // Victory panel once the boss they flank is gone.
    void DestroyAllMinions()
    {
        List<Minion> toDestroy = new List<Minion>(Minion.Active);
        foreach (Minion m in toDestroy)
        {
            if (m != null) Destroy(m.gameObject);
        }
    }
}
