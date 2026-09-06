using Nakama;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Client-only: applies the host's broadcast PartyStateSnapshot directly
// onto this scene's ships/boss/bullets, bypassing local gameplay logic
// entirely - safe because NetworkPartySpawner disables every ship's
// PlayerController on a non-host client and Level1Boss.enabled never turns
// on there (see LevelSequencer), so nothing else is fighting these writes.
// Ship position is lerped for smoothness; health/shield/boss state are
// plain overwrites. Bullets are reconciled declaratively each tick: spawn a
// visual-only instance for an id not seen before, drop one no longer
// present. Lives disabled in Gameplay.unity; NetworkPartySpawner
// Initialize()s and enables it only on a non-host client.
public class RemoteStateReceiver : MonoBehaviour
{
    public float positionLerpSpeed = 12f;
    // Both stripped of Bullet/Collider2D right after spawn, see SpawnBulletVisual()
    public GameObject playerBulletVisualPrefab; // PlayerBullet.prefab
    public GameObject enemyBulletVisualPrefab; // EnemyBullet.prefab
    public Level1Boss boss;

    readonly Dictionary<string, GameObject> shipsById = new Dictionary<string, GameObject>();
    readonly Dictionary<string, Vector2> targetPositions = new Dictionary<string, Vector2>();
    readonly Dictionary<long, GameObject> bulletVisuals = new Dictionary<long, GameObject>();
    readonly HashSet<long> bulletIdsThisTick = new HashSet<long>();

    public void Initialize(List<NetworkShipEntry> roster)
    {
        shipsById.Clear();
        foreach (NetworkShipEntry entry in roster) shipsById[entry.id] = entry.ship;
        NakamaConnection.Instance.Socket.ReceivedMatchState += HandleMatchState;
        enabled = true;
    }

    void OnDisable()
    {
        if (NakamaConnection.Instance != null)
            NakamaConnection.Instance.Socket.ReceivedMatchState -= HandleMatchState;
    }

    void HandleMatchState(IMatchState state)
    {
        if (state.OpCode != NetworkOpCodes.StateSnapshot) return;
        PartyStateSnapshot snapshot = PartyStateSnapshot.FromJson(Encoding.UTF8.GetString(state.State));
        foreach (ShipStateSnapshot ship in snapshot.ships) ApplyShipSnapshot(ship);
        ApplyBossSnapshot(snapshot.boss);
        ApplyBulletSnapshots(snapshot.bullets);
    }

    void ApplyShipSnapshot(ShipStateSnapshot ship)
    {
        if (!shipsById.TryGetValue(ship.id, out GameObject shipGO)) return;
        targetPositions[ship.id] = ship.position;
        shipGO.GetComponent<PlayerHealth>().ApplySyncedState(ship.health, ship.shield);
    }

    void ApplyBossSnapshot(BossStateSnapshot snapshot)
    {
        if (boss == null) return;
        if (!snapshot.active) { boss.Die(); return; }
        shipsById.TryGetValue(snapshot.currentTargetId, out GameObject target);
        boss.ApplySyncedState(snapshot.health, snapshot.phase2, target);
    }

    void ApplyBulletSnapshots(List<BulletStateSnapshot> bullets)
    {
        bulletIdsThisTick.Clear();
        foreach (BulletStateSnapshot bullet in bullets) ApplyBulletSnapshot(bullet);
        RemoveStaleBulletVisuals();
    }

    void ApplyBulletSnapshot(BulletStateSnapshot bullet)
    {
        bulletIdsThisTick.Add(bullet.id);
        if (!bulletVisuals.TryGetValue(bullet.id, out GameObject visual))
            visual = SpawnBulletVisual(bullet);
        visual.transform.position = bullet.position;
    }

    GameObject SpawnBulletVisual(BulletStateSnapshot bullet)
    {
        GameObject prefab = bullet.owner == "Player" ? playerBulletVisualPrefab : enemyBulletVisualPrefab;
        GameObject visual = Instantiate(prefab);
        Collider2D collider = visual.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        Destroy(visual.GetComponent<Bullet>());
        bulletVisuals[bullet.id] = visual;
        return visual;
    }

    void RemoveStaleBulletVisuals()
    {
        List<long> staleIds = new List<long>();
        foreach (KeyValuePair<long, GameObject> kv in bulletVisuals)
            if (!bulletIdsThisTick.Contains(kv.Key)) staleIds.Add(kv.Key);
        foreach (long id in staleIds) DestroyBulletVisual(id);
    }

    void DestroyBulletVisual(long id)
    {
        Destroy(bulletVisuals[id]);
        bulletVisuals.Remove(id);
    }

    void Update()
    {
        foreach (KeyValuePair<string, Vector2> target in targetPositions)
            LerpTowards(shipsById[target.Key].transform, target.Value);
    }

    void LerpTowards(Transform shipTransform, Vector2 targetPosition)
    {
        shipTransform.position = Vector2.Lerp(
            shipTransform.position, targetPosition, positionLerpSpeed * Time.deltaTime);
    }
}
