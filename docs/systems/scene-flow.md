# Scene Flow

Six scenes exist, in this order (Build Settings index in parens):
`MainMenu` (0) → `Lobby` (1) → `JoinLobby` (2) → `RoleSelect` (3) →
`LevelSelect` (4) → `Level` (5). Game Over/Victory/Pause stay same-scene UI
overlays inside `Level`, not separate scenes — see [combat.md](combat.md)
for Game Over/Victory and "Pause overlay" below.

**One shared `Level` scene, one prefab per boss** (Session 51). All three
levels play in `Level.unity`; only the boss differs, and each boss is its
own prefab — `MarauderBoss.prefab`, `HalcyonBoss.prefab`,
`WardenBoss.prefab` (Warden's `ArmA`/`ArmB`/`ArmC` are children inside it),
each saved with an **inactive** root at its home position (0, 4.2). This
replaced three `Ctrl+D` copies (`Level1`/`Level2`/`Level3.unity`) that had
already drifted apart — beating Halcyon or Warden never showed Victory,
because their `OnDefeated` listeners pointed at a removed component. The
original reasons for separate scenes no longer apply: only the selected
boss is ever instantiated, so two bosses can't run at once, and boss edits
live in their own prefab files rather than the shared scene. See "Level
setup (LevelSetup.cs)" below and [level-sequencing.md](level-sequencing.md).

## Level setup (LevelSetup.cs)

**Attached to:** `LevelSetup`, the first root object in `Level.unity`.
`[DefaultExecutionOrder(-1000)]` — the project's only use of it (see
`../architecture.md`'s "Execution Order").

`Awake()` builds the scene into the picked level:
1. Picks the `BossEntry` matching `LevelSelection.SelectedLevel`, or
   `editorDefaultLevel` (Level 1) when the scene is opened directly.
2. Instantiates that entry's boss prefab (still inactive) and hands it to
   `LevelSequencer.bossObject`.
3. Calls `PartySetupBootstrap.Setup(boss)`, which spawns or prepares the
   ships and wires them to the boss both ways (`PartyBossWiring`).
4. Forwards the boss's own `OnPhase2`/`OnDefeated` to the entry's events,
   then activates the boss, so its `Awake()` already sees its ships.
5. Invokes the entry's `onSpawned`.

`BossEntry.cs` is a plain `[Serializable]` class (not a ScriptableObject):
`level`, `bossPrefab` (the boss component on the prefab root), and three
Inspector-wired events reaching scene objects the prefab can't reference:

| Entry | `onSpawned` | `onPhase2` | `onDefeated` |
|---|---|---|---|
| Level 1 Marauder | `BossPanel_Marauder` `SetActive(true)` + `BossPanelUI.Bind` | `EnemySpawner.StartSpawning` | `BossPanelUI.ShowDefeated`, `VictoryUI.Show` |
| Level 2 Halcyon | `BossPanel_Halcyon` `SetActive(true)` + `HalcyonBossPanelUI.Bind` | — | `HalcyonBossPanelUI.ShowDefeated`, `VictoryUI.Show` |
| Level 3 Warden | `BossPanel_Warden` `SetActive(true)` + `WardenBossPanelUI.Bind` | — | `WardenBossPanelUI.ShowDefeated`, `VictoryUI.Show` |

The three boss panels sit side by side under `HUDCanvas`, all inactive
until their entry's `onSpawned`. Each panel's `Bind(GameObject)` fills its
typed fields from the spawned boss with `GetComponent`.

`BossEvents.cs` is the one place that switches on the concrete boss type
(`Phase2`/`Defeated` events, `AssignShips` — Marauder `targets`, Halcyon's
`HalcyonStaticField.ships`, Warden `ships` — and `Taunt`, a deliberate
no-op for Halcyon). `PartyBossWiring` gives every ship its `bossObject`
and a runtime `OnTaunt` listener that calls `BossEvents.Taunt`.

`Restart` (Pause, Game Over, Victory) reloads the active scene, and the
static `LevelSelection` survives the reload, so it replays the same level.

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
opening `Level` directly (bypassing
`Lobby`/`JoinLobby`/`RoleSelect` entirely) working for quick iteration.

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

`CoOpRoster.Players == null` (e.g. `Level`/`RoleSelect` opened directly,
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
single-vs-multi-picker mechanics. `StartGame()` (both the single-picker
`RoleSelectUI` and the multi-picker `RoleSelectMultiUI`) now loads
`LevelSelect` instead of a level scene directly — see below.

## LevelSelectUI.cs + LevelSelection.cs

**Attached to:** `Canvas` in `LevelSelect.unity` (new scene, between
`RoleSelect` and each level's own scene).

A card per level — **Marauder** / **Halcyon** / **Warden**, name plus a
one-line flavor blurb each (see [marauder-boss.md](bosses/marauder-boss.md)'s "At a
Glance" for Marauder's; Halcyon/Warden's mechanics aren't built yet, so
their cards are a name/flavor placeholder for now) — one click per card, no
separate confirm step needed (unlike `RoleSelectUI`'s pick-then-Start flow,
which exists specifically to gate on a variable-length multi-picker; here
each card is already a complete, unambiguous choice, closer to
`LobbyUI.SelectLocal()`'s shape). Each card's handler
(`SelectLevel1()`/`SelectLevel2()`/`SelectLevel3()`) writes the pick to
`LevelSelection.SelectedLevel` then loads the shared `"Level"` scene.
`Back()` loads `RoleSelect`.

`LevelSelection.cs` is a plain `public static class` holding `Level?
SelectedLevel` (`Level` enum: `Level1`/`Level2`/`Level3`) — built on exactly
the same static-carrier pattern as `GameModeSelection.cs`. `LevelSetup`
reads it to decide which boss to spawn; `null` (the `Level` scene opened
directly, bypassing `LevelSelect`) falls back to
`LevelSetup.editorDefaultLevel`.

## Pause overlay (PauseUI.cs)

**Attached to:** a standalone `PauseController` GameObject in each level
scene (`Level.unity`) — **not** the
`PausePanel` it shows/hides (see "Awake/OnEnable pitfall"
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
  An unset `Mode` — e.g. `Level` opened directly — is treated as allowed,
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
| `LevelSelect` | `Canvas` (`LevelSelectUI`, `MarauderButton`/`HalcyonButton`/`WardenButton` cards) plus `BackButton` |
| `Level` | `LevelSetup` (three `BossEntry` rows — see "Level setup" above); `HUDCanvas/PausePanel` (Resume/Restart/ChangeRoles/QuitToMainMenu buttons) and `BossPanel_Marauder`/`BossPanel_Halcyon`/`BossPanel_Warden`; standalone `PauseController` (`PauseUI`, `panelRoot` → `PausePanel`, `gameOverPanelRoot` → `GameOverPanel`, `victoryPanelRoot` → `VictoryPanel`); `PartySetup` (`PartySetupBootstrap`: the 4 scene ships plus the dynamic co-op spawner fields — see [player-roles.md](player-roles.md)); `LevelSequencer` (its `bossObject` is set at runtime by `LevelSetup`); `Global Volume` (Bloom, `Assets/Settings/LevelVolumeProfile.asset`) |

## Not yet built

- Online mode has no real backend — Lobby's `Online` button is a disabled
  placeholder until Nakama networking lands (see `roadmap.md`'s "Nakama
  networking").
- No Settings/Credits screens — not yet needed (no audio system).
- No `DeckBuild.unity` — the deck loadout screen is designed but not built.
  It will sit between `RoleSelect` and whichever level scene is chosen, in
  flow order, while being appended at whatever the next available Build
  Settings index is when it's built (currently **6**, since `LevelSelect`
  and `Level` occupy indices 4/5). Its exact position
  relative to `LevelSelect` (before or after level-picking) is a decision
  for that future work, not settled here. See [cards.md](cards.md)'s "Deck
  loadout scene".
- Warden (Level 3) doesn't have real boss mechanics yet — its placeholder
  exists so the level-select flow and scene plumbing didn't need to be
  re-touched once its own design lands. Halcyon (Level 2) does now — see
  [bosses/halcyon-boss.md](bosses/halcyon-boss.md) and `../roadmap.md`.
