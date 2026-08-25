# Level 1 Boss

Level 1's boss: two HP-based phases, a threat-table aggro system that Tank
taunt redirects, and 3 CPU-controlled AI teammates covering the roles the
human isn't playing. This is the project's core design bet
(`../overview.md`) — MMO-raid-style role coordination — reached before any
networking exists, per `../roadmap.md`'s priority order. The boss's own
component/script/prefab are named `Level1Boss` (not just `Boss`) because
`Gameplay.unity` is a reusable scene meant to host future levels, each with
its own boss — see [level-sequencing.md](level-sequencing.md) for the
intro/minion-phase/boss-entrance timeline this boss now fights inside of,
owned by a separate `LevelSequencer`.

## Level1Boss.cs

**Attached to:** `Boss` GameObject (`Assets/Prefabs/Level1Boss.prefab`, one
instance placed directly in `Gameplay` — not spawned; a boss is a one-off,
not a wave, so it doesn't go through `EnemySpawner.cs`). The GameObject
itself keeps the generic name `Boss`; only the script/class/prefab carry
the `Level1Boss` name. `LevelSequencer` calls the new `public
SetVisible(bool)` to hide the boss's `SpriteRenderer`/`Collider2D`/
shockwave ring until its own entrance begins — not `SetActive(false)` on
the whole GameObject, which was tried first and had a real side effect:
`MinionSpawner.cs` lives on this same GameObject, and deactivating it also
stopped kamikaze minions from spawning for the entire pre-boss window. The
`Boss` GameObject now stays active throughout; only its visibility/
collidability toggle. It's made visible again at the start of the entrance
glide but stays combat-inert (the `Level1Boss` component itself stays
disabled) until the glide finishes, at which point `LevelSequencer` enables
the component — firing `OnEnable()` and starting the movement-pattern
coroutine below. See
[level-sequencing.md](level-sequencing.md).
**Requires:** tag `Enemy` (so `Bullet.cs`'s player-bullet-vs-`Enemy` branch
collides with it), a `bulletPrefab` (reuses `EnemyBullet.prefab`), a
`targets[]` array wired to all 4 player-controlled ships.

### Movement and firing

**Scripted movement pattern, not AI**: the boss no longer decides where to
go — `OnEnable()` captures the boss's current position as `home` (always
wherever `LevelSequencer` just landed it after the entrance glide) and
starts `MovementPatternRoutine()`, which loops for the rest of the fight,
unchanged across both phases:

1. **Snap** instantly to `home.x + sideOffsetX` (2.2) — a one-frame
   teleport, not an eased move, so it reads as sudden.
2. **Advance** from there down toward the ships over `patternMoveDuration`
   (1.2s), via `MoveOverTime()`'s per-frame `Vector3.Lerp` (not
   `Rigidbody2D` physics — the boss sets `transform.position` directly, as
   before). The descent target is clamped by `ClampToBounds()` exactly like
   the old dash was, so "how far it can push" is still governed by
   `maxAdvanceFraction`.
3. **Retreat** back to the side position, same duration.
4. **Return** to `home`, same duration.
5. **Wait** `cycleGap` (1.5s), then repeat the whole sequence mirrored to
   `home.x - sideOffsetX` — right side, gap, left side, gap, forever.

`RunCycle(float side)` is the single-cycle coroutine (one side);
`MoveOverTime(from, to, duration)` is the shared per-frame lerp helper the
advance/retreat/return legs are all built from (`LevelSequencer`'s own
entrance-glide coroutine uses the same lerp idiom independently, since it
moves the boss before `Level1Boss` is even enabled — see
[level-sequencing.md](level-sequencing.md)).

`ClampToBounds()` is unchanged from the old dash system and still keeps the
boss on-screen and limits how far it can push toward the ships:

- **X** — clamped to the viewport width minus `screenPadding` (0.8 / 0.5),
  the same `Camera.ViewportToWorldPoint` clamp idiom
  `PlayerController.HandleMovement()` uses.
- **Y** — clamped to `[homeY - maxAdvanceFraction * viewportHeight, homeY]`,
  where `homeY` is captured once in `Awake()` (the boss's starting Y, ~4.2)
  and `viewportHeight` is the live 10-unit playable height.
  `maxAdvanceFraction` (0.4) means the boss can push down into roughly the
  top 40% of the playable height, never above its home row and never near
  the ships' own operating area.

Every `Update()`, it also picks the current target (see Aggro below) and
fires at it on an interval:

- **Phase 1** (HP > 50%): one aimed shot every `phase1FireInterval` (1.2s).
- **Phase 2** (HP ≤ 50%): fire interval halves (`phase2FireInterval`, 0.6s)
  and it fires a 3-bullet spread (`spreadAngle`, ±15°) instead of a single
  shot.

On top of the phase-based fire, three more attacks run independently (their
own timers, not phase-gated) — see "Body contact damage", "Shockwave", and
"Guided missile" below.

Phase 1 and Phase 2 are two difficulty tiers of the *same* HP bar
(100%→50% and 50%→0%), not two separate encounters or health bars. Reaching
0 HP in either phase calls `Die()` and ends the fight — there's no third
phase after Phase 2.

### Aggro / targeting

A plain threat table (`Dictionary<GameObject, float> aggro`, private,
populated in `Awake()` from `targets[]`): every point of damage a target
deals adds to that target's aggro; the boss's `CurrentTarget` is whichever
active target currently holds the highest aggro. No decay. `PickTarget()`
uses `Dictionary.TryGetValue` rather than a raw indexer, so it stays safe
even if `aggro` and `targets[]` were ever to drift out of sync.

`TauntedBy(GameObject taunter)` — the listener for Tank taunt (see
below) — sets the caster's aggro to `(current highest aggro) + tauntBonus`
(100), guaranteeing an immediate target switch to the taunter.

### Public API

- `CurrentHealth`, `IsPhase2`, `CurrentTarget` — read-only, drive
  `BossPanelUI.cs` (below).
- `GuidedMissileTargetRole` (`PlayerRole?`, read-only) — set the instant a
  guided missile locks on, cleared a few seconds after it fires; drives
  `BossPanelUI`'s warning text.
- `ShockwaveCooldownRemaining`, `GuidedMissileCooldownRemaining`,
  `PatternBarrageCooldownRemaining` — pure derived getters off internal
  timers, drive `BossPanelUI`'s cooldown text (see "BossPanelUI.cs" below).
- `PatternBarrageActivePattern` (`BulletPattern?`, read-only) — set the
  instant a Pattern Barrage begins telegraphing, cleared once it finishes
  firing; drives `BossPanelUI`'s warning text (see "Pattern Barrage" above).
- `TakeDamage(float amount, GameObject source)` — called by `Bullet.cs` on a
  player-bullet hit; `source` is the shooter, used for aggro attribution.
  `CurrentHealth` is `int`; `Mathf.RoundToInt(amount)` is subtracted from
  it, so no fractional HP shows anywhere in the UI. Crossing the 50%-HP
  threshold flips `IsPhase2` and fires `OnPhase2`; reaching 0 HP fires
  `OnDefeated` then `Destroy(gameObject)`.
- `TauntedBy(GameObject taunter)` — see above.
- `OnPhase2`, `OnDefeated` — `UnityEvent`s (`OnDefeated` has two listeners:
  `BossPanelUI.ShowDefeated()` and `VictoryUI.Show()`).
- `SetVisible(bool)` — called by `LevelSequencer` to hide/show the sprite,
  `Collider2D`, and shockwave ring together without touching the
  GameObject's active state (see [level-sequencing.md](level-sequencing.md)'s
  "Boss visibility/collision"). Disabling the collider isn't just cosmetic:
  it's what stops `Bullet.cs` from being able to hit and damage the boss
  while it's supposed to be hidden.

Key public fields: `maxHealth` (90), `sideOffsetX` (2.2)/
`patternMoveDuration` (1.2)/`cycleGap` (1.5)/`maxAdvanceFraction`/
`screenPadding` (movement, see above), `bulletPrefab`,
`phase1FireInterval`/`phase2FireInterval` (1.2 / 0.6), `bulletSpeed` (6),
`spreadAngle` (15°), `bulletDamage` (1 — see "Body contact damage" below),
`bodyContactDamageMultiplier`/`contactDamageCooldown`, `shockwaveRadius`/
`shockwaveDamageMultiplier`/`shockwaveKnockback`/`shockwaveCooldown`/
`shockwaveTelegraphTime`, `guidedMissileTargetRoles`/`guidedMissileInterval`/
`guidedMissileTelegraphTime`/`guidedMissileTurnRate`/`guidedMissileSpeed`/
`guidedMissileWarningLingerTime`, `patternBarrageCooldown` (7)/
`patternBarrageTelegraphTime` (0.7)/`fanBulletCount` (5)/`fanSpreadAngle`
(50°)/`ringBulletCount` (12)/`spiralBulletCount` (20)/`spiralAngleStep`
(25°)/`spiralShotInterval` (0.05), `targets[]`, `tauntBonus` (100).

### Solid-body collision (ships + boss)

Ships (`Player` + 3 `Teammate_*`) and the boss all have a solid body — no
two ships can occupy the same space, and neither can any ship occupy the
boss's. This is a manual position-correction step
(`Assets/Scripts/ShipCollisionUtil.cs`), not a physics-engine collision
response — none of the trigger/collider setup described elsewhere in this
doc changed.

Every ship's `PlayerController.HandleMovement()` (both the human `Player`
and every AI-driven `Teammate_*`, since `AIController` drives movement
through the same `SetMoveDirection`/`HandleMovement` path) resolves its
candidate position each `FixedUpdate` against every other ship
(`PlayerAbility.allies`, already wired on all 4 ships) and against the boss
(a new `PlayerController.boss` field, wired on all 4 ships) before the
existing viewport clamp. `ShipCollisionUtil.ResolveBoxOverlap()` does an
exact axis-aligned box-vs-box push-out along whichever axis has the
shallower penetration — ships and the boss never rotate, so this is exact,
not a circle approximation.

Because ships/the boss move in small discrete steps every frame rather than
teleporting, a momentary overlap is exactly the signal a real collision
happened — so the same overlap check that computes the push-back also
drives Body contact damage below, instead of relying on Unity's trigger
callbacks (which would stop firing once real overlap is actively
prevented). Ship-vs-ship overlap only ever gets pushed apart, no damage.
`ResolveShipCollisions()` runs two more of these loops the same way, over
`Minion.Active` (below) and `Enemy.Active` (see
[combat.md](combat.md)'s `Enemy.cs`) — both apply kamikaze contact damage
on overlap, unlike the ship-vs-ship push-only case here.

The boss's own movement-pattern coroutine doesn't need to know about
collision at all. Since its advance/retreat/return legs move incrementally
(`Vector3.Lerp` each frame, not a teleport — the initial side-snap is the
one deliberate exception), a ship's own next `FixedUpdate` naturally pushes
itself back out the moment the boss advances into it, the same as it would
for another ship. In practice the boss reads as "shoving" ships out of its
path rather than being blocked by them.

### Body contact damage

Reworked alongside the solid-body collision above. Previously fired off
Unity's `OnTriggerStay2D` (the boss's `BoxCollider2D` is non-trigger, ship
colliders are triggers, so Unity fired the callback on genuine overlap);
now fires from `PlayerController.ResolveShipCollisions()`'s own box-overlap
check the moment it detects a ship overlapping the boss, calling a new
`public Level1Boss.ApplyContactDamage(GameObject ship)`. Same math as before:
gated per-target by `contactDamageCooldown` (1s, a private
`Dictionary<GameObject, float>` of last-hit times) so standing against the
boss doesn't tick every frame, then deals
`Mathf.RoundToInt(bulletDamage * bodyContactDamageMultiplier)` — 1 × 2 = 2
by default, twice a regular boss bullet's damage. `bulletDamage` is the
single source of truth this mechanic and the shockwave below both multiply
against.

`ApplyContactDamage` resolves the ship's `PlayerHealth` via `GetComponent`,
not the old handler's `GetComponentInParent` — the resolver only ever
checks each ship's own body `BoxCollider2D`, never a child collider like
Tank's Shield Arc, so a parent lookup is no longer needed. One narrow
behavior change from this: Shield-Arc-only contact (touching the arc
without the ship's own body box overlapping) no longer independently
triggers contact damage — in practice both paths always led to the same
cooldown-gated hit on the same ship, so this is a low-impact
simplification, not a balance change.

### Shockwave

Punishes standing too close, not just touching the body: every `Update()`,
`CheckShockwave()` (off its own `shockwaveCooldown`, 3s) scans `targets[]`
for any active target within `shockwaveRadius` (1.7 — the boss's own
half-extent, 0.8, plus roughly 1.5 ship-widths [0.9] from its edge). If one
is found, it starts `ShockwaveRoutine()`: waits `shockwaveTelegraphTime`
(0.3s), **re-checks the radius** after the wait (a ship that retreats
during the telegraph escapes it), then for every target still inside, deals
`Mathf.RoundToInt(bulletDamage * shockwaveDamageMultiplier)` (1 × 3 = 3 by
default) and calls the target's `PlayerController.AddRecoil(pushDir *
shockwaveKnockback)` — reusing the same decaying-velocity recoil system
built for Attacker's Big Shot, so it folds into `HandleMovement()`'s
position formula and respects the viewport clamp for free.

`shockwaveKnockback` (33) is an *impulse*, not a distance —
`AddRecoil()` adds it to `PlayerController.recoilVelocity`, which decays
exponentially every `FixedUpdate` (`Vector2.Lerp(recoilVelocity,
Vector2.zero, recoilDamping * Time.fixedDeltaTime)`, `recoilDamping` 8).
With this project's Fixed Timestep (0.02) and `recoilDamping` 8, the total
displacement works out to roughly `impulse × 0.105` — so `33` gives ~3.5
units of actual push (~5.8 ship-widths, ~62% of the 5.625-unit playable
width).

**Visual**: a world-space ring at `shockwaveRadius`, built the same
procedural-`LineRenderer` way as `PlayerAbility.cs`'s Medic aura ring — dim
and always visible so the danger zone reads before it ever triggers
(`shockwaveRingColor`), pulses to a bright warning color
(`shockwaveRingTelegraphColor`) at `shockwaveTelegraphPulseSpeed` during the
telegraph wind-up, then flashes a distinct impact color
(`shockwaveRingImpactColor`) for `shockwaveImpactFlashDuration` on the exact
frame it hits. `CreateShockwaveRing()` builds it once in `Awake()`;
`UpdateShockwaveRing()` runs every `Update()`, re-centering it on the boss's
live (erratically-moving) position.

### Guided missile

A homing shot that calls out a specific role rather than whoever currently
holds aggro. Every `guidedMissileInterval` (5s), `CheckGuidedMissile()`
gathers active `targets[]` entries whose `PlayerRoleComponent.role` is in
`guidedMissileTargetRoles` (`{ Medic, Attacker }` by default — Role Select
always assigns all 4 roles exactly once, so both are always present among
the 4 ships, human or AI), picks one at random, and starts
`GuidedMissileRoutine(target)`: sets `GuidedMissileTargetRole` immediately
(driving `BossPanelUI`'s warning during the wind-up, not just during
flight, so Tank gets real reaction time), waits
`guidedMissileTelegraphTime` (0.8s), then spawns a bullet via
`Bullet.InitHoming(target.transform, guidedMissileTurnRate,
guidedMissileSpeed, "Enemy")`. `GuidedMissileTargetRole` is held for
`guidedMissileWarningLingerTime` (2s) after firing before clearing back to
`null`, so the HUD warning doesn't vanish the instant the shot fires.

`Bullet.cs`'s `InitHoming(...)` is an alternate init path alongside the
straight-line `Init(...)`: it re-aims `direction` toward the target's
*current* position every frame via `Vector3.RotateTowards`, capped by
`turnRateDegPerSec` (90°/s default) so it's dodgeable rather than
inescapable. If the target dies/deactivates mid-flight, the homing check
just stops re-aiming and the bullet continues straight on its last heading.

**Trade-off with Tank's blocking**: Tank's physical blocking (below) relies
on bullets traveling in a straight, predictable line. A guided missile can
curve *around* a Tank that isn't actively intercepting its current path —
Tank can still block it, but only by genuinely cutting across the bullet's
path, not by the same reliable "stand between the boss and the target"
guarantee it has against every other bullet in the game.

### Pattern Barrage

A standalone geometric bullet-spread attack, layered on top of the phase-based
fire above (Phase 1/2 are unchanged) rather than replacing it — one system with
three possible shapes, not three separate attacks, following the same "build
eligible options, `Random.Range` pick one" idiom `CheckGuidedMissile()` already
uses for target selection.

Every `Update()`, `CheckPatternBarrage()` (its own `patternBarrageCooldown`, 7s,
time-gated only — no proximity requirement, unlike Shockwave) requires
`CurrentTarget != null` (Fan needs an aim direction) and starts
`PatternBarrageRoutine()`:

1. `PickPattern()` randomly picks one of `{ Fan, Ring, Spiral }`, **excluding
   whichever shape fired last time** (`lastPatternBarragePattern`) — so the
   same shape can never fire twice in a row while still keeping the surprise of
   not knowing which of the other two is coming. Sets
   `PatternBarrageActivePattern` immediately (drives `BossPanelUI`'s warning
   text during the wind-up).
2. Waits `patternBarrageTelegraphTime` (0.7s).
3. Re-aims at `CurrentTarget` *after* the wait (same re-check-after-telegraph
   idiom `ShockwaveRoutine()` uses — the target may have moved or died during
   the wind-up), falling back to `Vector2.down` if the target is gone.
4. Fires the picked shape, then clears `PatternBarrageActivePattern`.

All three shapes reuse the existing private `SpawnBullet(Vector2 dir)` helper
(already used by `Fire()`) — no bullet pooling or new damage/speed fields, they
spend `bulletDamage`/`bulletSpeed` like every other shot:

- **Fan** — `fanBulletCount` (5) bullets spread evenly across `fanSpreadAngle`
  (50°, so ±25°) centered on the aim direction — same
  `Quaternion.Euler(0,0,angle) * dir` math as the existing Phase 2 spread,
  generalized to N bullets instead of a fixed 3.
- **Ring** — omnidirectional by definition (the boss never rotates, so there's
  no "facing" to aim relative to): `ringBulletCount` (12) bullets evenly spaced
  around a full 360°, with a randomized per-burst start-angle offset so the
  gaps between bullets don't always land in the same screen position (would
  otherwise create a permanent memorized safe lane).
- **Spiral** — `FireSpiralRoutine()`, a coroutine: starts aimed at the target
  like Fan (first shot reads as "aimed at you"), then sweeps by
  `spiralAngleStep` (25°) per shot, firing one bullet every
  `spiralShotInterval` (0.05s) for `spiralBulletCount` (20) shots — the one
  shape that actually delivers "rapid-fire," since Fan/Ring resolve in a
  single frame. 20 × 25° = 500°, so it sweeps past a full revolution rather
  than stopping exactly at 360°.

Verified live via the Unity MCP bridge: `FireFan`/`FireRing` produce exactly
`fanBulletCount`/`ringBulletCount` bullets at the expected angles (Fan:
evenly spaced across ±25° centered on the aim direction; Ring: exact 30°
gaps between all 12); manually draining `FireSpiralRoutine()`'s `IEnumerator`
produced exactly `spiralBulletCount` bullets; `PickPattern()`'s no-repeat rule
held over 30 consecutive draws (all 3 shapes seen, zero immediate repeats);
`CheckPatternBarrage()` correctly no-ops (no exception, cooldown untouched)
when `CurrentTarget` is `null`; `BossPanelUI`'s new warning/cooldown text
updated live during a real triggered barrage.

## Bullet.cs — boss damage dispatch

`Init(Vector2 dir, float spd, string ownerTag, GameObject ownerObject =
null)`'s optional `ownerObject` lets player bullets attribute damage back
to their shooter for aggro. `OnTriggerEnter2D`'s player-bullet-vs-`Enemy`-tag
branch checks for both an `Enemy` component and a `Level1Boss` component,
calling `boss.TakeDamage(damage, ownerObject)` on the latter. See
[combat.md](combat.md) for `Bullet.cs`'s full reference.

## AIController.cs

**Attached to:** the 3 `Teammate_*` GameObjects (`Assets/Prefabs/Teammate.prefab`
instances — see below), replacing `PlayerInput`.
**Requires:** `PlayerController`, `PlayerAbility`, `PlayerHealth`,
`PlayerRoleComponent` on the same GameObject (same component set as the
human `Player`) — reused as-is, no AI-specific duplicate logic.

Drives a CPU-controlled teammate every `Update()`:

- **Movement**: role-dependent. **Tank** steers toward a point biased
  toward the boss; **Medic** either does the same biased away from the boss
  (its default "hang back" position) or, if an ally is hurt, breaks off to
  approach that ally instead; **Support** roams the playable viewport freely
  via a random-waypoint wander; **Attacker** patrols side-to-side around the
  boss's own live X position, at a balanced mid-distance between Tank's and
  Medic's — see "Tank guard-point positioning", "Medic positioning + aura",
  "Support roaming positioning", and "Attacker patrol positioning" below.
  All paths go through `PlayerController.SetMoveDirection(Vector2)` — a
  non-input entry point on `PlayerController.cs` alongside the input-driven
  `OnMove(InputValue)`.
- **Firing**: continuous auto-fire via `PlayerController.SetFiring(bool)`,
  the equivalent non-input entry point for `OnFire(InputValue)`.
- **Abilities**: `PlayerAbility.TryUseAbility()` — a public method that goes
  through the same cooldown gate and role-dispatch switch the human player
  uses. Per-role heuristic: **Tank** taunts whenever it doesn't currently
  hold the boss's aggro (`boss.CurrentTarget != gameObject`); **Medic**
  fires its aura boost the instant it's off cooldown — flagged as a
  temporary, need-unaware heuristic, see "Medic positioning + aura" below;
  **Support**/**Attacker** just retry every frame, safe and cheap since
  `TryUseAbility()`'s own cooldown gate no-ops until ready.

Key public fields: `boss` (drag the `Boss` instance), `weaveFrequency` (0.8),
`weaveSpeed` (1), `teammates[]`/`guardBias` (0.65)/`guardDeadzone` (0.2),
`medicBias` (-0.3), `medicApproachThreshold` (0.55), `roamDeadzone` (0.3)/
`roamInterval` (3), `attackerBias` (0.45)/`attackerPatrolAmplitude` (1.5)/
`attackerDeadzone` (0.2), `minDistanceFromBoss` (1.9 — see "Boss avoidance"
below).

### Boss avoidance

A private `EnforceBossDistance(Vector2 point)` pushes any candidate target
point out to at least `minDistanceFromBoss` (1.9 — just outside
`Level1Boss.shockwaveRadius`'s 1.7, so default positioning doesn't self-trigger
the shockwave) from `boss.transform.position`, falling back to
`Vector2.up` if the point lands exactly on the boss. Applied to
`BiasedPositionDirection()`'s and `AttackerPositionDirection()`'s computed
`targetPoint` (Tank, Medic, Attacker) and to `RandomRoamPoint()`'s picked
point (Support), so all four roles' default positioning respects the
floor.

Tank's `guardBias` (0.65, leaning hard toward the boss for physical
blocking) still works with this floor — blocking only requires standing
between the boss and the ally it's shooting at, not touching the boss's
body.

### Tank guard-point positioning / physical blocking

Tank physically stands in incoming bullets' paths rather than weaving like
the other roles. This needs no special handling in `Bullet.cs` — bullets
travel in a straight line from their spawn direction and damage whichever
`Player`-tagged collider they hit first, so a correctly positioned Tank
already "blocks" an ally standing behind it via the existing trigger
collision.

`AIController.Update()`'s movement switch calls the private
`BiasedPositionDirection(bias, deadzone)` for both Tank and Medic:

1. Averages the positions of `teammates[]` entries that aren't `null`,
   aren't `transform` itself, and are `activeInHierarchy`.
2. `Vector2.LerpUnclamped`s from that ally center toward
   `boss.transform.position` by `bias` — Tank passes `guardBias` (0.65, so
   the guard point sits 65% of the way from the allies toward the boss);
   Medic passes `medicBias` (-0.3, a negative bias that extrapolates *past*
   ally center, away from the boss). `LerpUnclamped` (not `Lerp`, which
   would clamp `t` to `[0, 1]` and collapse a negative bias to `0`) is
   required for Medic's negative bias to work.
3. Returns the normalized direction from the caller's current position to
   that target point, or `Vector2.zero` inside `deadzone` (`guardDeadzone`,
   0.2 units, shared by both roles) to avoid jitter once it arrives.

**`teammates[]` deliberately only ever contains the 3 `Teammate_*`
transforms, never `Player`** — Tank's guard point is computed purely from
whichever transforms are wired into `teammates[]`. Tank's
taunt-when-not-holding-aggro heuristic runs alongside this — aggro-pulling
and physical blocking are both active at once, not alternatives.

Tank also has a second, unrelated mechanic — a wide, curved Shield Arc that
functionally blocks bullets beyond the guard-point positioning here,
passive and always-on, independent of Taunt. It lives on `PlayerAbility.cs`
(so it works for a human-controlled Tank too), not here — see
[player-roles.md](player-roles.md)'s "PlayerAbility.cs" for the full
mechanics.

### Medic positioning + aura

**Positioning**: Medic's default is `BiasedPositionDirection()` passed
`medicBias` (-0.3) instead of `guardBias`, giving Medic a "holds toward the
back of the party" position. But it's not unconditional: every frame,
`FindHurtAlly()` scans `PlayerAbility.allies` (all 4 ships) for whichever
ally has the lowest health-or-shield fraction, if any is at or below
`medicApproachThreshold` (0.55) in either pool — mirrors `TickAura()`'s own
"does this ally need anything" check. If one is found, Medic steers
directly at it (`ApproachDirection()`) instead of hanging back,
re-evaluated every frame.

**Aura + boost ability** (full mechanics in
[player-roles.md](player-roles.md)): Medic passively heals/shields nearby
allies via an always-on aura; pressing `E` triggers a temporary, drastic
expansion of that aura's radius/tick rate rather than an instant heal.

**AI trigger heuristic — a known, temporary limitation**: `AIController`
fires the aura boost the instant it's off cooldown, with no awareness of
whether anyone actually needs it — flagged in code and here for rework (the
obvious next step is triggering off `FindHurtAlly()` instead).

**Why the aura lives on `PlayerAbility.cs`, not here**: unlike Tank's
guard-point steering (an AI-only concern — a human Medic just moves via
WASD), the aura must behave identically whether Medic is human- or
AI-controlled. `AIController` only exists on the 3 `Teammate_*`
GameObjects; `PlayerAbility` exists on `Player` too.

**Wiring**: `PlayerAbility.allies[]` — a `Transform[]` of all 4 ships, self
included, filtered at runtime — is wired on all 4 ships' `PlayerAbility`
components, since `AIController.teammates[]` deliberately excludes `Player`
and can't be reused for something that must also heal the human.
`FindHurtAlly()` reuses this same array (`ability.allies`, read from
`AIController` via its cached `PlayerAbility` reference).

A dim, thin `LineRenderer` ring around the Medic shows the aura's current
radius live — bigger and brighter while boosted. Allies actually healed by
a tick get a distinct green flash via `PlayerDamageFlash.Flash(Color)`.

### Support roaming positioning

Support is intentionally the least constrained of the four roles — it
roams the available screen freely rather than holding a zone. Unlike
Tank/Medic's `BiasedPositionDirection()` (which steers toward a point
derived from allies/the boss and holds there), Support has no reference
point at all: `AIController.WanderDirection()` steers toward a private
`roamTarget` (`Vector2`), picking a new random one (`RandomRoamPoint()`)
whenever the current one is reached (within `roamDeadzone`, 0.3) **or**
after `roamInterval` (3s) elapses, whichever comes first. `RandomRoamPoint()`
picks a uniformly random point within the same viewport bounds
`PlayerController.HandleMovement()` clamps movement to.

Unlike `ApproachDirection()`/`BiasedPositionDirection()` (which hold
position once arrived), Support deliberately never returns `Vector2.zero`
inside the deadzone — arriving at a roam point immediately triggers picking
the next one, so it keeps moving continuously.

### Attacker patrol positioning

Ships never rotate and bullets only ever fire straight up (`Vector2.up`, no
homing — see Bullet.cs above), so Attacker's patrol is anchored to the
boss's *live* X rather than an independent center — otherwise it would
frequently drift out of the boss's lane and just miss.
`AIController.AttackerPositionDirection()`, same "compute a target point,
seek it, zero inside a deadzone" shape as `BiasedPositionDirection()`:

1. `targetY = Mathf.LerpUnclamped(GetAllyCenter().y, boss.transform.position.y, attackerBias)`
   — the same ally-center/boss blend Tank and Medic use, applied to Y only.
   `attackerBias` (0.45) sits between Medic's `-0.3` (hangs back) and Tank's
   `0.65` (leans hard toward the boss), giving Attacker a balanced
   stand-off distance. Because the boss sits near the top of the screen and
   ally center is naturally lower/mid-screen, this also keeps Attacker
   clear of the top edge for free.
2. `targetX = boss.transform.position.x + Mathf.Sin(Time.time * weaveFrequency) * attackerPatrolAmplitude`
   — patrols around the boss's current X (reusing `weaveFrequency`) instead
   of a fixed center, keeping shots landing as the boss moves.
   `attackerPatrolAmplitude` (1.5) controls how wide the swing is.
3. Returns the normalized direction to `(targetX, targetY)`, or
   `Vector2.zero` inside `attackerDeadzone` (0.2).

The ally-center averaging loop is a shared private `GetAllyCenter()`, used
by all three biased-positioning roles.

**Known degenerate case**: if every other AI teammate is dead,
`GetAllyCenter()` falls back to the caller's own current position, which
means `targetY`'s per-frame lerp keeps nudging toward
`boss.transform.position.y` using the ship's own just-updated position as
the new "ally center" each frame — over enough frames this asymptotically
converges Attacker's Y onto the boss's, rather than holding a mid-distance
stand-off. Shared by Tank's and Medic's positioning too; only matters in
the "down to one or two teammates" endgame.

### Bullet-dodging

All four AI roles now also react to nearby incoming fire, layered on top of
(not replacing) their positioning above, via a new private
`AIController.ComputeDodgeVector()` called once per `Update()` right after
the role switch computes its positioning direction and before that
direction is handed to `PlayerController.SetMoveDirection()` — a single
choke point all four roles already pass through, so this needed no
per-role changes.

Enumerating live bullets uses a new static `Bullet.Active` registry
(`List<Bullet>`, populated/depopulated in `Bullet.Awake()`/`OnDestroy()`)
rather than a per-frame `FindObjectsByType<Bullet>()` scan or a new
Unity tag — cheaper, and needed no changes to any bullet prefab. `Bullet`
also gained three public read-only accessors (`Direction`, `Speed`,
`Owner`) so `AIController` can read a bullet's current straight-line
heading without new coupling; the underlying fields stay private.

For each bullet in `Bullet.Active` with `Owner == "Enemy"`, within
`dodgeDetectionRadius` (3): projects the teammate's position onto the
bullet's current velocity (`Direction * Speed`) to find the time and point
of closest approach, clamped to `dodgeLookaheadTime` (0.6s) — this is
re-evaluated fresh every frame, so a homing guided missile's *current*
heading is still handled reasonably without full intercept prediction. If
the resulting miss distance is within `dodgeMissDistance` (0.6), the
bullet is "imminent": the teammate steers perpendicular to the bullet's
travel direction (a sideways step out of its lane, not a radial push away
from the bullet's position), on whichever side increases its own distance
from it. Multiple imminent bullets' escape vectors sum and normalize.

The result is blended **additively** into the role's own positioning
direction (`moveDirection + dodge * dodgeWeight`, both normalized), not an
override — this was a deliberate choice, including for Tank: an outright
override would occasionally yank Tank out of its guard point at exactly
the moment it should be standing still and blocking. `dodgeWeight` (1)
and the three detection numbers above are first-pass placeholders, tuned
after playtesting like every other stat in this project. The boss's
proximity shockwave is explicitly out of scope here (it's not a `Bullet`
instance, so it never enters `Bullet.Active`) — already handled by the
existing `minDistanceFromBoss` floor.

## BossPanelUI.cs

**Attached to:** `BossPanel` (child of `HUDCanvas` — see
[hud-layout.md](hud-layout.md)).
**Requires:** a direct `boss` reference (this panel is scene-bound, not a
reusable prefab like `PartyFrame.prefab`).

Every `Update()`, reads `Level1Boss.CurrentHealth/maxHealth` into a health-bar
`Image.fillAmount` + `"HP: x/y"` text, `Level1Boss.IsPhase2` into a `"Phase
1"`/`"Phase 2"` text, `Level1Boss.CurrentTarget`'s `PlayerRoleComponent.role` into
a `"Target: {role}"` text, `Level1Boss.GuidedMissileTargetRole` into a `"Guided
missile: {role}"` warning text (empty string when `null`),
`Level1Boss.PatternBarrageActivePattern` into an `"Incoming: {shape} Barrage"`
warning text (empty string when `null`), and
`Level1Boss.ShockwaveCooldownRemaining`/`Level1Boss.GuidedMissileCooldownRemaining`/
`Level1Boss.PatternBarrageCooldownRemaining` into `"Shockwave: {n}s"`/`"Guided
Missile: {n}s"`/`"Pattern Barrage: {n}s"` cooldown texts (`"Ready"` at
0) — same "HUD only reads, never owns game state" pattern as
`PartyFrameUI.cs`. `ShowDefeated()` (wired to `Level1Boss.OnDefeated`) overwrites
the phase text with `"DEFEATED"`.

`ShockwaveCooldownRemaining` reads `"Ready"` whenever no ship has gotten
close enough to trigger it yet, not just after its cooldown elapses — it's
proximity-triggered, not a fixed auto-cast, so "off cooldown" and "will
fire imminently" aren't the same thing. Body contact damage's per-target
cooldown deliberately has no `BossPanel` line — it's reactive/per-ship, not
a single global cooldown like the other two.

**Telegraph feedback, per attack** — every telegraphed attack gets its own
cooldown countdown, but only two of the three get a *named* text warning
during the wind-up; Shockwave deliberately uses a visual cue instead, since
it's about *where* a ship is standing rather than *who's* targeted:

| Attack | Warning during telegraph | Cooldown text |
| --- | --- | --- |
| Guided Missile | `warningText`: `"Guided missile: {role}"` (names whichever Medic/Attacker locked on) | `"Guided Missile: {n}s"` / `"...: Ready"` |
| Pattern Barrage | `patternBarrageWarningText`: `"Incoming: {Shape} Barrage"` (Fan/Ring/Spiral) | `"Pattern Barrage: {n}s"` / `"...: Ready"` |
| Shockwave | none (text) — the world-space danger ring around the boss itself pulses brighter during the wind-up and flashes on impact, see "Shockwave" above | `"Shockwave: {n}s"` / `"...: Ready"` |

Guided Missile and Pattern Barrage use **separate** text fields
(`warningText` vs. `patternBarrageWarningText`), so if both happen to
telegraph at once, both lines show simultaneously rather than one
overwriting the other.

Key public fields: `boss`, `healthBarFill`, `healthText`, `phaseText`,
`targetText`, `warningText`, `shockwaveCooldownText`,
`guidedMissileCooldownText`, `patternBarrageWarningText`,
`patternBarrageCooldownText`. Key public method: `ShowDefeated()`.

## PlayerAbility.cs / PlayerController.cs — non-input entry points

Both scripts have public methods so `AIController` can drive them without
going through `PlayerInput`'s input-callback path:

- `PlayerController.SetMoveDirection(Vector2)` / `SetFiring(bool)` — the
  non-input equivalents of `OnMove(InputValue)` / `OnFire(InputValue)`,
  which just unwrap the `InputValue` and call these.
- `PlayerAbility.TryUseAbility()` — the non-input equivalent of the
  dispatch inside `OnAbility(InputValue)`. The four `Trigger*` methods
  (`TriggerTaunt`, `TriggerAuraBoost`, `TriggerSpeedBoost`, `TriggerBigShot`)
  stay private.

`TryUseAbility()`'s public, self-cooldown-gated design is also what makes
manual teammate-ability triggering possible with zero changes to this
file: a party-frame click just calls the same method a third way,
alongside `AIController`'s auto-retry and the human's own `E` binding —
see [player-roles.md](player-roles.md)'s "PlayerAbility.cs" for the UI
side of this.

`PlayerController.SpawnBullet()` passes `gameObject` into
`Bullet.Init(..., ownerObject)` so aggro attribution works for player fire
too.

## Minion.cs / MinionSpawner.cs

Smaller enemy ships flanking the boss — a second, distinct threat type
(steady chip damage from a couple of adds) layered on top of the boss's own
attacks without touching any of them, same "independent `Check*()`, own
timer, no cross-talk" idiom as Shockwave/Guided Missile/Pattern Barrage
above.

**Independent of the pre-boss/phase-2 wave system** — those phases (see
[level-sequencing.md](level-sequencing.md)) use `EnemySpawner.cs`'s
wave-formation system instead (it doesn't need a live boss to anchor to, so
it also works before the boss exists). `MinionSpawner` starts disabled in
`Level1Boss.Awake()` and is enabled by `Level1Boss.OnEnable()` — i.e. it
starts spawning at the exact same moment boss combat begins, not any
earlier. This is deliberately *not* tied to `SetVisible()` (which fires at
the *start* of the entrance glide, several seconds earlier): starting
minions there let them spawn and overlap ships that were still frozen for
the rest of the entrance (`PlayerController.enabled == false`, so
`FixedUpdate`/`ResolveShipCollisions` never runs) — kamikaze contact would
silently do nothing against a frozen ship even while visibly overlapping
it. Gating on `OnEnable()` instead guarantees ships are already unfrozen
before any minion exists.

`MinionSpawner.cs` lives as a component **on the `Boss` GameObject itself**
(not a separately-referenced object like `EnemySpawner`) — `Awake()` gets a
free `GetComponent<Level1Boss>()` with no Inspector wiring, and the spawner is
destroyed automatically the instant `Level1Boss.Die()` destroys the GameObject.
Every `Update()`, while `Minion.Active.Count < maxConcurrentMinions` (2) and
its own `spawnInterval` (6s) timer allows, it instantiates `minionPrefab` at
`boss.transform.position` offset by `spawnRadius` (2) along ±X, alternating
sides each spawn (`spawnedLeftLast`) so two live minions read as flanking
the boss symmetrically. `MinionSpawner.Awake()` also calls
`boss.OnDefeated.AddListener(DestroyAllMinions)` directly in code (not an
Inspector wire-up, since this script already holds a direct `boss`
reference) — no stray minions survive into the Victory panel.

`Minion.cs` is modeled on `Enemy.cs`'s simplicity (health/`TakeDamage`/
self-destruct) rather than a scaled-down `Level1Boss.cs`:

- **Positioning** — no free sine-drift like wave `Enemy.cs`. Each `Update()`,
  `transform.position = boss.transform.position + flankOffset + wobble`,
  where `flankOffset` is the fixed per-minion offset assigned at spawn
  (`Init(Level1Boss, Vector2)`) and `wobble` is a small independent sine bob so it
  still reads as alive, not glued in place. This makes a minion track the
  boss's own erratic dash movement automatically, with zero pathfinding. If
  its `boss` reference is ever null (shouldn't happen outside teardown
  ordering), it self-destructs rather than sitting inert.
- **Targeting** — minions don't have their own aggro table. `Fire()` always
  aims at `boss.CurrentTarget` (already a public getter), exactly like
  `Level1Boss.Fire()` aims at it — this ties minion fire to the boss's existing
  aggro system for free: Tank taunt redirects minion fire too, with no
  minion-side code needed.
- **Health/damage** — `public void TakeDamage(float amount)`, round-to-int
  against an `int health` (2), routes into a shared private `Die()` at ≤0 —
  identical shape to `Enemy.cs` except for that indirection (see "Kamikaze
  contact + Explosive minions" below for why). `Bullet.cs`'s player-bullet-
  vs-`Enemy`-tag branch gained a third check (alongside its existing
  `Enemy`/`Level1Boss` checks): `other.GetComponent<Minion>()` → `TakeDamage(damage)`.
  Required since a `Minion` isn't literally an `Enemy` component, so without
  this a player bullet would pass through a minion with no effect.
- **Contact damage** — `public void ApplyContactDamage(GameObject ship)`
  deals `contactDamage` (1, a lesser hazard than the boss's own effective
  contact damage of 2) once, then the minion dies (see below) — no repeat-hit
  cooldown to track, unlike `Level1Boss.ApplyContactDamage`, since there's no
  second hit to gate.
- **Bullets** — reuses `Bullet.cs`/`EnemyBullet.prefab` as-is: a private
  `SpawnBullet(Vector2 dir)` instantiates `bulletPrefab`, sets `.damage`,
  calls `.Init(dir, bulletSpeed, "Enemy")`. This alone makes minion bullets
  damage players (existing enemy-vs-`Player` branch) and be dodge-relevant
  to AI teammates (`Bullet.Active` + `AIController.ComputeDodgeVector()`
  already filter on `Owner == "Enemy"`) with zero further changes.
- **Static registry** — `public static readonly List<Minion> Active`,
  populated in `Awake()`/depopulated in `OnDestroy()`, same pattern as
  `Bullet.Active`. This is what lets `PlayerController` resolve collision
  against however many minions currently exist without a fixed-size array —
  minions are spawned/destroyed at runtime by `MinionSpawner`, unlike the
  hand-placed `Player`/`Teammate_*`/`Boss`. Each `Minion` caches its own
  `HalfExtents` once in `Awake()` from its own `BoxCollider2D`, since (unlike
  the ally/boss colliders `PlayerController` caches once in `Start()`)
  minion colliders can't be cached ahead of time.

**Kamikaze contact + Explosive minions**: touching a minion now costs the
minion its life, not just a repeatable chip-damage tax on the ship. A private
`Die()` is the single funnel both death paths (`TakeDamage` from a player
bullet, `ApplyContactDamage` from ship contact) route through, guarded by a
private `bool isDead` — `Object.Destroy()` is deferred to end-of-frame, so
without the guard a minion hit by a bullet and touched by a ship in the same
physics step could double-fire its death logic before it actually
disappears. A new `MinionType` enum (`Standard`/`Explosive`) on `type`
(public field, set via a new `Init(Level1Boss, Vector2, MinionType)` overload
parameter — has to flow in through `Init()` rather than being set directly
post-`Instantiate`, since `Awake()`, which needs it for the tint below,
already ran by then) decides what `Die()` does next:

- **Standard** — same as before this pass, just now single-hit: dies
  immediately with no further effect, whether killed by a bullet or by
  touching a ship.
- **Explosive** — `Die()` also calls a new `SpawnFragments()`: an
  evenly-spaced ring of `fragmentCount` (8) more `Bullet` instances launched
  outward from the minion's position, random start-offset angle, reusing
  `Level1Boss.FireRing()`'s exact idiom (`step = 360 / fragmentCount`,
  `Quaternion.Euler(0, 0, angle) * Vector2.up` per direction). Each fragment
  is `Init(dir, fragmentSpeed, "Enemy")` with `damage = fragmentDamage` (1 —
  same whole-number constraint as `bulletDamage`/`contactDamage` below) — an
  ordinary enemy-owned `Bullet`, so `Bullet.cs`'s existing enemy-bullet-vs-
  `Player` routing needed zero changes to make fragments hurt ships,
  including the one that just killed the minion. Fires on **any** death, not
  just kamikaze contact — killing an Explosive minion with a well-placed
  bullet from a distance is exactly as dangerous as letting it touch you, by
  design, so players can't just snipe them safely. `fragmentPrefab` is
  optional and falls back to the minion's own `bulletPrefab` if left
  unassigned, so no dedicated prefab is required to use this.

An Explosive minion is visually distinct the instant it spawns: `Init()`
tints its `SpriteRenderer` to `explosiveTintColor` (orange, `(1, 0.45,
0.1)`) — consistent with this project's standing rule of always giving a new
hazard a visible tell (shockwave ring, aura ring, party-buff ring) rather
than a mechanic with no on-screen cue.

`MinionSpawner` decides the mix: a new `[Range(0,1)] explosiveMinionChance`
(0.3, tunable placeholder like every other balance value in this project) is
rolled independently on every `SpawnMinion()` call, so which of the (up to 2)
concurrent minions are Explosive varies spawn to spawn rather than being
pinned to a fixed flank slot.

**Solid-body collision**: minions physically block ships, same as the boss
does (see "Solid-body collision (ships + boss)" above).
`PlayerController.ResolveShipCollisions()` gained a loop over `Minion.Active`
after its existing ally/boss checks, calling `ShipCollisionUtil
.ResolveBoxOverlap()` against each minion's live position/`HalfExtents` and
`minion.ApplyContactDamage(gameObject)` on overlap — same shape as the
existing boss block, just iterating a dynamic list instead of one field.

**Found during verification, not shipped as originally written**:
`Minion`'s first-pass defaults for `bulletDamage` (0.4) and `contactDamage`
(0.5) both silently rounded to **zero** through
`PlayerHealth.TakeDamage(int)`'s `Mathf.RoundToInt` — `0.4` rounds down, and
`0.5` rounds to the nearest *even* integer (Unity's round-half-to-even),
which is also `0`. Every other player-facing damage value in the codebase
(`Level1Boss.bulletDamage`, `Enemy`'s default bullet damage, contact/shockwave
multipliers) happens to already be a whole number, so this footgun had never
come up before. Fixed by using whole numbers (`bulletDamage`/`contactDamage`
both `1`) — caught live via the Unity MCP bridge (`ApplyContactDamage`
produced no shield/health change at the old defaults, produced the expected
1-point drop after the fix), not by code review alone.

Verified live via the Unity MCP bridge: minion position tracks the boss
through a dash exactly (`boss.position + flankOffset`, wobble within
`wobbleAmplitude`); minion fire direction matches the vector to
`boss.CurrentTarget` exactly for two different minions/targets; `TakeDamage`
reduces health and destroys at 0, and `MinionSpawner` immediately refills
the freed slot on its next `Update()` (cap never exceeded 2 across repeated
forced spawns); `ResolveShipCollisions` (invoked directly) correctly
resolves a ship/minion overlap using `Minion.Active`; invoking
`Level1Boss.OnDefeated` destroys every live minion immediately
(`Minion.Active.Count` → 0).

**Kamikaze + Explosive verified live**, both death paths, both types: a
Standard minion's `ApplyContactDamage` dealt its shield/health hit exactly
once and a second immediate call on the same (not-yet-destroyed, per the
end-of-frame `Destroy()` deferral) instance was a confirmed no-op
(`isDead` guard held) with zero fragments spawned either way; an Explosive
minion killed via `ApplyContactDamage` **and** a separate one killed via
`TakeDamage` both spawned exactly `fragmentCount` (8) new `Bullet`s, all
`Owner == "Enemy"` and `damage == fragmentDamage`; a Standard minion killed
via `TakeDamage` spawned zero. Confirmed the tint (`SpriteRenderer.color`)
matched `explosiveTintColor` exactly on an Explosive-`Init()`'d minion. No
console errors or warnings throughout. (One incidental finding during this
pass, not a bug: `GameObject.Find` only searches active objects, and the
unattended human `Player` had been deactivated by ongoing boss fire between
tool calls — worked around by reactivating/healing all 4 ships via
`FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include, ...)` before
testing, not by changing any game code.)

## Scene wiring

### Boss

**Tag:** `Enemy`. **Prefab:** `Assets/Prefabs/Level1Boss.prefab` (SpriteRenderer,
Rigidbody2D at Gravity Scale 0, non-trigger BoxCollider2D — same physical
setup as `Enemy.prefab`). One instance in `Gameplay`, positioned at
`(0, 4.2, 0)` — must stay within the camera's visible range (Main Camera is
orthographic with size 5, so world Y outside roughly `[-5, 5]` is
off-screen). This is also the boss's "home" — `LevelSequencer` reads it,
teleports the boss above the screen, then glides it back down to exactly
this position for the entrance (see
[level-sequencing.md](level-sequencing.md)).

| Component      | Key inspector values                                                    |
| --------------- | ----------------------------------------------------------------------- |
| Transform       | position (0, 4.2, 0), scale (1.6, 1.6, 1) — not shrunk with the ships below |
| **Level1Boss.cs** | `maxHealth`: 90; `targets`: `Player` + all 3 `Teammate_*`; `bulletPrefab`: EnemyBullet prefab; `sideOffsetX`/`patternMoveDuration`/`cycleGap`: 2.2/1.2/1.5; `OnDefeated`: `BossPanel/BossPanelUI.ShowDefeated()` + `VictoryPanel/VictoryUI.Show()`; `OnPhase2`: `Spawner/EnemySpawner.StartSpawning()`; component **starts disabled** — `LevelSequencer` calls `SetVisible(false)` at `Start()` (hiding sprite/collider/ring, GameObject itself stays active) and enables the component once the entrance glide finishes |
| **MinionSpawner.cs** | `minionPrefab`: `Assets/Prefabs/Minion.prefab`; `maxConcurrentMinions`: 2; `spawnInterval`: 6; `spawnRadius`: 2; `explosiveMinionChance`: 0.3 — component **starts disabled**, same as `Level1Boss` itself; `Level1Boss.OnEnable()` enables it, so kamikaze minions only start spawning once boss combat actually begins (ships already unfrozen by then) |

### Teammate_Tank / Teammate_Medic / Teammate_Support

Each is a duplicate of `Player`'s component set with `PlayerInput` removed
and `AIController` added, tagged `Player` (so `Bullet.cs`'s player/enemy
tag logic treats them exactly like the human player), with a distinct
`PlayerRoleComponent.role` (Tank / Medic / Support — `Player` itself
defaults to `Attacker`, all overwritten at runtime by Role Select — see
[player-roles.md](player-roles.md)'s "Role Select scene") so all 4 roles
are covered exactly once. `Teammate_Tank` is linked to
`Assets/Prefabs/Teammate.prefab`; `Teammate_Medic`/`Teammate_Support` are
plain independent GameObjects with the same component values, not prefab
instances — an edit meant to apply to all three teammates has to be applied
to each individually (or to `Teammate.prefab` **plus** the two non-linked
copies).

| Component            | Key inspector values                                                        |
| --------------------- | ----------------------------------------------------------------------------- |
| Transform              | scale (0.6, 0.6, 1)                          |
| **AIController.cs**   | `boss`: the `Boss` instance                                                   |
| **PlayerController.cs** | `boss`: the `Boss` instance (solid-body collision, see "Solid-body collision" above — also set on `Player`, which has no `AIController`) |
| **PlayerRoleComponent** | role: Tank / Medic / Support respectively                                    |

### Tank taunt → boss aggro

On all 4 `PlayerAbility` components (`Player` + 3 `Teammate_*`), `OnTaunt`
has persistent listeners to `PlayerDamageFlash.Flash()` + `CameraShake.Shake()`
(feedback) and `Level1Boss.TauntedBy(GameObject)` with the fixed argument dragged
to that same GameObject (each player's taunt targets itself).

### Death handling

Only the human `Player`'s `PlayerHealth.OnDeath` shows `GameOverPanel`
(`GameOverPanel/GameOverUI.Show()`). Each `Teammate_*`'s `OnDeath` is wired
only to its own party frame (`PartyFrame_N/PartyFrameUI.OnPlayerDied()`) —
a teammate dying just grays its frame, it doesn't end the whole fight, so
the 3 CPU teammates keep fighting (and can still defeat the boss) after the
human is already gone.

`GameOverUI.cs` and `VictoryUI.cs` guard each other (`victoryPanelRoot`/
`gameOverPanelRoot`, one dragged to the other's panel) so this can't pop
`VictoryPanel` on top of an already-showing `GameOverPanel` — whichever
fires first is a true no-op for the other, not a flash-then-hide. Boss
combat itself is unaffected; `Level1Boss.Die()` still resolves and destroys the
`Boss` GameObject normally regardless of which panel is already up, only
the end-screen popup is guarded. See
[hud-layout.md](hud-layout.md)'s Scene wiring for the field-level detail.

## Not yet built

- **Local co-op / dynamic player count** — the party is 4 fixed,
  hand-placed scene objects, not a runtime spawner (see `../roadmap.md`'s
  "In Progress").
- **Out of scope by design**: a 3rd boss phase, an enrage state, or any
  behavior after Phase 2 beyond death. 2 phases ending in death is the
  complete, intended design.
