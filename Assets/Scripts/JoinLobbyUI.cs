using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Local co-op "press any button to join" screen, between Lobby and
// RoleSelect. JoinDeviceInput routes every button press here; joining pairs
// the device to a throwaway JoinSlotMarker.prefab PlayerInput through
// PlayerInputManager (JoinPlayersManually), and leaving destroys it. This
// script reflects PlayerInput.all into the 4 slot views and, on Continue,
// snapshots it into CoOpRoster.Players for RoleSelect/PartySetupBootstrap.
// See docs/systems/scene-flow.md.
public class JoinLobbyUI : MonoBehaviour
{
    private const int MaxPlayers = 4;
    private const string KeyboardScheme = "Keyboard&Mouse";
    private const string GamepadScheme = "Gamepad";

    public MenuScreen screen;
    public PlayerInputManager playerInputManager;
    public JoinSlotView[] slots;
    public Button continueButton;
    public TMP_Text counterText;

    public int JoinedCount => PlayerInput.all.Count;
    public bool AcceptsInput => !screen.IsLeaving;

    void Awake()
    {
        playerInputManager.onPlayerJoined += OnPlayerChanged;
        playerInputManager.onPlayerLeft += OnPlayerChanged;
    }

    void Start() => RefreshSlots();

    void OnDestroy()
    {
        playerInputManager.onPlayerJoined -= OnPlayerChanged;
        playerInputManager.onPlayerLeft -= OnPlayerChanged;
    }

    public bool IsJoined(InputDevice device) => PlayerInput.all.Any(p => p.devices.Contains(device));

    public void Handle(JoinPressAction action, InputDevice device)
    {
        if (action == JoinPressAction.Join) Join(device);
        else if (action == JoinPressAction.Leave) Leave(device);
        else if (action == JoinPressAction.Back) Back();
        else if (action == JoinPressAction.Continue) Continue();
    }

    // Snapshots the currently-paired devices into CoOpRoster and proceeds to
    // RoleSelect. Devices themselves (not the throwaway JoinSlotMarker
    // PlayerInput instances, which don't survive the scene change) are what
    // gets carried forward - see CoOpRoster.cs.
    public void Continue()
    {
        if (JoinedCount == 0) return;
        CoOpRoster.Players = PlayerInput.all.Select(ToJoinedPlayer).ToList();
        screen.GoTo("RoleSelect");
    }

    public void Back() => screen.GoBack("Lobby");

    private void Join(InputDevice device)
    {
        if (JoinedCount >= MaxPlayers) return;
        if (device is Gamepad) playerInputManager.JoinPlayer(-1, -1, GamepadScheme, device);
        else playerInputManager.JoinPlayer(-1, -1, KeyboardScheme, KeyboardAndMouse());
    }

    private static void Leave(InputDevice device)
    {
        PlayerInput player = PlayerInput.all.FirstOrDefault(p => p.devices.Contains(device));
        if (player != null) Destroy(player.gameObject);
    }

    private void OnPlayerChanged(PlayerInput player) => RefreshSlots();

    private void RefreshSlots()
    {
        IReadOnlyList<PlayerInput> joined = PlayerInput.all;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < joined.Count) slots[i].ShowJoined(SlotInfo(joined, i));
            else slots[i].ShowEmpty(i + 1);
        }
        counterText.text = $"{joined.Count}<color={NeonPalette.RichTextHex(PaletteColor.Muted)}> / {MaxPlayers}</color>";
        continueButton.interactable = joined.Count > 0;
        RefreshFocus(joined.Count);
    }

    private void RefreshFocus(int count)
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(count >= MaxPlayers ? continueButton.gameObject : null);
    }

    private static JoinSlotInfo SlotInfo(IReadOnlyList<PlayerInput> joined, int index)
    {
        bool keyboard = joined[index].currentControlScheme == KeyboardScheme;
        int gamepadNumber = joined.Take(index + 1).Count(p => p.currentControlScheme != KeyboardScheme);
        return new JoinSlotInfo { number = index + 1, isKeyboard = keyboard, gamepadNumber = gamepadNumber };
    }

    private static InputDevice[] KeyboardAndMouse()
    {
        if (Mouse.current == null) return new InputDevice[] { Keyboard.current };
        return new InputDevice[] { Keyboard.current, Mouse.current };
    }

    private static JoinedPlayer ToJoinedPlayer(PlayerInput player) => new JoinedPlayer
    {
        controlScheme = player.currentControlScheme,
        devices = player.devices.ToArray(),
        role = null,
    };
}
