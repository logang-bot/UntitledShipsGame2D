using System.Collections.Generic;
using UnityEngine;

// Host-only: every ticksBetweenBroadcasts FixedUpdates, gathers every
// spawned ship's, the boss's, and every active Bullet's state and sends it
// to every connected client over Nakama, so their scenes mirror this one.
// Lives disabled in Gameplay.unity; NetworkPartySpawner.Initialize()s and
// enables it only when NetworkSession.IsHost is true.
public class HostStateBroadcaster : MonoBehaviour
{
    public int ticksBetweenBroadcasts = 3;

    readonly List<NetworkShipEntry> ships = new List<NetworkShipEntry>();
    readonly Dictionary<GameObject, string> shipIdsByObject = new Dictionary<GameObject, string>();
    Level1Boss boss;
    int tickCounter;

    public void Initialize(List<NetworkShipEntry> roster, Level1Boss bossRef)
    {
        ships.Clear();
        ships.AddRange(roster);
        foreach (NetworkShipEntry entry in roster) shipIdsByObject[entry.ship] = entry.id;
        boss = bossRef;
        enabled = true;
    }

    void FixedUpdate()
    {
        if (++tickCounter < ticksBetweenBroadcasts) return;
        tickCounter = 0;
        Broadcast();
    }

    void Broadcast()
    {
        PartyStateSnapshot snapshot = BuildSnapshot();
        NakamaConnection.Instance.Socket.SendMatchStateAsync(
            NetworkSession.MatchId, NetworkOpCodes.StateSnapshot, snapshot.ToJson());
    }

    PartyStateSnapshot BuildSnapshot()
    {
        var snapshot = new PartyStateSnapshot { boss = BuildBossSnapshot() };
        foreach (NetworkShipEntry entry in ships) snapshot.ships.Add(BuildShipSnapshot(entry));
        foreach (Bullet bullet in Bullet.Active) snapshot.bullets.Add(BuildBulletSnapshot(bullet));
        return snapshot;
    }

    ShipStateSnapshot BuildShipSnapshot(NetworkShipEntry entry)
    {
        PlayerHealth health = entry.ship.GetComponent<PlayerHealth>();
        PlayerController controller = entry.ship.GetComponent<PlayerController>();
        return new ShipStateSnapshot
        {
            id = entry.id,
            position = entry.ship.transform.position,
            health = health.CurrentHealth,
            shield = health.CurrentShield,
            firing = controller.IsFiring
        };
    }

    BossStateSnapshot BuildBossSnapshot()
    {
        if (boss == null) return new BossStateSnapshot { active = false };
        string targetId = ResolveTargetId();
        return new BossStateSnapshot
        {
            active = true,
            health = boss.CurrentHealth,
            maxHealth = boss.maxHealth,
            phase2 = boss.IsPhase2,
            currentTargetId = targetId
        };
    }

    string ResolveTargetId() =>
        boss.CurrentTarget != null && shipIdsByObject.TryGetValue(boss.CurrentTarget, out string id) ? id : null;

    BulletStateSnapshot BuildBulletSnapshot(Bullet bullet) => new BulletStateSnapshot
    {
        id = (long)EntityId.ToULong(bullet.GetEntityId()),
        position = bullet.transform.position,
        owner = bullet.Owner
    };
}
