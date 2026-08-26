using UnityEngine;

public class PartyFrameManager : MonoBehaviour
{
    public GameObject[] players;
    public PartyFrameUI[] partyFrames;

void Awake()
    {
        int humanIndex = 0;
        int cpuIndex = 0;
        for (int i = 0; i < partyFrames.Length && i < players.Length; i++)
        {
            if (players[i] == null || partyFrames[i] == null) continue;

            // Every ship (human or AI) now carries both PlayerInput and
            // AIController (see Ship.prefab / docs/systems/player-roles.md's
            // "Co-op roster" section) - which one is actually driving the
            // ship is whichever is enabled, not which is present. Supports
            // multiple human slots for local co-op, unlike the old
            // hardcoded "Player 1" single-human assumption.
            UnityEngine.InputSystem.PlayerInput playerInput = players[i].GetComponent<UnityEngine.InputSystem.PlayerInput>();
            bool isHuman = playerInput != null && playerInput.enabled;
            string displayName = isHuman ? "Player " + (++humanIndex) : "CPU " + (++cpuIndex);
            partyFrames[i].Initialize(players[i], displayName, isHuman);
        }
    }
}
