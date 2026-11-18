using NUnit.Framework;
using UnityEngine;

// The Tank's Taunt box: READY, PULLING while the taunt holds, COOLDOWN
// until key 1 works again; plus the arc's free block during a Super Shield.
public class TauntDisplayTests
{
    private readonly TankAbilitySettings settings = new TankAbilitySettings();

    [Test]
    public void Ready_WhenNothingIsRunning()
    {
        TauntDisplay d = TauntDisplay.From(0f, 0f, settings);
        Assert.AreEqual(TauntMood.Ready, d.mood);
        Assert.AreEqual("READY", d.Label);
    }

    [Test]
    public void Pulling_TakesPrecedence_WithTheShareOfTheTauntLeft()
    {
        TauntDisplay d = TauntDisplay.From(1.5f, 2.5f, settings);
        Assert.AreEqual(TauntMood.Pulling, d.mood);
        Assert.AreEqual(1.5f, d.seconds, 1e-5f);
        Assert.AreEqual(0.5f, d.progress, 1e-5f);
    }

    [Test]
    public void Cooldown_AfterThePull_RefillsTowardReady()
    {
        TauntDisplay d = TauntDisplay.From(0f, 1f, settings);
        Assert.AreEqual(TauntMood.Cooldown, d.mood);
        Assert.AreEqual(1f - 1f / settings.tauntCooldown, d.progress, 1e-5f);
    }

    [Test]
    public void ShieldArc_AbsorbsOnlyWhileFreeBlockIsOn()
    {
        GameObject arcObj = new GameObject("ShieldArc", typeof(EdgeCollider2D), typeof(TankShieldArc));
        Collider2D hit = arcObj.GetComponent<EdgeCollider2D>();
        Assert.IsFalse(TankShieldArc.Absorbs(hit));
        arcObj.GetComponent<TankShieldArc>().FreeBlock = true;
        Assert.IsTrue(TankShieldArc.Absorbs(hit));
        Object.DestroyImmediate(arcObj);
    }
}
