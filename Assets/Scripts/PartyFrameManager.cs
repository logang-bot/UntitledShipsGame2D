using UnityEngine;
using UnityEngine.InputSystem;

public class PartyFrameManager : MonoBehaviour
{
    public GameObject[] players;
    public PartyFrameUI[] partyFrames;

    private int humanCount;
    private int cpuCount;

    void Awake()
    {
        for (int i = 0; i < partyFrames.Length && i < players.Length; i++)
            if (players[i] != null && partyFrames[i] != null) BindFrame(players[i], partyFrames[i]);
    }

    /// <summary>
    /// The death listener is added at runtime, not in the Inspector: ships
    /// spawned from Ship.prefab by the co-op flow can't carry a persistent
    /// listener to a scene object, so their frames never grayed out.
    /// </summary>
    private void BindFrame(GameObject player, PartyFrameUI frame)
    {
        bool isHuman = IsHuman(player);
        string displayName = isHuman ? "Player " + (++humanCount) : "CPU " + (++cpuCount);
        frame.Initialize(player, displayName, isHuman);
        player.GetComponent<PlayerHealth>().OnDeath.AddListener(frame.OnPlayerDied);
    }

    /// <summary>
    /// Every ship (human or AI) carries both PlayerInput and AIController
    /// (see Ship.prefab / docs/systems/player-roles.md's "Co-op roster") -
    /// which one is driving is whichever is enabled, not which is present.
    /// Only valid before LevelSequencer's first freeze disables PlayerInput.
    /// </summary>
    public static bool IsHuman(GameObject ship)
    {
        if (ship == null) return false;
        PlayerInput playerInput = ship.GetComponent<PlayerInput>();
        return playerInput != null && playerInput.enabled;
    }
}
