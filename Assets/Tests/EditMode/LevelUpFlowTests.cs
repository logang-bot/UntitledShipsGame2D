using NUnit.Framework;

// Covers the level-up screen's timing (design: Neon Fleet HUD v2, section 10)
// - 0.3 s input lock, 250 ms pick confirm, and the card states it implies.
public class LevelUpFlowTests
{
    private static LevelUpFlow OpenedAt(float now)
    {
        LevelUpFlow flow = new LevelUpFlow();
        flow.Open(now);
        return flow;
    }

    [Test]
    public void DuringTheLock_PicksAreDropped_AndCardsReadLocked()
    {
        LevelUpFlow flow = OpenedAt(10f);

        Assert.IsFalse(flow.TryPick(0, 10.2f));
        Assert.AreEqual(UpgradeCardState.Locked, flow.StateOf(0, 0, 10.2f));
    }

    [Test]
    public void AfterTheLock_FocusedCardIsFocus_OthersRest()
    {
        LevelUpFlow flow = OpenedAt(10f);

        Assert.AreEqual(UpgradeCardState.Focus, flow.StateOf(1, 1, 10.4f));
        Assert.AreEqual(UpgradeCardState.Rest, flow.StateOf(0, 1, 10.4f));
    }

    [Test]
    public void Pick_MarksPickedAndDim_ThenConfirmsAfter250ms()
    {
        LevelUpFlow flow = OpenedAt(10f);

        Assert.IsTrue(flow.TryPick(2, 10.5f));
        Assert.AreEqual(UpgradeCardState.Picked, flow.StateOf(2, 0, 10.6f));
        Assert.AreEqual(UpgradeCardState.Dim, flow.StateOf(0, 0, 10.6f));
        Assert.IsFalse(flow.IsConfirmDone(10.7f));
        Assert.IsTrue(flow.IsConfirmDone(10.75f));
    }

    [Test]
    public void SecondPick_WhileConfirming_IsIgnored()
    {
        LevelUpFlow flow = OpenedAt(10f);
        flow.TryPick(2, 10.5f);

        Assert.IsFalse(flow.TryPick(0, 10.6f));
        Assert.AreEqual(2, flow.PickedIndex);
    }

    [Test]
    public void Reopen_ClearsThePick_AndLocksAgain()
    {
        LevelUpFlow flow = OpenedAt(10f);
        flow.TryPick(1, 10.5f);
        flow.Open(11f);

        Assert.IsFalse(flow.HasPicked);
        Assert.IsTrue(flow.IsLocked(11.1f));
        Assert.AreEqual(0.5f, flow.LockElapsedFraction(11.15f), 0.0001f);
    }
}
