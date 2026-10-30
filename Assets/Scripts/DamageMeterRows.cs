using System.Collections.Generic;
using UnityEngine;

// Builds the damage meter's rows from the boss's BossDamageLedger: sorted by
// damage (ties keep role order), with each row's share and bar filled in.
// Rank is pure, so it's unit-tested (DamageMeterRowsTests).
public static class DamageMeterRows
{
    public static List<DamageMeterRow> Read(BossDamageLedger ledger, IEnumerable<GameObject> ships)
    {
        List<DamageMeterRow> rows = new List<DamageMeterRow>();
        foreach (GameObject ship in ships)
            if (ship != null && ship.TryGetComponent(out PlayerRoleComponent role)) rows.Add(RowFor(ledger, ship, role.role));
        return Rank(rows);
    }

    public static List<DamageMeterRow> Rank(List<DamageMeterRow> rows)
    {
        rows.Sort(ByDamageThenRole);
        float total = 0f;
        foreach (DamageMeterRow row in rows) total += row.damage;
        float top = rows.Count > 0 ? rows[0].damage : 0f;
        for (int i = 0; i < rows.Count; i++) rows[i] = WithFractions(rows[i], total, top);
        return rows;
    }

    private static DamageMeterRow RowFor(BossDamageLedger ledger, GameObject ship, PlayerRole role) => new DamageMeterRow
    {
        ship = ship, role = role, damage = ledger.GetDamageDealt(ship), dps = ledger.Dps(ship),
    };

    private static int ByDamageThenRole(DamageMeterRow a, DamageMeterRow b)
    {
        int byDamage = b.damage.CompareTo(a.damage);
        return byDamage != 0 ? byDamage : a.role.CompareTo(b.role);
    }

    private static DamageMeterRow WithFractions(DamageMeterRow row, float total, float top)
    {
        row.share = total > 0f ? row.damage / total : 0f;
        row.bar = top > 0f ? row.damage / top : 0f;
        return row;
    }
}
