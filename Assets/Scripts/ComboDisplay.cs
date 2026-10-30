// What the Attacker's combo box shows (design: HUD Party Frame): how many of
// the 1-2-3 keys have landed and the headline. COMBO ✓ and BROKEN each hold
// for FlashSeconds after they happen, while the rotation is back at key 1.
public struct ComboDisplay
{
    public const float FlashSeconds = 0.6f;

    public int landed;
    public ComboMood mood;
    public int nextSlot;

    public string Label => mood switch
    {
        ComboMood.Complete => "COMBO",
        ComboMood.Broken => "BROKEN",
        _ => $"NEXT ▸ {nextSlot}",
    };

    public static ComboDisplay From(int expectedSlot, float brokenAt, float finishedAt, float now)
    {
        ComboMood mood = MoodAt(expectedSlot, brokenAt, finishedAt, now);
        int landed = mood == ComboMood.Complete ? 3 : expectedSlot - 1;
        return new ComboDisplay { landed = landed, mood = mood, nextSlot = expectedSlot };
    }

    private static ComboMood MoodAt(int expectedSlot, float brokenAt, float finishedAt, float now)
    {
        if (expectedSlot != 1) return ComboMood.Next;
        bool brokeLast = brokenAt > finishedAt;
        float last = brokeLast ? brokenAt : finishedAt;
        if (last < 0f || now - last >= FlashSeconds) return ComboMood.Next;
        return brokeLast ? ComboMood.Broken : ComboMood.Complete;
    }
}
