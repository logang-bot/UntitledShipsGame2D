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
    public TMP_Text deviceLabel;
    public Image[] roleSwatches; // 4, index = (int)PlayerRole
    public TMP_Text[] roleLabels; // 4
    public TMP_Text statusText;
    public Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    public Color hoverColor = new Color(1f, 1f, 1f, 0.4f);
    public Color lockedColor = new Color(0.3f, 1f, 0.4f, 0.8f);
    public Color takenColor = new Color(1f, 0.3f, 0.3f, 0.3f);

    private RoleSelectMultiUI owner;
    private int slotIndex;
    private int hoverIndex;
    private PlayerRole? lockedRole;
    private Gamepad gamepad;
    private Keyboard keyboard;
    private float nextMoveTime;
    private const float MoveRepeatDelay = 0.25f;

    private static readonly string[] RoleNames = { "Attacker", "Tank", "Medic", "Support" };

    public void Init(JoinedPlayer joinedPlayer, int index, RoleSelectMultiUI ownerUI)
    {
        slotIndex = index;
        owner = ownerUI;
        if (deviceLabel != null) deviceLabel.text = $"Player {index + 1} ({joinedPlayer.controlScheme})";

        gamepad = null;
        keyboard = null;
        if (joinedPlayer.devices != null)
        {
            foreach (InputDevice d in joinedPlayer.devices)
            {
                if (d is Gamepad gp) gamepad = gp;
                if (d is Keyboard kb) keyboard = kb;
            }
        }

        for (int i = 0; i < roleLabels.Length && i < RoleNames.Length; i++)
            if (roleLabels[i] != null) roleLabels[i].text = RoleNames[i];

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

        if (Time.unscaledTime >= nextMoveTime)
        {
            int move = ReadMoveDirection();
            if (move != 0)
            {
                hoverIndex = (hoverIndex + move + 4) % 4;
                nextMoveTime = Time.unscaledTime + MoveRepeatDelay;
                Refresh();
            }
        }

        if (ConfirmPressed())
        {
            PlayerRole role = (PlayerRole)hoverIndex;
            if (owner.TryLockRole(slotIndex, role))
            {
                lockedRole = role;
                Refresh();
            }
        }
    }

    private int ReadMoveDirection()
    {
        if (gamepad != null)
        {
            if (gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame) return 1;
            if (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame) return -1;
        }
        if (keyboard != null)
        {
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) return 1;
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) return -1;
        }
        return 0;
    }

    private bool ConfirmPressed()
    {
        if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) return true;
        if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) return true;
        return false;
    }

    private bool ConfirmUnlockPressed()
    {
        if (gamepad != null && gamepad.buttonWest.wasPressedThisFrame) return true;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) return true;
        return false;
    }

    private void Unlock()
    {
        if (!lockedRole.HasValue) return;
        owner.Unlock(slotIndex, lockedRole.Value);
        lockedRole = null;
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < roleSwatches.Length; i++)
        {
            if (roleSwatches[i] == null) continue;
            PlayerRole role = (PlayerRole)i;
            bool isLocked = lockedRole.HasValue && lockedRole.Value == role;
            bool isTakenByOther = owner != null && owner.IsRoleTakenByOther(slotIndex, role);
            Color c = isLocked ? lockedColor
                : isTakenByOther ? takenColor
                : (hoverIndex == i ? hoverColor : normalColor);
            roleSwatches[i].color = c;
        }
        if (statusText != null)
            statusText.text = lockedRole.HasValue ? $"Locked: {lockedRole.Value}" : "<-/-> choose, Enter/A confirm";
    }
}
