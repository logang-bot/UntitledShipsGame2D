// A multiplayer role tile's state from one player's row: Hover is that
// player's cursor, Locked their pick, Taken another player's pick, and Muted
// the roles they passed over once locked.
public enum RoleTileState
{
    Normal,
    Hover,
    Locked,
    Taken,
    Muted
}
