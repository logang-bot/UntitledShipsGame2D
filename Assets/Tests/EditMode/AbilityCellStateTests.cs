using NUnit.Framework;

// Covers the party frame's ability cell mapping (design: HUD Party Frame) -
// solid sweep = ready, filling = active time left, grey track filling back
// up = cooldown, OFFLINE once the ship is down.
public class AbilityCellStateTests
{
    private static AbilityTimes Times(float cdLeft, float active = 0f, bool down = false) => new AbilityTimes
    {
        cooldownRemaining = cdLeft, cooldownTotal = 10f, activeRemaining = active, activeTotal = 4f, down = down,
    };

    [Test]
    public void NoCooldown_IsReady_WithAFullSweep()
    {
        AbilityCellState s = AbilityCellState.From(Times(0f));

        Assert.AreEqual(AbilityPhase.Ready, s.phase);
        Assert.AreEqual(1f, s.sweep);
    }

    [Test]
    public void Cooling_SweepFillsBackUpAsTheCooldownRunsOut()
    {
        AbilityCellState s = AbilityCellState.From(Times(7.5f));

        Assert.AreEqual(AbilityPhase.Cooldown, s.phase);
        Assert.AreEqual(0.25f, s.sweep, 0.0001f);
        Assert.AreEqual(7.5f, s.seconds, 0.0001f);
    }

    [Test]
    public void Active_TakesPrecedenceOverTheCooldownStartedWithIt()
    {
        AbilityCellState s = AbilityCellState.From(Times(9f, active: 3f));

        Assert.AreEqual(AbilityPhase.Active, s.phase);
        Assert.AreEqual(0.75f, s.sweep, 0.0001f);
        Assert.AreEqual(3f, s.seconds, 0.0001f);
    }

    [Test]
    public void Down_IsOffline_WhateverTheTimers()
    {
        AbilityCellState s = AbilityCellState.From(Times(0f, active: 2f, down: true));

        Assert.AreEqual(AbilityPhase.Offline, s.phase);
        Assert.AreEqual(0f, s.sweep);
    }

    [Test]
    public void ZeroCooldownTotal_DoesNotDivideByZero()
    {
        AbilityTimes t = Times(1f);
        t.cooldownTotal = 0f;

        Assert.AreEqual(0f, AbilityCellState.From(t).sweep);
    }

    [Test]
    public void IsCoolingDown_OnlyForCooldown()
    {
        Assert.IsTrue(AbilityCellState.From(Times(2f)).IsCoolingDown);
        Assert.IsFalse(AbilityCellState.From(Times(0f)).IsCoolingDown);
    }
}
