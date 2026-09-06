using UnityEngine;
using UnityEngine.InputSystem;

// Lives on a remote client's own locally-controlled ship, replacing
// PlayerController/PlayerAbility as the thing that turns local input into
// ship behaviour on that machine. Reads PlayerControls' "Player" action map
// directly through its own cloned InputActionAsset instance (the same
// standalone-InputAction idiom PauseUI.cs already uses, rather than
// PlayerInput's Send Messages) - GameObject.SendMessage reaches disabled
// Behaviours too, so leaving PlayerInput enabled on this ship would still
// fire PlayerController/PlayerAbility's own OnMove/OnFire/OnAbility and
// double-drive it locally. Ship.prefab's PlayerInput stays disabled on this
// ship; the host's own copy of it (via NetworkInputReceiver) is what
// actually simulates it - this component only captures and forwards.
public class NetworkInputSender : MonoBehaviour
{
    public InputActionAsset controlsAsset;

    InputAction moveAction;
    InputAction fireAction;
    InputAction abilityAction;
    InputAction combo1Action;
    InputAction combo2Action;
    InputAction combo3Action;
    bool initialized;

    // Not Awake(): Awake() always runs even while this component starts
    // disabled on Ship.prefab (only NetworkPartySpawner enables it, and only
    // for an online match's local human ship) - OnEnable() is what Unity
    // actually gates on the enabled flag, so offline/local play never
    // touches controlsAsset at all.
    void OnEnable()
    {
        if (initialized) return;
        InputActionMap map = Instantiate(controlsAsset).FindActionMap("Player");
        map.Enable();
        BindActions(map);
        initialized = true;
    }

    void BindActions(InputActionMap map)
    {
        moveAction = map.FindAction("Move");
        fireAction = map.FindAction("Fire");
        abilityAction = map.FindAction("Ability");
        combo1Action = map.FindAction("Combo1");
        combo2Action = map.FindAction("Combo2");
        combo3Action = map.FindAction("Combo3");
    }

    void FixedUpdate()
    {
        PlayerInputCommand command = ReadCommand();
        NakamaConnection.Instance.Socket.SendMatchStateAsync(
            NetworkSession.MatchId, NetworkOpCodes.PlayerInput, command.ToJson());
    }

    PlayerInputCommand ReadCommand() => new PlayerInputCommand
    {
        moveDirection = moveAction.ReadValue<Vector2>(),
        firing = fireAction.IsPressed(),
        abilityPressed = abilityAction.WasPressedThisFrame(),
        combo1Pressed = combo1Action.WasPressedThisFrame(),
        combo2Pressed = combo2Action.WasPressedThisFrame(),
        combo3Pressed = combo3Action.WasPressedThisFrame()
    };
}
