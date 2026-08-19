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
- **Game-over / respawn flow**: `PlayerHealth.OnDeath` (`UnityEvent`) fires on death;
  `GameOverUI.cs` shows a full-screen Game Over overlay (`GameOverPanel` on
  `HUDCanvas`) and its Restart button reloads `SampleScene` via
  `SceneManager.LoadScene`, resetting all state for free. Party frame grays out
  and stops polling stale values on death (`PartyFrameUI.OnPlayerDied()`).
- **Damage feedback**: `PlayerHealth.OnDamaged` (`UnityEvent`, fires only on
  non-fatal hits) drives `PlayerDamageFlash.cs` (sprite flash, reverting to
  the role's tint color, not white) and `CameraShake.cs` (brief camera
  offset decaying back to base position) — wired live via the Unity MCP
  bridge and verified end-to-end in Play mode, including rapid repeated hits
  and confirming a killing blow only triggers `OnDeath`, not `OnDamaged`.
- Portrait-locked screen layout (9:16): pillarboxed on PC, full-width on phones,
  handled automatically by `AspectRatioFitter.cs` at runtime.
- HUD canvas structure: `GameplayCanvas` (camera-confined) and `HUDCanvas`
  (full-screen overlay) split. Sidebars auto-sized by `HUDSidebarFitter.cs`.
- Live party frame (`PartyFrame_1`): avatar slot, name, role, health/move-speed/fire-rate stats.
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
- **Role abilities beyond stat multipliers**: `PlayerAbility.cs` (on `Player`,
  new `Ability` input action bound to `E`) — Tank taunt (`OnTaunt` UnityEvent,
  cooldown-gated; no real aggro/targeting system exists yet — see
  `systems/player-roles.md`'s "Aggro/targeting" section — so it drives a
  placeholder flash+camera-shake for now, added in Session 9), Medic heal
  (`PlayerHealth.Heal(int)`, self-targeted, clamps at `maxHealth`), Support
  buff (temporary move-speed/fire-rate multiplier, coroutine-reverted),
  Attacker Big Shot (3x-width, 3x-damage bullet with recoil — added in a
  follow-up pass alongside a party frame ability/cooldown display and a
  contrast fix, see Session 8 in `progress-log.md`). Wired live via the
  Unity MCP bridge and verified end-to-end in Play mode for all four roles.

## In Progress

- **Finish the HUD** — `PartyFrame_1` shows a role-tinted avatar slot, role
  label, and live health/move-speed/fire-rate stats, all driven by the real
  `Player`; it's now `Assets/Prefabs/PartyFrame.prefab`, reusable via
  `PartyFrameUI.Initialize(player)` (`systems/hud-layout.md`). The old
  unwired `PartyFrame_2..4` stub objects were deleted rather than kept in
  sync by hand. `BossPanel` has a "coming soon" placeholder. Remaining,
  deferred until there's real data to drive them: an actual spawner that
  loops over connected players and `Instantiate()`s `PartyFrame.prefab`
  once local co-op exists (`PartyFrameManager.cs` is the seam, not the
  spawner itself yet); `BossPanel`'s real content (boss HP bar,
  cast/telegraph bar, wave counter — needs a boss) using the
  nested-Layout-Group + Layout-Element pattern in `unity-notes.md`.

## Planned (not yet started)

### Player-vs-boss dynamics (CPU AI first)

- **Boss encounter prototype** — single boss, 2 HP-based phases, one role
  mechanic (Tank taunt forces boss aggro), tested with a single human player
  plus **CPU-controlled AI teammates** filling the other roles — not human
  local co-op. Core design bet — must be reached quickly to validate that
  role coordination is actually fun.

- **Shrink ship sprites** — reduce player/enemy sprite sizes so all four
  roles' ships fit comfortably on screen together; relevant once
  CPU-controlled AI teammates (and eventually local co-op) put multiple
  ships on screen at once.

- **Enemy spawn pattern variety** — design enemy spawn/movement patterns
  beyond the current simple top-to-bottom sine-wave drift (`EnemySpawner.cs`
  picks a random X, `Enemy.cs` sine-waves straight down); more varied
  formations feed into the boss encounter's bullet-pattern design language.

### Networking (last)

- **Scene scaffolding** — Main Menu, Role Select, and Lobby scenes; the
  project currently has only `SampleScene`. Deferred until role abilities
  and the boss prototype exist to inform what these screens actually need
  to show (e.g. which role abilities to preview on the select screen).
  Prerequisite for wiring up Nakama matchmaking below, since a lobby needs
  somewhere to live before players can be matched into it.
- **Nakama networking** — self-hosted on Fly.io, authoritative combat/boss
  state, matchmaking for 1–4 players. Offline/host mode using the same
  simulation layer. Only starts once the CPU-AI boss loop above is proven
  fun; this is what upgrades AI-controlled teammates to real human players.

### Art & audio (final pass)

- **Art pipeline** — Blender-rendered sprites with normal maps, URP 2D
  `Sprite-Lit-Default` shader + `Light2D` + `Shadow Caster 2D` for dynamic lighting.
  Role-based color variants via material emission swaps.

- **Audio** — FMOD adaptive music, intensity/phase shifts tied to boss HP thresholds.
