// Level-ups banked while the fight ran open back to back, one screen each.
// The streak numbers them (PICK 1 / 2) and says when another screen follows.
// Pure, so it's unit-tested (PickStreakTests).
public class PickStreak
{
    public int Index { get; private set; }
    public int Total { get; private set; }

    public bool IsMulti => Total > 1;
    public bool HasMore => Index < Total;
    public string PickText => $"PICK {Index} / {Total}";
    public string SubLine => HasMore ? $"+{Total - Index} MORE PICK AFTER THIS" : "FIGHT FROZEN · CHOOSE ONE";

    public void Start(int pendingPicks)
    {
        Total = pendingPicks;
        Index = 1;
    }

    public void Advance() => Index++;
}
