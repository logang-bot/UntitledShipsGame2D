using UnityEngine;

public class PartyFrameManager : MonoBehaviour
{
    public GameObject[] players;
    public PartyFrameUI[] partyFrames;

void Awake()
    {
        for (int i = 0; i < partyFrames.Length && i < players.Length; i++)
        {
            if (players[i] != null && partyFrames[i] != null) partyFrames[i].Initialize(players[i]);
        }
    }
}
