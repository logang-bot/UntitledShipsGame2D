using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Same harness approach as WardenArmHealthTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection.
public class AIBossTacticsTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly List<GameObject> created = new List<GameObject>();

    private GameObject Track(GameObject go)
    {
        created.Add(go);
        return go;
    }

    private T AddAwake<T>(GameObject go) where T : MonoBehaviour
    {
        T component = go.AddComponent<T>();
        typeof(T).GetMethod("Awake", Private).Invoke(component, null);
        return component;
    }

    private WardenBoss CreateWarden()
    {
        WardenBoss warden = AddAwake<WardenBoss>(Track(new GameObject("TestWarden")));
        warden.armA = CreateArm(warden, new Vector2(-1.2f, 0f));
        warden.armB = CreateArm(warden, new Vector2(1.2f, 0f));
        return warden;
    }

    private WardenArm CreateArm(WardenBoss warden, Vector2 position)
    {
        GameObject go = Track(new GameObject("TestArm"));
        go.transform.SetParent(warden.transform);
        go.transform.position = position;
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>();
        WardenArm arm = go.AddComponent<WardenArm>();
        AddAwake<WardenArmHealth>(go);
        return arm;
    }

    private MarauderBoss CreateMarauder(params GameObject[] targets)
    {
        MarauderBoss marauder = Track(new GameObject("TestMarauder")).AddComponent<MarauderBoss>();
        marauder.targets = targets;
        typeof(MarauderBoss).GetMethod("Awake", Private).Invoke(marauder, null);
        return marauder;
    }

    private AIController CreateAI()
    {
        return Track(new GameObject("TestAI")).AddComponent<AIController>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    [Test]
    public void For_PicksTacticsByBossType()
    {
        Assert.IsInstanceOf<MarauderAITactics>(AIBossTactics.For(CreateMarauder()));
        Assert.IsInstanceOf<WardenAITactics>(AIBossTactics.For(CreateWarden()));
        Assert.AreEqual(typeof(AIBossTactics), AIBossTactics.For(Track(new GameObject()).AddComponent<HalcyonBoss>()).GetType());
    }

    [Test]
    public void Default_FocusesBoss_NeverTaunts_NoPriorityFocus()
    {
        HalcyonBoss halcyon = Track(new GameObject("TestHalcyon")).AddComponent<HalcyonBoss>();
        halcyon.transform.position = new Vector2(2f, 3f);
        AIBossTactics tactics = AIBossTactics.For(halcyon);

        Assert.AreEqual(new Vector2(2f, 3f), tactics.FocusPoint());
        Assert.IsFalse(tactics.ShouldTaunt(CreateAI()));
        Assert.IsFalse(tactics.HasPriorityFocus());
    }

    [Test]
    public void Marauder_TauntsOnlyWhenNotAlreadyTheTarget()
    {
        AIController ai = CreateAI();
        AIController other = CreateAI();
        AIBossTactics tactics = AIBossTactics.For(CreateMarauder(other.gameObject, ai.gameObject));

        Assert.IsTrue(tactics.ShouldTaunt(ai), "Boss starts on targets[0], so this AI isn't holding aggro.");
        Assert.IsFalse(tactics.ShouldTaunt(other), "Already the target - no need to taunt.");
    }

    [Test]
    public void Warden_Shielded_FocusesLowestHealthArm()
    {
        WardenBoss warden = CreateWarden();
        warden.armB.GetComponent<WardenArmHealth>().TakeDamage(5f);
        AIBossTactics tactics = AIBossTactics.For(warden);

        Assert.IsTrue(tactics.HasPriorityFocus());
        Assert.AreEqual((Vector2)warden.armB.transform.position, tactics.FocusPoint());
    }

    [Test]
    public void Warden_EqualHealthArms_FocusesArmA()
    {
        WardenBoss warden = CreateWarden();

        Assert.AreEqual((Vector2)warden.armA.transform.position, AIBossTactics.For(warden).FocusPoint());
    }

    [Test]
    public void Warden_SkipsDownedArm()
    {
        WardenBoss warden = CreateWarden();
        WardenArmHealth armA = warden.armA.GetComponent<WardenArmHealth>();
        armA.TakeDamage(armA.maxHealth);

        Assert.AreEqual((Vector2)warden.armB.transform.position, AIBossTactics.For(warden).FocusPoint());
    }

    [Test]
    public void Warden_EveryArmDown_FocusesBoss()
    {
        WardenBoss warden = CreateWarden();
        warden.transform.position = new Vector2(0f, 4f);
        foreach (WardenArm arm in new[] { warden.armA, warden.armB })
            arm.GetComponent<WardenArmHealth>().TakeDamage(20f);
        AIBossTactics tactics = AIBossTactics.For(warden);

        Assert.IsFalse(tactics.HasPriorityFocus());
        Assert.AreEqual(new Vector2(0f, 4f), tactics.FocusPoint());
    }

    [Test]
    public void Warden_AlwaysTaunts()
    {
        Assert.IsTrue(AIBossTactics.For(CreateWarden()).ShouldTaunt(CreateAI()));
    }
}
