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
  cooldown-gated), Medic heal (`PlayerHealth.Heal(int)`, self-targeted, clamps
  at `maxHealth`), Support buff (temporary move-speed/fire-rate multiplier,
  coroutine-reverted), Attacker Big Shot (3x-width, 3x-damage bullet with
  recoil — added in a follow-up pass alongside a party frame ability/cooldown
  display and a contrast fix, see Session 8 in `progress-log.md`). Wired live
  via the Unity MCP bridge and verified end-to-end in Play mode for all four
  roles. Taunt's placeholder flash+shake feedback (Session 9) was superseded
  by a real aggro-redirect listener once the boss existed — see below.

- **Boss encounter prototype** — a single `Boss` with 2 HP-based phases
  (Phase 2 at ≤50% HP: fire interval halves, single aimed shot becomes a
  3-bullet spread) and a real threat-table aggro system that Tank taunt
  (`PlayerAbility.OnTaunt` → `Boss.TauntedBy(GameObject)`) redirects, tested
  with the human `Player` plus 3 **CPU-controlled AI teammates**
  (`AIController.cs`) covering Tank/Medic/Support. Validates the project's
  core design bet — MMO-raid-style role coordination — before any networking
  exists. Full writeup: `systems/boss.md`.

- **Finish the HUD** — `PartyFrame_1..4` (all instances of
  `Assets/Prefabs/PartyFrame.prefab`) show every player's/teammate's
  role-tinted avatar, live health/move-speed/fire-rate/ability stats, driven
  by an array-based `PartyFrameManager.cs`. `BossPanel` now shows the boss's
  real HP bar, phase, and current-target role, driven by `BossPanelUI.cs`.
  See `systems/hud-layout.md` and `systems/boss.md`.

- **Shrink ship sprites** — `Player`/`Teammate_*` ship scale reduced from
  `1.0` to `0.6` (the `Boss` stays at its larger `1.6` scale so it still
  reads as the central target) — done as part of tuning the boss fight, and
  leaves room for minions planned around the boss. See `systems/boss.md`.

## In Progress

- **Local co-op / dynamic player count** — the party is still 4 fixed scene
  objects (`Player` + 3 hand-placed `Teammate_*`), not a spawner that reacts
  to however many humans/AI are actually present; `PartyFrameManager.cs`
  is array-based now but still Inspector-wired to those 4 fixed slots, not
  a real "loop over connected players and `Instantiate()`" spawner. Deferred
  until local co-op (or the minion/AI-teammate count) needs to vary at
  runtime.

## Planned (not yet started)

### Player-vs-boss dynamics (CPU AI first)

- **AI teammate behavior** — `AIController.cs` currently only weaves in X
  (`Mathf.Sin(Time.time * weaveFrequency)`, `Vector2.up` component always
  `0`) with no bullet-, boss-, or teammate-awareness. Recommended **next**,
  ahead of minions below: with dumb, non-dodging teammates constantly
  eating hits, it's hard to tell whether a rough playtest result means "the
  role-coordination mechanic isn't fun" or just "the AI can't dodge." See
  `systems/boss.md`'s "Future work" section for concrete directions.
- **Boss combat dynamism** — `Boss.cs`'s movement (a subtle, slow sine
  drift) and both attack patterns (single aimed shot / 3-bullet spread,
  both flat-timer) are static; it reads as a stationary turret rather than
  an active opponent. See `systems/boss.md`'s "Future work" section.
- **Minions around the boss** — smaller enemy ships flanking the `Boss`,
  motivated the ship-shrink above; not yet designed or built (no minion
  script/prefab exists). Do this **after** the two items above — more
  on-screen threats on top of a still-too-simple AI/boss would make the
  encounter harder to read, not more fun.

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
