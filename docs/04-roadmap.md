# Roadmap

Current build status and what comes next. Session-by-session history lives in
`01-progress-log.md` — this file tracks state, not narrative.

## Implemented

- Base gameplay loop: player movement, shooting, enemy waves (sine-wave movement,
  return fire), collision and damage in both directions.
- Player health system: `PlayerHealth.cs` component, enemy bullets deal 1 damage on
  hit, player disables at 0 HP.
- Portrait-locked screen layout (9:16): pillarboxed on PC, full-width on phones,
  handled automatically by `AspectRatioFitter.cs` at runtime.
- HUD canvas structure: `GameplayCanvas` (camera-confined) and `HUDCanvas`
  (full-screen overlay) split. Sidebars auto-sized by `HUDSidebarFitter.cs`.
- Placeholder party frame (`PartyFrame_1`) with name/role/health-bar layout.

## In Progress

- **360-degree ship rotation + omnidirectional enemy spawning** — requires a design
  decision first: twin-stick aim (separate move/aim inputs, lean is yes) vs.
  auto-face-movement (Asteroids-style). Affects `PlayerController.cs`, the
  `PlayerControls` actions asset, `Bullet.cs` firing direction, and
  `EnemySpawner.cs` spawn-edge logic.

- **Finish the HUD** — duplicate `PartyFrame_1` for players 2–4; build out
  `BossPanel` (boss HP bar, cast/telegraph bar, wave counter) using the same
  nested-Layout-Group + Layout-Element pattern in `05-unity-notes.md`.

## Planned (not yet started)

- **Role architecture** — `PlayerRole` enum (`Attacker`, `Tank`, `Medic`, `Support`)
  on each player instance driving per-role base stats: health multiplier, fire rate,
  move speed. Sprite color tint per role for visual differentiation (no art yet).
  Prerequisite for everything below.

- **Boss encounter prototype** — single boss, 2 HP-based phases, one role mechanic
  (Tank taunt forces boss aggro) tested locally with 2 players. Core design bet —
  must be reached quickly to validate that role coordination is actually fun.

- **Nakama networking** — self-hosted on Fly.io, authoritative combat/boss state,
  matchmaking for 1–4 players. Offline/host mode using the same simulation layer.
  Only starts after local 2-player role loop is proven.

- **Art pipeline** — Blender-rendered sprites with normal maps, URP 2D
  `Sprite-Lit-Default` shader + `Light2D` + `Shadow Caster 2D` for dynamic lighting.
  Role-based color variants via material emission swaps.

- **Audio** — FMOD adaptive music, intensity/phase shifts tied to boss HP thresholds.
