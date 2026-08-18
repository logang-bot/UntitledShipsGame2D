# Roadmap

Current build status and what comes next. Session-by-session history lives in
`progress-log.md` — this file tracks state, not narrative. Per-system
reference docs live under `systems/`.

## Development priority order

1. **Full basic mechanics** — finish the core single-player loop before
   adding boss-specific or multiplayer complexity.
2. **Player-vs-boss dynamics, validated with CPU-controlled AI** — build and
   prove the raid-style boss fight using AI-controlled teammates for the
   non-human roles, so role-coordination can be shown to be fun with a
   single human player, before any networking exists.
3. **Networking (Nakama) — last.** Only starts once the CPU-AI boss loop is
   proven fun. This is what upgrades the AI-controlled teammates to real
   human players, not a separate feature bolted on afterward.
4. **Art & audio — final pass**, once everything above works with
   placeholder assets.

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
- **Ship orientation resolved**: static (no rotation), Galaga-style — ships
  strafe within the viewport and always fire straight up. `PlayerController.cs`
  already matches this (`Vector2.up` fire direction); omnidirectional enemy
  spawning is no longer planned since it was only motivated by twin-stick
  rotation.
- **Role architecture** — `PlayerRole.cs`: enum (`Attacker`, `Tank`, `Medic`,
  `Support`), static `PlayerRoleStats` lookup table (health/fire-rate/move-speed
  multipliers + sprite tint color per role), and `PlayerRoleComponent` attached
  to the `Player` GameObject. `PlayerController.cs` and `PlayerHealth.cs` apply
  the multipliers on `Start`/`Awake`. Values are placeholder balancing, tunable
  later. No HUD role display yet — that's part of "Finish the HUD" below.

## In Progress

- **Finish the HUD** — duplicate `PartyFrame_1` for players 2–4; build out
  `BossPanel` (boss HP bar, cast/telegraph bar, wave counter) using the same
  nested-Layout-Group + Layout-Element pattern in `unity-notes.md`.

## Planned (not yet started)

### Basic mechanics (remaining)

- **Game-over / respawn flow** — `PlayerHealth.Die()` currently just disables
  the GameObject; no game-over screen or respawn exists yet.
- **Damage feedback** — no visual feedback on taking damage (flash,
  screen shake, etc.) — not started.

### Player-vs-boss dynamics (CPU AI first)

- **Role abilities beyond stat multipliers** — Tank taunt, Medic heal,
  Support buffs. Only passive stat multipliers/tint exist today (see
  `systems/player-roles.md`). These are a prerequisite for the boss
  mechanic below.

- **Boss encounter prototype** — single boss, 2 HP-based phases, one role
  mechanic (Tank taunt forces boss aggro), tested with a single human player
  plus **CPU-controlled AI teammates** filling the other roles — not human
  local co-op. Core design bet — must be reached quickly to validate that
  role coordination is actually fun.

### Networking (last)

- **Nakama networking** — self-hosted on Fly.io, authoritative combat/boss
  state, matchmaking for 1–4 players. Offline/host mode using the same
  simulation layer. Only starts once the CPU-AI boss loop above is proven
  fun; this is what upgrades AI-controlled teammates to real human players.

### Art & audio (final pass)

- **Art pipeline** — Blender-rendered sprites with normal maps, URP 2D
  `Sprite-Lit-Default` shader + `Light2D` + `Shadow Caster 2D` for dynamic lighting.
  Role-based color variants via material emission swaps.

- **Audio** — FMOD adaptive music, intensity/phase shifts tied to boss HP thresholds.
