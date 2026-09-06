using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Online counterpart to JoinLobbyUI - connects to the self-hosted Nakama
// server (see docs/nakama-local-setup/) and offers the two join methods
// requested: create a match and share its code, or join one by code. Quick
// Match hands off to Nakama's matchmaker instead of a specific code.
// NakamaConnection.MatchJoined fires for all three paths, so one handler
// covers them.
public class OnlineJoinUI : MonoBehaviour
{
    public TMP_Text statusText;
    public TMP_InputField matchCodeInput;
    public Button createButton;
    public Button joinButton;
    public Button quickMatchButton;

    async void Awake()
    {
        SetButtonsInteractable(false);
        NakamaConnection.Instance.MatchJoined += HandleMatchJoined;
        statusText.text = "Connecting...";
        await NakamaConnection.Instance.ConnectAsync();
        statusText.text = "Connected. Create a match or join one by code.";
        SetButtonsInteractable(true);
    }

    void OnDestroy()
    {
        if (NakamaConnection.Instance != null)
            NakamaConnection.Instance.MatchJoined -= HandleMatchJoined;
    }

    void SetButtonsInteractable(bool value)
    {
        createButton.interactable = value;
        joinButton.interactable = value;
        quickMatchButton.interactable = value;
    }

    public async void CreateMatch()
    {
        SetButtonsInteractable(false);
        statusText.text = "Creating match...";
        await NakamaConnection.Instance.CreateMatchAsync();
    }

    public async void JoinByCode()
    {
        if (string.IsNullOrWhiteSpace(matchCodeInput.text)) return;
        SetButtonsInteractable(false);
        statusText.text = "Joining match...";
        await NakamaConnection.Instance.JoinMatchAsync(matchCodeInput.text.Trim());
    }

    public async void QuickMatch()
    {
        SetButtonsInteractable(false);
        statusText.text = "Searching for a match...";
        await NakamaConnection.Instance.JoinMatchmakerAsync();
    }

    void HandleMatchJoined(Nakama.IMatch match)
    {
        statusText.text = "Joined match: " + match.Id;
        SceneManager.LoadScene("RoleSelect");
    }

    public void Back()
    {
        SceneManager.LoadScene("Lobby");
    }
}
