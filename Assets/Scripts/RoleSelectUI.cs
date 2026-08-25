using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoleSelectUI : MonoBehaviour
{
    public Button startButton;

    private PlayerRole? selectedRole;

    void Awake()
    {
        startButton.interactable = false;
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
        PartyRoleAssignment.HumanRole = selectedRole.Value;
        SceneManager.LoadScene("Gameplay");
    }

    public void Back()
    {
        SceneManager.LoadScene("Lobby");
    }
}
