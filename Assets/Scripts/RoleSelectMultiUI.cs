using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Multi-human half of RoleSelectUI's flow (CoOpRoster.Players.Count >= 2) -
// one RolePickerRow per joined player, each polling its own paired device
// directly (see RolePickerRow.cs). Enforces distinct role picks across rows,
// shows the resulting squad (unlocked roles read as CPU), and enables Start
// once every joined player has locked a role.
public class RoleSelectMultiUI : MonoBehaviour
{
    private const int MaxPlayers = 4;

    public MenuScreen screen;
    public RolePickerRow rowPrefab;
    public Transform rowContainer;
    public Button startButton;
    public MenuButton startMenuButton;
    public SquadStripView squad;
    public TMP_Text playerCountText;
    public GameObject notJoinedLine;
    public TMP_Text notJoinedText;

    private RolePickerRow[] rows;
    private PlayerRole?[] lockedRoles;

    void OnEnable()
    {
        if (CoOpRoster.Players == null) return;
        BuildRows();
    }

    public int? HolderOf(PlayerRole role)
    {
        if (lockedRoles == null) return null;
        int index = Array.FindIndex(lockedRoles, r => r == role);
        return index >= 0 ? index : (int?)null;
    }

    public bool IsRoleTakenByOther(int slotIndex, PlayerRole role)
    {
        int? holder = HolderOf(role);
        return holder.HasValue && holder.Value != slotIndex;
    }

    public bool TryLockRole(int slotIndex, PlayerRole role)
    {
        if (IsRoleTakenByOther(slotIndex, role)) return false;
        SetLock(slotIndex, role);
        return true;
    }

    public void Unlock(int slotIndex, PlayerRole role) => SetLock(slotIndex, null);

    public void StartGame()
    {
        if (!AllLocked()) return;
        screen.GoTo("LevelSelect");
    }

    public void Back()
    {
        screen.GoBack("JoinLobby");
    }

    private void BuildRows()
    {
        for (int i = rowContainer.childCount - 1; i >= 0; i--)
            Destroy(rowContainer.GetChild(i).gameObject);
        int count = CoOpRoster.Players.Count;
        rows = new RolePickerRow[count];
        lockedRoles = new PlayerRole?[count];
        for (int i = 0; i < count; i++) rows[i] = BuildRow(i);
        ShowHeaderTexts(count);
        RefreshSquadAndStart();
    }

    // Resets any stale pick from a previous visit to this scene (e.g. via
    // "Change Roles") before the row reads it.
    private RolePickerRow BuildRow(int index)
    {
        JoinedPlayer entry = CoOpRoster.Players[index];
        entry.role = null;
        CoOpRoster.Players[index] = entry;
        RolePickerRow row = Instantiate(rowPrefab, rowContainer);
        row.gameObject.SetActive(true);
        row.Init(entry, index, this);
        return row;
    }

    private void SetLock(int slotIndex, PlayerRole? role)
    {
        lockedRoles[slotIndex] = role;
        JoinedPlayer entry = CoOpRoster.Players[slotIndex];
        entry.role = role;
        CoOpRoster.Players[slotIndex] = entry;
        RefreshSquadAndStart();
        foreach (RolePickerRow row in rows) if (row != null) row.Refresh();
    }

    private void ShowHeaderTexts(int count)
    {
        playerCountText.text = $"04 / 05 · {count} PLAYERS";
        notJoinedLine.SetActive(count < MaxPlayers);
        notJoinedText.text = NotJoinedLine(count);
    }

    private void RefreshSquadAndStart()
    {
        squad.Show(SquadLineup.FromLocks(lockedRoles));
        startButton.interactable = AllLocked();
        screen.Focus(AllLocked() ? startButton : null);
        int waiting = Array.FindIndex(lockedRoles, r => !r.HasValue);
        startMenuButton.SetDisabledReason(waiting >= 0 ? $"WAITING FOR P{waiting + 1} TO LOCK" : "");
    }

    private bool AllLocked() => lockedRoles != null && Array.TrueForAll(lockedRoles, r => r.HasValue);

    private static string NotJoinedLine(int count)
    {
        string missing = "";
        for (int n = count + 1; n <= MaxPlayers; n++) missing += (missing.Length > 0 ? " · " : "") + "P" + n;
        return $"{missing} NOT JOINED — THEIR ROLES GO TO CPU";
    }
}
