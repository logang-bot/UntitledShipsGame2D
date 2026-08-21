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
  `Support`), static `PlayerRoleStats` lookup table, and `PlayerRoleComponent`
  attached to the `Player` GameObject. Originally health/fire-rate/move-speed
  multipliers on a shared base; **replaced by fixed, absolute per-role values**
  in a later architecture change — see "Fixed per-role stats overhaul" below.
  `PlayerController.cs` and `PlayerHealth.cs` assign these directly on
  `Start`/`Awake`. Values are placeholder balancing, tunable later. No HUD
  role display yet — that's part of "Finish the HUD" below.
- **Role abilities beyond stats**: `PlayerAbility.cs` (on `Player`,
  new `Ability` input action bound to `E`) — Tank taunt (`OnTaunt` UnityEvent,
  cooldown-gated) plus a passive Shield Arc (see below), Medic aura boost
  (temporarily expands the passive heal/shield aura's radius/tick rate —
  originally an instant self-heal, replaced entirely once the aura shipped,
  see below), Support Speed Boost (a party-wide, non-destructive move-speed
  + fire-rate multiplier — originally self-only, redesigned, see below),
  Attacker Big Shot (wider, harder-hitting bullet with recoil — damage now a
  live multiplier of the caster's own fire damage, see below). Wired live
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

- **Shield stat + Tank AI positioning** — a second, health-like `shield`
  pool per role (`PlayerHealth.maxShield`/`CurrentShield`, absorbs damage
  before health, no passive regen — see `systems/player-roles.md`'s
  "Shield stat"), and Tank teammates now steer to a guard point between the
  boss and the rest of the AI-controlled party (`AIController.BiasedPositionDirection()`),
  physically standing in bullets' paths for free (`Bullet.cs` bullets don't
  home). A shield bar was added to the party frame. (Tank later also got a
  Shield Arc — a wider, functional blocking mechanic — see "Fixed per-role
  stats overhaul + ability rework" below.) Full writeup:
  `systems/boss.md`'s "Tank guard-point positioning / physical blocking".

- **Boss HP / player damage tuning** — `Boss.maxHealth` doubled (30 → 60)
  and every role's player-dealt fire damage cut 40% (regular fire `1` →
  `0.6`, Attacker's Big Shot `3` → `1.8`); `Bullet.damage` and
  `Enemy.TakeDamage`/`Boss.TakeDamage` changed `int` → `float` to allow the
  fractional values. Boss/enemy-dealt damage is unchanged. See
  `systems/boss.md`'s "Tuning" section.

- **Medic AI positioning + proximity aura** — Medic teammates default to
  hanging back from the boss (`AIController.BiasedPositionDirection()`,
  shared with Tank's guard point, generalized to take a bias parameter
  instead of being Tank-only), but break off to approach whichever ally
  has the lowest health/shield fraction once one drops to ≤55% in either
  pool (`AIController.FindHurtAlly()`, checked every frame, reacts to the
  human `Player` being hurt too). Medic's `E` ability was replaced
  entirely: instead of an instant self-heal, Medic has a passive proximity
  heal/shield aura (tiny by default, allies must nearly touch it) that `E`
  temporarily expands into a large, fast aura for a few seconds. Works
  identically whether Medic is human- or AI-controlled (lives on
  `PlayerAbility.cs`, not `AIController.cs`). Resolves the long-standing
  "Medic heal only targets self" gap. A radius ring and a green heal-flash
  give it visual feedback. **The AI's trigger condition for actually
  pressing `E` is still a temporary placeholder** ("fire the instant it's
  off cooldown," no need-awareness) — the first version (fire below the
  Medic's *own* HP threshold) turned out to almost never trigger, since
  hanging back means Medic rarely takes damage itself; flagged in code and
  docs for a smarter, need-aware rework. Full writeup: `systems/boss.md`'s
  "Medic positioning + proximity aura", `systems/player-roles.md`'s
  "PlayerAbility.cs".

- **Support AI positioning** — Support
  teammates now roam the playable viewport freely (random-waypoint wander,
  `AIController.WanderDirection()`/`RandomRoamPoint()`) instead of the
  shared X-only sine weave, matching Tank/Medic's already-implemented
  role-differentiated positioning. (This item originally also covered
  Support's fire-rate/damage catch-up via a `fireRateMultiplier`/
  `damageMultiplier` stat pair — that mechanism was fully replaced by the
  fixed-stats overhaul immediately below; Support's cadence/damage are now
  just direct numbers in the table there.) See `systems/boss.md`'s "Support
  roaming positioning".

- **Fixed per-role stats overhaul + ability rework** — replaced the entire
  `base × multiplier` stat system with fixed, absolute per-role values
  (health/shield/fire damage/fire rate/move speed — see
  `systems/player-roles.md`'s "Fixed per-role stats" for the full table),
  the single source of truth for a role's numbers, no multipliers left
  anywhere in the base stats. Fire rate is now expressed as shots/second
  (higher = faster), replacing the old, misleadingly-inverted `fireRate`
  field. Temporary effects layer on **non-destructively** instead —
  `PlayerController.speedBuffMultiplier`/`fireRateBuffMultiplier`, read at
  the point of use, never mutated into the base stats. Four ability
  changes shipped alongside this: Attacker's Big Shot damage is now a live
  `2x` multiplier of the caster's current fire damage; Support's ability
  became a **party-wide** Speed Boost (all 4 allies, not self-only) with a
  shared gold ring visual on every ship while active, and its cooldown
  went up (8s → 15s, flagged overpowered); Medic's boosted aura radius was
  halved (3 → 1.5); Tank got a new passive Shield Arc — a wide, curved
  visual **and** a real trigger collider that blocks bullets across a
  width wider than Tank's own body, absorbing them into Tank's own
  shield/health (needed a one-line `Bullet.cs` fix,
  `GetComponentInParent<PlayerHealth>()`, so a hit on this child collider
  still routes to the ship's own health pool). `Boss.maxHealth` also went
  ×1.5 (60 → 90) to give this larger rework enough playtest runway. See
  `systems/player-roles.md` (full mechanics) and `systems/boss.md` (boss
  HP tuning, cross-references).

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

- **AI teammate behavior (Attacker)** — Tank's, Medic's, and now Support's
  positioning are all implemented (see "Implemented" above);
  `AIController.cs`'s last remaining role still just weaves in X
  (`Mathf.Sin(Time.time * weaveFrequency)`) with no bullet-, boss-, or
  teammate-awareness. Recommended **next**, ahead of minions below: with a
  dumb, non-dodging Attacker constantly eating hits, it's hard to tell
  whether a rough playtest result means "the role-coordination mechanic
  isn't fun" or just "the AI can't dodge." Role-differentiated positioning
  (patrolling screen width, staying clear of the boss and top edge) and a
  click/tap-to-trigger-teammate-ability mechanic on the party frame are
  designed — see `systems/boss.md`'s "AI teammate behavior" and "Manual
  teammate ability triggering". Bullet-dodging and teammate separation are
  still undesigned. Not yet implemented.
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
