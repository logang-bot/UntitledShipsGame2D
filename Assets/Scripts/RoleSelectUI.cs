using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Routes to one of two panels depending on how many local humans joined via
// JoinLobby: the original single 4-button picker (CoOpRoster.Players ==
// null - direct scene open, or exactly 1 joined player) or
// RoleSelectMultiUI's per-player row picker (2+ joined players). See
// docs/systems/player-roles.md's "Role Select scene" section.
public class RoleSelectUI : MonoBehaviour
{
    public GameObject singlePickerPanel;
    public GameObject multiPickerPanel;
    public Button startButton;

    private PlayerRole? selectedRole;

    void Awake()
    {
        startButton.interactable = false;

        bool useMulti = CoOpRoster.Players != null && CoOpRoster.Players.Count >= 2;
        if (singlePickerPanel != null) singlePickerPanel.SetActive(!useMulti);
        if (multiPickerPanel != null) multiPickerPanel.SetActive(useMulti);
    }

    public void SelectAttacker() { SelectRole(PlayerRole.Attacker); }
    public void SelectTank() { SelectRole(PlayerRole.Tank); }
    public void SelectMedic() { SelectRole(PlayerRole.Medic); }
    public void SelectSupport() { SelectRole(PlayerRole.Support); }

    private void SelectRole(PlayerRole role)
    {
        selectedRole = role;
        startButton.interactable = true;
    }

    public void StartGame()
    {
        if (!selectedRole.HasValue) return;

        // Exactly 1 human joined through JoinLobby still needs to carry a
        // paired device into Gameplay via CoOpRoster - only a true
        // direct-scene-open (CoOpRoster.Players == null) uses the older
        // PartyRoleAssignment single-field carrier.
        if (CoOpRoster.Players != null && CoOpRoster.Players.Count == 1)
        {
            JoinedPlayer entry = CoOpRoster.Players[0];
            entry.role = selectedRole.Value;
            CoOpRoster.Players[0] = entry;
        }
        else
        {
            PartyRoleAssignment.HumanRole = selectedRole.Value;
        }
        SceneManager.LoadScene("Gameplay");
    }

    public void Back()
    {
        // JoinLobby precedes RoleSelect now whenever the co-op flow was
        // used; a direct-open (CoOpRoster.Players == null) still has
        // somewhere sane to go back to.
        SceneManager.LoadScene(CoOpRoster.Players != null ? "JoinLobby" : "Lobby");
    }
}
