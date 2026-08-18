using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int enemiesPerWave = 5;
    public float spawnInterval = 0.5f; // delay between each enemy in a wave
    public float waveInterval = 4f;    // delay between waves
    public float spawnWidth = 6f;      // horizontal spread of spawn points

    void Start()
    {
        InvokeRepeating(nameof(SpawnWave), 1f, waveInterval);
    }

    void SpawnWave()
    {
        StartCoroutine(SpawnWaveRoutine());
    }

    System.Collections.IEnumerator SpawnWaveRoutine()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            float x = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
            Vector3 spawnPos = new Vector3(x, transform.position.y, 0);
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

}
