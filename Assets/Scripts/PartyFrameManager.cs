using UnityEngine;

public class PartyFrameManager : MonoBehaviour
{
    public GameObject player;
    public PartyFrameUI partyFrame1;

    void Awake()
    {
        partyFrame1.Initialize(player);
    }
}
