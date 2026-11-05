using UnityEngine;

// One boss attack's cooldown tile: cooling (radial fill + seconds), READY,
// or NOW while the attack is in progress.
public struct CooldownTileState
{
    public Sprite icon;
    public string label;
    public float remaining;
    public float progress;
    public bool inUse;
}
