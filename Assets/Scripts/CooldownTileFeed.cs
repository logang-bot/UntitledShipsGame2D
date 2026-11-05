using UnityEngine;

// One boss attack's cooldown tile as a feed keeps it: its icon and label,
// and the CooldownClock that learns its total. Read() turns this frame's
// remaining time into the tile's state.
public class CooldownTileFeed
{
    private readonly Sprite icon;
    private readonly string label;
    private readonly CooldownClock clock = new CooldownClock();

    public CooldownTileFeed(Sprite icon, string label)
    {
        this.icon = icon;
        this.label = label;
    }

    public CooldownTileState Read(float remaining, bool inUse) => new CooldownTileState
    {
        icon = icon, label = label, remaining = remaining, progress = clock.Progress(remaining), inUse = inUse,
    };
}
