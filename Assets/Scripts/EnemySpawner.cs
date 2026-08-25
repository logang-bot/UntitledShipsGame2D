using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // One system, several shapes, same idiom as Level1Boss.cs's Pattern Barrage - the shape is
    // picked at random each wave (no no-repeat guard, unlike Pattern Barrage - simplest change
    // that satisfies "random order" here, since this is the only caller of formationOrder today).
    public enum WaveFormation { Random, Line, Cluster, VFormation }

    public GameObject enemyPrefab;
    public int enemiesPerWave = 5;
    public float spawnInterval = 0.5f; // delay between each enemy in a wave
    public float waveInterval = 4f;    // delay between waves
    public float spawnWidth = 6f;      // horizontal spread of spawn points

    [Header("Wave formation (picked at random each wave)")]
    public WaveFormation[] formationOrder = { WaveFormation.Random, WaveFormation.Line, WaveFormation.Cluster, WaveFormation.VFormation };
    public float clusterJitter = 0.5f;   // +/- random offset from the cluster's center X
    public float vFormationYStep = 0.4f; // Y offset per position from center, builds the V shape

    // Externally controlled by LevelSequencer - doesn't self-start, so the
    // minion-phase timing (pre-boss, then again after phase 2) is entirely
    // owned by the sequencer rather than this component's own Start().
    public void StartSpawning()
    {
        CancelInvoke(nameof(SpawnWave));
        InvokeRepeating(nameof(SpawnWave), 0f, waveInterval);
    }

    // Stops scheduling new waves; a wave already in flight (SpawnWaveRoutine)
    // finishes naturally so a formation doesn't spawn half its enemies.
    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnWave));
    }

    void SpawnWave()
    {
        StartCoroutine(SpawnWaveRoutine());
    }

    System.Collections.IEnumerator SpawnWaveRoutine()
    {
        WaveFormation formation = formationOrder[Random.Range(0, formationOrder.Length)];
        Enemy.MovementPattern movementPattern = MovementPatternFor(formation);
        // Rolled once per wave, not per enemy, so every enemy in a Cluster wave jitters around the
        // same shared center instead of each picking its own independent center.
        float clusterCenterX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);

        for (int i = 0; i < enemiesPerWave; i++)
        {
            (float x, float yOffset) = PositionFor(formation, i, clusterCenterX);
            Vector3 spawnPos = new Vector3(x, transform.position.y + yOffset, 0);
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemyObj.GetComponent<Enemy>().movementPattern = movementPattern;

            // Line reads clearly as a line only if it spawns near-simultaneously; every other
            // formation keeps the original stagger.
            if (formation != WaveFormation.Line) yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Formation -> movement pattern pairing lives in one place so SpawnWaveRoutine() doesn't
    // duplicate the mapping.
    Enemy.MovementPattern MovementPatternFor(WaveFormation formation)
    {
        switch (formation)
        {
            case WaveFormation.Cluster: return Enemy.MovementPattern.ZigZag;
            case WaveFormation.VFormation: return Enemy.MovementPattern.StraightDive;
            default: return Enemy.MovementPattern.SineWave; // Random, Line
        }
    }

    // Returns (x, yOffset) for the i-th enemy in the current wave under the given formation.
    // clusterCenterX is only used by the Cluster case, rolled once per wave by the caller.
    (float, float) PositionFor(WaveFormation formation, int i, float clusterCenterX)
    {
        switch (formation)
        {
            case WaveFormation.Line:
            {
                float x = enemiesPerWave <= 1
                    ? 0f
                    : -spawnWidth / 2f + i * (spawnWidth / (enemiesPerWave - 1));
                return (x, 0f);
            }
            case WaveFormation.Cluster:
                return (clusterCenterX + Random.Range(-clusterJitter, clusterJitter), 0f);
            case WaveFormation.VFormation:
            {
                float mid = (enemiesPerWave - 1) / 2f;
                float distFromCenter = Mathf.Abs(i - mid);
                float x = (i - mid) * (spawnWidth / Mathf.Max(1, enemiesPerWave - 1));
                return (x, distFromCenter * vFormationYStep);
            }
            default: // Random
                return (Random.Range(-spawnWidth / 2f, spawnWidth / 2f), 0f);
        }
    }
}
