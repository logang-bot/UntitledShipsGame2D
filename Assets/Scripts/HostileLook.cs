using UnityEngine;

// The Combat FX sprites of a wave enemy or a minion: which hull it shows
// (per movement pattern, standard or explosive) and the burst it leaves
// when it dies. Enemy and Minion stay art-agnostic and only report type.
[RequireComponent(typeof(SpriteRenderer))]
public class HostileLook : MonoBehaviour
{
    // Index = Enemy.MovementPattern; minions use index 0.
    public Sprite[] standardSprites;
    public Sprite[] explosiveSprites;
    public GameObject deathFxPrefab;
    public GameObject explosiveDeathFxPrefab;

    public void Apply(int variant, bool explosive)
    {
        Sprite[] set = explosive ? explosiveSprites : standardSprites;
        if (set == null || set.Length == 0) return;
        GetComponent<SpriteRenderer>().sprite = set[Mathf.Clamp(variant, 0, set.Length - 1)];
    }

    public void SpawnDeath(bool explosive)
    {
        GameObject prefab = explosive ? explosiveDeathFxPrefab : deathFxPrefab;
        if (prefab != null) Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
