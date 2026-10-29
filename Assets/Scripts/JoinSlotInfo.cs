// What one joined Join Lobby slot shows: its player number and which device
// that player is on (keyboard + mouse, or the Nth gamepad in join order).
public struct JoinSlotInfo
{
    public int number;
    public bool isKeyboard;
    public int gamepadNumber;

    public string DeviceLabel => isKeyboard ? "KEYBOARD + MOUSE" : $"GAMEPAD {gamepadNumber}";
    public string LeaveHint => isKeyboard ? "ESC · LEAVE" : "B · LEAVE";
}
