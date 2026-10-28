using System;
using UnityEngine;
using UnityEngine.Events;

// One level's boss, as LevelSetup's Inspector lists it. bossPrefab is the
// IBoss component on the boss prefab's (inactive) root. The events are
// wired in the Inspector to scene objects the prefab can't reference:
// onSpawned to that boss's panel (activate + Bind), onDefeated to the
// panel and VictoryUI, onPhase2 to anything a phase change starts.
[Serializable]
public class BossEntry
{
    public Level level;
    public MonoBehaviour bossPrefab;
    public UnityEvent<GameObject> onSpawned;
    public UnityEvent onPhase2;
    public UnityEvent onDefeated;
}
