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

    // ------------------------------------------------------------------ DPS
    // RoleStats.Dps is derived (fireDamage x shotsPerSecond), so these pin the
    // *designed* damage output of each role - the number DpsMeterUI's rows are
    // measured against. A change to either factor that moves a role's DPS will
    // fail here, which is the point: DPS is the balance lever, and it's easy to
    // move by accident while tuning only one of its two inputs.

    [Test]
    public void Dps_IsDerivedFromDamageTimesFireRate()
    {
        RoleStats stats = PlayerRoleStats.Get(PlayerRole.Attacker);

        Assert.AreEqual(stats.fireDamage * stats.shotsPerSecond, stats.Dps, 0.0001f);
    }

    [Test]
    public void Dps_Attacker_IsFivePerSecond()
    {
        Assert.AreEqual(5.0f, PlayerRoleStats.Get(PlayerRole.Attacker).Dps, 0.0001f);
    }

    [Test]
    public void Dps_Support_IsTwoPerSecond()
    {
        Assert.AreEqual(2.0f, PlayerRoleStats.Get(PlayerRole.Support).Dps, 0.0001f);
    }

    [Test]
    public void Dps_Medic_IsJustOverOnePerSecond()
    {
        Assert.AreEqual(1.05f, PlayerRoleStats.Get(PlayerRole.Medic).Dps, 0.0001f);
    }

    [Test]
    public void Dps_Tank_IsOnePerSecond()
    {
        Assert.AreEqual(1.0f, PlayerRoleStats.Get(PlayerRole.Tank).Dps, 0.0001f);
    }

    [Test]
    public void Dps_AttackerOutDamagesEveryOtherRole()
    {
        // The Attacker's whole identity. If this ever fails, either Attacker
        // was nerfed too far or another role was buffed past it.
        float attacker = PlayerRoleStats.Get(PlayerRole.Attacker).Dps;

        Assert.Greater(attacker, PlayerRoleStats.Get(PlayerRole.Tank).Dps);
        Assert.Greater(attacker, PlayerRoleStats.Get(PlayerRole.Medic).Dps);
        Assert.Greater(attacker, PlayerRoleStats.Get(PlayerRole.Support).Dps);
    }
}
