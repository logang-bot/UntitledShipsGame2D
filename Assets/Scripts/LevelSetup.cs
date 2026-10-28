using System;
using UnityEngine;

// Builds the one shared Level scene into the level picked in LevelSelect:
// spawns only that level's boss prefab, then has PartySetupBootstrap
// spawn and wire the party against it. Runs before every other Awake(),
// so LevelSequencer, the ships and the boss itself all see a fully wired
// scene - the project's only [DefaultExecutionOrder] (docs/architecture.md).
// Opening the scene directly (no LevelSelection) uses editorDefaultLevel.
[DefaultExecutionOrder(-1000)]
public class LevelSetup : MonoBehaviour
{
    public BossEntry[] bosses;
    public Level editorDefaultLevel = Level.Level1;
    public LevelSequencer levelSequencer;
    public PartySetupBootstrap partySetup;

    void Awake()
    {
        BossEntry entry = PickEntry(bosses, LevelSelection.SelectedLevel ?? editorDefaultLevel);
        SpawnBackground(entry);
        MonoBehaviour boss = Instantiate(entry.bossPrefab);
        levelSequencer.bossObject = boss;
        partySetup.Setup(boss);
        ForwardBossEvents(boss, entry);
        boss.gameObject.SetActive(true);
        entry.onSpawned.Invoke(boss.gameObject);
    }

    public static BossEntry PickEntry(BossEntry[] entries, Level level)
    {
        return Array.Find(entries, entry => entry.level == level);
    }

    private static void SpawnBackground(BossEntry entry)
    {
        if (entry.backgroundPrefab != null)
        {
            Instantiate(entry.backgroundPrefab);
        }
    }

    private static void ForwardBossEvents(MonoBehaviour boss, BossEntry entry)
    {
        BossEvents.Phase2(boss).AddListener(entry.onPhase2.Invoke);
        BossEvents.Defeated(boss).AddListener(entry.onDefeated.Invoke);
    }
}
