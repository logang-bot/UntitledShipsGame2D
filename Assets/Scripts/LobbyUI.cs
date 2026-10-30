using UnityEngine;

// Mode select: LOCAL goes on to JoinLobby. ONLINE is a locked card, not a
// button - there's no Nakama backend yet (see roadmap.md's "Nakama
// networking") - so focus skips it and a click only shows a toast.
public class LobbyUI : MonoBehaviour
{
    public MenuScreen screen;
    public MenuToast toast;
    public string onlineNotice = "Online co-op arrives in a later update.";

    public void SelectLocal()
    {
        GameModeSelection.Mode = GameMode.Local;
        // JoinLobby (co-op device join screen) now precedes RoleSelect - see
        // docs/systems/scene-flow.md.
        screen.GoTo("JoinLobby");
    }

    public void ShowOnlineNotice() => toast.Show(onlineNotice);

    public void Back()
    {
        screen.GoBack("MainMenu");
    }
}
