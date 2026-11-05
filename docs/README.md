# Docs Index

## Overview / Status

- [overview.md](overview.md) — concept, prior art, tech stack decisions,
  architecture principles.
- [current-state.md](current-state.md) — **start here**: what's built right
  now, what isn't, known issues, and how to run it in the Editor.
- [roadmap.md](roadmap.md) — current build status: implemented / in
  progress / planned.
- [progress-log.md](progress-log.md) — session-by-session narrative history
  (the "why" behind decisions, troubleshooting notes), Sessions 28 onward.
- [progress-log-archive.md](progress-log-archive.md) — archived Sessions
  1-27 (pre-boss fundamentals through the boss-encounter arc and the Level 1
  rework), moved out for length.
- [art-roadmap.md](art-roadmap.md) — the phased art plan (2D neon
  vector sprites, then a 3D bake, neon effects and powers, anime cut-ins,
  menus and HUD on a shared UI kit, mobile
  quality tiers). It runs in parallel with the gameplay work.
- [architecture.md](architecture.md) — concrete code-level conventions:
  script organization, communication patterns, deliberate omissions.

## Player guide

What a player sees and can do on every screen, from the player's side.
Update it whenever a screen, a control or a visible rule changes.

- [player-guide/README.md](player-guide/README.md) — the screen map, controls
  at a glance, settings (none yet), and the guide's pages.
- [player-guide/menus.md](player-guide/menus.md) — Main Menu, Lobby, Join
  Lobby, Role Select (solo and co-op), Level Select.
- [player-guide/the-fight.md](player-guide/the-fight.md) — how a level runs,
  reading the HUD, damage and survival, enemies, CPU teammates, level-ups.
- [player-guide/roles.md](player-guide/roles.md) — the four roles: stats,
  passives, abilities, how to play each.
- [player-guide/bosses.md](player-guide/bosses.md) — Marauder, Halcyon and
  Warden from the player's side.
- [player-guide/overlays.md](player-guide/overlays.md) — Pause, Level-up,
  Game Over, Victory.

## Systems

Per-system reference: what each script does, what it's attached to, and how
the scene is wired.

- [systems/scene-flow.md](systems/scene-flow.md) — Main Menu → Lobby → Join
  Lobby → Role Select → Level Select → Level scene flow, the persistent
  `MenuBackground` scene and screen transitions, `GameModeSelection`
  (Local/Online), local co-op device joining, and the Pause overlay.
- [systems/ui-kit.md](systems/ui-kit.md) — the menus' UI kit, fonts and the
  project-wide palette (`NeonPalette`).
- [systems/movement.md](systems/movement.md) — ship movement, fixed
  orientation decision.
- [systems/combat.md](systems/combat.md) — shooting, bullets, health, enemy
  waves.
- [systems/player-roles.md](systems/player-roles.md) — `PlayerRole`
  enum/stats/component.
- [systems/roguelike-progression.md](systems/roguelike-progression.md) — the
  Attacker's in-fight XP, level-ups and upgrade cards (test build, this branch
  only): what changes from `main`, the exact numbers, how to play-test it, the
  tuning knobs and the questions the playtest should answer.
- [systems/cards.md](systems/cards.md) — **design spec, not yet built**: the
  deckbuilding layer merged in from `tohou_deck`. Grazing/energy, the 3-slot
  hand, deck rules, the `DeckBuild` loadout scene, and boss spell cards.
- [systems/hud-layout.md](systems/hud-layout.md) — portrait/crossplay
  screen layout, HUD canvases.
- [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md) — **Marauder**, Level 1's
  boss: phases, aggro/taunt, CPU-controlled AI teammates, boss-fight HUD,
  tuning.
- [systems/bosses/halcyon-boss.md](systems/bosses/halcyon-boss.md) — **Halcyon**, Level 2's
  boss: a pure positioning fight — full-arena roam, a Surge vulnerability
  window, and a Static Field proximity pulse as the real damage source.
- [systems/bosses/warden-boss.md](systems/bosses/warden-boss.md) — **Warden**, Level 3's
  boss: a lane-coverage fight with turret-arms that shield the boss until
  they're knocked down (a third joins at Phase 2), Lockdown volley, and
  Big Shot.
- [systems/level-sequencing.md](systems/level-sequencing.md) — the
  intro/minion-phase/boss-entrance timeline that orchestrates a level.
- [systems/input.md](systems/input.md) — New Input System setup,
  `PlayerControls` actions asset.

## Reference

- [unity-notes.md](unity-notes.md) — general Unity/editor gotchas
  (Layout Groups, canvases, `[ExecuteAlways]`) worth knowing before building
  further UI or editor-preview tooling, independent of any one system.
