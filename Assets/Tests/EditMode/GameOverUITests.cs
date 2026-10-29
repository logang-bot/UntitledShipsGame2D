using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Same harness approach as WardenBossDamageTests: AddComponent<T>() in this
// EditMode-test context does not synchronously run Awake(), so it's invoked
// explicitly by reflection. A ship counts as human when its PlayerInput is
// enabled, the same rule PartyFrameManager/LevelSequencer use.
public class GameOverUITests
{
    private static readonly MethodInfo AwakeMethod = typeof(GameOverUI)
        .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly List<GameObject> created = new List<GameObject>();
    private GameOverUI gameOver;
    private PartyFrameManager party;

    [SetUp]
    public void SetUp()
    {
        party = Track(new GameObject("TestPartyFrameManager")).AddComponent<PartyFrameManager>();
        gameOver = Track(new GameObject("TestGameOverUI")).AddComponent<GameOverUI>();
        gameOver.panelRoot = Track(new GameObject("TestGameOverPanel"));
        gameOver.partyFrameManager = party;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in created)
            if (go != null) Object.DestroyImmediate(go);
        created.Clear();
    }

    private GameObject Track(GameObject go)
    {
        created.Add(go);
        return go;
    }

    private PlayerHealth Ship(bool human)
    {
        GameObject ship = Track(new GameObject(human ? "HumanShip" : "AIShip"));
        ship.AddComponent<PlayerInput>().enabled = human;
        return ship.AddComponent<PlayerHealth>();
    }

    private void StartWatching(params PlayerHealth[] ships)
    {
        party.players = System.Array.ConvertAll(ships, s => s.gameObject);
        AwakeMethod.Invoke(gameOver, null);
    }

    [Test]
    public void OnlyHumanDies_ShowsGameOver()
    {
        PlayerHealth human = Ship(true);
        StartWatching(human, Ship(false));
        human.TakeDamage(999);
        Assert.IsTrue(gameOver.panelRoot.activeSelf);
    }

    [Test]
    public void OneOfTwoHumansDies_NoGameOver()
    {
        PlayerHealth first = Ship(true);
        StartWatching(first, Ship(true));
        first.TakeDamage(999);
        Assert.IsFalse(gameOver.panelRoot.activeSelf);
    }

    [Test]
    public void BothHumansDie_ShowsGameOver()
    {
        PlayerHealth first = Ship(true);
        PlayerHealth second = Ship(true);
        StartWatching(first, second);
        first.TakeDamage(999);
        second.TakeDamage(999);
        Assert.IsTrue(gameOver.panelRoot.activeSelf);
    }

    [Test]
    public void OnlyAIDies_NoGameOver()
    {
        PlayerHealth ai = Ship(false);
        StartWatching(Ship(true), ai);
        ai.TakeDamage(999);
        Assert.IsFalse(gameOver.panelRoot.activeSelf);
    }
}
