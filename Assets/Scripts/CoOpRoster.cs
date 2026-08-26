using System.Collections.Generic;
using UnityEngine.InputSystem;

// Carries the set of locally-joined human players (device + control scheme,
// chosen role) from JoinLobby.unity through RoleSelect.unity into
// Gameplay.unity. A plain static class, mirroring PartyRoleAssignment.cs/
// GameModeSelection.cs's exact pattern: survives SceneManager.LoadScene
// within one Play session, resets to null on domain reload. Players is left
// null whenever the co-op join flow wasn't used (e.g. Gameplay opened
// directly, or RoleSelect reached without going through JoinLobby) - every
// consumer treats null the same way PartyRoleAssignment.HumanRole == null is
// already treated, falling back to whatever roles are hand-set in the
// Inspector, preserving the existing quick-iteration workflow.
//
// Devices are captured by reference at JoinLobby and re-used as-is in
// Gameplay: InputDevice instances (the physical Keyboard/Mouse/Gamepad
// singletons) persist for the whole application session independent of
// scene loads, so no DontDestroyOnLoad or PlayerInput persistence is needed
// here - each scene re-pairs the same physical devices fresh.
public struct JoinedPlayer
{
    public string controlScheme; // "Keyboard&Mouse" or "Gamepad"
    public InputDevice[] devices;
    public PlayerRole? role;
}

public static class CoOpRoster
{
    public static List<JoinedPlayer> Players;
}
