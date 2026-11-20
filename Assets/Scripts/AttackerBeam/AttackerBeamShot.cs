using UnityEngine;

// Everything one Nova Beam needs from its caster, handed to AttackerBeam.Fire().
public struct AttackerBeamShot
{
    public GameObject source;
    public Transform muzzle;
    public float width;
    public float seconds;
    public float tickInterval;
    public float damagePerTick;
}
