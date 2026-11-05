using System;
using NUnit.Framework;

public class RoleStatScaleTests
{
    [Test]
    public void Fraction_StrongestRoleFillsTheBar()
    {
        foreach (RoleStat stat in Enum.GetValues(typeof(RoleStat)))
        {
            float best = 0f;
            foreach (PlayerRole role in Enum.GetValues(typeof(PlayerRole))) best = Math.Max(best, RoleStatScale.Fraction(role, stat));
            Assert.AreEqual(1f, best, 1e-5f, stat.ToString());
        }
    }

    [Test]
    public void Fraction_IsValueOverLargest()
    {
        float expected = RoleStatScale.Value(PlayerRole.Medic, RoleStat.Health) / RoleStatScale.Value(PlayerRole.Tank, RoleStat.Health);
        Assert.AreEqual(expected, RoleStatScale.Fraction(PlayerRole.Medic, RoleStat.Health), 1e-5f);
    }

    [Test]
    public void Format_FireRateIsPerSecond()
    {
        StringAssert.EndsWith("/s", RoleStatScale.Format(PlayerRole.Attacker, RoleStat.FireRate));
    }
}
