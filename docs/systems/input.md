# Input

Unity's New Input System is used throughout (not the legacy Input Manager) —
see the Tech Stack Decisions table in [../overview.md](../overview.md) for
why, and [../progress-log.md](../progress-log.md) (Session 1) for the full
migration story and troubleshooting notes from switching over.

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
