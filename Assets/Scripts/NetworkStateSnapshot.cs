using System;
using System.Collections.Generic;
using UnityEngine;

// Per-ship state the host broadcasts to remote clients every few
// FixedUpdate ticks so their scene mirrors the host's own authoritative
// simulation. Health/shield are plain overwrites (see
// PlayerHealth.ApplySyncedState) - death/damage-feedback triggering from
// this data is a later-phase follow-up, not handled here yet.
[Serializable]
public struct ShipStateSnapshot
{
    public string id;
    public Vector2 position;
    public int health;
    public int shield;
    public bool firing;
}

// active is a separate flag rather than relying on a null Level1Boss
// reference, since JsonUtility's null handling for reference-typed fields
// is unreliable - currentTargetId is one of ShipStateSnapshot.id above,
// resolved back to a GameObject via RemoteStateReceiver's own roster.
[Serializable]
public struct BossStateSnapshot
{
    public bool active;
    public int health;
    public int maxHealth;
    public bool phase2;
    public string currentTargetId;
}

// One of every currently-active Bullet.cs instance (Bullet.Active) each
// broadcast tick - reconciled declaratively on the client (spawn ids it
// doesn't know yet, drop ids no longer present) rather than as discrete
// spawn/despawn event messages, which is simpler and can't desync from a
// missed message. id is EntityId.ToULong(bullet.GetEntityId()), host-side
// only (GetInstanceID() is obsolete as of this Unity version).
[Serializable]
public struct BulletStateSnapshot
{
    public long id;
    public Vector2 position;
    public string owner; // "Player" or "Enemy" - which visual prefab RemoteStateReceiver spawns
}

[Serializable]
public class PartyStateSnapshot
{
    public List<ShipStateSnapshot> ships = new List<ShipStateSnapshot>();
    public BossStateSnapshot boss;
    public List<BulletStateSnapshot> bullets = new List<BulletStateSnapshot>();

    public string ToJson() => JsonUtility.ToJson(this);

    public static PartyStateSnapshot FromJson(string json) =>
        JsonUtility.FromJson<PartyStateSnapshot>(json);
}
