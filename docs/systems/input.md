# Input

Unity's New Input System is used throughout (not the legacy Input Manager) —
see the Tech Stack Decisions table in [../overview.md](../overview.md) for
why, and [../progress-log-archive.md](../progress-log-archive.md)
(Session 1) for the full migration story and troubleshooting notes from
switching over.

## PlayerControls (Input Actions asset)

**Assigned to:** the `Player Input` component on every ship — `Ship.prefab`
now carries one `PlayerInput` (plus an `AIController`, only one of the two
ever enabled at a time — see [player-roles.md](player-roles.md)'s "Role
Select scene" for the co-op spawner that decides which).

Custom-created (not Unity's auto-generated default) to keep exact control
over action names, since `Player Input`'s Send Messages behavior matches
methods by name (`Move` action → `OnMove`, `Fire` action → `OnFire`).

- Action map: `Player`
  - `Move` — Value / Vector2, 2D Vector composite (WASD, `Keyboard&Mouse`
    scheme) plus a `<Gamepad>/leftStick` binding (`Gamepad` scheme) — arrow
    keys are still **not** bound
  - `Fire` — Button (Space and left mouse button, `Keyboard&Mouse`;
    `<Gamepad>/buttonSouth`, `Gamepad`)
  - `Ability` — Button (`E`, `Keyboard&Mouse`; `<Gamepad>/buttonWest`,
    `Gamepad`)
- Control schemes (added for local co-op — see
  [scene-flow.md](scene-flow.md)'s "JoinLobbyUI.cs" section): `Keyboard&Mouse`
  (requires both a `<Keyboard>` and a `<Mouse>`) and `Gamepad` (requires a
  `<Gamepad>`). Every pre-existing binding got tagged into the
  `Keyboard&Mouse` group (previously group-less/scheme-agnostic) so a
  `Gamepad`-scheme player and a `Keyboard&Mouse`-scheme player active in the
  same scene don't both respond to every binding.

`OnMove`/`OnFire` are consumed by `PlayerController.cs` — see
[movement.md](movement.md) and [combat.md](combat.md). `OnAbility` is
consumed by `PlayerAbility.cs` — see [player-roles.md](player-roles.md). None
of these consumers needed any code change for gamepad support — they already
only handle input through `PlayerInput`'s Send Messages, agnostic to which
device/scheme triggered it.

## Scene wiring — Ship.prefab

| Component        | Key inspector values                                                     |
| ------------------ | ---------------------------------------------------------------------------- |
| **Player Input**   | Actions: `PlayerControls` asset, Default Map: `Player`, Default Scheme: `Keyboard&Mouse`, Behavior: Send Messages. Prefab default `enabled: false` — the co-op spawner (`PartySetupBootstrap.cs`) explicitly enables it only for a human-controlled slot; a plain `AIController`-driven slot leaves it disabled so it never auto-pairs itself to an already-claimed device. |

## Pause action (standalone, not part of PlayerControls)

`PauseUI.cs` (see [scene-flow.md](scene-flow.md)'s "Pause overlay") needs a
global Escape/Start listener, but the project has no UI-facing action map and
every `PlayerInput` component's Send Messages only reaches components on
that same GameObject — no fit for a scene-global concern, doubly so now that
a co-op party can have several `PlayerInput`s at once. Rather than extending
`PlayerControls.inputactions`, `PauseUI` builds its own `InputAction`
directly in code with two bindings —
`new InputAction("Pause", InputActionType.Button)` then
`AddBinding("<Keyboard>/escape")` and `AddBinding("<Gamepad>/start")` —
enabled in `OnEnable()`/disabled in `OnDisable()`, entirely independent of
the asset-based setup above and not restricted to a specific paired device,
so **any** joined player's gamepad Start (or the keyboard) pauses for
everyone — a shared pause, matching local co-op convention. This keeps Pause
decoupled from `LevelSequencer`'s per-ship freeze/enable toggling and from
AI-vs-human ship swapping, at the cost of being a second, code-only input
path alongside the asset-based one — worth revisiting if more global
(non-per-ship) input needs show up later.

## Local co-op join screen input (JoinLobby)

Device pairing itself (which physical device becomes which player) isn't
driven by `PlayerControls.inputactions` at all — `JoinLobby.unity`'s
`PlayerInputManager` (`JoinPlayersWhenButtonIsPressed`) listens for *any*
button press on *any* unpaired device and handles the join/scheme-matching
internally; see [scene-flow.md](scene-flow.md)'s "JoinLobbyUI.cs" section.
Once joined, `RoleSelectMultiUI`/`RolePickerRow.cs` (the 2+-human role
picker in `RoleSelect.unity`) also bypass the Input Actions asset entirely —
each row polls its own player's already-paired `Gamepad`/`Keyboard` object
directly (`gamepad.dpad.right.wasPressedThisFrame`, etc.) rather than
standing up a second `EventSystem`/`InputSystemUIInputModule` per player,
which is real, correct Unity functionality but more infrastructure than a
4-role button grid needs. The single-human picker (`RoleSelectUI`'s
`SinglePickerPanel`, used whenever 0 or 1 players joined) is unaffected —
still an ordinary single-cursor Unity UI panel, operable by whichever device
Player 1 joined with since every menu `EventSystem` already uses Unity's
`DefaultInputActions.inputactions` for UI navigation (mouse, keyboard, and
gamepad Navigate/Submit all bound out of the box).
