using NUnit.Framework;

// Covers the Attack and Crit cells on the roguelike Attacker frame (design:
// HUD Party Frame) - the sweep refills as the cooldown runs out, and the cell
// flashes for 80 ms (attack) or 120 ms (crit) after firing.
public class CooldownCellStateTests
{
    private static CooldownReading Reading(float remaining, float total, float sinceFired) =>
        new CooldownReading { remaining = remaining, total = total, sinceFired = sinceFired };

    [Test]
    public void HalfwayThroughTheCooldown_SweepIsHalf()
    {
        CooldownCellState s = CooldownCellState.From(Reading(1.5f, 3f, 1.5f), CooldownCellSpec.Crit);

        Assert.AreEqual(0.5f, s.sweep, 0.0001f);
        Assert.AreEqual("1.5s", s.timer);
        Assert.IsFalse(s.flashing);
    }

    [Test]
    public void AttackTimer_HasNoUnit()
    {
        CooldownCellState s = CooldownCellState.From(Reading(0.2f, 0.4f, 0.2f), CooldownCellSpec.Attack);

        Assert.AreEqual("0.2", s.timer);
    }

    [Test]
    public void JustFired_FlashesForItsOwnDuration()
    {
        Assert.IsTrue(CooldownCellState.From(Reading(0.4f, 0.4f, 0.05f), CooldownCellSpec.Attack).flashing);
        Assert.IsFalse(CooldownCellState.From(Reading(0.3f, 0.4f, 0.1f), CooldownCellSpec.Attack).flashing);
        Assert.IsTrue(CooldownCellState.From(Reading(2.9f, 3f, 0.1f), CooldownCellSpec.Crit).flashing);
    }

    [Test]
    public void NeverFired_DoesNotFlash()
    {
        CooldownCellState s = CooldownCellState.From(Reading(0f, 3f, float.PositiveInfinity), CooldownCellSpec.Crit);

        Assert.IsFalse(s.flashing);
        Assert.AreEqual(1f, s.sweep);
    }
}
