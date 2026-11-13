using UnityEngine;

// Routes a player hit to whichever enemy script the object carries: a boss,
// a Warden arm, a wave enemy or a minion. Shared by Bullet and the
// Attacker's Nova Beam, so both damage exactly the same things.
public static class EnemyDamage
{
    /// <summary>Damages the target if it is an enemy. Returns whether anything took the hit.</summary>
    public static bool TryDamage(GameObject target, float amount, GameObject source)
    {
        if (TryDamageBoss(target, amount, source)) return true;
        return TryDamageMinor(target, amount);
    }

    private static bool TryDamageBoss(GameObject target, float amount, GameObject source)
    {
        if (target.TryGetComponent(out MarauderBoss marauder)) marauder.TakeDamage(amount, source);
        else if (target.TryGetComponent(out HalcyonBoss halcyon)) halcyon.TakeDamage(amount, source);
        else if (target.TryGetComponent(out WardenBoss warden)) warden.TakeDamage(amount, source);
        else if (target.TryGetComponent(out WardenArmHealth arm)) arm.TakeDamage(amount, source);
        else return false;
        return true;
    }

    private static bool TryDamageMinor(GameObject target, float amount)
    {
        if (target.TryGetComponent(out Enemy enemy)) enemy.TakeDamage(amount);
        else if (target.TryGetComponent(out Minion minion)) minion.TakeDamage(amount);
        else return false;
        return true;
    }
}
