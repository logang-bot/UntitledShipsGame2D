using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One row of RoleSelectMultiUI's picker grid - one instance per joined
// local player. Polls that player's own paired device directly (dpad/stick
// or WASD to move a highlight, South/Enter to confirm, West/Escape to
// unlock) rather than going through the PlayerControls InputAction asset or
// Unity's EventSystem UI navigation - a second EventSystem/
// InputSystemUIInputModule per player is real, correct Unity functionality
// but meaningfully more infrastructure than a 4-role button grid needs here.
public class RolePickerRow : MonoBehaviour
{
    private const float MoveRepeatDelay = 0.25f;
    private const string KeyboardScheme = "Keyboard&Mouse";

    public TMP_Text playerNumber;
    public Image deviceIcon;
    public TMP_Text deviceLabel;
    public TMP_Text statusText;
    public RoleTileView[] tiles; // index = (int)PlayerRole
    public Sprite keyboardIcon;
    public Sprite gamepadIcon;

    private RoleSelectMultiUI owner;
    private int slotIndex;
    private int hoverIndex;
    private PlayerRole? lockedRole;
    private Gamepad gamepad;
    private Keyboard keyboard;
    private float nextMoveTime;

    public void Init(JoinedPlayer joinedPlayer, int index, RoleSelectMultiUI ownerUI)
    {
        slotIndex = index;
        owner = ownerUI;
        gamepad = joinedPlayer.devices?.OfType<Gamepad>().FirstOrDefault();
        keyboard = joinedPlayer.devices?.OfType<Keyboard>().FirstOrDefault();
        ShowPlayer(joinedPlayer.controlScheme == KeyboardScheme);
        hoverIndex = 0;
        lockedRole = null;
        Refresh();
    }

    void Update()
    {
        if (owner == null) return;
        if (lockedRole.HasValue)
        {
            if (ConfirmUnlockPressed()) Unlock();
            return;
        }
        MoveHover();
        if (ConfirmPressed()) TryLock((PlayerRole)hoverIndex);
    }

    public void Refresh()
    {
        foreach (RoleTileView tile in tiles) ShowTile(tile);
        bool locked = lockedRole.HasValue;
        statusText.text = locked ? $"LOCKED · {RoleCardText.RoleName(lockedRole.Value)}" : "CHOOSING…";
        statusText.color = NeonPalette.Get(locked ? RolePalette.TokenFor(lockedRole.Value) : PaletteColor.Notice);
    }

    private void ShowPlayer(bool isKeyboard)
    {
        playerNumber.text = $"P{slotIndex + 1}";
        deviceIcon.sprite = isKeyboard ? keyboardIcon : gamepadIcon;
        deviceLabel.text = isKeyboard ? "KEYBOARD + MOUSE" : $"GAMEPAD {GamepadNumber()}";
    }

    private int GamepadNumber() => CoOpRoster.Players.Take(slotIndex + 1).Count(p => p.controlScheme != KeyboardScheme);

    private void ShowTile(RoleTileView tile)
    {
        int? holder = owner != null ? owner.HolderOf(tile.role) : null;
        if (lockedRole == tile.role) tile.Show(RoleTileState.Locked, slotIndex + 1);
        else if (holder.HasValue && holder.Value != slotIndex) tile.Show(RoleTileState.Taken, holder.Value + 1);
        else if (lockedRole.HasValue) tile.Show(RoleTileState.Muted, slotIndex + 1);
        else tile.Show(hoverIndex == (int)tile.role ? RoleTileState.Hover : RoleTileState.Normal, slotIndex + 1);
    }

    private void MoveHover()
    {
        if (Time.unscaledTime < nextMoveTime) return;
        int move = ReadMoveDirection();
        if (move == 0) return;
        hoverIndex = (hoverIndex + move + tiles.Length) % tiles.Length;
        nextMoveTime = Time.unscaledTime + MoveRepeatDelay;
        Refresh();
    }

    private void TryLock(PlayerRole role)
    {
        if (!owner.TryLockRole(slotIndex, role)) return;
        lockedRole = role;
        Refresh();
    }

    private int ReadMoveDirection()
    {
        if (gamepad != null && (gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame)) return 1;
        if (gamepad != null && (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame)) return -1;
        if (keyboard != null && (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)) return 1;
        if (keyboard != null && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)) return -1;
        return 0;
    }

    private bool ConfirmPressed() =>
        (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) || (keyboard != null && keyboard.enterKey.wasPressedThisFrame);

    private bool ConfirmUnlockPressed() =>
        (gamepad != null && gamepad.buttonWest.wasPressedThisFrame) || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);

    private void Unlock()
    {
        owner.Unlock(slotIndex, lockedRole.Value);
        lockedRole = null;
        Refresh();
    }
}
