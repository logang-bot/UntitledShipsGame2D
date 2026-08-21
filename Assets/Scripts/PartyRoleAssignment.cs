// Carries the human player's chosen role from the RoleSelect scene into
// Gameplay scene. A plain static field survives SceneManager.LoadScene
// within one Play session but resets to null on domain reload (i.e. each
// fresh Play session) - this is what preserves the original Inspector-only
// role testing workflow when Gameplay is opened directly, bypassing
// RoleSelect: PartySetupBootstrap sees no assignment and no-ops.
public static class PartyRoleAssignment
{
    public static PlayerRole? HumanRole;
}
