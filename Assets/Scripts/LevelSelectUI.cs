using UnityEngine;
using UnityEngine.SceneManagement;

// Card-per-level picker between RoleSelect and the shared Level scene. One
// click per card, no separate confirm step (unlike RoleSelectUI's
// pick-then-Start flow, which exists to gate on a variable-length
// multi-picker) - each card is already a complete, unambiguous choice, same
// shape as LobbyUI.SelectLocal(). LevelSetup reads the pick from
// LevelSelection to decide which boss to spawn.
public class LevelSelectUI : MonoBehaviour
{
    public void SelectLevel1() { LoadLevel(Level.Level1); }
    public void SelectLevel2() { LoadLevel(Level.Level2); }
    public void SelectLevel3() { LoadLevel(Level.Level3); }

    private void LoadLevel(Level level)
    {
        LevelSelection.SelectedLevel = level;
        SceneManager.LoadScene("Level");
    }

    public void Back()
    {
        SceneManager.LoadScene("RoleSelect");
    }
}
