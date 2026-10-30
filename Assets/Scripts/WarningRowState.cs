using UnityEngine;

// One warning bar on the boss panel (design: HUD Boss Panel): solid boss
// color while it counts down, outlined once the attack has fired. `drain` is
// the base line's length (0..1); `target` names the role it locked onto.
public struct WarningRowState
{
    public Sprite icon;
    public string title;
    public string sub;
    public PlayerRole? target;
    public float seconds;
    public float drain;
    public bool fired;
}
