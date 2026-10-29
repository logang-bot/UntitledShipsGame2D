using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public MenuScreen screen;
    public Button onlineButton;

    void Awake()
    {
        // No Nakama backend yet (see roadmap.md's "Nakama networking" item) -
        // Online stays a disabled placeholder until networking lands.
        onlineButton.interactable = false;
    }

    public void SelectLocal()
    {
        GameModeSelection.Mode = GameMode.Local;
        // JoinLobby (co-op device join screen) now precedes RoleSelect - see
        // docs/systems/scene-flow.md.
        screen.GoTo("JoinLobby");
    }

    public void Back()
    {
        screen.GoBack("MainMenu");
    }
}
