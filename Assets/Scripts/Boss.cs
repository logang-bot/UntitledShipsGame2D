using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Boss : MonoBehaviour
{
    [Header("Health / Phases")]
    public int maxHealth = 90; // was 60 - x1.5'd to give the fixed-stats/ability rework enough runway to observe
    public UnityEvent OnPhase2;
    public UnityEvent OnDefeated;

    [Header("Movement")]
    public float sineAmplitude = 2f;
    public float sineFrequency = 0.5f;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public float phase1FireInterval = 1.2f;
    public float phase2FireInterval = 0.6f;
    public float bulletSpeed = 6f;
    public float spreadAngle = 15f; // phase 2 side-bullet offset

    [Header("Aggro / Targets")]
    public GameObject[] targets; // drag Player + 3 Teammates
    public float tauntBonus = 100f;

    [Header("Scene hookup")]
    public GameObject enemySpawner; // drag Spawner; auto-disabled on Awake so wave enemies don't confound the boss fight

    public int CurrentHealth { get; private set; }
    public bool IsPhase2 { get; private set; }
    public GameObject CurrentTarget { get; private set; }

    private readonly Dictionary<GameObject, float> aggro = new Dictionary<GameObject, float>();
    private float startX;
    private float nextFireTime;

    void Awake()
    {
        CurrentHealth = maxHealth;
        startX = transform.position.x;

        foreach (GameObject t in targets)
        {
            if (t != null) aggro[t] = 0f;
        }
        CurrentTarget = targets.Length > 0 ? targets[0] : null;

        if (enemySpawner != null) enemySpawner.SetActive(false);
    }

    void Update()
    {
        float newX = startX + Mathf.Sin(Time.time * sineFrequency) * sineAmplitude;
        transform.position = new Vector3(newX, transform.position.y, 0);

        PickTarget();

        float interval = IsPhase2 ? phase2FireInterval : phase1FireInterval;
        if (CurrentTarget != null && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + interval;
            Fire();
        }
    }

void PickTarget()
    {
        float bestAggro;
        if (CurrentTarget == null || !aggro.TryGetValue(CurrentTarget, out bestAggro)) bestAggro = -1f;

        foreach (GameObject t in targets)
        {
            if (t == null || !t.activeInHierarchy) continue;
            float candidateAggro;
            if (!aggro.TryGetValue(t, out candidateAggro)) continue;
            if (candidateAggro > bestAggro)
            {
                bestAggro = candidateAggro;
                CurrentTarget = t;
            }
        }
    }

    void Fire()
    {
        if (bulletPrefab == null || CurrentTarget == null) return;

        Vector2 dir = (CurrentTarget.transform.position - transform.position).normalized;
        SpawnBullet(dir);

        if (IsPhase2)
        {
            SpawnBullet(Quaternion.Euler(0, 0, spreadAngle) * dir);
            SpawnBullet(Quaternion.Euler(0, 0, -spreadAngle) * dir);
        }
    }

    void SpawnBullet(Vector2 dir)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet b = bulletObj.GetComponent<Bullet>();
        b.Init(dir, bulletSpeed, "Enemy");
    }

    public void TakeDamage(float amount, GameObject source)
    {
        CurrentHealth -= Mathf.RoundToInt(amount);
        if (source != null && aggro.ContainsKey(source)) aggro[source] += amount;

        if (!IsPhase2 && CurrentHealth <= maxHealth / 2)
        {
            IsPhase2 = true;
            OnPhase2?.Invoke();
        }

        if (CurrentHealth <= 0) Die();
    }

    public void TauntedBy(GameObject taunter)
    {
        if (!aggro.ContainsKey(taunter)) return;

        float highest = 0f;
        foreach (KeyValuePair<GameObject, float> kv in aggro) highest = Mathf.Max(highest, kv.Value);
        aggro[taunter] = highest + tauntBonus;
    }

    void Die()
    {
        OnDefeated?.Invoke();
        Destroy(gameObject);
    }
}
