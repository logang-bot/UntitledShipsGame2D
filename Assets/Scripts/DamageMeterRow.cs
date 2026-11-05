using UnityEngine;

// One ship's line on the damage meter: who, total damage, DPS, and the two
// derived fractions DamageMeterRows fills in (share of the party total, bar
// length relative to the top row).
public struct DamageMeterRow
{
    public GameObject ship;
    public PlayerRole role;
    public float damage;
    public float dps;
    public float share;
    public float bar;
}
