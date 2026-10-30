using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Binds the left rail's squad frames to the party's ships. Frames always run
// in role order (Attacker, Tank, Medic, Support) so a slot never moves; seat
// tags are numbered in players[] order - humans P1, P2..., CPUs CPU 1, 2...
// The first human is the local player.
public class PartyFrameManager : MonoBehaviour
{
    public GameObject[] players;
    public PartyFrameUI[] partyFrames;
    public TMP_Text squadSummary;

    private readonly Dictionary<GameObject, FrameSeat> seats = new Dictionary<GameObject, FrameSeat>();

    void Awake()
    {
        if (players == null || partyFrames == null) return;
        AssignSeats();
        GameObject[] ordered = InRoleOrder(players);
        for (int i = 0; i < partyFrames.Length; i++)
            if (partyFrames[i] != null) BindSlot(partyFrames[i], i < ordered.Length ? ordered[i] : null);
        if (squadSummary != null) squadSummary.text = SquadSummary();
    }

    public FrameSeat SeatOf(GameObject ship) => ship != null && seats.TryGetValue(ship, out FrameSeat seat) ? seat : default;

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

    private void AssignSeats()
    {
        int humans = 0, cpus = 0;
        foreach (GameObject ship in players)
        {
            if (ship == null) continue;
            bool human = IsHuman(ship);
            string label = human ? "P" + ++humans : "CPU " + ++cpus;
            seats[ship] = new FrameSeat { label = label, isHuman = human, isLocal = human && humans == 1 };
        }
    }

    private static GameObject[] InRoleOrder(GameObject[] ships)
    {
        GameObject[] ordered = Array.FindAll(ships, ship => ship != null && ship.GetComponent<PlayerRoleComponent>() != null);
        Array.Sort(ordered, (a, b) => a.GetComponent<PlayerRoleComponent>().role.CompareTo(b.GetComponent<PlayerRoleComponent>().role));
        return ordered;
    }

    /// <summary>
    /// The death listener is added at runtime, not in the Inspector: ships
    /// spawned from Ship.prefab by the co-op flow can't carry a persistent
    /// listener to a scene object, so their frames never grayed out.
    /// </summary>
    private void BindSlot(PartyFrameUI frame, GameObject ship)
    {
        frame.gameObject.SetActive(ship != null);
        if (ship == null) return;
        frame.Initialize(ship, seats[ship]);
        ship.GetComponent<PlayerHealth>().OnDeath.AddListener(frame.OnPlayerDied);
    }

    private string SquadSummary()
    {
        int humans = 0;
        foreach (FrameSeat seat in seats.Values) if (seat.isHuman) humans++;
        int cpus = seats.Count - humans;
        return $"{humans} PILOT{(humans == 1 ? "" : "S")} · {cpus} CPU";
    }
}
