# Networking (Nakama, host-authoritative)

Online co-op over a self-hosted Nakama server — LAN testing for now (setup
steps in `../nakama-local-setup/`, kept separate from this reference since
it's server/infra, not Unity code). Jumped the roadmap's stated order
(`roadmap.md` originally sequenced this after the bullet-hell/deck layer)
because the deck layer is being built by a different developer in parallel,
so there's no technical dependency forcing a wait.

**Authority model: host-authoritative / relayed, not Nakama-authoritative.**
Nakama just relays match-state messages between clients; one connected
player's own client ("the host" — whoever called `CreateMatchAsync()`) runs
the *entire* existing single-player/local-co-op simulation exactly as
before, and everyone else's clients become thin renderers. This was a
deliberate choice over writing a true Nakama authoritative match handler
(Go/Lua/TS): this is cooperative PvE, so a cheating host can only ruin their
own party's fight, not gain advantage over an opponent — a much lower-stakes
problem than in competitive PvP — and host-authoritative reuses 100% of the
existing C# simulation instead of reimplementing boss AI/combat/collision a
second time in an unfamiliar server runtime. **Known, deliberately deferred
limitation**: a modified host client can fake damage/kills/HP against its
own party. The planned fix is migrating specific state (starting with boss
HP/damage resolution) to a real Nakama authoritative match handler once
online play is proven fun; not started.

## The core seam: a third caller of the existing dual-entry-point pattern

`architecture.md`'s "Dual Entry-Point Pattern" already lets `AIController`
drive a ship through `PlayerController.SetMoveDirection()`/`SetFiring()`
and `PlayerAbility.TryUseAbility()`/`TryComboAttack()` — the same methods a
human's `PlayerInput` Send-Messages calls trigger. Networking adds a
**third** caller, `NetworkInputReceiver`, fed by network messages instead
of a behavior tree. No existing gameplay script's core logic changes as a
result — the host's simulation of a network-driven ship is identical in
every way to an AI- or human-driven one.

## NakamaConnection.cs

**Attached to:** a `NakamaConnection` GameObject in `MainMenu.unity`
(`DontDestroyOnLoad`, survives every scene load through `Gameplay`). The
one deliberate exception to "no singletons" in this codebase (see
`architecture.md`) — a live Nakama socket needs exactly one owner.

Device-ID authentication (`SystemInfo.deviceUniqueIdentifier`) — no account
system needed for LAN testing. `scheme`/`host`/`port`/`serverKey` are
Inspector fields pointing at the self-hosted server (`nakama-local-setup/`).
`ConnectAsync()` authenticates and opens the socket; `CreateMatchAsync()`/
`JoinMatchAsync(matchId)`/`JoinMatchmakerAsync()` cover both join methods
`OnlineJoinUI` offers. `OnMatchJoined()` seeds `NetworkSession.Players` from
`IMatch.Presences` and subscribes to `ReceivedMatchPresence` to keep that
roster in sync (join/leave) for the match's lifetime — `NetworkRoleSelectUI`
only ever edits an existing entry's `role`, never adds/removes players.

## NetworkSession.cs / NetworkShipEntry.cs

`NetworkSession` is a plain static carrier, same pattern as
`CoOpRoster.cs`/`PartyRoleAssignment.cs`: `MatchId`, `IsHost`, and
`List<NetworkPlayer>` (`presenceId` + picked `PlayerRole?`). `null`/empty
means the online flow wasn't used — every consumer falls back through the
same chain `CoOpRoster.Players`/`PartyRoleAssignment.HumanRole` already
established. `NetworkShipEntry` (`id` + `GameObject`) is the small handshake
type `NetworkPartySpawner` hands to `HostStateBroadcaster`/
`RemoteStateReceiver` so a snapshot's ship id resolves back to the right
scene object — a presence's `UserId` for a player-owned ship, or
`"ai:<role>"` for an AI-filled slot.

## PlayerInputCommand.cs — the missing primitive

Today's input is continuous `Update()`/`FixedUpdate()` polling with nothing
serializable to send over a wire. `PlayerInputCommand` is that: a
`[Serializable]` struct (`moveDirection`, `firing`, `abilityPressed`,
`combo1/2/3Pressed`) with `ToJson()`/`FromJson()` via `JsonUtility` — plain
data, matching this project's no-extra-library style. Covered by
`Assets/Tests/EditMode/PlayerInputCommandTests.cs`.

## NetworkInputSender.cs / NetworkInputReceiver.cs

Both live on `Ship.prefab` (disabled by default, alongside `PlayerInput`/
`AIController`) — extending the "every ship carries every driver, only one
enabled" convention `player-roles.md`'s "Local co-op" section already
established, to a third driver kind.

- **`NetworkInputSender`** (a remote client's own locally-controlled ship):
  reads `PlayerControls`' "Player" action map directly through its own
  cloned `InputActionAsset` instance — the same standalone-`InputAction`
  idiom `PauseUI.cs` already uses — rather than `PlayerInput`'s Send
  Messages. This matters: `GameObject.SendMessage` reaches disabled
  `MonoBehaviour`s too, so leaving `PlayerInput` enabled here would still
  fire `PlayerController`/`PlayerAbility`'s own `OnMove`/`OnFire` and
  double-drive the ship locally. Polls every `FixedUpdate`, sends a
  `PlayerInputCommand` (`NetworkOpCodes.PlayerInput`).
- **`NetworkInputReceiver`** (the host, one per remote-owned ship, parallel
  to `AIController`): buffers the latest command from its owning
  `ownerUserId` and calls `SetMoveDirection()`/`SetFiring()`/
  `TryUseAbility()`/`TryComboAttack()` every frame — move/fire re-applied
  continuously (idempotent, matches `AIController`'s own pattern), ability/
  combo presses consumed exactly once per edge.

Both use `OnEnable()`/`OnDisable()`, not `Awake()`/`OnDestroy()`, for their
Nakama-touching setup — `Awake()` always runs even while a component starts
disabled, so touching `NakamaConnection.Instance` there would throw in
every offline session.

## NetworkPartySpawner.cs

Network-mode counterpart to `PartySetupBootstrap.SpawnDynamicParty()`, kept
in its own file since that method alone was already near this project's
per-file line budget. `PartySetupBootstrap.Awake()` now checks
`NetworkSession.Players` first (before `CoOpRoster.Players`, before the
legacy `PartyRoleAssignment` fallback) and delegates here. Two extracted
helpers on `PartySetupBootstrap` — `CaptureMarkerPositions()` and
`WireSpawnedParty()` — are shared with `SpawnDynamicParty()` so the spawn
tail (allies/boss wiring, `levelSequencer.ships`, `partyFrameManager.players`,
`boss.targets`) isn't duplicated.

Per `NetworkPlayer`: the local human's own ship gets `PlayerInput` enabled
(host) or `NetworkInputSender` enabled (client); every other player's ship
gets `NetworkInputReceiver` enabled *on the host only*; unpicked roles still
fall back to `AIController`, exactly like `SpawnDynamicParty()`. On a
non-host client, every ship's `PlayerController` is explicitly disabled
(`ConfigureThinClient`) — no client-side prediction, the host is the sole
simulator, full stop (see "Explicitly out of scope" below).

## Authority gating — simpler than expected

`EnemySpawner`, `Level1Boss`, and `MinionSpawner` all already "don't
self-start" (confirmed in code, not assumed) — `LevelSequencer.RunSequence()`
is the *only* thing that ever calls `EnemySpawner.StartSpawning()` or
enables `Level1Boss` (which itself enables `MinionSpawner` via its own
`OnEnable()`). So gating just `LevelSequencer.Start()` — skip running the
whole coroutine on a non-host client — is sufficient; no separate
"authority gate" component was needed. `LevelSequencer`'s freeze/unfreeze
driver-restoration (previously a `shipIsHuman` bool, see `player-roles.md`'s
"Local co-op") was generalized to a 3-way `ShipDriver { Human, AI,
NetworkReceiver }` enum so the host's own freeze/unfreeze during the intro/
boss-entrance correctly restores `NetworkInputReceiver`-driven ships too.

## HostStateBroadcaster.cs / RemoteStateReceiver.cs / NetworkStateSnapshot.cs

Both live disabled in `Gameplay.unity` (wired into `PartySetupBootstrap`'s
Inspector fields); `NetworkPartySpawner` `Initialize()`s and enables
whichever one applies. Every `ticksBetweenBroadcasts` `FixedUpdate`s
(default 3, ~20Hz), the host sends one `PartyStateSnapshot`
(`NetworkOpCodes.StateSnapshot`) covering:

- **Ships** (`ShipStateSnapshot`): `id`, `position`, `health`, `shield`,
  `firing` (cosmetic). `RemoteStateReceiver` lerps position for smoothness
  and overwrites health/shield via `PlayerHealth.ApplySyncedState()` — a
  plain field overwrite, deliberately bypassing `TakeDamage()`/`Heal()`'s
  die/event logic (the host already resolved those). **Damage-flash/
  camera-shake/death-overlay feedback from synced state isn't wired yet** —
  a follow-up, not a correctness gap.
- **Boss** (`BossStateSnapshot`): `active` (explicit flag, not a nullable
  reference — `JsonUtility`'s null handling for reference-typed fields is
  unreliable), `health`, `maxHealth`, `phase2`, `currentTargetId` (one of
  the ship ids above). `Level1Boss.ApplySyncedState()` fires `OnPhase2` on
  the same transition edge `TakeDamage()` would, so `BossPanelUI`/
  `VictoryUI`'s existing listeners work unchanged on a remote client;
  `active == false` calls `Level1Boss.Die()` directly (made `public` for
  this).
- **Bullets** (`BulletStateSnapshot`): every currently-active `Bullet.Active`
  entry (`id`, `position`) — reconciled **declaratively** on the client
  (spawn a visual for an id it hasn't seen, drop one no longer present)
  rather than as discrete spawn/despawn event messages. Simpler, and can't
  desync from a dropped message. A visual is a `PlayerBullet.prefab`/
  `EnemyBullet.prefab` clone (`owner` field picks which) with its `Bullet`
  script destroyed and `Collider2D` disabled *synchronously* right after
  spawn, so it can never resolve damage locally even for one frame. `id` is
  `EntityId.ToULong(bullet.GetEntityId())` cast to `long` — `GetInstanceID()`
  is a hard compile error (obsolete) as of this project's Unity version.

**Explicitly not replicated to remote clients yet** (host-resolved
correctness is unaffected either way): wave enemies/minions, and boss
attack telegraph/warning UI (`BossPanel`'s shockwave/guided-missile/
pattern-barrage warnings). Both are cosmetic gaps for a future pass, not
oversights.

## Online lobby & role-select flow

- **`Lobby.unity`**: `OnlineButton` is live (`LobbyUI.SelectOnline()` sets
  `GameModeSelection.Mode = GameMode.Online`, loads `OnlineJoin`) — no
  longer a disabled placeholder.
- **`OnlineJoin.unity`** (new scene, Build Settings index 6 — see
  `cards.md`'s note on why not 5 — duplicated from `JoinLobby.unity` to
  reuse its Canvas/EventSystem/Camera shape, `PlayerInputManager` dropped):
  `OnlineJoinUI.cs` connects on `Awake()`, then offers Create Match (shows
  the resulting code to share), Join by Code (`TMP_InputField`), and Quick
  Match (Nakama's matchmaker) — both join methods, per design. `MatchJoined`
  routes everyone to `RoleSelect`.
- **`RoleSelect.unity`** gained a **third** panel, `NetworkPickerPanel`
  (`NetworkRoleSelectUI.cs`), alongside the existing single/multi pickers —
  not a separate scene. `RoleSelectUI.Awake()` now checks
  `NetworkSession.Players != null` first. Duplicated from `SinglePickerPanel`
  (same 4-role-button layout); each client picks locally and broadcasts a
  `RolePickMessage` (`NetworkOpCodes.RolePick`) reflected to everyone via
  `NetworkSession.Players`. Only the host sees a Start button; starting
  broadcasts `NetworkOpCodes.GameStart` so every client loads `Gameplay`
  together, not just the host.

### MCP/Editor tooling notes (for next time)

- `manage_components set_property` on a `Button`'s `onClick` doesn't accept
  an arbitrary persistent-listener dict — the reliable path is
  `UnityEditor.Events.UnityEventTools.AddPersistentListener`/
  `AddIntPersistentListener`/`AddVoidPersistentListener` called directly
  against the live `Button.onClick`, via the Unity MCP bridge's code-execution
  tool.
- `GameObject.Find` only searches **active** objects — an inactive panel
  (`NetworkPickerPanel` starts inactive, like `MultiPickerPanel`) has to be
  reached through a live component reference instead.

## Explicitly out of scope (this pass)

- Migrating any state to a true Nakama authoritative match handler.
- Host migration on host disconnect (the match simply ends for everyone).
- Client-side prediction/reconciliation — a remote client feels its own
  ship's input with round-trip latency. Acceptable for LAN; revisit if it
  doesn't feel good.
- Wave enemy/minion visual replication and boss attack telegraph/warning UI
  on remote clients (see above).

## Testing

No live Nakama server → `NakamaConnection.ConnectAsync()` blocks
indefinitely on the auth call; every other online-only component
(`NetworkInputSender/Receiver`, `HostStateBroadcaster`/`RemoteStateReceiver`)
stays disabled and inert, so offline/local-co-op play is completely
unaffected — verified via the full EditMode suite plus repeated Play-mode
smoke tests through this work. End-to-end two-machine verification (host +
remote client through a real fight) needs `nakama-local-setup/`'s server
actually running and is still pending.
