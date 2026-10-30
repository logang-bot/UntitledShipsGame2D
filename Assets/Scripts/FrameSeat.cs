// Who flies a party frame's ship: its tag ("P1", "CPU 2"), whether a human
// is driving, and whether it is this machine's first human (the design's
// local player: solid role frame, inset edge, glow, filled tag).
public struct FrameSeat
{
    public string label;
    public bool isHuman;
    public bool isLocal;
}
