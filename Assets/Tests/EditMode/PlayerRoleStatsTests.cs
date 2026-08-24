using NUnit.Framework;

public class PlayerRoleStatsTests
{
    [Test]
    public void Get_Attacker_ReturnsExpectedStats()
    {
        RoleStats stats = PlayerRoleStats.Get(PlayerRole.Attacker);

        Assert.AreEqual(6, stats.maxHealth);
        Assert.AreEqual(5, stats.maxShield);
        Assert.AreEqual(2.0f, stats.fireDamage);
        Assert.AreEqual(2.5f, stats.shotsPerSecond);
        Assert.AreEqual(3.0f, stats.moveSpeed);
    }

    [Test]
    public void Get_Tank_ReturnsExpectedStats()
    {
        RoleStats stats = PlayerRoleStats.Get(PlayerRole.Tank);

        Assert.AreEqual(8, stats.maxHealth);
        Assert.AreEqual(20, stats.maxShield);
        Assert.AreEqual(1.0f, stats.fireDamage);
        Assert.AreEqual(1f, stats.shotsPerSecond);
        Assert.AreEqual(1.5f, stats.moveSpeed);
    }

    [Test]
    public void Get_Medic_ReturnsExpectedStats()
    {
        RoleStats stats = PlayerRoleStats.Get(PlayerRole.Medic);

        Assert.AreEqual(4, stats.maxHealth);
        Assert.AreEqual(3, stats.maxShield);
        Assert.AreEqual(0.7f, stats.fireDamage);
        Assert.AreEqual(1.5f, stats.shotsPerSecond);
        Assert.AreEqual(3.0f, stats.moveSpeed);
    }

    [Test]
    public void Get_Support_ReturnsExpectedStats()
    {
        RoleStats stats = PlayerRoleStats.Get(PlayerRole.Support);

        Assert.AreEqual(5, stats.maxHealth);
        Assert.AreEqual(3, stats.maxShield);
        Assert.AreEqual(1.0f, stats.fireDamage);
        Assert.AreEqual(2f, stats.shotsPerSecond);
        Assert.AreEqual(4.5f, stats.moveSpeed);
    }
}
