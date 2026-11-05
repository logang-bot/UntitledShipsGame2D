using NUnit.Framework;

// Covers the Attacker combo box (design: HUD Party Frame) - keys already
// landed, NEXT ▸ n, a held COMBO ✓ after a finisher and a 600 ms BROKEN
// flash after a wrong key.
public class ComboDisplayTests
{
    [Test]
    public void ExpectedSlotTwo_MeansOneKeyLanded()
    {
        ComboDisplay d = ComboDisplay.From(2, -1f, -1f, 10f);

        Assert.AreEqual(1, d.landed);
        Assert.AreEqual(ComboMood.Next, d.mood);
        Assert.AreEqual("NEXT ▸ 2", d.Label);
    }

    [Test]
    public void FreshFinisher_ShowsCompleteWithAllThreeLanded()
    {
        ComboDisplay d = ComboDisplay.From(1, -1f, 9.8f, 10f);

        Assert.AreEqual(ComboMood.Complete, d.mood);
        Assert.AreEqual(3, d.landed);
    }

    [Test]
    public void OldFinisher_FallsBackToNextOne()
    {
        ComboDisplay d = ComboDisplay.From(1, -1f, 5f, 10f);

        Assert.AreEqual(ComboMood.Next, d.mood);
        Assert.AreEqual(0, d.landed);
    }

    [Test]
    public void BreakWithin600ms_ShowsBroken()
    {
        ComboDisplay d = ComboDisplay.From(1, 9.5f, -1f, 10f);

        Assert.AreEqual(ComboMood.Broken, d.mood);
        Assert.AreEqual("BROKEN", d.Label);
    }

    [Test]
    public void BreakAfter600ms_IsForgotten()
    {
        Assert.AreEqual(ComboMood.Next, ComboDisplay.From(1, 9.3f, -1f, 10f).mood);
    }

    [Test]
    public void NewerOfBreakAndFinish_Wins()
    {
        Assert.AreEqual(ComboMood.Broken, ComboDisplay.From(1, 9.9f, 9.7f, 10f).mood);
        Assert.AreEqual(ComboMood.Complete, ComboDisplay.From(1, 9.7f, 9.9f, 10f).mood);
    }

    [Test]
    public void ProgressAfterAFinish_ShowsTheNewRotation()
    {
        ComboDisplay d = ComboDisplay.From(2, -1f, 9.9f, 10f);

        Assert.AreEqual(ComboMood.Next, d.mood);
        Assert.AreEqual(1, d.landed);
    }
}
