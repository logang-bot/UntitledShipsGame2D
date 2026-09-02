# Scene Flow

Five scenes now exist, in this order (Build Settings index in parens):
`MainMenu` (0) → `Lobby` (1) → `JoinLobby` (2) → `RoleSelect` (3) →
`Gameplay` (4). Game Over/Victory/Pause stay same-scene UI overlays inside
`Gameplay`, not separate scenes — see [combat.md](combat.md) for Game
Over/Victory and "Pause overlay" below.

## MainMenuUI.cs

**Attached to:** `Canvas` in `MainMenu.unity`.

Two buttons: `Play` → `SceneManager.LoadScene("Lobby")`, `Quit` →
`Application.Quit()` (`#if UNITY_EDITOR` guarded to
`EditorApplication.isPlaying = false` instead, so it does something
observable when testing in-editor).

## LobbyUI.cs + GameModeSelection.cs

**Attached to:** `Canvas` in `Lobby.unity`.

Two mode buttons plus a Back button: `Local` → sets
`GameModeSelection.Mode = GameMode.Local`, then loads `JoinLobby` (the co-op
device-join screen, see below — previously loaded `RoleSelect` directly,
before local co-op existed). `Online` → `interactable = false`, set in
`Awake()` — there's no Nakama backend yet (see `roadmap.md`'s "Nakama
networking"), so it's a placeholder until then, same "disable until ready"
idiom `RoleSelectUI.Awake()` already uses for its own Start button. `Back` →
loads `MainMenu`.

`GameModeSelection.cs` is a plain `public static class` holding
`GameMode? Mode` (`GameMode` enum: `Local`/`Online`) — built on exactly the
same pattern as `PartyRoleAssignment.cs`/`CoOpRoster.cs` (see
[player-roles.md](player-roles.md)'s "Role Select scene"): survives
`SceneManager.LoadScene` within one Play session, resets to `null` on domain
reload. Anything that gates on `Mode` treats `null` as "allowed"/local — the
same "unset = no-op fallback" contract `PartySetupBootstrap` relies on for
`PartyRoleAssignment.HumanRole`/`CoOpRoster.Players`, which is what keeps
opening `Gameplay` directly (bypassing `Lobby`/`JoinLobby`/`RoleSelect`
entirely) working for quick iteration.

## JoinLobbyUI.cs + CoOpRoster.cs — local co-op device join

**Attached to:** `Canvas` in `JoinLobby.unity` (new scene, between `Lobby`
and `RoleSelect`). A `PlayerInputManager` GameObject in the same scene
(`joinBehavior: JoinPlayersWhenButtonIsPressed`, `maxPlayerCount: 4`,
`playerPrefab: JoinSlotMarker.prefab` — a throwaway GameObject holding only a
`PlayerInput`, never used for actual gameplay) auto-pairs any device that
presses a button: keyboard/mouse together for one join (a `Keyboard&Mouse`
control scheme requiring both, see [input.md](input.md)), or any individual
gamepad for another (`Gamepad` scheme). `JoinLobbyUI.cs` polls
`PlayerInput.all` every join/leave event, reflecting each paired player into
one of 4 slot rows (`"Slot N: Empty"` / `"Slot N: {scheme}"`), and enables
`Continue` once at least one player has joined.

`Continue()` snapshots `PlayerInput.all` into a new static
`CoOpRoster.Players` list (`CoOpRoster.cs`, mirroring
`GameModeSelection.cs`/`PartyRoleAssignment.cs`'s exact static-carrier
pattern) — one `JoinedPlayer` struct per joined player holding
`controlScheme`, the paired `InputDevice[]`, and a `role` left `null` until
`RoleSelect` fills it in — then loads `RoleSelect`. Devices themselves
(the physical `Keyboard`/`Mouse`/`Gamepad` singletons) persist for the whole
application session independent of scene loads, so no `DontDestroyOnLoad` or
persisted `PlayerInput` objects are needed — each later scene re-pairs the
same physical devices fresh via `PlayerInput.Instantiate(...)` (see
`PartySetupBootstrap.cs` below). `Back` → loads `Lobby`.

`CoOpRoster.Players == null` (e.g. `Gameplay`/`RoleSelect` opened directly,
bypassing `JoinLobby`) is treated as "co-op flow wasn't used" everywhere it's
read, falling back to the older single-human `PartyRoleAssignment.HumanRole`
carrier or (if that's unset too) the Inspector-only manual role assignment —
same fallback contract every other static carrier in this project already
uses.

## RoleSelectUI.cs — Back button and single/multi picker routing

`RoleSelect.unity` is no longer the entry point now that
`MainMenu`/`Lobby`/`JoinLobby` exist ahead of it, so its `Back` button now
loads `JoinLobby` when `CoOpRoster.Players != null` (the co-op flow was
used) or `Lobby` otherwise (direct-open fallback). `RoleSelectUI.Awake()`
also now picks between two child panels based on how many players joined —
see [player-roles.md](player-roles.md)'s "Role Select scene" for the full
single-vs-multi-picker mechanics.

## Pause overlay (PauseUI.cs)

**Attached to:** a standalone `PauseController` GameObject in `Gameplay.unity`
— **not** the `PausePanel` it shows/hides (see "Awake/OnEnable pitfall"
below for why that distinction matters). `PausePanel` itself lives under
`HUDCanvas`, alongside `GameOverPanel`/`VictoryPanel`, and follows the same
overlay shape (forced inactive in `Awake()`, an `Image` background, TMP
title text, and Button children) — see [combat.md](combat.md) for
`GameOverUI.cs`/`VictoryUI.cs`, the pattern this mirrors.

- **Escape / gamepad Start**: no UI-facing input action map exists in the
  project (see [input.md](input.md)), and every ship's own `PlayerInput`
  broadcasts via Unity's "Send Messages" behavior, which only reaches
  components on that same GameObject. Rather than entangling Pause with
  per-ship input or `LevelSequencer`'s freeze/enable toggling, `PauseUI`
  builds and owns its own standalone `InputAction` with two bindings
  (`<Keyboard>/escape` and `<Gamepad>/start`) in `Awake()`, enabled in
  `OnEnable()`/disabled in `OnDisable()`, entirely independent of
  `PlayerControls.inputactions`. Unrestricted to any specific device/player
  index, matching local co-op convention (a shared pause) — added
  specifically so a gamepad-only human (any co-op slot, not just the first)
  still has a way to pause without a keyboard.
- **`Time.timeScale`**: `Show()` sets it to `0`; `Resume()` and every
  scene-transition method (`Restart`, `ChangeRoles`, `QuitToMainMenu`) reset
  it to `1` *before* calling `SceneManager.LoadScene` — `Time.timeScale` is
  a global engine setting, not scene-scoped, so a reload while paused would
  otherwise stay frozen in the next scene too.
- **Gating**: pressing Escape does nothing if `GameOverPanel`/`VictoryPanel`
  is already showing (`PauseUI` holds `gameOverPanelRoot`/`victoryPanelRoot`
  references and checks `activeSelf` — mirrors the mutual-exclusion guard
  `GameOverUI`/`VictoryUI` already use on each other), or if
  `GameModeSelection.Mode == GameMode.Online` (pausing a
  networked/authoritative match won't make sense once Online is real — this
  makes Pause auto-disable itself then, with no further code change needed).
  An unset `Mode` — e.g. `Gameplay` opened directly — is treated as allowed,
  matching `GameModeSelection`'s fallback contract above.

### Awake/OnEnable pitfall (why PauseUI isn't on its own panel)

First attempt put `PauseUI` directly on `PausePanel` (matching
`GameOverUI`/`VictoryUI`'s shape exactly, where the script sits on the panel
it controls). That broke Pause entirely: `Awake()` calls
`panelRoot.SetActive(false)` to hide the panel at startup — but when
`panelRoot` *is* the same GameObject the script lives on, deactivating it
happens synchronously inside its own `Awake()`, before Unity ever gets to
call `OnEnable()` on it. Unity only calls `OnEnable()` if the object is
still active once `Awake()` finishes; since it was deactivated already, the
new `InputAction` built in `Awake()` never had `.Enable()` called on it, so
Escape silently never registered a press. Caught live via the Unity MCP
bridge (`FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include)`
confirmed `pauseAction.enabled == false` in this state, and reflection-
invoking the callback directly still worked, isolating the bug to the
enable path specifically, not the pause logic itself).

Fixed by splitting the two roles: `PauseUI` now lives on a separate
`PauseController` GameObject that stays active for the whole scene (so its
`OnEnable()`/`Update`-driven `InputAction` always run), while `panelRoot` is
just a plain reference to `PausePanel`, toggled via `SetActive()` from
`Show()`/`Resume()` like any other field — never the GameObject the script
itself is attached to.

## Scene wiring

| Scene | Key GameObjects |
| --- | --- |
| `MainMenu` | `Canvas` (`MainMenuUI`), `EventSystem`, `Main Camera` |
| `Lobby` | `Canvas` (`LobbyUI`, `onlineButton` → `OnlineButton`), `EventSystem`, `Main Camera` |
| `JoinLobby` | `Canvas` (`JoinLobbyUI`, 4 slot-text rows, `ContinueButton`/`BackButton`), `PlayerInputManager`, `EventSystem`, `Main Camera` |
| `RoleSelect` | `Canvas` (`RoleSelectUI`, `singlePickerPanel` → `SinglePickerPanel` [original 4 role buttons + Start], `multiPickerPanel` → `MultiPickerPanel` [`RoleSelectMultiUI`, `RolePickerRow.prefab` instances]) plus `BackButton` |
| `Gameplay` | `HUDCanvas/PausePanel` (Resume/Restart/ChangeRoles/QuitToMainMenu buttons); standalone `PauseController` (`PauseUI`, `panelRoot` → `PausePanel`, `gameOverPanelRoot` → `GameOverPanel`, `victoryPanelRoot` → `VictoryPanel`); `PartySetup` (`PartySetupBootstrap`, both the legacy fixed-4-object fallback fields and the new dynamic co-op spawner fields — see [player-roles.md](player-roles.md)) |

## Not yet built

- Online mode has no real backend — Lobby's `Online` button is a disabled
  placeholder until Nakama networking lands (see `roadmap.md`'s "Nakama
  networking").
- No Settings/Credits/Level Select screens — not yet needed (no audio system,
  only one level exists).
- No `DeckBuild.unity` — the deck loadout screen is designed but not built.
  It will sit between `RoleSelect` and `Gameplay` in flow order while being
  appended at Build Settings index **5**, so `Gameplay` stays at 4 and
  nothing renumbers. See [cards.md](cards.md)'s "Deck loadout scene".
