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
  `HUDCanvas`) and its Restart button reloads `Gameplay` via
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
  roles. Taunt's placeholder flash+shake feedback (Session 9, see
  progress-log-archive.md) was superseded by a real aggro-redirect listener
  once the boss existed — see below.

- **Boss encounter prototype** — a single `Boss` with 2 HP-based phases
  (Phase 2 at ≤50% HP: fire interval halves, single aimed shot becomes a
  3-bullet spread) and a real threat-table aggro system that Tank taunt
  (`PlayerAbility.OnTaunt` → `Boss.TauntedBy(GameObject)`) redirects, tested
  with the human `Player` plus 3 **CPU-controlled AI teammates**
  (`AIController.cs`) covering Tank/Medic/Support. Validates the project's
  core design bet — MMO-raid-style role coordination — before any networking
  exists. Full writeup: `systems/level1-boss.md`.

- **Finish the HUD** — `PartyFrame_1..4` (all instances of
  `Assets/Prefabs/PartyFrame.prefab`) show every player's/teammate's
  role-tinted avatar, live health/move-speed/fire-rate/ability stats, driven
  by an array-based `PartyFrameManager.cs`. `BossPanel` now shows the boss's
  real HP bar, phase, and current-target role, driven by `BossPanelUI.cs`.
  See `systems/hud-layout.md` and `systems/level1-boss.md`.

- **Shrink ship sprites** — `Player`/`Teammate_*` ship scale reduced from
  `1.0` to `0.6` (the `Boss` stays at its larger `1.6` scale so it still
  reads as the central target) — done as part of tuning the boss fight, and
  leaves room for minions planned around the boss. See `systems/level1-boss.md`.

- **Shield stat + Tank AI positioning** — a second, health-like `shield`
  pool per role (`PlayerHealth.maxShield`/`CurrentShield`, absorbs damage
  before health, no passive regen — see `systems/player-roles.md`'s
  "Shield stat"), and Tank teammates now steer to a guard point between the
  boss and the rest of the AI-controlled party (`AIController.BiasedPositionDirection()`),
  physically standing in bullets' paths for free (`Bullet.cs` bullets don't
  home). A shield bar was added to the party frame. (Tank later also got a
  Shield Arc — a wider, functional blocking mechanic — see "Fixed per-role
  stats overhaul + ability rework" below.) Full writeup:
  `systems/level1-boss.md`'s "Tank guard-point positioning / physical blocking".

- **Boss HP / player damage tuning** — `Boss.maxHealth` doubled (30 → 60)
  and every role's player-dealt fire damage cut 40% (regular fire `1` →
  `0.6`, Attacker's Big Shot `3` → `1.8`); `Bullet.damage` and
  `Enemy.TakeDamage`/`Boss.TakeDamage` changed `int` → `float` to allow the
  fractional values. Boss/enemy-dealt damage is unchanged. See
  `systems/level1-boss.md`'s "Tuning" section.

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
  docs for a smarter, need-aware rework. Full writeup: `systems/level1-boss.md`'s
  "Medic positioning + aura", `systems/player-roles.md`'s
  "PlayerAbility.cs".

- **Support AI positioning** — Support
  teammates now roam the playable viewport freely (random-waypoint wander,
  `AIController.WanderDirection()`/`RandomRoamPoint()`) instead of the
  shared X-only sine weave, matching Tank/Medic's already-implemented
  role-differentiated positioning. (This item originally also covered
  Support's fire-rate/damage catch-up via a `fireRateMultiplier`/
  `damageMultiplier` stat pair — that mechanism was fully replaced by the
  fixed-stats overhaul immediately below; Support's cadence/damage are now
  just direct numbers in the table there.) See `systems/level1-boss.md`'s "Support
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
  `systems/player-roles.md` (full mechanics) and `systems/level1-boss.md` (boss
  HP tuning, cross-references).

- **AI teammate behavior (Attacker)** — `AIController.AttackerPositionDirection()`:
  patrols side-to-side around the boss's own live X position (rather than
  an independent, boss-unaware center) at a balanced mid-distance between
  Tank's and Medic's, so its shots — which never home and only ever fire
  straight up — keep landing as the boss sine-drifts. Supersedes the
  original "patrol screen width, avoid the boss" decided design (agreed
  2026-08-20) with a hybrid worked out in conversation once the mismatch
  between "patrol independently" and "ships can't rotate to aim" was
  pointed out: keeps the independent patrol motion for spread/coverage, but
  anchors its center to the boss's X instead of a fixed point. Ability
  firing ("retry the instant it's off cooldown") needed no change — it
  already worked this way. Completes Tank/Medic/Support/Attacker positioning;
  Tank, Medic, and Support were done in Sessions 12/13/15. See
  `systems/level1-boss.md`'s "Attacker patrol positioning".

- **Role Select scene + Victory screen** — a new `RoleSelect.unity` scene
  (Build Settings index 0, entry point) lets the human pick one of the 4
  roles and Start; the 3 AI teammates automatically take the remaining 3
  roles (`RoleSelectUI.cs`, `PartySetupBootstrap.cs`, `PartyRoleAssignment.cs`
  static carrier). Replaces the old manual "hand-edit `PlayerRoleComponent.role`
  in the Inspector and swap a teammate to match" testing workflow. A new
  `VictoryPanel`/`VictoryUI.cs` (mirrors `GameOverUI.cs`) shows on
  `Boss.OnDefeated` (a second listener alongside the existing
  `BossPanelUI.ShowDefeated()`), and `GameOverPanel` gained a matching
  "Change Roles" button. Both end screens offer "play again with the
  current party" (reloads `Gameplay`, roles preserved via the static
  carrier) or "change roles" (clears back to `RoleSelect`). Built ahead of
  the originally-deferred "Scene scaffolding" item below because fast
  role-switching was needed now for testing the 4-role AI behavior, not
  because the full scaffolding timeline moved up — Main Menu and Lobby are
  still not built. See `systems/player-roles.md`'s "Role Select scene" for
  the full mechanics writeup.

- **Boss combat dynamism** — replaced the boss's continuous, predictable
  sine drift with an erratic dash-or-hold movement decision (random
  probability every ~1.5s) bounded to a limited vertical advance toward the
  ships (roughly the top 2/5 of the playable height); added three new
  attacks on top of the existing phase-based fire: body contact damage
  (touching the boss directly costs 2x its bullet damage), a telegraphed
  proximity shockwave (getting within ~1.5 ship-widths costs 3x bullet
  damage plus a knockback via the existing recoil system), and a guided
  missile that homes in on a locked Medic or Attacker (`Bullet.cs` gained a
  true-homing `InitHoming()` path, capped turn rate, alongside the existing
  straight-line `Init()`), with a `BossPanel` warning naming the targeted
  role during lock-on. AI teammates (`AIController.minDistanceFromBoss`)
  now keep a floor distance from the boss by default so they don't wander
  into the new contact/shockwave range — this also fixed a previously-
  documented degenerate case where AI positioning could collapse onto the
  boss once allies died. Resolves this item's "Future work" design
  questions from `systems/level1-boss.md` (erratic repositioning, telegraphed
  heavier attacks, curved bullet trajectories); the guided missile
  knowingly loosens (doesn't break) Tank's straight-line-blocking guarantee,
  a confirmed trade-off, not an oversight. A visible world-space ring at
  `shockwaveRadius` (dim and always on, pulsing during the telegraph,
  flashing on impact) was added right after an initial playtest found the
  danger zone invisible. A follow-up tuning pass then raised
  `shockwaveKnockback` (6 → 33, ~0.63 → ~3.5 units of actual push — derived
  via the same closed-form recoil-decay formula Session 8 (see
  progress-log-archive.md) verified for Attacker's Big Shot) after the
  original knockback proved barely
  noticeable, and added live shockwave/guided-missile cooldown countdowns to
  `BossPanel` (`"Ready"` at 0). See `systems/level1-boss.md`'s "Movement and
  firing", "Body contact damage", "Shockwave", "Guided missile", "Boss
  avoidance", and "BossPanelUI.cs" sections.

- **Solid-body ship/boss collision** — no two ships (human or AI) can
  overlap each other, and no ship can overlap the boss's body, via a new
  `ShipCollisionUtil.cs` (exact axis-aligned box-vs-box push-out, not a
  physics-engine collision response — ships/the boss never rotate, so this
  is exact). Every ship's `PlayerController.HandleMovement()` resolves its
  candidate position against every other ship and the boss each
  `FixedUpdate`, before the existing viewport clamp; the boss's own dash
  movement needed no changes, since a ship's next `FixedUpdate` naturally
  pushes itself back out if the boss dashes into it. Supersedes the
  originally-planned "repulsion term" steering-nudge approach with real
  solid-body resolution, and also extends it to cover ship-vs-boss, which
  that original plan didn't. The boss's "touching its body deals contact
  damage" hazard was reworked to detect off this same overlap check instead
  of a Unity trigger callback (which stopped being reachable once overlap
  is actively prevented) — same cooldown-gated damage as before. See
  `systems/level1-boss.md`'s "Solid-body collision (ships + boss)".

- **Pattern Barrage (geometric bullet spread patterns)** — a new standalone
  boss attack, layered on top of the existing Phase 1/2 fire (unchanged) the
  same way Shockwave and Guided Missile are: its own cooldown
  (`patternBarrageCooldown`, 7s) and telegraph (`patternBarrageTelegraphTime`,
  0.7s). Rather than three separate cooldown/telegraph/HUD stacks (one per
  shape), it's one system that randomly picks a shape each activation —
  `Fan`, `Ring`, or `Spiral` — reusing the same "build eligible options,
  `Random.Range` pick one" idiom `CheckGuidedMissile()` already uses for
  target selection, plus a no-immediate-repeat rule
  (`lastPatternBarragePattern`) so the same shape never fires twice in a row.
  Fan generalizes the existing Phase 2 3-bullet spread math to N bullets
  (`fanBulletCount` 5, `fanSpreadAngle` 50°) aimed at the current target;
  Ring is omnidirectional (`ringBulletCount` 12 bullets evenly spaced around
  360°, randomized start offset so the gaps don't always land in the same
  spot); Spiral is the one that actually delivers "rapid-fire" — a coroutine
  firing `spiralBulletCount` (20) bullets one at a time, sweeping
  `spiralAngleStep` (25°) between shots. All three reuse the existing private
  `Boss.SpawnBullet(Vector2 dir)` helper, no new damage/speed fields or
  bullet pooling. Verified live via the Unity MCP bridge: exact bullet counts
  and angle math confirmed for all three shapes, the no-repeat rule held over
  30 consecutive draws, `CheckPatternBarrage()` correctly no-ops with no
  target, and `BossPanelUI`'s new warning/cooldown text tracks live state.
  See `systems/level1-boss.md`'s "Pattern Barrage".

- **Bullet-dodging + manual ability triggering** — the two halves of this
  roadmap item shipped together. AI teammates now steer away from
  imminent enemy fire (a new `AIController.ComputeDodgeVector()`,
  blended additively into whatever positioning direction their role
  already computed, not an override) using a new `Bullet.Active` static
  registry and three new public read-only accessors
  (`Direction`/`Speed`/`Owner`) rather than a per-frame scene scan or a
  new Unity tag. Separately, each `PartyFrame_N`'s ability status line
  (`AbilityText`) now doubles as a clickable button calling that
  teammate's `PlayerAbility.TryUseAbility()` directly — hidden on the
  human's own frame, interactable state driven by the same
  `CooldownRemaining` the status text already reads. Neither half needed
  any change to `PlayerAbility.cs`'s ability logic itself. See
  `systems/level1-boss.md`'s "Bullet-dodging" and `systems/player-roles.md`'s
  "PlayerAbility.cs" (manual triggering) for the full writeups.

- **Minions around the boss** — a new `MinionSpawner.cs` (on the `Boss`
  GameObject, destroyed automatically alongside it) keeps up to 2 `Minion`s
  (`Minion.cs`, modeled on `Enemy.cs` rather than a scaled-down `Boss.cs`)
  flanking the boss at all times, spawning from the start of the fight
  rather than gated to Phase 2. Each minion tracks the boss's own erratic
  dash movement (position = boss position + a fixed per-minion flank offset
  + a small independent wobble) and always fires at `boss.CurrentTarget`
  (so Tank taunt redirects minion fire too, with no minion-side aggro table
  needed). Minions are solid — `PlayerController.ResolveShipCollisions()`
  gained a loop over a new `Minion.Active` static registry (mirroring
  `Bullet.Active`), pushing ships out and applying contact damage. `Bullet.cs`
  gained one new check (`GetComponent<Minion>()`) alongside its existing
  `Enemy`/`Boss` checks so player fire damages them. A first-pass bug
  (fractional `bulletDamage`/`contactDamage` defaults silently rounding to
  zero through `PlayerHealth.TakeDamage(int)`'s round-half-to-even
  behavior) was caught live via the Unity MCP bridge and fixed by switching
  both to whole numbers. **Follow-up (kamikaze contact + Explosive
  minions)**: touching a ship now costs a minion its life instead of just
  dealing repeatable cooldown-gated chip damage — contact still deals its
  damage once, then the minion dies immediately, funneled through a shared
  `Die()` alongside the existing bullet-kill path (guarded by an `isDead`
  flag against a same-frame double-kill). A new `MinionType.Explosive`
  variant, randomly chosen per spawn (`MinionSpawner.explosiveMinionChance`,
  30%) and visually tinted orange, bursts into a ring of 8 more `Bullet`
  fragments on **any** death — bullet-killed or kamikaze alike — reusing
  `Boss.FireRing()`'s exact "evenly-spaced ring, random start offset" idiom
  with zero changes needed to `Bullet.cs` itself. See `systems/level1-boss.md`'s
  "Minion.cs / MinionSpawner.cs" (including its "Kamikaze contact +
  Explosive minions" subsection).

- **Enemy spawn pattern variety** — `EnemySpawner.cs` picks a formation at random each wave from 4
  options (`Random`, `Line`, `Cluster`, `VFormation`; originally a fixed escalating order, changed
  to random as part of the Level 1 rework below), each paired with one of
  3 `Enemy.cs` movement patterns (`SineWave`, `ZigZag`, `StraightDive`) via `MovementPatternFor()`.
  Follows the same "one system, several shapes" idiom as `Level1Boss.cs`'s Pattern Barrage. See
  `systems/combat.md`'s `Enemy.cs`/`EnemySpawner.cs` sections. Was the last open item under
  "Player-vs-boss dynamics" — that sub-phase is now fully implemented.
- **Explosive wave enemies** — `Enemy.cs` gained `Minion.cs`'s `Explosive`-type fragment-burst
  mechanic (flagged as a straightforward follow-up back in Session 31, now done): a new `EnemyType`
  enum (`Standard`/`Explosive`), rolled independently per spawn by `EnemySpawner.explosiveEnemyChance`
  (0.3, matching `MinionSpawner.explosiveMinionChance`'s idiom exactly), tinted orange, and bursting
  into a ring of 8 `Bullet` fragments on death — gunfire or kamikaze contact alike, since both already
  funnel through the same `Die()`. `EnemySpawner` now calls a new `Enemy.Init(movementPattern,
  enemyType)` instead of setting `movementPattern` as a bare field. See `systems/combat.md`'s
  `Enemy.cs`/`EnemySpawner.cs` sections.
- **Level 1 rework: sequencing + scripted boss movement** — the boss fight is now framed as
  "Level 1," one of many levels to come: `Boss.cs`/`Boss.prefab` renamed to `Level1Boss`
  (class/script/prefab only — the in-scene GameObject stays named `Boss`, and `Gameplay.unity`
  itself is unrenamed since it's meant to be reused by future levels). A new `LevelSequencer.cs`
  (see `architecture.md`'s "Sequencing: One Top-Level Orchestrator Per Level" — a new coordination
  shape, but not a break from any existing convention) owns the whole pre-fight timeline: ships
  glide up from off-screen into a starting line (~4s, frozen), free movement (~4s), minion waves
  (`EnemySpawner`, now randomly-ordered — see above) for ~2 minutes, then — once the screen is
  clear — the boss glides down from off-screen into position (~4s, ships frozen: no movement,
  firing, or abilities). The boss's sprite/collider/shockwave ring stay hidden (`Level1Boss.SetVisible(false)`)
  until this entrance begins, so it can neither be seen nor be hit by player bullets early — the
  `Boss` GameObject itself stays active throughout (not `SetActive(false)`). `MinionSpawner`'s
  boss-flanking kamikaze minions start disabled too, but on their own separate trigger
  (`Level1Boss.OnEnable()`, i.e. the moment boss combat actually begins) rather than `SetVisible` —
  starting them at the entrance instead would let minions spawn while ships were still frozen for
  the glide and couldn't react to contact at all; `Enemy.prefab`'s collider was also changed to a trigger
  (like every ship's) so enemy-vs-enemy and
  enemy-vs-boss physics collision can't happen even once the boss is active again in phase 2, and
  its scale now matches the ships' (was previously oversized). `Level1Boss`'s old erratic
  random-dash movement was replaced entirely with a fixed scripted pattern (snap to a side, advance
  toward the ships, retreat, return home, wait 1.5s, repeat mirrored), looping unchanged through
  both phases; its other systems (aggro, Fire, Shockwave, Guided Missile, Pattern Barrage) are
  untouched. Reaching phase 2 also resumes minion waves via a persistent `Level1Boss.OnPhase2`
  listener straight to `EnemySpawner.StartSpawning()` — no sequencer involvement needed. Two
  follow-up fixes after user testing: `MinionSpawner` (boss-flanking kamikaze minions) now starts
  disabled and only enables in `Level1Boss.OnEnable()` (i.e. once boss combat actually begins,
  ships already unfrozen) rather than any earlier point in the sequence, and `Enemy.cs` (the wave
  enemies from `EnemySpawner`) gained the same kamikaze contact-damage mechanic `Minion.cs` already
  had (one hit then destroyed, via the same manual `ResolveShipCollisions()` overlap check) — it
  previously had none at all. See
  `systems/level1-boss.md`, `systems/level-sequencing.md`, and `systems/combat.md`.

## In Progress

- **Local co-op / dynamic player count** — the party is still 4 fixed scene
  objects (`Player` + 3 hand-placed `Teammate_*`), not a spawner that reacts
  to however many humans/AI are actually present; `PartyFrameManager.cs`
  is array-based now but still Inspector-wired to those 4 fixed slots, not
  a real "loop over connected players and `Instantiate()`" spawner. Deferred
  until local co-op (or the minion/AI-teammate count) needs to vary at
  runtime.

## Planned (not yet started)

### Networking (last)

- **Scene scaffolding** — Main Menu and Lobby scenes still don't exist.
  Role Select shipped early (see "Implemented" above, built ahead of this
  item's original timeline for testing purposes) — `RoleSelect.unity`
  exists and is Build Settings index 0, but it's a standalone role-picker,
  not part of a Main Menu flow yet. Main Menu/Lobby remain deferred until
  the boss prototype (specifically boss combat dynamism) is further along
  to inform what those screens actually need to show. Prerequisite for
  wiring up Nakama matchmaking below, since a lobby needs
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
