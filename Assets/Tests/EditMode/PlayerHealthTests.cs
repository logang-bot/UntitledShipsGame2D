using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealthTests
{
    private static readonly MethodInfo AwakeMethod = typeof(PlayerHealth)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

    private GameObject go;

    // Confirmed directly via the Unity MCP bridge (execute_code) before
    // writing this: AddComponent<PlayerHealth>() in this Editor's
    // EditMode-test context does NOT synchronously run Awake() or populate
    // the serialized OnDeath/OnDamaged UnityEvent fields the way Unity
    // normally does when a component is added via the Inspector or loaded
    // from a scene/prefab - currentHealth/currentShield stayed at C#'s
    // implicit 0 default and both events stayed null immediately after
    // AddComponent returned. So both are driven explicitly here instead of
    // relying on Unity's usual automatic behavior.
    private PlayerHealth CreatePlayerHealth()
    {
        go = new GameObject("TestPlayer");
        PlayerHealth health = go.AddComponent<PlayerHealth>();
        health.OnDamaged = new UnityEvent();
        health.OnDeath = new UnityEvent();
        AwakeMethod.Invoke(health, null);
        return health;
    }

    [TearDown]
    public void TearDown()
    {
        if (go != null)
            Object.DestroyImmediate(go);
    }

    [Test]
    public void TakeDamage_LessThanShield_OnlyDrainsShield()
    {
        // Default maxHealth=5, maxShield=3.
        PlayerHealth h = CreatePlayerHealth();

        h.TakeDamage(2);

        Assert.AreEqual(1, h.CurrentShield);
        Assert.AreEqual(5, h.CurrentHealth);
    }

    [Test]
    public void TakeDamage_ExceedsShield_OverflowSpillsToHealth()
    {
        PlayerHealth h = CreatePlayerHealth();

        h.TakeDamage(4); // 3 absorbed by shield, 1 spills to health

        Assert.AreEqual(0, h.CurrentShield);
        Assert.AreEqual(4, h.CurrentHealth);
    }

    [Test]
    public void TakeDamage_NonLethal_InvokesOnDamagedNotOnDeath()
    {
        PlayerHealth h = CreatePlayerHealth();
        bool damaged = false;
        bool died = false;
        h.OnDamaged.AddListener(() => damaged = true);
        h.OnDeath.AddListener(() => died = true);

        h.TakeDamage(1); // absorbed by shield, nowhere near lethal

        Assert.IsTrue(damaged);
        Assert.IsFalse(died);
        Assert.IsTrue(h.gameObject.activeSelf);
    }

    [Test]
    public void TakeDamage_Lethal_InvokesOnDeathAndDeactivatesGameObject()
    {
        PlayerHealth h = CreatePlayerHealth();
        bool damaged = false;
        bool died = false;
        h.OnDamaged.AddListener(() => damaged = true);
        h.OnDeath.AddListener(() => died = true);

        h.TakeDamage(20); // 3 absorbed by shield, 17 spills to health -> -12

        Assert.IsTrue(died);
        Assert.IsFalse(damaged); // lethal hit fires OnDeath only, never OnDamaged
        // TakeDamage does not clamp health at zero - Die() fires as soon as
        // currentHealth <= 0, whatever the exact negative value is.
        Assert.AreEqual(-12, h.CurrentHealth);
        Assert.IsFalse(h.gameObject.activeSelf);
    }

    [Test]
    public void Heal_DoesNotExceedMaxHealth()
    {
        PlayerHealth h = CreatePlayerHealth();
        h.TakeDamage(3); // fully drains the default 3 shield, health untouched
        h.TakeDamage(3); // shield now 0, so this hits health directly: 5 -> 2

        h.Heal(100);

        Assert.AreEqual(5, h.CurrentHealth);
    }

    [Test]
    public void RestoreShield_DoesNotExceedMaxShield()
    {
        PlayerHealth h = CreatePlayerHealth();
        h.TakeDamage(2); // shield now 1

        h.RestoreShield(100);

        Assert.AreEqual(3, h.CurrentShield);
    }
}
