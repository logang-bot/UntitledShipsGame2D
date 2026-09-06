using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

// The one deliberate exception to this project's "no singletons / no
// service locator" convention (architecture.md) - a live Nakama socket
// needs exactly one owner for the whole session. Lives on a GameObject
// placed in MainMenu.unity (DontDestroyOnLoad), so it survives every scene
// load from MainMenu through Gameplay. Device-ID authentication is used
// (SystemInfo.deviceUniqueIdentifier) since LAN testing has no account
// system. host/port point at the self-hosted local Nakama server from
// docs/nakama-local-setup.md - edit them in the Inspector to match it.
public class NakamaConnection : MonoBehaviour
{
    public static NakamaConnection Instance { get; private set; }

    [Header("Local Nakama server")]
    public string scheme = "http";
    public string host = "127.0.0.1";
    public int port = 7350;
    public string serverKey = "defaultkey";

    public ISocket Socket { get; private set; }
    public string LocalUserId => session?.UserId;
    public event Action<IMatch> MatchJoined;

    private IClient client;
    private ISession session;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task ConnectAsync()
    {
        client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
        session = await AuthenticateAsync();
        await ConnectSocketAsync();
    }

    async Task<ISession> AuthenticateAsync() =>
        await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier);

    async Task ConnectSocketAsync()
    {
        Socket = Nakama.Socket.From(client);
        Socket.ReceivedMatchmakerMatched += HandleMatchmakerMatched;
        await Socket.ConnectAsync(session);
    }

    public async Task CreateMatchAsync()
    {
        IMatch match = await Socket.CreateMatchAsync();
        OnMatchJoined(match, isHost: true);
    }

    public async Task JoinMatchAsync(string matchId)
    {
        IMatch match = await Socket.JoinMatchAsync(matchId, null);
        OnMatchJoined(match, isHost: false);
    }

    public Task<IMatchmakerTicket> JoinMatchmakerAsync() =>
        Socket.AddMatchmakerAsync();

    async void HandleMatchmakerMatched(IMatchmakerMatched matched)
    {
        IMatch match = await Socket.JoinMatchAsync(matched);
        OnMatchJoined(match, isHost: false);
    }

    void OnMatchJoined(IMatch match, bool isHost)
    {
        NetworkSession.MatchId = match.Id;
        NetworkSession.IsHost = isHost;
        NetworkSession.Players = match.Presences.Select(ToNetworkPlayer).ToList();
        Socket.ReceivedMatchPresence += HandleMatchPresence;
        MatchJoined?.Invoke(match);
    }

    static NetworkPlayer ToNetworkPlayer(IUserPresence presence) =>
        new NetworkPlayer { presenceId = presence.UserId, role = null };

    // Keeps NetworkSession.Players in sync with who's actually still
    // connected for the whole lifetime of the match - NetworkRoleSelectUI
    // only ever edits the `role` field of an existing entry, never adds/
    // removes players itself.
    void HandleMatchPresence(IMatchPresenceEvent presenceEvent)
    {
        foreach (IUserPresence joined in presenceEvent.Joins)
            NetworkSession.Players.Add(ToNetworkPlayer(joined));
        foreach (IUserPresence left in presenceEvent.Leaves)
            NetworkSession.Players.RemoveAll(p => p.presenceId == left.UserId);
    }
}
