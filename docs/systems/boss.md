# Boss Encounter

A single boss with two HP-based phases, a threat-table aggro system that
Tank taunt redirects, and 3 CPU-controlled AI teammates covering the roles
the human isn't playing. This is the project's core design bet
(`../overview.md`) — MMO-raid-style role coordination — reached before any
networking exists, per `../roadmap.md`'s priority order.

## Boss.cs

**Attached to:** `Boss` GameObject (`Assets/Prefabs/Boss.prefab`, one
instance placed directly in `Gameplay` — not spawned; a boss is a one-off,
not a wave, so it doesn't go through `EnemySpawner.cs`).
**Requires:** tag `Enemy` (so `Bullet.cs`'s player-bullet-vs-`Enemy` branch
collides with it), a `bulletPrefab` (reuses `EnemyBullet.prefab`), a
`targets[]` array wired to all 4 player-controlled ships.

### Movement and firing

**Erratic dash-or-hold movement**: every `dashDecisionInterval` (1.5s),
`HandleMovement()` rolls `dashProbability` (0.35) to decide whether to dash
or hold still. On a dash, it picks a random X direction and a random Y
direction (toward/away from the ships) and moves to a new point offset by
`dashDistanceX`/`dashDistanceY` (1.2 / 0.8), clamped by `ClampToBounds()`;
on a hold, it stays put until the next decision. Movement toward the chosen
point uses `Vector3.MoveTowards` at `dashSpeed` (8 — faster than any ship's
own move speed, so a dash reads as a rapid burst), not `Rigidbody2D`
physics — the boss sets `transform.position` directly.

`ClampToBounds()` keeps the boss on-screen and limits how far it can push
toward the ships:

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

Key public fields: `maxHealth` (90), `dashDecisionInterval`/
`dashProbability`/`dashDistanceX`/`dashDistanceY`/`dashSpeed`/
`maxAdvanceFraction`/`screenPadding` (movement, see above), `bulletPrefab`,
`phase1FireInterval`/`phase2FireInterval` (1.2 / 0.6), `bulletSpeed` (6),
`spreadAngle` (15°), `bulletDamage` (1 — see "Body contact damage" below),
`bodyContactDamageMultiplier`/`contactDamageCooldown`, `shockwaveRadius`/
`shockwaveDamageMultiplier`/`shockwaveKnockback`/`shockwaveCooldown`/
`shockwaveTelegraphTime`, `guidedMissileTargetRoles`/`guidedMissileInterval`/
`guidedMissileTelegraphTime`/`guidedMissileTurnRate`/`guidedMissileSpeed`/
`guidedMissileWarningLingerTime`, `patternBarrageCooldown` (7)/
`patternBarrageTelegraphTime` (0.7)/`fanBulletCount` (5)/`fanSpreadAngle`
(50°)/`ringBulletCount` (12)/`spiralBulletCount` (20)/`spiralAngleStep`
(25°)/`spiralShotInterval` (0.05), `targets[]`, `tauntBonus` (100),
`enemySpawner` (drag `Spawner` — auto-disabled in `Awake()` so wave enemies
from `EnemySpawner.cs` don't confound a boss-fight test).

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

The boss's own `HandleMovement()`/dash logic is untouched — it doesn't need
to know about collision at all. Since its dash is already incremental
(`Vector3.MoveTowards` each `Update()`, not a teleport), a ship's own next
`FixedUpdate` naturally pushes itself back out the moment the boss dashes
into it, the same as it would for another ship. In practice the boss reads
as "shoving" ships out of its path rather than being blocked by them.

### Body contact damage

Reworked alongside the solid-body collision above. Previously fired off
Unity's `OnTriggerStay2D` (the boss's `BoxCollider2D` is non-trigger, ship
colliders are triggers, so Unity fired the callback on genuine overlap);
now fires from `PlayerController.ResolveShipCollisions()`'s own box-overlap
check the moment it detects a ship overlapping the boss, calling a new
`public Boss.ApplyContactDamage(GameObject ship)`. Same math as before:
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
branch checks for both an `Enemy` component and a `Boss` component,
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
`Boss.shockwaveRadius`'s 1.7, so default positioning doesn't self-trigger
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

## BossPanelUI.cs

**Attached to:** `BossPanel` (child of `HUDCanvas` — see
[hud-layout.md](hud-layout.md)).
**Requires:** a direct `boss` reference (this panel is scene-bound, not a
reusable prefab like `PartyFrame.prefab`).

Every `Update()`, reads `Boss.CurrentHealth/maxHealth` into a health-bar
`Image.fillAmount` + `"HP: x/y"` text, `Boss.IsPhase2` into a `"Phase
1"`/`"Phase 2"` text, `Boss.CurrentTarget`'s `PlayerRoleComponent.role` into
a `"Target: {role}"` text, `Boss.GuidedMissileTargetRole` into a `"Guided
missile: {role}"` warning text (empty string when `null`),
`Boss.PatternBarrageActivePattern` into an `"Incoming: {shape} Barrage"`
warning text (empty string when `null`), and
`Boss.ShockwaveCooldownRemaining`/`Boss.GuidedMissileCooldownRemaining`/
`Boss.PatternBarrageCooldownRemaining` into `"Shockwave: {n}s"`/`"Guided
Missile: {n}s"`/`"Pattern Barrage: {n}s"` cooldown texts (`"Ready"` at
0) — same "HUD only reads, never owns game state" pattern as
`PartyFrameUI.cs`. `ShowDefeated()` (wired to `Boss.OnDefeated`) overwrites
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

`PlayerController.SpawnBullet()` passes `gameObject` into
`Bullet.Init(..., ownerObject)` so aggro attribution works for player fire
too.

## Scene wiring

### Boss

**Tag:** `Enemy`. **Prefab:** `Assets/Prefabs/Boss.prefab` (SpriteRenderer,
Rigidbody2D at Gravity Scale 0, non-trigger BoxCollider2D — same physical
setup as `Enemy.prefab`). One instance in `Gameplay`, positioned at
`(0, 4.2, 0)` — must stay within the camera's visible range (Main Camera is
orthographic with size 5, so world Y outside roughly `[-5, 5]` is
off-screen).

| Component      | Key inspector values                                                    |
| --------------- | ----------------------------------------------------------------------- |
| Transform       | position (0, 4.2, 0), scale (1.6, 1.6, 1) — not shrunk with the ships below |
| **Boss.cs**     | `maxHealth`: 90; `targets`: `Player` + all 3 `Teammate_*`; `bulletPrefab`: EnemyBullet prefab; `enemySpawner`: `Spawner`; `OnDefeated`: `BossPanel/BossPanelUI.ShowDefeated()` + `VictoryPanel/VictoryUI.Show()` |

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
(feedback) and `Boss.TauntedBy(GameObject)` with the fixed argument dragged
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
combat itself is unaffected; `Boss.Die()` still resolves and destroys the
`Boss` GameObject normally regardless of which panel is already up, only
the end-screen popup is guarded. See
[hud-layout.md](hud-layout.md)'s Scene wiring for the field-level detail.

## Not yet built

- **Bullet-dodging** — AI teammates don't react to nearby bullets, only to
  their role-zone positioning. Candidate approach: each frame, check for
  `EnemyBullet`-tagged objects (or bullets owned by `Boss`) within some
  radius/lane ahead of the teammate and bias `moveInput` away from them.
- **Manual teammate ability triggering** — the player should be able to
  force any teammate's ability to fire right now (subject to that
  teammate's own cooldown), overriding the AI's per-role heuristic for that
  instant. Mechanic: each `PartyFrame_N`'s ability line/icon
  (`PartyFrameUI.abilityText`, see [hud-layout.md](hud-layout.md)) becomes
  a clickable/tappable UI element that calls that teammate's
  `PlayerAbility.TryUseAbility()` directly — the same public,
  cooldown-gated method `AIController.cs` already calls and the human
  `Player`'s own `OnAbility(InputValue)` already wraps. Needs no new
  ability logic, only a UI-side click/tap handler.
- **Minions around the boss** — smaller enemy ships flanking the `Boss`; no
  minion script or prefab exists yet.
- **Local co-op / dynamic player count** — the party is 4 fixed,
  hand-placed scene objects, not a runtime spawner (see `../roadmap.md`'s
  "In Progress").
- **Out of scope by design**: a 3rd boss phase, an enrage state, or any
  behavior after Phase 2 beyond death. 2 phases ending in death is the
  complete, intended design.
