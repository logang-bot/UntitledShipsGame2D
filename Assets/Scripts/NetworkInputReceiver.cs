using Nakama;
using System.Text;
using UnityEngine;

// Lives on the host, one per remote-owned ship - parallel to AIController.
// Subscribes to this ship's owning player's PlayerInputCommand messages and
// calls the exact same non-input entry points AIController already uses
// (SetMoveDirection/SetFiring/TryUseAbility/TryComboAttack), so the host's
// simulation of a remote-controlled ship is identical in every way to a
// human- or AI-controlled one - just fed by network data instead. Disabled
// by default on Ship.prefab; NetworkPartySpawner enables it only for an
// online match's remote-owned ships, on the host only.
public class NetworkInputReceiver : MonoBehaviour
{
    public string ownerUserId;

    PlayerController controller;
    PlayerAbility ability;
    bool hasPendingCommand;
    PlayerInputCommand pendingCommand;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        ability = GetComponent<PlayerAbility>();
    }

    // OnEnable()/OnDisable(), not Awake()/OnDestroy(): Awake() always runs
    // even while this component starts disabled, so touching
    // NakamaConnection.Instance there would NRE in every offline session.
    void OnEnable() => NakamaConnection.Instance.Socket.ReceivedMatchState += HandleMatchState;

    void OnDisable()
    {
        if (NakamaConnection.Instance != null)
            NakamaConnection.Instance.Socket.ReceivedMatchState -= HandleMatchState;
    }

    void HandleMatchState(IMatchState state)
    {
        if (state.OpCode != NetworkOpCodes.PlayerInput) return;
        if (state.UserPresence.UserId != ownerUserId) return;
        pendingCommand = PlayerInputCommand.FromJson(Encoding.UTF8.GetString(state.State));
        hasPendingCommand = true;
    }

    void Update()
    {
        if (!hasPendingCommand) return;
        controller.SetMoveDirection(pendingCommand.moveDirection);
        controller.SetFiring(pendingCommand.firing);
        ApplyOneShotActions();
    }

    void ApplyOneShotActions()
    {
        if (pendingCommand.abilityPressed) ability.TryUseAbility();
        if (pendingCommand.combo1Pressed) ability.TryComboAttack(1);
        if (pendingCommand.combo2Pressed) ability.TryComboAttack(2);
        if (pendingCommand.combo3Pressed) ability.TryComboAttack(3);
        ClearOneShotFlags();
    }

    void ClearOneShotFlags()
    {
        pendingCommand.abilityPressed = false;
        pendingCommand.combo1Pressed = false;
        pendingCommand.combo2Pressed = false;
        pendingCommand.combo3Pressed = false;
    }
}
