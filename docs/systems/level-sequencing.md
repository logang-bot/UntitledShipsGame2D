# Level Sequencing

Owns the pre-fight-to-boss timeline for a level: ships glide in, free
movement, minions spawn and fight for a while, the boss glides in once the
screen is clear, then boss combat begins (with minions returning at phase
2). See [marauder-boss.md](bosses/marauder-boss.md) for the boss itself and
[combat.md](combat.md) for `EnemySpawner.cs`'s wave-formation minion
system this reuses.

This is the codebase's first "top-level orchestrator" — see
[architecture.md](../architecture.md)'s "Sequencing: One Top-Level
Orchestrator Per Level" for why this is a new coordination *shape* without
actually breaking any of this project's existing conventions (still a
plain `MonoBehaviour`, still Inspector-wired, no singleton/
`FindObjectOfType`). Kept intentionally minimal: one script, no generic
framework. Named generically (not `Level1`-prefixed) since it's duplicated
as-is into each level's own scene (`Level1`/`Level2`/`Level3`) — only the
boss instance and
this component's own Inspector-tuned durations are Level-1-specific.

## LevelSequencer.cs

**Attached to:** a standalone `LevelSequencer` GameObject in each level
scene (`Level1`/`Level2`/`Level3`).
**Requires:** `ships[]` (drag `Player` + all 3 `Teammate_*`), `enemySpawner`
(drag `Spawner`), `bossObject` (drag the `Boss` instance — `MonoBehaviour`-typed
and cast to `IBoss` internally so this same script drives either
`MarauderBoss` or `HalcyonBoss` with zero sequencing changes; see
`../architecture.md`'s "Boss-type-agnostic orchestration: IBoss").

A `SequenceState` enum (`Intro`, `FreeMovement`, `MinionPhase1`,
`WaitingForClear`, `BossEntrance`, `BossCombat`) tracks progress, exposed
read-only as `CurrentState` for inspection. `Start()` runs one coroutine
(`RunSequence()`) that drives every phase linearly, in order:

1. **Intro** (`introDuration`, 4s) — all 4 ships start below the visible
   viewport (computed from `Camera.main.ViewportToWorldPoint`, not authored
   marker Transforms) and lerp up to their scene-placed positions. Ships are
   frozen throughout (see "Freezing a ship" below).
2. **FreeMovement** (`freeMovementDuration`, 4s) — ships unfreeze, nothing
   else happens yet. No minions.
3. **MinionPhase1** (`minionPhase1Duration`, 120s) — calls
   `enemySpawner.StartSpawning()`; minions fight the party using
   `EnemySpawner`'s existing wave-formation system (now picking formations
   at random — see [combat.md](combat.md)).
4. **WaitingForClear** — calls `enemySpawner.StopSpawning()` (no new waves,
   but an in-flight wave finishes), then polls `Enemy.Active.Count` every
   frame until it hits 0. The boss's entrance never starts while minions are
   still on screen.
5. **BossEntrance** (`bossEntranceDuration`, 4s) — ships freeze again. The
   boss — hidden and non-collidable since `Start()` via `SetVisible(false)`
   (see "Boss visibility/collision" below) — is made visible/collidable
   again here. Its scene-placed position is read as `home`, then it's
   teleported above the top of the viewport and lerped back down to `home`.
   `MarauderBoss`'s component stays disabled throughout the glide, so it does
   nothing but move — no firing, no aggro, no movement pattern.
6. **BossCombat** — ships unfreeze, `bossObject.enabled = true`. This fires
   the boss's own `OnEnable()` — for `MarauderBoss`, captures its current
   position as its own `home` and starts its movement-pattern coroutine
   (see [marauder-boss.md](bosses/marauder-boss.md)'s "Movement and
   firing"); for `HalcyonBoss`, enables its three sibling mechanics (see
   [halcyon-boss.md](bosses/halcyon-boss.md)). The sequence coroutine ends
   here — everything past this point is ordinary boss combat, unowned by
   `LevelSequencer`.

**Phase 2 minions need no sequencer state at all.** `MarauderBoss.OnPhase2` has
a persistent `UnityEvent` listener straight to `enemySpawner.StartSpawning()`,
wired in the Inspector — same "decoupled notification" shape as
`MarauderBoss.OnDefeated`'s own listeners (see
[marauder-boss.md](bosses/marauder-boss.md)). Minions return in the same
random-formation waves as phase 1, running concurrently with ongoing boss
combat, with zero additional code.

### Boss visibility/collision (Start / BossEntrance)

`LevelSequencer.Start()` calls `bossObject.SetVisible(false)` — `IBoss`'s
`SetVisible(bool)`, implemented by both boss types (`MarauderBoss` disables
its `SpriteRenderer`/`Collider2D`/shockwave ring; `HalcyonBoss` its
`SpriteRenderer`/`Collider2D`/Static Field ring — see
[marauder-boss.md](bosses/marauder-boss.md)/[halcyon-boss.md](bosses/halcyon-boss.md)).
The narrative below was worked out against `MarauderBoss` specifically, but
the mechanism and the reasoning both generalize — it's why `SetVisible`
became an `IBoss` method rather than staying `MarauderBoss`-only. It does
**not** call `SetActive(false)` on the whole `Boss` GameObject, and the
choice not to matters:

- **First attempt** disabled only the `MarauderBoss` component. That left the
  `SpriteRenderer` visible and the `Collider2D` live at the boss's home
  position the entire pre-boss sequence — visibly sitting there before its
  own phase started, and (since both it and wave `Enemy` instances were
  solid, `Dynamic`-body colliders) physically shoved around by real Box2D
  physics as enemies passed through its position.
- **Second attempt** deactivated the whole `Boss` GameObject instead
  (`SetActive(false)`), which does hide it and stop the physics push — but
  `MinionSpawner.cs` also lives on that GameObject, and deactivating it
  disabled `MinionSpawner` too, silently stopping kamikaze minions from
  spawning for the entire pre-boss window (a real regression, caught after
  a user reported it — see progress-log.md's Session 29).
- **Current approach**, `SetVisible(bool)`, only touches the boss's own
  `SpriteRenderer`/`Collider2D`/ring — the `Boss` GameObject itself stays
  active the whole time (unlike the second attempt above). Disabling the
  `Collider2D` (not just the sprite) also matters on its own: leaving it
  enabled let player bullets hit and damage the invisible boss via
  `Bullet.cs`'s normal trigger-based detection — caught live
  (`MarauderBoss.CurrentHealth` had already dropped below `maxHealth` seconds
  into the level, well before the entrance).

`MinionSpawner` itself is **not** toggled by `SetVisible` — a follow-up fix
(Session 30) moved it to `MarauderBoss.OnEnable()` instead, gated on actual
combat start rather than mere visibility. `SetVisible(false)` fires at the
*start* of the entrance glide, while ships are still frozen for several
more seconds; enabling `MinionSpawner` there too let kamikaze minions spawn
and overlap ships that couldn't react yet (`PlayerController.enabled ==
false` during the freeze means `FixedUpdate`/`ResolveShipCollisions` never
runs) — contact would silently do nothing even though the overlap was
plainly visible. `MarauderBoss.OnEnable()` fires later, exactly when
`LevelSequencer` sets `bossObject.enabled = true` at `BossCombat` — by
which point `SetShipsFrozen(false)` has already run, so ships can always
react to any minion that exists. See
[marauder-boss.md](bosses/marauder-boss.md)'s "Minion.cs / MinionSpawner.cs".
(`HalcyonBoss` has no minions, but its own `OnEnable()` — enabling
`HalcyonRoam`/`HalcyonSurge`/`HalcyonStaticField` — is gated on the exact
same `bossObject.enabled = true` call, for the same reason: nothing of
Halcyon's should act before ships can react to it.)

**Awake-order gotcha**: `SetVisible` is called from `LevelSequencer.Start()`,
not `Awake()`. `MarauderBoss.Awake()` is what caches the `SpriteRenderer`
reference `SetVisible` needs — but Unity doesn't guarantee one object's
`Awake()` runs before another's, only that *every* `Awake()` finishes
before *any* `Start()`. Calling it from `LevelSequencer.Awake()` worked or
silently no-op'd depending on arbitrary Awake ordering; moving it to
`Start()` makes it correct unconditionally.

See also `EnemySpawner.cs`/`Enemy.cs` in [combat.md](combat.md): `Enemy.prefab`'s
collider was separately changed to `isTrigger: true` (matching every ship's
collider) so enemy-vs-enemy and enemy-vs-boss physics collision can't
happen even once the boss is visible again (phase 2 minions run
concurrently with an active, visible boss) — `SetVisible(false)` alone only
covers the pre-boss window, and doesn't touch `Enemy`'s own collider setup.

### Freezing a ship (Intro / BossEntrance)

Disabling only `PlayerInput`/`AIController` is **not** enough to actually
stop a ship: `PlayerController.HandleMovement()` runs every `FixedUpdate`
and its held-fire check runs every `Update()`, regardless of those driver
components' enabled state — they only control who *calls*
`SetMoveDirection`/`SetFiring`, not whether `PlayerController` keeps acting
on the last values it was given. A ship still holding a key (or an AI
mid-autofire) the instant a freeze starts would otherwise keep drifting or
firing until the viewport clamp caught it.

`SetShipsFrozen(bool)`'s correct sequence, freezing: disable
`PlayerInput`/`AIController` first (blocks new input) → call
`SetMoveDirection(Vector2.zero)`/`SetFiring(false)` (clears stale input
state) → disable `PlayerController` itself last (stops `Update`/`FixedUpdate`
from running at all). Unfreezing reverses the order. `PlayerAbility`'s
ability input flows through `PlayerInput`'s Send-Messages (human) and is
only ever called by `AIController.TryUseAbility()` (CPU), so disabling
those two components alone is sufficient to also block ability use — no
separate lock needed there.

Key public fields: `ships[]`, `enemySpawner`, `bossObject`, `introDuration`
(4)/`freeMovementDuration` (4)/`minionPhase1Duration` (120)/
`bossEntranceDuration` (4)/`offScreenMargin` (1). Key public property:
`CurrentState`.

## Scene wiring

### LevelSequencer

Standalone empty GameObject in each level scene.

| Component | Key inspector values |
| --- | --- |
| **LevelSequencer.cs** | `ships`: `Player` + all 3 `Teammate_*`; `enemySpawner`: `Spawner`; `bossObject`: the `Boss` instance (`MarauderBoss` in `Level1`, `HalcyonBoss` in `Level2`); `introDuration`/`freeMovementDuration`/`bossEntranceDuration`: 4; `minionPhase1Duration`: 120 (15 in the current scenes, shortened for testing); `offScreenMargin`: 1 |

The 4 ships' Transform positions in the saved scene are each ship's "home"
— `LevelSequencer` captures them once in `Awake()`, before anything moves,
so they must already form the intended horizontal line near the bottom of
the screen.

## Not yet built

- A single hardcoded level flow — no support yet for chaining multiple
  levels/sequencers back to back, or for per-level data (boss reference,
  durations) living anywhere other than this component's own Inspector
  values in each level's scene.
- No third minion phase or any behavior past `MarauderBoss.OnDefeated` — see
  [marauder-boss.md](bosses/marauder-boss.md)'s "Not yet built".
