// Carries the mode chosen in the Lobby scene (Local vs. Online) into
// RoleSelect/Gameplay. A plain static field survives SceneManager.LoadScene
// within one Play session but resets to null on domain reload (i.e. each
// fresh Play session) - mirrors PartyRoleAssignment.cs's pattern exactly.
// Unset is treated as "allowed"/local by anything that gates on this (see
// PauseUI.cs), preserving the ability to open Gameplay directly, bypassing
// Lobby, for quick iteration.
public enum GameMode
{
    Local,
    Online
}

public static class GameModeSelection
{
    public static GameMode? Mode;
}
