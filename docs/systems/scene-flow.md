# Scene Flow

Four scenes now exist, in this order (Build Settings index in parens):
`MainMenu` (0) → `Lobby` (1) → `RoleSelect` (2) → `Gameplay` (3). Game
Over/Victory/Pause stay same-scene UI overlays inside `Gameplay`, not
separate scenes — see [combat.md](combat.md) for Game Over/Victory and
"Pause overlay" below.

## MainMenuUI.cs

**Attached to:** `Canvas` in `MainMenu.unity`.

Two buttons: `Play` → `SceneManager.LoadScene("Lobby")`, `Quit` →
`Application.Quit()` (`#if UNITY_EDITOR` guarded to
`EditorApplication.isPlaying = false` instead, so it does something
observable when testing in-editor).

## LobbyUI.cs + GameModeSelection.cs

**Attached to:** `Canvas` in `Lobby.unity`.

Two mode buttons plus a Back button: `Local` → sets
`GameModeSelection.Mode = GameMode.Local`, then loads `RoleSelect`. `Online`
→ `interactable = false`, set in `Awake()` — there's no Nakama backend yet
(see `roadmap.md`'s "Nakama networking"), so it's a placeholder until then,
same "disable until ready" idiom `RoleSelectUI.Awake()` already uses for its
own Start button. `Back` → loads `MainMenu`.

`GameModeSelection.cs` is a plain `public static class` holding
`GameMode? Mode` (`GameMode` enum: `Local`/`Online`) — built on exactly the
same pattern as `PartyRoleAssignment.cs` (see
[player-roles.md](player-roles.md)'s "Role Select scene"): survives
`SceneManager.LoadScene` within one Play session, resets to `null` on domain
reload. Anything that gates on `Mode` treats `null` as "allowed"/local — the
same "unset = no-op fallback" contract `PartySetupBootstrap` relies on for
`PartyRoleAssignment.HumanRole`, which is what keeps opening `Gameplay`
directly (bypassing `Lobby`/`RoleSelect` entirely) working for quick
iteration.

## RoleSelectUI.cs — Back button

`RoleSelect.unity` is no longer the entry point now that `MainMenu`/`Lobby`
exist ahead of it, so it gained a `Back` button (`RoleSelectUI.Back()` →
loads `Lobby`) alongside its existing role-pick buttons and Start button
(see [player-roles.md](player-roles.md)'s "Role Select scene" for those).

## Pause overlay (PauseUI.cs)

**Attached to:** a standalone `PauseController` GameObject in `Gameplay.unity`
— **not** the `PausePanel` it shows/hides (see "Awake/OnEnable pitfall"
below for why that distinction matters). `PausePanel` itself lives under
`HUDCanvas`, alongside `GameOverPanel`/`VictoryPanel`, and follows the same
overlay shape (forced inactive in `Awake()`, an `Image` background, TMP
title text, and Button children) — see [combat.md](combat.md) for
`GameOverUI.cs`/`VictoryUI.cs`, the pattern this mirrors.

- **Escape key**: no UI-facing input action map exists in the project (see
  [input.md](input.md)), and the only `PlayerInput` component lives on the
  human `Player` ship, broadcasting via Unity's "Send Messages" behavior
  which only reaches components on that same GameObject. Rather than
  entangling Pause with per-ship input or `LevelSequencer`'s freeze/enable
  toggling, `PauseUI` builds and owns its own standalone `InputAction`
  (`new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape")`)
  in `Awake()`, enabled in `OnEnable()`/disabled in `OnDisable()`, entirely
  independent of `PlayerControls.inputactions`.
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
| `RoleSelect` | `Canvas` (`RoleSelectUI`) — unchanged plus new `BackButton` |
| `Gameplay` | `HUDCanvas/PausePanel` (Resume/Restart/ChangeRoles/QuitToMainMenu buttons); standalone `PauseController` (`PauseUI`, `panelRoot` → `PausePanel`, `gameOverPanelRoot` → `GameOverPanel`, `victoryPanelRoot` → `VictoryPanel`) |

## Not yet built

- Online mode has no real backend — Lobby's `Online` button is a disabled
  placeholder until Nakama networking lands (see `roadmap.md`'s "Nakama
  networking").
- No Settings/Credits/Level Select screens — not yet needed (no audio system,
  only one level exists).
- Local co-op (multiple *human* players) — `GameModeSelection.Mode.Local`
  currently just routes into the existing single-human + 3-AI-teammate flow
  unchanged; a future pass would read this same flag to spawn/wire up
  multiple local human players instead.
