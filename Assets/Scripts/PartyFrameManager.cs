using UnityEngine;

public class PartyFrameManager : MonoBehaviour
{
    public GameObject[] players;
    public PartyFrameUI[] partyFrames;

void Awake()
    {
        int cpuIndex = 0;
        for (int i = 0; i < partyFrames.Length && i < players.Length; i++)
        {
            if (players[i] == null || partyFrames[i] == null) continue;

            // The human is whichever ship has no AIController (attached to
            // all 3 Teammate_* GameObjects, absent from Player - same
            // signal docs/systems/boss.md already uses for this distinction).
            bool isHuman = players[i].GetComponent<AIController>() == null;
            string displayName = isHuman ? "Player 1" : "CPU " + (++cpuIndex);
            partyFrames[i].Initialize(players[i], displayName);
        }
    }
}
