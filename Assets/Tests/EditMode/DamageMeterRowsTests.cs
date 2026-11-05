using System.Collections.Generic;
using NUnit.Framework;

// Covers the damage meter's row math (design: Neon Fleet HUD v2, left rail):
// sorted by damage, share of the party total, bar length relative to the
// top damage dealer.
public class DamageMeterRowsTests
{
    private static DamageMeterRow Row(PlayerRole role, float damage) => new DamageMeterRow { role = role, damage = damage };

    [Test]
    public void Sort_PutsTheTopDamageDealerFirst()
    {
        List<DamageMeterRow> rows = DamageMeterRows.Rank(new List<DamageMeterRow> { Row(PlayerRole.Medic, 48f), Row(PlayerRole.Attacker, 262f), Row(PlayerRole.Tank, 62f) });

        Assert.AreEqual(PlayerRole.Attacker, rows[0].role);
        Assert.AreEqual(PlayerRole.Tank, rows[1].role);
        Assert.AreEqual(PlayerRole.Medic, rows[2].role);
    }

    [Test]
    public void Share_IsEachRowsPercentOfTheTotal()
    {
        List<DamageMeterRow> rows = DamageMeterRows.Rank(new List<DamageMeterRow> { Row(PlayerRole.Attacker, 75f), Row(PlayerRole.Tank, 25f) });

        Assert.AreEqual(0.75f, rows[0].share, 0.0001f);
        Assert.AreEqual(0.25f, rows[1].share, 0.0001f);
    }

    [Test]
    public void Bar_IsRelativeToTheTopRow()
    {
        List<DamageMeterRow> rows = DamageMeterRows.Rank(new List<DamageMeterRow> { Row(PlayerRole.Attacker, 200f), Row(PlayerRole.Tank, 50f) });

        Assert.AreEqual(1f, rows[0].bar, 0.0001f);
        Assert.AreEqual(0.25f, rows[1].bar, 0.0001f);
    }

    [Test]
    public void NoDamageYet_LeavesSharesAndBarsAtZero()
    {
        List<DamageMeterRow> rows = DamageMeterRows.Rank(new List<DamageMeterRow> { Row(PlayerRole.Attacker, 0f), Row(PlayerRole.Tank, 0f) });

        Assert.AreEqual(0f, rows[0].share);
        Assert.AreEqual(0f, rows[0].bar);
    }

    [Test]
    public void EqualDamage_KeepsRoleOrder()
    {
        List<DamageMeterRow> rows = DamageMeterRows.Rank(new List<DamageMeterRow> { Row(PlayerRole.Support, 10f), Row(PlayerRole.Attacker, 10f) });

        Assert.AreEqual(PlayerRole.Attacker, rows[0].role);
    }
}
