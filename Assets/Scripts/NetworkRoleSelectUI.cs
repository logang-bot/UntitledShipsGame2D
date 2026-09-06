using Nakama;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Online counterpart to RoleSelectUI's single-picker panel - each connected
// client picks its own role locally; picks are broadcast over the match and
// reflected to everyone via NetworkSession.Players (kept in sync by
// NakamaConnection's roster tracking). Unlike local co-op's RolePickerRow
// grid, there isn't one screen showing every player's own row - each client
// only sees/confirms its own pick, plus greyed-out buttons for roles others
// already locked. Only the host can start the match once ready; everyone
// else waits for the host's GameStart broadcast.
public class NetworkRoleSelectUI : MonoBehaviour
{
    public Button[] roleButtons; // 4, index = (int)PlayerRole
    public Button startButton;
    // Shown so the host can still read/share the match code after
    // OnlineJoinUI's own scene has already moved on to this one.
    public TMP_Text matchCodeText;

    PlayerRole? selectedRole;

    void Awake()
    {
        startButton.interactable = false;
        startButton.gameObject.SetActive(NetworkSession.IsHost);
        if (matchCodeText != null) matchCodeText.text = "Match code: " + NetworkSession.MatchId;
        NakamaConnection.Instance.Socket.ReceivedMatchState += HandleMatchState;
        RefreshButtons();
    }

    void OnDestroy()
    {
        if (NakamaConnection.Instance != null)
            NakamaConnection.Instance.Socket.ReceivedMatchState -= HandleMatchState;
    }

    public void SelectRole(int roleIndex)
    {
        PlayerRole role = (PlayerRole)roleIndex;
        if (IsRoleTaken(role)) return;

        selectedRole = role;
        ApplyPick(NakamaConnection.Instance.LocalUserId, role);
        if (NetworkSession.IsHost) startButton.interactable = true;
        BroadcastPick(role);
        RefreshButtons();
    }

    void BroadcastPick(PlayerRole role)
    {
        var message = new RolePickMessage { presenceId = NakamaConnection.Instance.LocalUserId, role = role };
        NakamaConnection.Instance.Socket.SendMatchStateAsync(
            NetworkSession.MatchId, NetworkOpCodes.RolePick, JsonUtility.ToJson(message));
    }

    void HandleMatchState(IMatchState state)
    {
        if (state.OpCode == NetworkOpCodes.RolePick) HandleRolePick(state);
        else if (state.OpCode == NetworkOpCodes.GameStart) SceneManager.LoadScene("Gameplay");
    }

    void HandleRolePick(IMatchState state)
    {
        var message = JsonUtility.FromJson<RolePickMessage>(Encoding.UTF8.GetString(state.State));
        ApplyPick(message.presenceId, message.role);
        RefreshButtons();
    }

    void ApplyPick(string presenceId, PlayerRole role)
    {
        for (int i = 0; i < NetworkSession.Players.Count; i++)
        {
            if (NetworkSession.Players[i].presenceId != presenceId) continue;
            NetworkPlayer p = NetworkSession.Players[i];
            p.role = role;
            NetworkSession.Players[i] = p;
        }
    }

    bool IsRoleTaken(PlayerRole role) => NetworkSession.Players.Any(
        p => p.role == role && p.presenceId != NakamaConnection.Instance.LocalUserId);

    void RefreshButtons()
    {
        for (int i = 0; i < roleButtons.Length; i++)
            roleButtons[i].interactable = selectedRole != (PlayerRole)i && !IsRoleTaken((PlayerRole)i);
    }

    public void StartGame()
    {
        NakamaConnection.Instance.Socket.SendMatchStateAsync(NetworkSession.MatchId, NetworkOpCodes.GameStart, "");
        SceneManager.LoadScene("Gameplay");
    }
}
