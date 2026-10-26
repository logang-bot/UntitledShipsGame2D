// Carries which level was picked in LevelSelect into that level's own scene.
// A plain static field survives SceneManager.LoadScene within one Play
// session but resets to null on domain reload - mirrors GameModeSelection.cs
// / PartyRoleAssignment.cs's pattern exactly. Nothing currently branches on
// this (each level's own scene already fully determines its own behavior via
// its own LevelSequencer/boss); it exists so a future HUD "Level N: Halcyon"
// title has something to read.
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
