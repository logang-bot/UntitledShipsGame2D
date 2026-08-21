# Boss Encounter

The boss encounter prototype: a single boss with two HP-based phases, a
threat-table aggro system that Tank taunt redirects, and 3 CPU-controlled AI
teammates covering the roles the human isn't playing. This is the project's
core design bet (`../overview.md`) — proving MMO-raid-style role coordination
is fun — reached before any networking exists, per `../roadmap.md`'s
priority order.

## Boss.cs

**Attached to:** `Boss` GameObject (`Assets/Prefabs/Boss.prefab`, one
instance placed directly in `Gameplay` — not spawned; a boss is a
one-off, not a wave, so it doesn't go through `EnemySpawner.cs`).
**Requires:** tag `Enemy` (so `Bullet.cs`'s existing player-bullet-vs-`Enemy`
branch collides with it), a `bulletPrefab` (reuses `EnemyBullet.prefab`), a
`targets[]` array wired to all 4 player-controlled ships.

### Movement and firing

Sine-drifts near the top of the screen — same pattern as `Enemy.cs`, no
pathfinding. Every `Update()`, it picks the current target (see Aggro below)
and fires at it on an interval:

- **Phase 1** (HP > 50%): one aimed shot every `phase1FireInterval` (1.2s).
- **Phase 2** (HP ≤ 50%): fire interval halves (`phase2FireInterval`, 0.6s)
  and it fires a 3-bullet spread (`spreadAngle`, ±15°) instead of a single
  shot.

**This is the whole "2-phase" design** — Phase 1 and Phase 2 are two
difficulty tiers of the *same* HP bar (100%→50% and 50%→0%), not two
separate encounters or health bars. Reaching 0 HP in either phase calls
`Die()` and ends the fight; there's no third phase after Phase 2 by design,
since defeating the boss is the intended end state, not a transition to
something else. If a future encounter design wants an enrage tier or a
scripted final phase beyond simple death, that's a bigger, separate change
from what's built here.

### Aggro / targeting

A plain threat table (`Dictionary<GameObject, float> aggro`, private,
populated in `Awake()` from `targets[]`): every point of damage a target
deals adds to that target's aggro; the boss's `CurrentTarget` is whichever
active target currently holds the highest aggro. No decay — kept
prototype-simple, matching the project's "prove fun before infra" style
(see `player-roles.md`'s prior "Aggro/targeting" note, now implemented
here instead of deferred).

`PickTarget()` uses `Dictionary.TryGetValue` rather than a raw indexer —
found live during testing that a raw `aggro[t]` indexer crashed with a
`KeyNotFoundException` every `Update()` (and silently stopped the boss from
firing, since the exception aborted the rest of `Update()` before reaching
`Fire()`) under conditions not fully root-caused; `TryGetValue` makes the
lookup safe regardless of whether `aggro` and `targets[]` ever drift out of
sync, at negligible cost.

`TauntedBy(GameObject taunter)` — the real listener for Tank taunt (see
below) — sets the caster's aggro to `(current highest aggro) + tauntBonus`
(100), guaranteeing an immediate target switch to the taunter.

### Public API

- `CurrentHealth`, `IsPhase2`, `CurrentTarget` — read-only, drive
  `BossPanelUI.cs` (below) and are the values to inspect when testing via
  the Unity MCP bridge.
- `TakeDamage(float amount, GameObject source)` — called by `Bullet.cs` on a
  player-bullet hit; `source` is the shooter, used for aggro attribution.
  `amount` is `float`, not `int` (changed in the damage-tuning pass below,
  since player fire damage is no longer a whole number) — `CurrentHealth`
  itself stays `int`, `Mathf.RoundToInt(amount)` is subtracted from it, so
  no fractional HP anywhere in the UI. Crossing the 50%-HP threshold flips
  `IsPhase2` and fires `OnPhase2`; reaching 0 HP fires `OnDefeated` then
  `Destroy(gameObject)`.
- `TauntedBy(GameObject taunter)` — see above.
- `OnPhase2`, `OnDefeated` — `UnityEvent`s for other systems to react to
  (currently only `OnDefeated` has a listener: `BossPanelUI.ShowDefeated()`).

Key public fields: `maxHealth` (**90**, up from 60 — see "Tuning" below),
`sineAmplitude`/`sineFrequency` (2 / 0.5), `bulletPrefab`,
`phase1FireInterval`/`phase2FireInterval` (1.2 / 0.6), `bulletSpeed` (6),
`spreadAngle` (15°), `targets[]`, `tauntBonus` (100), `enemySpawner` (drag
`Spawner` — auto-disabled in `Awake()` so wave enemies from
`EnemySpawner.cs` don't confound a boss-fight test).

## Bullet.cs — boss damage dispatch

`Init(Vector2 dir, float spd, string ownerTag, GameObject ownerObject =
null)` gained the optional `ownerObject` param (default keeps `Enemy.cs`'s
existing `Init(Vector2.down, bulletSpeed, "Enemy")` call compiling
unchanged) so player bullets can attribute damage back to their shooter for
aggro. `OnTriggerEnter2D`'s player-bullet-vs-`Enemy`-tag branch now also
checks for a `Boss` component (in addition to the existing `Enemy` check)
and calls `boss.TakeDamage(damage, ownerObject)` — needed because bullet
damage previously only ever dispatched to `Enemy.TakeDamage`. See
[combat.md](combat.md) for `Bullet.cs`'s full reference, including the
`damage: int` → `float` change from the damage-tuning pass below.

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
  Medic's — see "Tank guard-point positioning", "Medic positioning +
  proximity aura", "Support roaming positioning", and "Attacker patrol +
  boss-tracking positioning" below.
  All paths go through `PlayerController.SetMoveDirection(Vector2)` — a
  non-input entry point added to `PlayerController.cs` alongside the
  existing input-driven `OnMove(InputValue)`, so movement can be driven
  directly without constructing a fake `InputValue` (which isn't valid
  outside a real input callback).
- **Firing**: continuous auto-fire via `PlayerController.SetFiring(bool)`,
  the equivalent non-input entry point for `OnFire(InputValue)`.
- **Abilities**: `PlayerAbility.TryUseAbility()` — a public method extracted
  from the private dispatch inside `OnAbility(InputValue)`, so it's callable
  directly and still goes through the same shared cooldown gate the human
  player uses. Per-role heuristic: **Tank** taunts whenever it doesn't
  currently hold the boss's aggro (`boss.CurrentTarget != gameObject`);
  **Medic** fires its aura boost the instant it's off cooldown — **TEMPORARY**,
  see "Medic positioning + proximity aura" below for why this one heuristic
  is flagged for rework and the others aren't; **Support**/**Attacker** just
  retry every frame — safe and cheap since `TryUseAbility()`'s own cooldown
  gate no-ops until ready.

Key public fields: `boss` (drag the `Boss` instance), `weaveFrequency` (0.8),
`weaveSpeed` (1), `teammates[]`/`guardBias` (0.65)/`guardDeadzone` (0.2),
`medicBias` (-0.3), `medicApproachThreshold` (0.55), `roamDeadzone` (0.3)/
`roamInterval` (3), `attackerBias` (0.45)/`attackerPatrolAmplitude` (1.5)/
`attackerDeadzone` (0.2) — see below.

### Tank guard-point positioning / physical blocking (implemented)

Agreed design (2026-08-20), built the same session: Tank physically stands
in incoming bullets' paths rather than weaving like the other roles. This
needed **zero changes to `Bullet.cs`** — bullets don't home, they travel in
a straight line from their spawn direction and damage whichever
`Player`-tagged collider they hit first (see Bullet.cs above and
[combat.md](combat.md)), so a correctly positioned Tank already "blocks" an
ally standing behind it, for free, via the existing trigger collision. Pure
positioning problem, not a collision problem.

`AIController.Update()`'s movement switch calls the private
`BiasedPositionDirection(bias, deadzone)` for both Tank and Medic (see
"Medic positioning + proximity aura" below); at the time this was built,
Attacker/Support both kept the exact original sine-weave (unchanged code
path — verified via Play-mode position sampling that they still only moved
in X while Tank's/Medic's Y also changed). Support and Attacker have since
gotten their own positioning too — see "Support roaming positioning" and
"Attacker patrol + boss-tracking positioning" below; no role is left on the
original weave anymore except the `default` safety fallback for any future
unhandled role. Originally written Tank-only as
`GuardPointDirection()`,
generalized in Session 13 once Medic needed the same shape with a
different bias, rather than duplicating the Lerp/deadzone logic a second
time; the ally-averaging step itself was further extracted into
`GetAllyCenter()` in Session 17 once Attacker's positioning needed the same
average without the boss-biased-lerp part. `BiasedPositionDirection(bias, deadzone)`:

1. Averages the positions of `teammates[]` entries that aren't `null`,
   aren't `transform` itself, and are `activeInHierarchy` (same liveness
   filter already used in `Boss.PickTarget()`).
2. `Vector2.LerpUnclamped`s from that ally center toward
   `boss.transform.position` by `bias` — Tank passes `guardBias` (0.65, so
   the guard point sits 65% of the way from the allies toward the boss);
   Medic passes `medicBias` (-0.3, a negative bias that extrapolates
   *past* ally center, away from the boss). **`LerpUnclamped`, not
   `Lerp`** — Unity's `Vector2.Lerp` silently clamps `t` to `[0, 1]`, which
   would have collapsed Medic's negative bias to `0` (landing exactly on
   ally center instead of pulling away from the boss) had it shipped
   unnoticed.
3. Returns the normalized direction from the caller's current position to
   that target point, or `Vector2.zero` inside `deadzone` (`guardDeadzone`,
   0.2 units, shared by both roles) to avoid jitter once it arrives.

**`teammates[]` deliberately only ever contains the 3 `Teammate_*`
transforms, never `Player`** — this is how "ignore the human player's
position" is achieved, with no runtime human-detection check needed: Tank's
guard point is computed purely from whichever transforms are wired into
`teammates[]`, and `Player` is simply never one of them. Tank's existing
taunt-when-not-holding-aggro heuristic (above) is untouched and runs
alongside this — aggro-pulling and physical blocking are both active at
once, as designed, not alternatives.

**Gotcha hit while wiring `teammates[]` in the scene**: setting the field
via `execute_code` + `EditorUtility.SetDirty()` alone did *not* survive a
scene save for `Teammate_Tank` specifically — it's a `Teammate.prefab`
instance (see Scene wiring below), and instance-level overrides on
object-reference fields need
`PrefabUtility.RecordPrefabInstancePropertyModifications()` called on the
component in addition to `SetDirty()`, or the override silently doesn't
serialize. Caught by forcing a full scene reload from disk after saving and
finding `teammates[]` empty — always verify a scene-wiring change survives
a reload, don't trust a "success" result alone. `Teammate_Medic`/
`Teammate_Support` (not prefab instances, see Session 10/11) didn't need
this extra call.

The still-open bullet-dodging/separation/targeted-bullet questions are
unchanged from "Future work" below — Session 12 built Tank, Session 13
built Medic (below), Session 15 built Support (below), Session 17 built
Attacker (below).

**Tank also got a second, unrelated new mechanic in Session 16**: a wide,
curved Shield Arc that functionally blocks bullets beyond the guard-point
positioning here — passive, always-on, independent of Taunt. It lives on
`PlayerAbility.cs`, not here (same "must work for a human Tank too"
reasoning as Medic's aura), so it's not a positioning change — see
[player-roles.md](player-roles.md)'s "PlayerAbility.cs" for the full
mechanics and the `Bullet.cs` fix it depends on.

### Medic positioning + proximity aura (implemented)

Built in Session 13, alongside the "Manual teammate ability triggering"
design's prerequisites — see [player-roles.md](player-roles.md)'s
"PlayerAbility.cs" for the full aura/boost mechanics reference; this
section covers the positioning half and the pieces specific to
`AIController.cs`.

**Positioning**: Medic's default is `BiasedPositionDirection()` (see
above), passed `medicBias` (-0.3) instead of `guardBias` — the negative
bias extrapolates past ally center, away from the boss, giving Medic a
"holds toward the back of the party" position rather than Tank's "stands
between the party and the boss." But it's not unconditional (Session 14):
every frame, `FindHurtAlly()` scans `PlayerAbility.allies` (all 4 ships,
see "New wiring" below) for whichever ally has the lowest health-or-shield
fraction, if any is at or below `medicApproachThreshold` (0.55) in either
pool — mirrors `TickAura()`'s own "does this ally need anything" check, so
positioning and healing agree on what counts as hurt. If one is found,
Medic steers directly at it (`ApproachDirection()`) instead of hanging
back, re-evaluated every frame so it re-targets immediately as the
situation changes.

**Aura + boost ability** (full mechanics in
[player-roles.md](player-roles.md)): replaced Medic's old instant
self-heal entirely — pressing `E` now triggers a temporary, drastic
expansion of an always-on passive aura rather than an instant heal. This
was a deliberate design revision made mid-session (superseding what this
doc and `player-roles.md` previously described as "always a 2.25-ship
radius"): the aura is **tiny by default** (allies must nearly touch the
Medic) and **E temporarily makes it large and fast** instead. This also
resolves the long-standing "Medic heal only targets self" item — as a
proximity aura, not manual ally-targeting, matching the original design
intent.

**AI trigger heuristic — TEMPORARY (Session 14)**: `AIController` fires
the boost the instant it's off cooldown, with no awareness of whether
anyone actually needs it. The original heuristic (fire below 60% of the
*Medic's own* HP) was worse than useless — Medic's positioning keeps it
away from the boss specifically so it doesn't take damage, so that gate
almost never opened, and playtesting confirmed it: the boost never fired
once across a full session. Rather than guess at a better condition
without more playtesting data, it was replaced with the same
"fire on cooldown" pattern Support/Attacker already use, explicitly flagged
in code and here for rework — the obvious next step is triggering off
`FindHurtAlly()` (below) instead of nothing, but that wasn't done yet.

**Why the aura lives on `PlayerAbility.cs`, not here**: unlike Tank's
guard-point steering (an AI-only concern — a human Medic just moves via
WASD), the aura must behave identically whether Medic is human- or
AI-controlled, per Session 10's "AI teammates are mechanically identical to
a human player except for input" principle. `AIController` only exists on
the 3 `Teammate_*` GameObjects; `PlayerAbility` exists on `Player` too, so
that's where role-specific ability/aura logic belongs — `AIController`'s
only role in this feature is the movement/approach logic above and the
(currently trivial) trigger heuristic just above.

**New wiring**: `PlayerAbility.allies[]` — a `Transform[]` of all 4 ships,
self included, filtered at runtime — had to be wired fresh on all 4 ships'
`PlayerAbility` components, since `AIController.teammates[]` deliberately
excludes `Player` and can't be reused for something that must also heal
the human. Hit the familiar prefab-instance gotcha once more (see below):
`Teammate_Tank` needed `RecordPrefabInstancePropertyModifications()`,
`Teammate_Medic`/`Teammate_Support` didn't. Session 14's `FindHurtAlly()`
reuses this same array (`ability.allies`, read from `AIController` via its
already-cached `PlayerAbility` reference) rather than adding a second one
— same reasoning: it needs to react to the human `Player` being hurt too,
which `teammates[]` can't do.

**Visual feedback** (requested as an immediate follow-up once the mechanic
was working but invisible): a dim, thin `LineRenderer` ring around the
Medic shows the aura's current radius live — bigger and brighter while
boosted — built procedurally in `PlayerAbility.Awake()` only when
`role == Medic`. Allies actually healed by a tick get a distinct green
flash via a new `PlayerDamageFlash.Flash(Color)` overload (the existing
parameterless `Flash()` is unchanged, now just a thin wrapper), separate
from the white damage flash so the two read as different events.

### Support roaming positioning (implemented)

Completes the "decided design" (2026-08-20) for Support: "intentionally the
least constrained of the four — roams the available screen freely rather
than holding a zone." Unlike Tank/Medic's `BiasedPositionDirection()` (which
steers toward a point derived from allies/the boss and holds there),
Support has no reference point at all — it's a random-waypoint wander:

`AIController.WanderDirection()` steers toward a private `roamTarget`
(`Vector2`), picking a new random one (`RandomRoamPoint()`) whenever the
current one is reached (within `roamDeadzone`, 0.3) **or** after
`roamInterval` (3s) elapses, whichever comes first — the timer exists so
Support can't get stuck endlessly closing the last stretch of distance.
`RandomRoamPoint()` picks a uniformly random point within the same
viewport bounds `PlayerController.HandleMovement()` already clamps
movement to (`Camera.main.ViewportToWorldPoint`, inset by
`PlayerController.screenPadding` — reused directly as the single source of
truth for the inset, not duplicated as a separate constant).

**Deliberately does not return `Vector2.zero` inside the deadzone** — unlike
`ApproachDirection()`/`BiasedPositionDirection()`, which hold position once
arrived (correct for Tank's guard point or Medic hanging back), arriving at
a roam point immediately triggers picking the next one, so Support keeps
moving continuously rather than pausing. This is the one place Support's
positioning code deliberately diverges from the existing steer-and-deadzone
pattern, not an oversight.

No boss-avoidance or top-edge exclusion — Support is explicitly the least
constrained of the four roles; that concern belongs to Attacker's design
instead, see "Attacker patrol + boss-tracking positioning" below.

New fields are brand-new, not changes to already-serialized existing ones,
so — unlike most of this project's prior scene-wiring passes — no
prefab-instance/scene-value gotcha applied here; every `Teammate_*`
instance picks up the script defaults (`roamDeadzone`/`roamInterval`)
automatically.

Verified via the Unity MCP bridge in Play mode: reflection-called
`WanderDirection()` on `Teammate_Support` directly, confirming it returns a
normalized direction with a non-trivial Y component (unlike the old X-only
weave) and that `roamTarget` lands within viewport bounds; sampled its
transform position over several pumped frames and confirmed both X and Y
changed over time (cross-checked against `Teammate_Tank`/`Teammate_Medic`,
whose existing positioning was unaffected). No console errors/warnings.

### Attacker patrol + boss-tracking positioning (implemented)

Session 17. Superseded the original 2026-08-20 "decided design" for
Attacker (*"patrols to cover the available screen width... while staying
clear of the boss and the top edge"*) with a hybrid design worked out live
in conversation, not implemented as originally written.

**Why the original design got revised**: ships never rotate and bullets
only ever fire straight up (`Vector2.up`, no homing — see Bullet.cs above)
— an Attacker patrolling a fixed, boss-independent center would frequently
drift out of the boss's current lane as it sine-drifts, and just miss.
Pointed out mid-conversation, before any code was written. The user
proposed tracking the boss's X directly instead, at a balanced mid-distance
(not Tank-close, not Medic-far); resolved as a **hybrid** — keep
independent side-to-side patrol motion for spread/coverage and visual
variety, but anchor the patrol's *center* to the boss's live X rather than
a fixed point, so it's never far out of the boss's lane while it still
moves around within it.

`AIController.AttackerPositionDirection()`, same "compute a target point,
seek it, zero inside a deadzone" shape as `BiasedPositionDirection()`/
`ApproachDirection()`:

1. `targetY = Mathf.LerpUnclamped(GetAllyCenter().y, boss.transform.position.y, attackerBias)`
   — the same ally-center/boss blend Tank and Medic use, applied to Y only.
   `attackerBias` (0.45) sits between Medic's `-0.3` (hangs back) and Tank's
   `0.65` (leans hard toward the boss), giving the balanced stand-off
   distance the design calls for. Because the boss sits near the top of the
   screen (world Y fixed at `4.2`, see Scene wiring below) and ally center
   is naturally lower/mid-screen, this also keeps Attacker clear of the top
   edge for free — no separate top-edge check was needed for that part of
   the original design intent to still hold.
2. `targetX = boss.transform.position.x + Mathf.Sin(Time.time * weaveFrequency) * attackerPatrolAmplitude`
   — patrols around the boss's *current* X (reusing the existing
   `weaveFrequency` field rather than adding a second oscillation-speed
   constant) instead of an independent center. This is the actual fix for
   the "shots miss" problem: the patrol always stays anchored under
   wherever the boss currently is. `attackerPatrolAmplitude` (1.5) controls
   how wide the swing is.
3. Returns the normalized direction to `(targetX, targetY)`, or
   `Vector2.zero` inside `attackerDeadzone` (0.2, matching
   `guardDeadzone`'s default).

The ally-center averaging loop (previously inlined only in
`BiasedPositionDirection()`) was extracted into a shared private
`GetAllyCenter()` so this doesn't duplicate it a second time — same
"extract instead of duplicate" precedent as Session 13 generalizing
`GuardPointDirection()` into `BiasedPositionDirection()` itself.

**No changes needed to the ability-triggering switch, `Bullet.cs`,
`PlayerController.cs`, or `PlayerAbility.cs`.** The other half of the
original ask — "fire the ability the instant it's ready" — was already
exactly how Attacker's `TryUseAbility()` heuristic worked (see above); this
session was positioning-only.

New fields are brand-new, so — same as Support's `roamDeadzone`/
`roamInterval` above — no prefab-instance/scene-value gotcha applied;
every `Teammate_*` instance (including the `Teammate_Tank` prefab
instance) picked up the script defaults (`attackerBias`/
`attackerPatrolAmplitude`/`attackerDeadzone`) automatically, confirmed by
reading them back live in Play mode.

**Known degenerate case, not unique to Attacker**: if every other AI
teammate is dead, `GetAllyCenter()` falls back to the caller's own current
position (its documented behavior, shared by Tank/Medic already), which
means `targetY`'s per-frame lerp keeps nudging toward `boss.transform.position.y`
using the ship's own just-updated position as the new "ally center" each
frame — over enough frames this asymptotically converges Attacker's Y onto
the boss's, rather than holding a mid-distance stand-off. Observed live
during verification (see below) once Tank and Medic had both died mid-test.
Not a new bug introduced here — the same fallback already governs Tank's
and Medic's positioning when allies die — and only matters in the
"down to one or two teammates" endgame, not normal play.

Verified via the Unity MCP bridge in Play mode: temporarily reassigned
`Player` to Support and `Teammate_Support` to Attacker (so an AI teammate
actually played Attacker for the test, since the default scene has the
human `Player` on Attacker), entered Play mode, and sampled
`Teammate_Support`'s position against `Boss.transform.position.x` over
several pumped frames — X stayed within `attackerPatrolAmplitude` of the
boss's current X throughout (never drifted to an independent center), and Y
climbed from near the back of the party toward the mid-distance blend as
expected. The boss was defeated in-test (~18s of continuous 4-ship fire,
Attacker contributing real DPS the whole time) with no console
errors/warnings throughout. Reverted the temporary role reassignment
afterward and confirmed via a full scene reload from disk that the
original assignment (`Player` = Attacker) was restored.

### Support fire-cadence/damage catch-up (implemented, then superseded by Session 16's fixed-stats overhaul)

The other half of Support's decided design — "the same fire cadence as
Attacker, the same fire damage as Tank" — was originally implemented
(Session 15) as a `fireRateMultiplier`/`damageMultiplier` on top of a
shared base. **Session 16 replaced the entire base×multiplier stat
architecture with fixed, absolute per-role values** (see
[player-roles.md](player-roles.md)'s "Fixed per-role stats") — Support's
fire rate/damage are no longer derived from a multiplier at all, just a
direct number in the `RoleStats` table. The *design intent* this section
originally captured (Support fast + hard-hitting, matching Attacker's
cadence and Tank's damage) carried forward into the new fixed values
unchanged; only the underlying mechanism changed.

## BossPanelUI.cs

**Attached to:** `BossPanel` (child of `HUDCanvas`, replacing its old
"Boss stats coming soon" placeholder — see [hud-layout.md](hud-layout.md)).
**Requires:** a direct `boss` reference (this panel is scene-bound, not a
reusable prefab like `PartyFrame.prefab`).

Every `Update()`, reads `Boss.CurrentHealth/maxHealth` into a health-bar
`Image.fillAmount` + `"HP: x/y"` text, `Boss.IsPhase2` into a `"Phase
1"`/`"Phase 2"` text, and `Boss.CurrentTarget`'s `PlayerRoleComponent.role`
into a `"Target: {role}"` text — same "HUD only reads, never owns game
state" pattern as `PartyFrameUI.cs`. `ShowDefeated()` (wired to
`Boss.OnDefeated`) overwrites the phase text with `"DEFEATED"`.

Key public fields: `boss`, `healthBarFill`, `healthText`, `phaseText`,
`targetText`. Key public method: `ShowDefeated()`.

## PlayerAbility.cs / PlayerController.cs — non-input entry points

Both scripts gained public methods so `AIController` can drive them without
going through `PlayerInput`'s input-callback path:

- `PlayerController.SetMoveDirection(Vector2)` / `SetFiring(bool)` —
  extracted from `OnMove(InputValue)` / `OnFire(InputValue)`, which now just
  unwrap the `InputValue` and call these. No behavior change for the human
  `Player`.
- `PlayerAbility.TryUseAbility()` — extracted from the private dispatch
  previously inline in `OnAbility(InputValue)`. The four `Trigger*` methods
  (`TriggerTaunt`, `TriggerAuraBoost`, `TriggerSpeedBoost`, `TriggerBigShot`
  — `TriggerHeal` originally, renamed when Medic's ability changed;
  `TriggerBuff` originally, renamed to `TriggerSpeedBoost` when Support's
  ability was redesigned party-wide — see "Medic positioning + proximity
  aura" above and [player-roles.md](player-roles.md) respectively) stay
  private/unchanged.

Also: `PlayerController.SpawnBullet()` now passes `gameObject` into
`Bullet.Init(..., ownerObject)` so aggro attribution works for player fire
too (see Bullet.cs above).

## Scene wiring

### Boss

**Tag:** `Enemy`. **Prefab:** `Assets/Prefabs/Boss.prefab` (SpriteRenderer,
Rigidbody2D at Gravity Scale 0, non-trigger BoxCollider2D — same physical
setup as `Enemy.prefab`). One instance in `Gameplay`, positioned at
`(0, 4.2, 0)` — **must stay within the camera's visible range**: the Main
Camera is orthographic with size 5, so world Y outside roughly `[-5, 5]` is
off-screen (an earlier placement at `y=6` was invisible in Play mode; caught
via a screenshot during testing, not by inspecting numbers alone).

| Component      | Key inspector values                                                    |
| --------------- | ----------------------------------------------------------------------- |
| Transform       | position (0, 4.2, 0), scale (1.6, 1.6, 1) — **not** shrunk with the ships below |
| **Boss.cs**     | `maxHealth`: 90 (see "Tuning" below); `targets`: `Player` + all 3 `Teammate_*`; `bulletPrefab`: EnemyBullet prefab; `enemySpawner`: `Spawner`; `OnDefeated`: `BossPanel/BossPanelUI.ShowDefeated()` |

### Teammate_Tank / Teammate_Medic / Teammate_Support

Each is a duplicate of `Player`'s component set with `PlayerInput` removed
and `AIController` added, tagged `Player` (so `Bullet.cs`'s existing
player/enemy tag logic treats them exactly like the human player), with a
distinct `PlayerRoleComponent.role` (Tank / Medic / Support — `Player`
itself stays the default `Attacker`) so all 4 roles are covered exactly
once. `Teammate_Tank` is the one actually linked to
`Assets/Prefabs/Teammate.prefab`; `Teammate_Medic`/`Teammate_Support` were
duplicated from it *before* the prefab link was created, so they're
plain independent GameObjects with the same component values, not prefab
instances — a future edit meant to apply to all three teammates has to be
applied to each individually (or to `Teammate.prefab` **plus** the two
non-linked copies), not just to the prefab asset.

| Component            | Key inspector values                                                        |
| --------------------- | ----------------------------------------------------------------------------- |
| Transform              | scale (0.6, 0.6, 1) — see "Ship scale" tuning below                          |
| **AIController.cs**   | `boss`: the `Boss` instance                                                   |
| **PlayerRoleComponent** | role: Tank / Medic / Support respectively                                    |

Role assignment (who plays which of the 4 roles) is **Inspector-only** —
matching the existing single-player role-selection pattern
(`player-roles.md`) — there's no in-game role-select screen; swap
`PlayerRoleComponent.role` on `Player` and the corresponding `Teammate_*`
by hand to test a different human role.

### Tank taunt → boss aggro

On all 4 `PlayerAbility` components (`Player` + 3 `Teammate_*`), `OnTaunt`
has a persistent listener to `Boss.TauntedBy(GameObject)` with the fixed
argument dragged to that same GameObject (each player's taunt targets
itself). This is **additive** to the Session 9 placeholder feedback
(`PlayerDamageFlash.Flash()` + `CameraShake.Shake()`) — both listeners fire
on every taunt, not a replacement.

### Death handling

Only the human `Player`'s `PlayerHealth.OnDeath` shows `GameOverPanel`
(`GameOverPanel/GameOverUI.Show()`). Each `Teammate_*`'s `OnDeath` is wired
only to its own party frame (`PartyFrame_N/PartyFrameUI.OnPlayerDied()`) —
a teammate dying just grays its frame, it doesn't end the whole test.

## Tuning: fire cadence and ship scale

Two follow-up balance passes on the base prototype:

- **Fire cadence** — `PlayerController.fireRate`'s base value went from
  `0.2` to `0.35` (script default, `Teammate.prefab`, and all 4 scene
  ships), making the fight take more sustained effort. Role multipliers
  still apply on top unchanged, so relative balance between roles is
  preserved: Attacker `0.35 × 0.75 = 0.2625s`, Medic `0.35 × 1.0 = 0.35s`,
  Tank `0.35 × 1.2 = 0.42s` (Support's multiplier changed in a later pass
  below — now also `0.2625s`, matching Attacker).
- **Ship scale** — `Player`/`Teammate_*` `Transform.localScale` went from
  `1.0` to `0.6` (40% smaller). The `Boss` was deliberately **not** shrunk
  (stays `1.6`) so it still reads as the big, central target — this leaves
  visual room for minions planned around the boss (see `../roadmap.md`)
  without the boss itself shrinking to match. Child objects (`FirePoint`)
  and `BoxCollider2D` needed no separate edit: Unity scales a child's
  effective position and a collider's size by the parent transform's scale
  automatically.
- **Boss health + player damage** (a third follow-up pass, same session as
  the Tank guard-point work above) — `Boss.maxHealth` doubled (`30` → `60`)
  and every role's player-dealt fire damage cut 40%: regular fire damage
  `PlayerController.Fire()`'s `1` → `0.6`, Attacker's Big Shot
  `PlayerAbility.bigShotDamage`'s `3` → `1.8`. Together this meaningfully
  lengthens the fight without touching fire cadence again. Values are
  round/arbitrary picks (no specific numbers were requested), tunable like
  everything else here. Boss/enemy-dealt damage is untouched — only
  player-dealt damage was in scope. See [combat.md](combat.md) for the
  `int` → `float` type change this required on `Bullet.damage` (and
  `Enemy.TakeDamage`/`Boss.TakeDamage`'s signatures) to allow fractional
  damage values, and the gotcha below about re-hitting the same
  already-serialized-scene-value issue from the fire-cadence pass above.

- **Support fire cadence/damage** (a fourth follow-up pass, alongside the
  Support roaming positioning above) — completes `boss.md`'s "decided
  design" for Support. `PlayerRoleStats`'s `Support` entry: `fireRateMultiplier`
  `1.0` → `0.75` (matches Attacker's cadence). A new `RoleStats.damageMultiplier`
  stat (didn't exist before — every role dealt the same flat regular-fire
  damage) was added and applied in `PlayerController.Start()`/`Fire()` via
  a new `fireDamage` field (base `0.6`, scaled by the multiplier, replacing
  the old hardcoded `SpawnBullet(1f, 0.6f)` literal). Tank and Support both
  get `1.5x` ("hard-hitting"); Attacker/Medic stay at the `1.0x` baseline —
  Attacker's high damage output already comes from Big Shot, which this
  stat doesn't touch. **Necessary side effect**: since no role had elevated
  fire damage before this pass, giving Support "the same fire damage as
  Tank" required deciding Tank's own multiplier too (`1.5x`, round
  placeholder, tunable) — Tank's regular-fire damage output changes from
  this pass as well, not just Support's. See
  [player-roles.md](player-roles.md)'s balancing table for the full
  before/after.

**Gotcha, hit twice in this pass**: same class of issue as the fire-cadence
tuning above — changing a public field's *script* default (`Boss.maxHealth`,
`PlayerAbility.bigShotDamage`) does **not** retroactively update a value
already serialized on an existing scene GameObject or prefab instance. Both
had to be set explicitly on the live scene instances (all 4 ships for
`bigShotDamage`) **and** on `Boss.prefab`/`Teammate.prefab`'s defaults, with
`Teammate_Tank`'s prefab-instance override additionally needing
`PrefabUtility.RecordPrefabInstancePropertyModifications()` (same as the
`teammates[]` gotcha above) — verified each time by forcing a full scene
reload from disk rather than trusting the in-memory value.

- **Fixed per-role stats overhaul + boss HP bump** (Session 16) — replaced
  the entire `base × multiplier` stat architecture with fixed absolute
  values (see [player-roles.md](player-roles.md)'s "Fixed per-role stats"
  for the full table and reasoning); also added Tank's Shield Arc, redesigned
  Support's ability into a party-wide Speed Boost, and halved Medic's
  boosted aura radius (all `PlayerAbility.cs`, see
  [player-roles.md](player-roles.md)). Alongside this, `Boss.maxHealth`
  went ×1.5 (`60` → `90`), purely to give the reworked stats/abilities
  enough runway in a full playthrough to actually be observed rather than
  the fight ending before their effects are visible. **Same
  already-serialized-value gotcha hit again**: `Boss.maxHealth` and
  `PlayerAbility.auraBoostRadius` both had to be set explicitly on the live
  scene instances (all 4 ships for `auraBoostRadius`) and on
  `Boss.prefab`/`Teammate.prefab`'s defaults, verified by a full scene
  reload from disk. Every genuinely *new* field this pass (`fireDamage`
  becoming role-scaled directly, `speedBuffMultiplier`/
  `fireRateBuffMultiplier`, the Shield Arc's fields, the party-buff ring's
  fields) did **not** need this treatment — only edits to fields that
  already existed and were already serialized hit the gotcha.

## Known environment quirk hit during testing

Same one documented in `../progress-log.md` Sessions 6–8: this Unity Editor
instance does not reliably tick Play-mode `Update()` while its window is
unfocused/idle, so real elapsed time between MCP tool calls can be
near-zero for a while and then jump substantially once the window regains
focus. During boss-fight testing this showed up as the boss appearing to
take almost no damage across several tool calls and then being defeated
between the next two — not a bug in `Boss.cs`, just this same simulation-
pacing quirk. `manage_camera` screenshot calls (each forces one manual
frame step) remain the reliable way to pump deterministic frames for
testing, as in prior sessions.

## Verified (Unity MCP bridge, Play mode)

- Phase transition flips `IsPhase2` exactly at the 50%-HP boundary and
  fires `OnPhase2` exactly once.
- Aggro correctly tracks the highest damage-dealer as `CurrentTarget`;
  `TauntedBy()` redirects it to the taunter; a second immediate taunt is
  blocked by `PlayerAbility.CooldownRemaining`.
- AI teammates autonomously move, auto-fire, and trigger their role's
  ability over time (observed Tank's taunt firing for real the moment it
  didn't hold aggro, and Support's Speed Boost auto-activating).
- Boss defeat (`TakeDamage` to 0) fires `OnDefeated`, `BossPanelUI` shows
  `"DEFEATED"`, and the `Boss` GameObject is destroyed with no console
  errors.
- All 4 `PartyFrameUI` instances (see [hud-layout.md](hud-layout.md)) and
  `BossPanelUI` read live, correct values with no drift from `Boss`'s /
  each `PlayerHealth`'s actual state.

## Future work

Two concrete gaps a playtest surfaced, written up here (rather than just
listed under "Not yet built" below) so a future session can act on them
directly without re-deriving the current limitations from scratch. Both
are recommended **before** "Minions around the boss" (`../roadmap.md`) —
adding more on-screen threats on top of a still-too-simple AI/boss would
make it harder to read whether the encounter is actually fun, not easier.

### AI teammate behavior

Exact original limitation (Tank/Medic/Support/Attacker have since all been
built, see below): `AIController.Update()` set movement as
`controller.SetMoveDirection(new Vector2(Mathf.Sin(Time.time * weaveFrequency), 0f) * weaveSpeed)` —
every frame, unconditionally, X-only (Y always `0`), with zero awareness of
incoming bullets, the boss's position, or where the other teammates are.
This was a deliberate "just prove the aggro/taunt mechanic" simplification
for the prototype (see Session 10 in `../progress-log.md`), not a finished
AI. It now only survives as the `default` safety fallback for any future
unhandled role — no actual role uses it anymore.

**All four roles now have real, role-differentiated positioning.** Applies
only to AI-controlled `Teammate_*` ships — if a human plays a given role
instead, none of this positioning logic runs for that ship (there's no
`AIController` on `Player`) — though Medic's aura and Support/Tank's fire
stats still work for a human, since they live on `PlayerAbility.cs`/
`PlayerRoleStats` rather than `AIController.cs` (see "Medic positioning +
proximity aura" / "Support roaming positioning" above). See "Tank
guard-point positioning / physical blocking", "Medic positioning +
proximity aura", "Support roaming positioning" / "Support fire-cadence/
damage catch-up", and "Attacker patrol + boss-tracking positioning" above
for each role's full writeup — Attacker's supersedes the original
"patrol screen width, avoid the boss" decided design (2026-08-20) with a
hybrid patrol-plus-boss-tracking design worked out in Session 17, see that
section for why.

Still open, from the original prototype-era list, and still needed
regardless of the above:

- **Bullet-dodging** — react to nearby bullets rather than moving purely by
  role-zone. Candidate approach: each frame, check for `EnemyBullet`-tagged
  objects (or bullets owned by `Boss`) within some radius/lane ahead of the
  teammate and bias `moveInput` away from them; exact detection method
  (`OverlapCircle`, tag+distance check, etc.) and "how close counts as a
  threat" are open.
- **Basic separation** — teammates currently have no awareness of each
  other and can end up stacked/overlapping; a simple repulsion term (push
  away from the nearest other `Player`-tagged ship within some radius) is
  still needed on top of the role-zone steering above.

**Open question, not yet decided**: today's bullets are all straight-line
(direction fixed once at spawn — see Bullet.cs above), which is exactly why
Tank's physical blocking above works "for free." If a future boss/minion
attack fires bullets that curve or re-aim mid-flight (see "Boss combat
dynamism" below), a bullet already past the Tank's position — or one that
curves around it — wouldn't be stoppable through positioning alone. Not a
blocker for building the design above (the boss's existing two patterns are
still straight-line), but worth remembering before or while designing any
homing/curving attack.

### Manual teammate ability triggering

**Decided design (not yet implemented)**, agreed 2026-08-20: the player can
force any teammate's ability to fire right now (subject to that teammate's
own cooldown), overriding the AI's per-role heuristic for that instant —
e.g. timing a Tank taunt or a Support buff deliberately rather than waiting
for the AI to decide on its own. Mechanic: each `PartyFrame_N`'s ability
line/icon (`PartyFrameUI.abilityText`, see [hud-layout.md](hud-layout.md))
becomes a clickable/tappable UI element that calls that teammate's
`PlayerAbility.TryUseAbility()` directly — the exact same public,
cooldown-gated method `AIController.cs` already calls (see above) and the
human `Player`'s own `OnAbility(InputValue)` already wraps (see
[player-roles.md](player-roles.md)). This needs **no new ability logic** —
`TryUseAbility()` already exists, is already cooldown-gated, and already
dispatches per-role — only a UI-side click/tap handler on the party frame.
Click (PC) and tap (mobile) both fire Unity UI's standard pointer-click
event, so this is one mechanic across both platforms with no separate
control scheme, hotkey binding, or radial menu needed. Doesn't change
ability *targeting* (still self-targeted per role, same as today) — only
*when* it fires.

### Boss combat dynamism

Exact current limitation: `Boss.cs`'s movement is a fixed, slow sine drift
(`sineAmplitude: 2`, `sineFrequency: 0.5`) around a static Y, and both
attack patterns are flat-timer — Phase 1 fires one aimed shot every
`phase1FireInterval` (1.2s), Phase 2 fires a 3-bullet spread every
`phase2FireInterval` (0.6s) — with no variety beyond the one Phase-1→Phase-2
switch. It reads as a stationary turret, not an opponent actively fighting
back.

Directions to design against:

- **A rapid-fire burst attack** — a short telegraph (e.g. a brief color
  flash or scale pulse, giving players a fair warning) followed by a quick
  volley of shots at a much faster interval than the existing steady fire,
  then a return to normal-interval firing. Distinct from — not a
  replacement for — the existing Phase 1/2 patterns; could trigger on a
  timer, at random, or at specific HP thresholds within a phase.
- **More deliberate repositioning** — rather than a continuous drift,
  periodically pick a new target X (or X/Y) and move toward it over time,
  then hold, giving movement more shape/intent than a pure sine wave.
- **Telegraphing** — any new heavier attack (the burst above, or a wider
  spread) should have a brief visible wind-up so it reads as fair/readable
  rather than just harder — consistent with the existing fire-cadence
  tuning goal ("Tuning" section above) of "hard but not unfair."
- **Targeted/curved bullet trajectories** (open question, not yet decided)
  — today's "aimed shot" only ever computes a fixed direction once at fire
  time (straight line afterward, see Bullet.cs above); a future attack could
  re-aim at its target's *current* position over the bullet's lifetime, or
  curve/spiral, for more dynamic threat shapes. Flagged specifically because
  it interacts with the Tank physical-blocking design in "AI teammate
  behavior" above — a curving/homing bullet may not be interceptable just by
  standing in its original path, so this needs revisiting once/if such an
  attack is actually designed.

**Explicitly out of scope for this direction**: adding a 3rd phase, an
enrage state, or any behavior after Phase 2 beyond death. This is about
movement/attack *variety within* the existing 2-phase structure — see
"Movement and firing" above for why 2 phases ending in death is the
complete, intended design, not a partial one. That's a separate, bigger
design question if it ever comes up.

## Not yet built

- **Minions around the boss** — motivated the ship-shrink above; no
  minion script or prefab exists yet. Recommended after the two "Future
  work" items above, not before.
- **Local co-op / dynamic player count** — the party is 4 fixed, hand-
  placed scene objects, not a runtime spawner (see `../roadmap.md`'s "In
  Progress").
