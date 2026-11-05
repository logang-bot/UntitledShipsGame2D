using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// Listens to every button press on the Join Lobby screen and turns it into
// join / leave / back / continue (JoinPressRouter) for JoinLobbyUI. Replaces
// PlayerInputManager's own "join on any button", which would also join a
// player on the Esc / B press the design uses to leave or go back.
public class JoinDeviceInput : MonoBehaviour
{
    public JoinLobbyUI lobby;

    private IDisposable subscription;

    void OnEnable() => subscription = InputSystem.onAnyButtonPress.Call(OnPress);

    void OnDisable() => subscription?.Dispose();

    private void OnPress(InputControl control)
    {
        InputDevice device = control.device;
        if (!lobby.AcceptsInput || !(device is Keyboard || device is Mouse || device is Gamepad)) return;
        JoinPressAction action = JoinPressRouter.Route(KindOf(control), lobby.IsJoined(device), lobby.JoinedCount > 0);
        lobby.Handle(action, device);
    }

    private static JoinPressKind KindOf(InputControl control)
    {
        if (control.device is Keyboard keyboard) return KeyboardKind(keyboard, control);
        if (control.device is Gamepad gamepad) return GamepadKind(gamepad, control);
        return JoinPressKind.Other;
    }

    private static JoinPressKind KeyboardKind(Keyboard keyboard, InputControl control)
    {
        if (control == keyboard.escapeKey) return JoinPressKind.Back;
        bool enter = control == keyboard.enterKey || control == keyboard.numpadEnterKey;
        return enter ? JoinPressKind.Continue : JoinPressKind.Other;
    }

    private static JoinPressKind GamepadKind(Gamepad gamepad, InputControl control)
    {
        if (control == gamepad.buttonEast) return JoinPressKind.Back;
        return control == gamepad.startButton ? JoinPressKind.Continue : JoinPressKind.Other;
    }
}
