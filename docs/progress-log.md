# Progress Log

Session-by-session narrative history (the "why" behind decisions,
troubleshooting notes). Sessions 1-27 (pre-boss fundamentals through the
boss-encounter arc and the Level 1 rework) were archived to
[progress-log-archive.md](progress-log-archive.md), in two passes, to keep
this file scoped to recent sessions — cross-session references to Sessions
1-27 below point there. A few Unity/MCP environment gotchas that kept
getting re-explained across many sessions (Editor-idle Play-mode ticking,
prefab-instance property recording, stale serialized defaults) were also
consolidated into single canonical write-ups in
[unity-notes.md](unity-notes.md), with the sessions below now pointing at
those instead of re-stating the mechanism each time.

## Session 28 — Level 1 Follow-up Fixes: Boss Visibility, Enemy Collision, Enemy Scale

Three bugs reported after playing Session 27's (see progress-log-archive.md)
build: the boss was visible
sitting at its home position from the start of the level instead of only
appearing at its own phase; wave enemies were physically pushing the boss
around; and wave enemies rendered noticeably larger than the party's ships.

### Boss visible too early

`LevelSequencer` was only disabling the `Level1Boss` **component**
(`enabled = false`), which stops its `Update()`/combat logic but does
nothing about the GameObject's `SpriteRenderer` or `BoxCollider2D` — both
stayed live at the boss's home position the entire pre-boss sequence, so it
was plainly visible (and collidable) well before its entrance. Fixed by
deactivating the whole `Boss` GameObject instead
(`level1Boss.gameObject.SetActive(false)` in `LevelSequencer.Awake()`),
reactivating it at the start of `BossEntranceRoutine()` right before the
off-screen-to-home glide. The `Level1Boss` component itself still stays
disabled through the glide as before, so it does nothing but move until
`LevelSequencer` enables it at `BossCombat`.

### Wave enemies pushing the boss

Root cause: `Enemy.prefab`'s `BoxCollider2D` was `isTrigger: false` (solid)
on a `Dynamic` `Rigidbody2D` — same setup as `Level1Boss`'s own collider.
Two solid colliders on `Dynamic` bodies get real Box2D physics collision
response (separation impulses) applied automatically by Unity, entirely
independent of this project's own scripted movement — nothing in
`Level1Boss.cs`'s or `Enemy.cs`'s code was doing this, it was the physics
engine reacting to genuine overlaps as `Enemy` waves descended through the
boss's fixed high position. This had never surfaced before because
`EnemySpawner` was always disabled during a boss fight prior to this
project's Level 1 rework (Session 27, see progress-log-archive.md), so wave
enemies and the boss were
never both live and moving through the same space at once.

Fixed by setting `Enemy.prefab`'s `BoxCollider2D.isTrigger = true` —
matching the pattern every ship's collider already uses (per
`level1-boss.md`'s "Solid-body collision" — ships resolve their own
positions manually via `ShipCollisionUtil`, not real physics, specifically
because trigger colliders never generate physics response). This
eliminates collision response for enemy-vs-boss **and** enemy-vs-enemy
(e.g. a `Cluster` formation's enemies no longer jostle each other either),
with zero effect on `Bullet.cs`'s hit detection — `OnTriggerEnter2D` still
fires normally for a trigger-vs-trigger or trigger-vs-solid pair, so
player-bullet-vs-enemy damage is unaffected.

### Enemy scale

`Enemy.prefab`'s `Transform.localScale` was still `(1, 1, 1)` — never
touched by the earlier "Shrink ship sprites" pass (see `roadmap.md`) that
brought every ship down to `0.6`. Set to `(0.6, 0.6, 1)` to match.

### Verified

Both prefab changes applied via the Unity MCP bridge's `manage_prefabs`
(confirmed live via `execute_code`: `Enemy.prefab`'s loaded asset shows
`scale=(0.6, 0.6, 1)`, `isTrigger=True`). In Play mode: confirmed
`level1Boss.gameObject.activeSelf == false` throughout intro/free-movement/
minion-phase-1 and a screenshot showed no boss sprite on screen during that
window; forced the boss to phase 2 (concurrent wave enemies + an active,
visible boss) and directly teleported a live enemy to the boss's exact
position to force a dead-center overlap — `Rigidbody2D.linearVelocity`
stayed `(0, 0)` immediately after, confirming the trigger fix holds even
under a forced worst-case overlap, not just incidentally. No console errors
throughout.

### Still open

Same as Session 27's (see progress-log-archive.md) "Still open" — this was a
bugfix pass on top of that
work, not new scope. A real human playtest still hasn't happened.

## Session 29 — Fix: Kamikaze Minions Silently Disabled by the Boss-Visibility Fix

User report: after Session 28's fix, the boss-flanking kamikaze `Minion`
mechanic (Sessions 24/26) had stopped working — ships could no longer take
contact damage from minions. Root cause: `MinionSpawner.cs` lives on the
same `Boss` GameObject as `Level1Boss`, and Session 28's fix
(`level1Boss.gameObject.SetActive(false)` to hide the boss and stop it
being physically pushed by wave enemies) deactivated the *entire*
GameObject — silently disabling `MinionSpawner` too for the whole pre-boss
window, since `Minion.cs`'s kamikaze contact damage is pure manual
overlap math (`PlayerController.ResolveShipCollisions()`), never actually
touched by the collider/trigger changes from that session.

### Fix: `Level1Boss.SetVisible(bool)` instead of `SetActive`

Replaced the whole-GameObject toggle with a new `Level1Boss.SetVisible(bool)`
method that only disables the boss's own `SpriteRenderer`, `Collider2D`,
and shockwave-ring child — leaving the `Boss` GameObject (and everything
else on it, chiefly `MinionSpawner`) active and running the entire time.
Kamikaze minions now spawn and deal contact damage from the very start of
the level again, exactly as before Session 28, independent of whether the
boss itself is visible yet.

Caught two more real issues while fixing this, both via the Unity MCP
bridge in Play mode, not by inspection alone:

1. **Awake-order bug**: `LevelSequencer.Awake()` originally called
   `level1Boss.SetVisible(false)` immediately — but `Level1Boss.Awake()`
   (which caches the `SpriteRenderer` reference `SetVisible` needs) isn't
   guaranteed to run before `LevelSequencer.Awake()`; Unity only guarantees
   every object's `Awake()` finishes before any `Start()`. Confirmed live:
   the boss was visible at `Time.time ≈ 0.34` despite `SetVisible(false)`
   having been called — the cached `SpriteRenderer` field was still `null`
   at that point, so the call silently no-op'd. Fixed by moving the call to
   `LevelSequencer.Start()` instead, which Unity does guarantee runs after
   every object's `Awake()`.
2. **Bullets damaging the invisible boss**: `SetVisible` initially only
   toggled the `SpriteRenderer`, not the `Collider2D`. With the collider
   left enabled, player bullets could still hit and damage the (invisible)
   boss via `Bullet.cs`'s normal trigger detection — confirmed live,
   `Level1Boss.CurrentHealth` had already dropped to 83/90 within the first
   few seconds of a fresh Play session, well before the entrance. Fixed by
   having `SetVisible` also toggle `Collider2D.enabled`.

### Verified

Unity MCP bridge, Play mode, `Application.runInBackground = true` (see
Session 27's (see progress-log-archive.md) environment note). Confirmed at
`Time.time ≈ 0.02` (the very
first frame): `SpriteRenderer.enabled == false`,
`Collider2D.enabled == false`, `Level1Boss.CurrentHealth == maxHealth`.
Let the level run into minion phase 1: boss still hidden/undamaged
(`CurrentHealth` still 90) while 2 kamikaze minions had already spawned
flanking its (hidden) position. Forced a minion onto the human `Player`'s
exact position — contact damage applied (fatal, since the player was
already low from earlier combat) and the minion itself died on the same
hit (`Minion.Active.Count` dropped by one), confirming the full kamikaze
round-trip. Let the sequence continue into `BossCombat`: boss correctly
became visible (`SpriteRenderer.enabled == true`), collidable
(`Collider2D.enabled == true`), and started taking real combat damage
(70/90). No console errors throughout either Play session.

**Environment note**: hit one transient issue unrelated to this session's
code — `manage_editor(action="play")` occasionally leaves
`editor.play_mode.is_changing` stuck `true` for tens of seconds (editor
state stuck in `"playmode_transition"`), during which `GameObject.Find`
calls return `null` for objects that do exist. Stopping and re-entering
Play mode cleared it every time it occurred. Not reproduced consistently
enough to root-cause; noting here in case it recurs in a future session.

### Still open

Same as Sessions 27/28. Still no real human playtest.

## Session 30 — Fix: Kamikaze Minions Spawning During the Frozen Boss Entrance

Two follow-up reports after Session 29: (1) minions visibly overlapping
ships still weren't dealing contact damage or dying, and (2) confusion
about why boss-flanking minions were appearing from the very start of the
level at all — the latter suggesting Session 29's fix (tying
`MinionSpawner` to the same always-active `Boss` GameObject) wasn't
actually the right call, just a fix for the wrong symptom.

### Investigation

Verified the core kamikaze mechanics directly, bypassing all live-timing
uncertainty: invoked `PlayerController.ResolveShipCollisions()` via
reflection with a candidate position exactly matching a live minion's — it
correctly detected the overlap, pushed the ship out, and flipped the
minion's `isDead` flag. Confirmed `ShipCollisionUtil.ResolveBoxOverlap()`
returns `wasOverlapping = true` for realistic ship/minion half-extents
matching known deep-overlap positions. So the underlying detection and
`ApplyContactDamage()`/`Die()` logic were never broken.

Found the real bug instead: Session 29's fix enabled `MinionSpawner`
(via `Level1Boss.SetVisible(true)`) at the *start* of `BossEntranceRoutine()`
— the exact same moment `LevelSequencer` also freezes the ships for the
entrance glide (`PlayerController.enabled = false`). Since
`ResolveShipCollisions()` only ever runs from `PlayerController.FixedUpdate()`,
disabling the component stops `FixedUpdate` entirely — so for the whole
~4s entrance window, minions could spawn and visibly overlap ships that
were structurally incapable of reacting to them. This exactly matches "I
can see them overlap and nothing happens": the overlap was real and
visible, but the one code path that could act on it never ran.

### Fix

Moved `MinionSpawner`'s enable from `SetVisible(bool)` to
`Level1Boss.OnEnable()` — which `LevelSequencer` only triggers at
`BossCombat`, after `SetShipsFrozen(false)` has already unfrozen every
ship. `SetVisible()` now only ever touches the sprite/collider/ring, not
`MinionSpawner`. Net effect: boss-flanking minions now only start
appearing once boss combat has actually begun (not during the pre-boss
sequence, and not during the frozen entrance), and by construction every
ship is already able to react the instant the first one spawns. This
resolves report (2) as a side effect of fixing report (1) — both traced to
the same root cause (minions existing during a window ships couldn't act
in), just at different points in the timeline.

### Verified

Confirmed via the Unity MCP bridge: `MinionSpawner.enabled == false`
throughout `Intro`/`FreeMovement`/`MinionPhase1`/`WaitingForClear`/
`BossEntrance` (no minions spawn in any of them); at the instant
`CurrentState` becomes `BossCombat`, `MinionSpawner.enabled == true` and
all 4 ships already report `PlayerController.enabled == true`. Did not
manage to get a clean, un-confounded "real gameplay" confirmation that a
ship organically colliding with a minion (as opposed to a
reflection-invoked call) applies damage — every attempt in this session's
Play-mode testing was disrupted by extreme time compression (this
project's `Application.runInBackground = true` workaround for an unfocused
Editor, see Session 27 (progress-log-archive.md), has no framerate cap while
unfocused, so seconds of
wall-clock time can compress into 10-20+ seconds of game time between tool
calls) and repeated, hard-to-attribute party wipes from concurrent boss/
minion fire. Confident in the fix given the isolated verification above,
but flagged to the user as not fully closed-loop-confirmed in a live,
organic playthrough — worth a real human check.

### Open question raised back to the user

The original report also mentioned wanting contact-triggered
damage/explosion on "minions" the user could see overlapping ships during
the pre-boss minion-wave phase. `Enemy.cs` (the actual entity spawned
during that phase, via `EnemySpawner`) has no contact-damage code at all —
it only ever fires bullets, never had a kamikaze/explosion mechanic. If
that's the intended target (not the boss-flanking `Minion.cs` system this
session fixed), it would be new scope: porting/adapting `Minion.cs`'s
kamikaze + Explosive-fragment mechanics onto `Enemy.cs`. Not implemented
this session pending clarification.

### Still open

Same as prior sessions. Still no real human playtest. Whether "pattern
minions" (`Enemy.cs`) should also get kamikaze/explosion contact damage is
an open question for the user, not yet scoped.

## Session 31 — Kamikaze Contact Damage for Enemy.cs (Wave Enemies)

Resolved Session 30's open question directly: the user confirmed "minions"
in every prior report meant `Enemy.cs` (`EnemySpawner`'s wave-spawned
"pattern minions"), not `Minion.cs` — so the contact-damage complaint was
never a bug, `Enemy.cs` genuinely never had any contact-damage code, only
bullets. Asked to add it, matching `Minion.cs`'s existing kamikaze
behavior (basic version — no Explosive-type fragment burst, not requested).

### `Enemy.cs`

Added the same shape `Minion.cs` already uses: `contactDamage` (1, public
field), a `HalfExtents` property cached from the `BoxCollider2D` in
`Awake()` (mirrors `Minion.HalfExtents` exactly — needed since
runtime-spawned colliders can't be cached ship-side), `public void
ApplyContactDamage(GameObject ship)` (deals `contactDamage` once via
`PlayerHealth.TakeDamage`, then dies), and a private `Die()` funnel guarded
by `isDead` (prevents a same-frame double-kill from a bullet hit + ship
contact landing together, same reasoning as `Minion.cs`'s guard —
`Destroy()` is deferred to end-of-frame). Routed the existing
`TakeDamage(float)` and the off-screen self-destruct through the same
`Die()` for consistency.

### `PlayerController.cs`

Added a third `ResolveShipCollisions()` loop, over `Enemy.Active`, directly
alongside the existing `Minion.Active` one — identical shape (manual
`ShipCollisionUtil.ResolveBoxOverlap()` overlap check, calls
`enemy.ApplyContactDamage(gameObject)` on overlap). `Enemy.prefab`'s
collider being a trigger (Session 28's fix, to stop physics-engine pushing)
doesn't affect this at all — it's unrelated manual math, not a Unity
trigger/physics callback.

### Verified

Unity MCP bridge, Play mode. Given Session 30's difficulty getting clean
"organic gameplay" confirmation under this environment's extreme
background time-compression, verified more surgically this time:
`typeof(PlayerController).GetMethod("ResolveShipCollisions", ...)` invoked
directly via reflection with a candidate position exactly matching a live
`Enemy`'s — correctly flipped its `isDead` to `true` and reduced
`Enemy.Active.Count`. A first attempt to also confirm the player took
damage came back ambiguous (health/shield read unchanged across two
separate tool calls) — traced to Medic's passive aura healing the shield
back up in the real time elapsed *between* those two calls, not a failure
of the mechanic. Re-verified with a single, no-time-gap call instead:
called `enemy.ApplyContactDamage(player)` directly and read
`PlayerHealth.CurrentShield` immediately before/after in the same
execution — dropped exactly 1 point (5 → 4), matching `contactDamage`.
Confirmed `Enemy.HalfExtents` computes correctly (`(0.30, 0.30)`, matching
the ship-scale-matched 0.6 scale from Session 28). No console errors.

### Still open

Same as prior sessions. Still no real human playtest.
`MinionType.Explosive`'s fragment-burst mechanic was deliberately not
ported to `Enemy.cs` this session (not requested) — would be a
straightforward follow-up if wanted, reusing the same `SpawnFragments()`
idiom from `Minion.cs`.
