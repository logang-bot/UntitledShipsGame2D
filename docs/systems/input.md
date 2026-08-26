# Input

Unity's New Input System is used throughout (not the legacy Input Manager) —
see the Tech Stack Decisions table in [../overview.md](../overview.md) for
why, and [../progress-log-archive.md](../progress-log-archive.md)
(Session 1) for the full migration story and troubleshooting notes from
switching over.

## PlayerControls (Input Actions asset)

**Assigned to:** `Player Input` component on the `Player` GameObject.

Custom-created (not Unity's auto-generated default) to keep exact control
over action names, since `Player Input`'s Send Messages behavior matches
methods by name (`Move` action → `OnMove`, `Fire` action → `OnFire`).

- Action map: `Player`
  - `Move` — Value / Vector2, 2D Vector composite (WASD only — arrow keys
    are **not** currently bound)
  - `Fire` — Button (Space and left mouse button)
  - `Ability` — Button (`E`)

`OnMove`/`OnFire` are consumed by `PlayerController.cs` — see
[movement.md](movement.md) and [combat.md](combat.md). `OnAbility` is
consumed by `PlayerAbility.cs` — see [player-roles.md](player-roles.md).

## Scene wiring — Player

| Component        | Key inspector values                                                     |
| ------------------ | ---------------------------------------------------------------------------- |
| **Player Input**   | Actions: `PlayerControls` asset, Default Map: `Player`, Behavior: Send Messages |

## Pause action (standalone, not part of PlayerControls)

`PauseUI.cs` (see [scene-flow.md](scene-flow.md)'s "Pause overlay") needs a
global Escape-key listener, but the project has no UI-facing action map and
the only `PlayerInput` component lives on the human `Player` ship (Send
Messages only reaches components on that same GameObject — no fit for a
scene-global concern). Rather than extending `PlayerControls.inputactions`,
`PauseUI` builds its own `InputAction` directly in code —
`new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape")` —
enabled in `OnEnable()`/disabled in `OnDisable()`, entirely independent of
the asset-based setup above. This keeps Pause decoupled from
`LevelSequencer`'s per-ship freeze/enable toggling and from AI-vs-human ship
swapping, at the cost of being a second, code-only input path alongside the
asset-based one — worth revisiting if more global (non-per-ship) input needs
show up later.
