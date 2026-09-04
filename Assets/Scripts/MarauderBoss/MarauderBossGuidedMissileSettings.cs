using UnityEngine;

// Guided missile's tunable values, grouped into one Inspector-visible field
// on MarauderBoss instead of several top-level fields - keeps
// MarauderBoss.cs under this project's file-size cap without losing
// Inspector tunability. Read exclusively by MarauderBossAttacks.cs.
[System.Serializable]
public class MarauderBossGuidedMissileSettings
{
    public PlayerRole[] targetRoles = { PlayerRole.Medic, PlayerRole.Attacker };
    public float interval = 5f;
    public float telegraphTime = 0.8f;
    public float turnRate = 90f; // degrees/second
    public float speed = 5f;
    public float warningLingerTime = 2f; // keep the HUD warning up briefly after firing
}
