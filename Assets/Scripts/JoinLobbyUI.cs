using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Local co-op "press any button to join" screen, between Lobby and
// RoleSelect. PlayerInputManager (JoinPlayersWhenButtonIsPressed) pairs each
// joining device to a throwaway JoinSlotMarker.prefab instance - this script
// just reflects PlayerInput.all's live state into 4 slot rows and, on
// Continue, snapshots it into CoOpRoster.Players for RoleSelect/
// PartySetupBootstrap to consume. See docs/systems/scene-flow.md.
public class JoinLobbyUI : MonoBehaviour
{
    public PlayerInputManager playerInputManager;
    public TMP_Text[] slotTexts; // 4 rows, one per possible local player
    public Button continueButton;

    void Awake()
    {
        continueButton.interactable = false;
        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined += OnPlayerChanged;
            playerInputManager.onPlayerLeft += OnPlayerChanged;
        }
        RefreshSlots();
    }

    void OnDestroy()
    {
        if (playerInputManager != null)
        {
            playerInputManager.onPlayerJoined -= OnPlayerChanged;
            playerInputManager.onPlayerLeft -= OnPlayerChanged;
        }
    }

    void OnPlayerChanged(PlayerInput p)
    {
        RefreshSlots();
    }

    void RefreshSlots()
    {
        IReadOnlyList<PlayerInput> joined = PlayerInput.all;
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null) continue;
            slotTexts[i].text = i < joined.Count
                ? $"Slot {i + 1}: {joined[i].currentControlScheme}"
                : $"Slot {i + 1}: Empty - press any button/key to join";
        }
        if (continueButton != null) continueButton.interactable = joined.Count > 0;
    }

    // Snapshots the currently-paired devices into CoOpRoster and proceeds to
    // RoleSelect. Devices themselves (not the throwaway JoinSlotMarker
    // PlayerInput instances, which don't survive the scene load) are what
    // gets carried forward - see CoOpRoster.cs.
    public void Continue()
    {
        IReadOnlyList<PlayerInput> joined = PlayerInput.all;
        if (joined.Count == 0) return;

        CoOpRoster.Players = new List<JoinedPlayer>();
        foreach (PlayerInput pi in joined)
        {
            CoOpRoster.Players.Add(new JoinedPlayer
            {
                controlScheme = pi.currentControlScheme,
                devices = pi.devices.ToArray(),
                role = null
            });
        }
        SceneManager.LoadScene("RoleSelect");
    }

    public void Back()
    {
        SceneManager.LoadScene("Lobby");
    }
}
