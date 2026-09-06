using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    public void SelectLocal()
    {
        GameModeSelection.Mode = GameMode.Local;
        // JoinLobby (co-op device join screen) now precedes RoleSelect - see
        // docs/systems/scene-flow.md.
        SceneManager.LoadScene("JoinLobby");
    }

    // Nakama networking landed - see roadmap.md's "Nakama networking" item -
    // routes to OnlineJoin (create/join-by-code/quick match) instead of
    // JoinLobby's local device-join screen.
    public void SelectOnline()
    {
        GameModeSelection.Mode = GameMode.Online;
        SceneManager.LoadScene("OnlineJoin");
    }

    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
