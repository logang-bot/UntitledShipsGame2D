// Carries which level was picked in LevelSelect into the shared Level scene,
// where LevelSetup spawns that level's boss. A plain static field survives
// SceneManager.LoadScene within one Play session (so Restart, which reloads
// the active scene, replays the same level) but resets to null on domain
// reload - mirrors GameModeSelection.cs / PartyRoleAssignment.cs's pattern
// exactly. Null (scene opened directly) means LevelSetup.editorDefaultLevel.
public enum Level
{
    Level1,
    Level2,
    Level3
}

public static class LevelSelection
{
    public static Level? SelectedLevel;
}
