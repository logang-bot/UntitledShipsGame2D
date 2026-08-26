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

## Session 32 — Explosive Wave Enemies (port of Minion.cs's Explosive type)

Followed up on Session 31's flagged-but-unrequested item: ported
`Minion.cs`'s `MinionType.Explosive` fragment-burst mechanic onto `Enemy.cs`
(the `EnemySpawner`-spawned wave enemies), on request.

### `Enemy.cs`

Added a nested `EnemyType` enum (`Standard`, `Explosive`) and an
`[Header("Explosive Death")]` field block, copied field-for-field from
`Minion.cs`: `type`, `fragmentPrefab` (falls back to `bulletPrefab` if
unassigned), `fragmentCount` (8), `fragmentSpeed` (5), `fragmentDamage` (1),
`explosiveTintColor`. Added a public `Init(MovementPattern pattern,
EnemyType enemyType)` — `Enemy` previously had no `Init()` at all;
`EnemySpawner` just set `movementPattern` as a bare field post-`Instantiate`.
The new method sets both fields and, for `Explosive`, tints the
`SpriteRenderer` — safe to call any time after `Instantiate()` returns since
Unity runs `Awake()` synchronously during `Instantiate`, same reasoning
`Minion.Init()` already relies on. Extended `Die()` to call a new private
`SpawnFragments()` (verbatim copy of `Minion.cs`'s version, using `Enemy`'s
own fragment fields) before `Destroy()` when `type == Explosive`. Both
existing kill paths (`TakeDamage` for gunfire, `ApplyContactDamage` for
kamikaze contact) already funneled through this same `Die()`, so both
trigger the burst with no further changes.

### `EnemySpawner.cs`

Added `explosiveEnemyChance` (`[Range(0f,1f)]`, default `0.3`) — same
name pattern and default as `MinionSpawner.explosiveMinionChance`. In
`SpawnWaveRoutine()`, replaced the bare `movementPattern` field assignment
with a call through the new `Init(movementPattern, enemyType)`, rolling
`EnemyType` independently per enemy (`Random.value < explosiveEnemyChance`).

### Verified

Unity MCP bridge, Play mode, all via direct `execute_code` calls
(no scene-flow dependency — instantiated `Enemy.prefab` directly rather than
running a full level, same surgical style as Sessions 30-31):

- `Init(SineWave, Explosive)` tints the `SpriteRenderer` to exactly
  `explosiveTintColor`; a fresh instance's `SpriteRenderer.color` is white
  before `Init` runs.
- Killing an `Explosive` enemy via `TakeDamage(999f)` (gunfire path) spawned
  exactly `fragmentCount` (8) new `Bullet` instances at its position.
- Killing an `Explosive` enemy via `ApplyContactDamage(ship)` (kamikaze
  path) spawned the same 8-fragment burst; inspected one fragment directly —
  `damage == fragmentDamage` (1), `owner == "Enemy"`.
- Killing a `Standard` enemy via `TakeDamage` spawned zero fragments.
- Invoked `EnemySpawner.SpawnWaveRoutine()` via reflection 40 times (203
  enemies total) and tallied `Enemy.type` across the results: 63 Explosive /
  140 Standard ≈ 31%, matching `explosiveEnemyChance` (0.3) within normal
  sampling noise.
- No console errors or warnings across any of the above.

### Follow-up: tint the fragments themselves

User noticed the fragment bullets were using the plain bullet sprite's
default color — indistinguishable from a regular shot on screen. Added one
line to both `Enemy.SpawnFragments()` and `Minion.SpawnFragments()` (the
latter had the same gap, never addressed when the mechanic first shipped):
right after `b.Init(...)`, `fragObj.GetComponent<SpriteRenderer>().color =
explosiveTintColor` — each fragment now matches its own source's orange
tint. Verified live: an Explosive `Enemy`'s and an Explosive `Minion`'s
fragments both came back with `SpriteRenderer.color == explosiveTintColor`
exactly, 8/8 for each; no console errors.

### Still open

Same as prior sessions. Still no real human playtest.

## Session 33 — Scene Scaffolding: Main Menu, Lobby, Pause Menu

Picked up `roadmap.md`'s long-deferred "Scene scaffolding" item (Main
Menu/Lobby, originally deferred until the boss prototype was further along —
it now is, per Sessions 28-32). Scoped in conversation beyond the docs'
original framing: Lobby should actually fork **Local vs. Online** mode (not
just be an empty pass-through — Online has no backend yet, so it's a
disabled placeholder), and a Pause menu (flagged as a real gap during
discussion, not in any prior doc) should ship alongside it, valid only
offline.

### New scenes and scripts

`MainMenu.unity` (new Build Settings index 0, `MainMenuUI.cs`: Play → Lobby,
Quit) and `Lobby.unity` (index 1, `LobbyUI.cs`: Local → sets
`GameModeSelection.Mode` and proceeds to `RoleSelect`; Online →
non-interactable placeholder). Both built via the Unity MCP bridge's
`execute_code`, constructing the Canvas/CanvasScaler/EventSystem/Button
hierarchy directly in C# (cheaper than dozens of granular tool calls) after
first reading `RoleSelect.unity`'s existing Canvas settings live
(`ScreenSpaceOverlay`, `ScaleWithScreenSize` at 1920x1080, the
`DefaultInputActions` asset on its `InputSystemUIInputModule`) so the new
scenes match its look exactly rather than guessing values. `RoleSelect.unity`
(now index 2) gained a `Back` button (`RoleSelectUI.Back()` → `Lobby`), since
it's no longer the entry point. Build Settings reordered to
`MainMenu`/`Lobby`/`RoleSelect`/`Gameplay` (0-3) via `manage_build(action=
"scenes")` — every existing `SceneManager.LoadScene(...)` call uses scene
names, not indices (`GameOverUI.Restart()`'s
`SceneManager.GetActiveScene().buildIndex` is self-referential/index-
agnostic), so reordering broke nothing already wired.

`GameModeSelection.cs` (new): a `GameMode?` (`Local`/`Online`) static
carrier, built on the exact same pattern as `PartyRoleAssignment.cs` —
survives `SceneManager.LoadScene` within a session, resets to `null` on
domain reload, unset treated as "allowed"/local everywhere it's read
(preserves the existing "open any scene directly" quick-iteration workflow).

### Pause overlay + a real Awake/OnEnable bug

`PauseUI.cs` (new) adds a same-scene `PausePanel` overlay to `Gameplay`,
mirroring `GameOverUI.cs`/`VictoryUI.cs`'s shape (Resume/Restart/Change
Roles/Quit to Main Menu buttons), toggled by Escape via a **standalone**
`InputAction` built in code (`new InputAction("Pause", InputActionType.
Button, "<Keyboard>/escape")`) rather than extending
`PlayerControls.inputactions` — the project has no UI-facing action map, and
the only `PlayerInput` lives on the human `Player` ship, whose Send-Messages
behavior can't reach a scene-global listener. `Show()`/scene-transition
methods reset `Time.timeScale` (global, not scene-scoped) so a mid-pause
Restart doesn't reload into a frozen scene. Gated off whenever Game Over/
Victory is already showing (mirrors their existing mutual-exclusion guard on
each other) or once `GameModeSelection.Mode == GameMode.Online`.

First implementation attached `PauseUI` directly to `PausePanel` itself
(matching `GameOverUI`/`VictoryUI`'s exact shape). This silently broke Pause
entirely: `Awake()` calls `panelRoot.SetActive(false)` to hide the panel at
startup, but since `panelRoot` *was* the same GameObject the script lived
on, that deactivation happened synchronously inside its own `Awake()` —
before Unity ever reached `OnEnable()` for it. Unity only calls `OnEnable()`
if the object is still active once `Awake()` finishes, so the `InputAction`
built earlier in that same `Awake()` never got `.Enable()` called on it, and
Escape silently did nothing. Caught live via the Unity MCP bridge, not by
inspection: `FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include)`
followed by reflecting out the private `pauseAction` field confirmed
`enabled == false` in Play mode, isolating the fault to the enable path
specifically. Fixed by splitting the two roles: `PauseUI` now lives on a
separate `PauseController` GameObject that stays active for the whole
scene, while `panelRoot` is just a plain field reference to `PausePanel`,
toggled via `SetActive()` like any other field — never the GameObject the
script itself is attached to. Re-verified after the fix: `OnEnable` fires,
`pauseAction.enabled == true`, and a reflection-invoked press of the actual
callback correctly opened/closed the panel and flipped `Time.timeScale`.

### Verified

Unity MCP bridge, Play mode, full click-through of the real flow
(`MainMenuUI.Play()` → `LobbyUI.SelectLocal()` → `RoleSelectUI.SelectTank()`
+ `StartGame()` → `Gameplay`), confirming `GameModeSelection.Mode`/
`PartyRoleAssignment.HumanRole` set correctly and the right scene active at
each step. In `Gameplay`: confirmed `Online`'s `interactable == false` back
in `Lobby`; confirmed the Pause gating with Game Over forced active (Escape
correctly did nothing); confirmed `Restart()` resets `Time.timeScale` to `1`
*before* the reload (checked in the same call, no time-gap). No console
errors or warnings across the entire flow, both before and after the
Awake/OnEnable fix. Full mechanics writeup: `systems/scene-flow.md`; the
standalone Pause `InputAction` also documented in `systems/input.md`.

### Still open

Same as prior sessions. Still no real human playtest. Local co-op (multiple
*human* players) remains unbuilt — `GameModeSelection.Mode.Local` currently
just routes into the existing single-human + 3-AI-teammate flow unchanged; a
future pass would read this same flag to actually spawn/wire up multiple
local human players.

## Session 34 — Fix: Sub-Pixel Seam at the Pillarbox/HUD-Sidebar Edges

User report, with screenshots: two thin, colored vertical strips visible at
the left and right edges of the playable game area in `Gameplay`, right
where the gray HUD sidebars (`LeftSidebar`, `BossPanel`) meet the game
viewport — present both entering via `RoleSelect` and opening `Gameplay`
directly, ruling out anything scene-scaffolding-related from Session 33.

### Investigation (two wrong theories first)

First guess: leftover `RoleSelect` scene objects not being unloaded. Ruled
out by grepping `Gameplay.unity`'s saved file for `RoleSelect`'s button
names — no matches; `SceneManager.LoadScene` is `Single` mode everywhere, so
nothing was ever left behind on disk. Second guess: the AI teammates'
intro-glide animation (`LevelSequencer.IntroRoutine()`) being momentarily
visible — discarded once the user's screenshots showed the strips at the
**sides**, not the bottom, and `IntroRoutine()` only moves ships vertically.

Third theory, confirmed correct: `AspectRatioFitter.cs` (sets `Main
Camera.rect` for the pillarbox) and `HUDSidebarFitter.cs` (sizes the HUD
sidebars to close the gap against it) independently compute the same pixel
boundary from `Screen.width`/`Screen.height`, but neither ever rounds it.
Computed the boundary for a spread of common resolutions directly: `2560x
1440` lands exactly on pixel `875` (a coincidence of that specific 16:9
resolution against the 9:16 target), but `1920x1080` lands on `656.25px`,
`1600x900` on `546.875px`, `1280x720` on `437.5px`, and so on — fractional
at most sizes. `Camera.rect` drives the GPU viewport, which Unity rounds to
an integer pixel internally to actually render; `HUDSidebarFitter` reads the
same rect back as an *unrounded* float pixel value
(`GetViewportPixelRect()`) to size the sidebar. Two independent roundings of
the same fractional boundary can disagree by under a pixel, leaving a
sub-pixel seam where neither the sidebar nor the camera's rendered content
fully covers that column — visible as a thin, anti-aliased sliver of
whatever's directly behind it, which is why its color varied between the
user's two screenshots (not a fixed leftover object, just inconsistent
partial coverage of whatever happened to be there). This also explains why
the first live check (at `2560x1440`, picked without knowing this yet) found
a perfect `0px` diff between the sidebar edge and the camera viewport edge —
that resolution happens to be one of the few that lands on a whole pixel by
coincidence, so it couldn't have reproduced the bug.

### Fix

`AspectRatioFitter.ApplyPillarbox()`: after computing the pillarbox/letterbox
`Rect` as before, snap `x`/`y`/`width`/`height` each to the nearest whole
pixel (`Mathf.Round(rect.x * Screen.width) / Screen.width`, etc.) before
assigning `cam.rect`. Guarantees `GetViewportPixelRect()` always returns
whole-pixel values, so `HUDSidebarFitter`'s sidebar sizing and the camera's
actual rendered viewport always agree on the exact same boundary, with
nothing left to round independently downstream.

### Verified

Unity MCP bridge. Recomputed the boundary math for the same resolution
spread with the fix applied — every previously-fractional case (`1920x1080`,
`1600x900`, `1280x720`, `1912x1043`, `1918x1038`, `2000x1125`, `1707x960`)
now lands exactly on a whole pixel on both edges. Live in Play mode at
`2560x1440` (the one resolution directly reproducible in this environment):
re-confirmed `LeftSidebar`/`BossPanel`'s `RectTransform` world-corner edges
still match `AspectRatioFitter.GetViewportPixelRect()` exactly (`0px` diff,
no regression from the already-working case), screenshotted for a visual
sanity check, and confirmed no console errors/warnings. Could not directly
reproduce a fractional-boundary resolution live in this environment (no way
to force the Editor Game view to an exact arbitrary pixel size through the
available tools), so the fix's correctness rests on the resolution-spread
math check above rather than a live before/after screenshot at a failing
size — flagged here in case a real human playtest at a non-16:9-multiple
window size is worth double-checking against.

### Docs

`hud-layout.md`'s `HUDCanvas` table claimed `Canvas Scaler: Scale With
Screen Size` — the actual saved scene value is `Constant Pixel Size` (scale
factor 1), found while investigating. Unrelated to this bug (Constant Pixel
Size means 1 canvas unit = 1 screen pixel, so it isn't what let the two
systems disagree) but a real doc/reality drift, corrected in the same pass.

### Still open

Same as prior sessions. Still no real human playtest — this fix in
particular would benefit from one at a genuinely arbitrary (non-16:9)
window size, since that's the exact condition this environment couldn't
reproduce directly.
