using System.Collections.Generic;
using UnityEngine;

// Lives on the MarauderBoss GameObject itself (not a separate referenced object like
// EnemySpawner) - gets a free GetComponent<MarauderBoss>() reference in Awake with
// no Inspector wiring, and is destroyed automatically the instant
// MarauderBoss.Die() destroys the GameObject, so no explicit spawner cleanup is
// needed either.
public class MinionSpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject minionPrefab;
    public int maxConcurrentMinions = 2;
    public float spawnInterval = 6f;
    public float spawnRadius = 2f;
    // Placeholder/tunable like every other balance value in this project -
    // rolled independently per spawn, so the mix isn't fixed to a slot.
    [Range(0f, 1f)] public float explosiveMinionChance = 0.3f;

    private MarauderBoss boss;
    private float nextSpawnTime;
    private bool spawnedLeftLast; // alternate flank sides so minions read as symmetric

    void Awake()
    {
        boss = GetComponent<MarauderBoss>();
        if (boss != null) boss.OnDefeated.AddListener(DestroyAllMinions);
    }

    // Fires when MarauderBoss.OnEnable() flips this component on, right as the
    // boss's entrance finishes - spawns both flank minions together so they
    // read as arriving "with" the boss, instead of one immediately and the
    // other up to spawnInterval seconds later with no warning (SpawnMinion()
    // alternates sides each call, so two calls back-to-back covers both).
    // Update()'s own timed spawning then takes over for later reinforcements.
    void OnEnable()
    {
        if (minionPrefab == null || boss == null) return;

        SpawnMinion();
        SpawnMinion();
        nextSpawnTime = Time.time + spawnInterval;
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
        if (minion != null)
        {
            Minion.MinionType type = Random.value < explosiveMinionChance ? Minion.MinionType.Explosive : Minion.MinionType.Standard;
            minion.Init(boss, offset, type);
        }
    }

    // Wired to MarauderBoss.OnDefeated in Awake - no stray minions survive into the
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
