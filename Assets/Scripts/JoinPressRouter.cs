// The Join Lobby rules from the Pre-Fight Menus design, as one decision:
// any button on a new device joins it; Back leaves for a joined device, or
// returns to Lobby when nobody has joined yet; Continue from a joined device
// moves on. A new device pressing Back while others are joined does nothing,
// so a stray B can't pull the whole squad back a screen.
public static class JoinPressRouter
{
    public static JoinPressAction Route(JoinPressKind kind, bool deviceJoined, bool anyoneJoined)
    {
        if (kind == JoinPressKind.Back) return RouteBack(deviceJoined, anyoneJoined);
        if (deviceJoined) return kind == JoinPressKind.Continue ? JoinPressAction.Continue : JoinPressAction.None;
        return JoinPressAction.Join;
    }

    private static JoinPressAction RouteBack(bool deviceJoined, bool anyoneJoined)
    {
        if (deviceJoined) return JoinPressAction.Leave;
        return anyoneJoined ? JoinPressAction.None : JoinPressAction.Back;
    }
}
