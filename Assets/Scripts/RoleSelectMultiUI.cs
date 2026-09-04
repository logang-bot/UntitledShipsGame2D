using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Multi-human half of RoleSelectUI's flow (CoOpRoster.Players.Count >= 2) -
// one RolePickerRow per joined player, each polling its own paired device
// directly (see RolePickerRow.cs). Enforces distinct role picks across rows
// and enables Start once every joined player has locked a role.
public class RoleSelectMultiUI : MonoBehaviour
{
    public RolePickerRow rowPrefab;
    public Transform rowContainer;
    public Button startButton;

    private RolePickerRow[] rows;
    private PlayerRole?[] lockedRoles;

    void OnEnable()
    {
        if (CoOpRoster.Players == null) return;
        BuildRows();
    }

    private void BuildRows()
    {
        for (int i = rowContainer.childCount - 1; i >= 0; i--)
            Destroy(rowContainer.GetChild(i).gameObject);

        int count = CoOpRoster.Players.Count;
        rows = new RolePickerRow[count];
        lockedRoles = new PlayerRole?[count];
        for (int i = 0; i < count; i++)
        {
            // Reset any stale pick from a previous visit to this scene
            // (e.g. via "Change Roles").
            JoinedPlayer entry = CoOpRoster.Players[i];
            entry.role = null;
            CoOpRoster.Players[i] = entry;

            RolePickerRow row = Instantiate(rowPrefab, rowContainer);
            row.gameObject.SetActive(true);
            row.Init(entry, i, this);
            rows[i] = row;
        }
        RefreshStartButton();
    }

    public bool IsRoleTakenByOther(int slotIndex, PlayerRole role)
    {
        if (lockedRoles == null) return false;
        for (int i = 0; i < lockedRoles.Length; i++)
        {
            if (i == slotIndex) continue;
            if (lockedRoles[i].HasValue && lockedRoles[i].Value == role) return true;
        }
        return false;
    }

    public bool TryLockRole(int slotIndex, PlayerRole role)
    {
        if (IsRoleTakenByOther(slotIndex, role)) return false;
        lockedRoles[slotIndex] = role;
        JoinedPlayer entry = CoOpRoster.Players[slotIndex];
        entry.role = role;
        CoOpRoster.Players[slotIndex] = entry;
        RefreshStartButton();
        RefreshAllRows();
        return true;
    }

    public void Unlock(int slotIndex, PlayerRole role)
    {
        lockedRoles[slotIndex] = null;
        JoinedPlayer entry = CoOpRoster.Players[slotIndex];
        entry.role = null;
        CoOpRoster.Players[slotIndex] = entry;
        RefreshStartButton();
        RefreshAllRows();
    }

    private void RefreshAllRows()
    {
        if (rows == null) return;
        foreach (RolePickerRow row in rows)
            if (row != null) row.Refresh();
    }

    private void RefreshStartButton()
    {
        bool allLocked = lockedRoles != null && System.Array.TrueForAll(lockedRoles, r => r.HasValue);
        startButton.interactable = allLocked;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void Back()
    {
        SceneManager.LoadScene("JoinLobby");
    }
}
