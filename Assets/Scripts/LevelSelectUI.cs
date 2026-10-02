using UnityEngine;
using UnityEngine.SceneManagement;

// Card-per-level picker between RoleSelect and each level's own scene. One
// click per card, no separate confirm step (unlike RoleSelectUI's
// pick-then-Start flow, which exists to gate on a variable-length
// multi-picker) - each card is already a complete, unambiguous choice, same
// shape as LobbyUI.SelectLocal(). See LevelSelection.cs.
public class LevelSelectUI : MonoBehaviour
{
    public void SelectLevel1() { LoadLevel(Level.Level1, "Level1"); }
    public void SelectLevel2() { LoadLevel(Level.Level2, "Level2"); }
    public void SelectLevel3() { LoadLevel(Level.Level3, "Level3"); }

    private void LoadLevel(Level level, string sceneName)
    {
        LevelSelection.SelectedLevel = level;
        SceneManager.LoadScene(sceneName);
    }

    public void Back()
    {
        SceneManager.LoadScene("RoleSelect");
    }
}
