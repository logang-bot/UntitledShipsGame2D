# Docs Index

## Overview / Status

- [overview.md](overview.md) — concept, prior art, tech stack decisions,
  architecture principles.
- [current-state.md](current-state.md) — **start here to see what's
  playable right now** and how to test it in the Editor.
- [roadmap.md](roadmap.md) — current build status: implemented / in
  progress / planned.
- [progress-log.md](progress-log.md) — session-by-session narrative history
  (the "why" behind decisions, troubleshooting notes), Sessions 28 onward.
- [progress-log-archive.md](progress-log-archive.md) — archived Sessions
  1-27 (pre-boss fundamentals through the boss-encounter arc and the Level 1
  rework), moved out for length.
- [architecture.md](architecture.md) — concrete code-level conventions:
  script organization, communication patterns, deliberate omissions.

## Systems

Per-system reference: what each script does, what it's attached to, and how
the scene is wired.

- [systems/scene-flow.md](systems/scene-flow.md) — Main Menu → Lobby → Join
  Lobby → Role Select → Gameplay scene flow, `GameModeSelection`
  (Local/Online), local co-op device joining, and the Pause overlay.
- [systems/movement.md](systems/movement.md) — ship movement, fixed
  orientation decision.
- [systems/combat.md](systems/combat.md) — shooting, bullets, health, enemy
  waves.
- [systems/player-roles.md](systems/player-roles.md) — `PlayerRole`
  enum/stats/component.
- [systems/hud-layout.md](systems/hud-layout.md) — portrait/crossplay
  screen layout, HUD canvases.
- [systems/level1-boss.md](systems/level1-boss.md) — Level 1's boss: phases,
  aggro/taunt, CPU-controlled AI teammates, boss-fight HUD, tuning.
- [systems/level-sequencing.md](systems/level-sequencing.md) — the
  intro/minion-phase/boss-entrance timeline that orchestrates a level.
- [systems/input.md](systems/input.md) — New Input System setup,
  `PlayerControls` actions asset.

## Reference

- [unity-notes.md](unity-notes.md) — general Unity/editor gotchas
  (Layout Groups, canvases, `[ExecuteAlways]`) worth knowing before building
  further UI or editor-preview tooling, independent of any one system.
