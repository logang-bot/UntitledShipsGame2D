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

## Session 35 — Local Co-op / Dynamic Player Count

**Correcting the record first**: every prior session above notes "still no
real human playtest." That's no longer true as of this session — the user
has since playtested the game for real. Not revising those old entries
(this is a chronological log), just flagging it here so the refrain doesn't
keep getting treated as current.

Picked up `roadmap.md`'s "In Progress" item: let up to 4 local humans play
together on one machine (extra players via gamepad, the first via
keyboard+mouse or gamepad), each picking a distinct role, any unpicked role
auto-filled by AI exactly as before. Key framing that shaped the whole
design: the party is **always exactly 4 ships**, one per `PlayerRole` — only
the human/AI split of those 4 fixed slots varies, not the total count. That
meant `PlayerRoleStats`'s 4-entry table, `PartyFrameManager`'s 4 hand-placed
HUD frames, `LevelSequencer.ships`'s always-4-element array, and the boss's
aggro table all stayed untouched.

### Prerequisite: Awake → Start refactor

`PartySetupBootstrap` has always worked by setting `.role` on
already-existing scene objects before their own `Awake()` runs, via
`[DefaultExecutionOrder(-1000)]`. That guarantee breaks for a dynamically
`Instantiate()`d ship (needed for co-op): `Instantiate()` runs the new
object's `Awake()` synchronously, before the caller can set `.role`. Moved
every role-dependent side effect — `PlayerRoleComponent`'s sprite tint,
`PlayerHealth`'s `maxHealth`/`maxShield`, `PlayerAbility`'s aura ring/shield
arc construction and initial cooldown — from `Awake()` to `Start()`, which
Unity does *not* run synchronously on a freshly-instantiated object.
Behavior-preserving for the legacy scene-placed path (role is already set
well before any `Start()` runs either way); verified the existing
single-player flow was unchanged before building anything else on top.

### Ship.prefab

Replaced the long-standing inconsistency (`Player` and 2 of the 3
`Teammate_*` GameObjects were plain hand-placed duplicates, not real prefab
instances — see `unity-notes.md`'s "Duplicating a GameObject before it's a
prefab instance") with one unified `Assets/Prefabs/Ship.prefab`, created
from `Player` via the Unity MCP bridge's `create_from_gameobject` (which
correctly converted `Player` into a real instance in place, no duplicate
object). Carries both `PlayerInput` and `AIController` — never adds/removes
either at runtime, only toggles `.enabled`. Deleted and re-instantiated the
3 `Teammate_*` objects from the new prefab, then rewired every
cross-reference that pointed at their old (now-destroyed) component
instances: `PartySetupBootstrap.teammates[]`, `LevelSequencer.ships[]`,
`PartyFrameManager.players[]`, every ship's `PlayerAbility.allies[]`, and
every AI ship's `AIController.teammates[]`.

**Real bug caught immediately by re-testing this checkpoint in Play mode**:
`PartyFrameManager`'s old humanness check (`GetComponent<AIController>() ==
null`) broke the instant `Player` also had an `AIController` (disabled) —
every party frame read "CPU". Not a hypothetical, an MCP-bridge screenshot
showed all 4 frames labeled CPU immediately. Fixed by checking
`GetComponent<PlayerInput>().enabled` instead (see "PartyFrameManager.cs
fix" below) — this turned out to be needed *before* any co-op-specific code
existed, purely from unifying the prefab.

### Input Actions asset

Added `Keyboard&Mouse` and `Gamepad` control schemes to
`PlayerControls.inputactions`; tagged every pre-existing binding into
`Keyboard&Mouse` (previously group-less/scheme-agnostic — a real hazard once
a `Gamepad`-scheme player and a `Keyboard&Mouse`-scheme player are both live
at once); added `<Gamepad>/leftStick`/`buttonSouth`/`buttonWest` bindings
for Move/Fire/Ability. Zero code changes needed in `PlayerController.cs`/
`PlayerAbility.cs` — confirmed live (join a virtual `Gamepad` device via
`InputSystem.AddDevice<Gamepad>()`, pair it through `PlayerInputManager`,
verify `PlayerInput.currentControlScheme == "Gamepad"`) that input
consumption was already fully device-agnostic.

### CoOpRoster.cs, JoinLobby.unity, RoleSelect overhaul

`CoOpRoster.cs` (new static carrier, mirrors `PartyRoleAssignment.cs`/
`GameModeSelection.cs`'s pattern exactly) holds a `List<JoinedPlayer>`
(`controlScheme`, paired `devices[]`, `role`), `null` meaning "co-op flow
wasn't used." New `JoinLobby.unity` scene (Build index 2, `RoleSelect`→3,
`Gameplay`→4) hosts a `PlayerInputManager`
(`JoinPlayersWhenButtonIsPressed`, a throwaway `JoinSlotMarker.prefab`) and
`JoinLobbyUI.cs`, which reflects `PlayerInput.all` into 4 live slot rows and
snapshots them into `CoOpRoster.Players` on Continue. `LobbyUI.SelectLocal()`
now routes here instead of straight to `RoleSelect`.

`RoleSelectUI.cs` now routes to one of two child panels:
`SinglePickerPanel` (the original 4-button picker, unchanged, used for 0-1
joined players) or a new `MultiPickerPanel` (`RoleSelectMultiUI.cs` +
`RolePickerRow.prefab`, one row per joined player). Each row polls its own
paired device *directly* (`Gamepad`/`Keyboard` objects, dpad/stick or WASD
to move a highlight, South/Enter to confirm) rather than standing up a
second `EventSystem`/`InputSystemUIInputModule` per player — real, correct
Unity functionality, but judged more infrastructure than a 4-role button
grid needs for a first co-op pass. A shared taken-role check across rows
blocks duplicate picks; Start enables once every row has locked a role.

### PartySetupBootstrap.cs dynamic spawn branch

Added a `SpawnDynamicParty()` branch, checked first in `Awake()`, gated on
`CoOpRoster.Players`. The legacy branch (single-human `PartyRoleAssignment`
or Inspector-only fallback) is untouched. The 4 legacy scene ships are
reused purely as position markers (read position, `SetActive(false)`) so
both branches share one authored set of spawn points.

**Two real bugs caught live, both traced by direct Play-mode testing, not
inspection**:

1. `PlayerInput.Instantiate(shipPrefab, controlScheme, pairWithDevices)`
   pairs devices but does **not** flip the prefab's serialized
   `PlayerInput.enabled: false` (the AI-slot default) back to `true` — the
   human-joined ship came out with a correctly-paired `PlayerInput` that was
   simply disabled. Caught by reading `PI.enabled` on the spawned ship
   directly after `StartGame()`; fixed with an explicit `pi.enabled = true`
   right after `Instantiate`.
2. Complementary bug: the AI-slot default (`PlayerInput` enabled, before the
   fix above) meant a plain `Instantiate(shipPrefab)` for an *AI* slot would
   have its `PlayerInput` try to auto-pair itself to an already-claimed
   device the instant it was created, logging "Cannot find matching control
   scheme" — caught via `read_console` during the exact same test. Fixed by
   flipping `Ship.prefab`'s own defaults (`PlayerInput.enabled: false`,
   `AIController.enabled: true` — "most slots are AI"), with the human path
   explicitly overriding both after `PlayerInput.Instantiate`.

A third bug surfaced only after forcing a genuine scene reload from disk
(the standing verification habit from `unity-notes.md`'s "Prefab-instance
overrides" section): `Player`'s `PlayerInput.enabled = true` /
`AIController.enabled = false` had been set *while `Ship.prefab`'s own
defaults still matched those exact values* (the prefab was created from
`Player`), so neither was recorded as a real per-instance override — no
diff, nothing to record. When `Ship.prefab`'s defaults were changed to the
AI-slot shape above, `Player` silently inherited the new defaults too,
flipping it to AI-controlled with no error, no warning, and a
correct-looking in-memory read right up until the disk reload exposed it.
Fixed by re-applying and re-recording `Player`'s values *after* the prefab
default change; documented as a sharper variant of the existing gotcha in
`unity-notes.md`.

### LevelSequencer.cs / PartyFrameManager.cs fixes

`LevelSequencer.SetShipsFrozen()`'s unfreeze branch used to unconditionally
re-enable both `PlayerInput` and `AIController` whenever non-null — safe
only because exactly one of the two ever existed per ship before
`Ship.prefab`. Fixed by caching `shipIsHuman[i]` once in `Awake()` (after
`PartySetupBootstrap`'s `-1000` `Awake()` has already configured each ship)
and restoring each ship to *its own* real driver on unfreeze, not both.
Verified via reflection-invoked `SetShipsFrozen(true)` then `(false)` on
both a legacy 1-human party and a live 2-human co-op party, confirming
`PlayerInput`/`AIController.enabled` came back exactly right for every ship
both times. `PartyFrameManager`'s humanness check (see "Ship.prefab" above)
also picked up a display-name fix alongside its `enabled`-based check: the
hardcoded `"Player 1"` became a running counter so multiple humans get
distinct names.

### PauseUI.cs

Added a `<Gamepad>/start` binding alongside the existing
`<Keyboard>/escape` one, unrestricted to any device — a shared pause,
matching local co-op convention, so a gamepad-only human isn't stuck unable
to pause.

### Verified

Unity MCP bridge, Play mode, three full end-to-end passes:

1. **Legacy** (`Gameplay` opened directly, no `CoOpRoster`/
   `PartyRoleAssignment` set) — confirmed identical to pre-session behavior,
   including after a forced freeze/unfreeze cycle.
2. **Single human through the lobby** — real `JoinLobby` join (keyboard +
   mouse), `Continue`, single-picker `RoleSelect`, `StartGame` — confirmed
   the spawned party has exactly one `PlayerInput`-enabled ship at the
   picked role, 3 AI ships at the rest, correct after freeze/unfreeze, and
   `PartyFrameManager` labels it "Player 1."
3. **2-human co-op** — one real keyboard+mouse join plus one **virtual**
   `Gamepad` device added via `InputSystem.AddDevice<Gamepad>()` and joined
   through `PlayerInputManager.JoinPlayer(...)` (this environment has no
   physical second controller to press). Confirmed the multi-picker built 2
   rows, enforced distinct role locks (a duplicate-role lock attempt
   correctly failed), and that `Gameplay` spawned exactly 2
   `PlayerInput`-enabled ships (one per scheme/device set) plus 2 AI ships,
   correct after freeze/unfreeze, `PartyFrameManager` labeling them "Player
   1"/"Player 2"/"CPU 1"/"CPU 2".

No console errors or warnings across any of the three passes (after the bugs
above were fixed).

### Still open

- **Real multiple-gamepad playtest** — this session's 2-human co-op
  verification used one real device plus one virtual `Gamepad` added
  programmatically; the MCP bridge has no way to press buttons on actual
  physical controllers, so a real human check with 2+ genuine gamepads
  (device-unplug-mid-session handling, real button-press join timing, the
  keyboard+mouse-as-one-scheme join case with a real mouse click, actual
  gamepad D-pad/stick navigation feel in the row picker) hasn't happened
  yet.
- 3+/4-human co-op wasn't separately exercised beyond the architecture (the
  spawner loop and role-taken logic are count-agnostic, verified structurally,
  but not run live at 3 or 4 joined players).
- The detailed boss-encounter narrative in `current-state.md` still describes
  "the human `Player`" and "3 CPU-controlled AI teammates" throughout — left
  as-is this session (still substantively accurate per-role regardless of
  the human/AI split) rather than rewritten line-by-line; worth a pass if it
  reads as confusing against the new co-op reality.

## Session 36 — Halcyon (Level 2's Boss): Design + Implementation

Picked up `roadmap.md`'s next open item: Halcyon's design doc
(`halcyon-boss.md`) was a pitch with explicit open questions, not an
implementation-ready spec. Ran a full brainstorming session with the user
first (recorded as
`docs/superpowers/specs/2026-09-04-halcyon-boss-design.md`) to resolve
every open question before writing any code — identity (a pure positioning
fight, no ambient bullets at all), which of Marauder's mechanics carry over
(only body contact damage), the roam pattern (full-arena waypoint-to-
waypoint), Surge window timing (8s cooldown / 1s telegraph / 2s vulnerable
window, unaffected by phase), Static Field's numbers (6s/4s pulse cooldown,
1.8-unit boss range, 0.6-unit cluster range, 3x bullet damage), and
explicitly dropping aggro/taunt entirely.

### Code architecture: sibling MonoBehaviours, not owned helper classes

Unlike `MarauderBoss` (one component owning several non-`MonoBehaviour`
helper classes), Halcyon's three mechanics are separate sibling
`MonoBehaviour`s on the same `Boss` GameObject (`HalcyonRoam.cs`,
`HalcyonSurge.cs`, `HalcyonStaticField.cs`), each independently sized and
independently toggleable - closer to how `MinionSpawner` already sits
alongside `MarauderBoss`. `HalcyonBoss.cs` itself stays small (HP/phases/
contact damage only) and, in `OnEnable()` (fired when `LevelSequencer`
enables it at `BossCombat`), enables the three siblings - mirroring how
`MarauderBoss.OnEnable()` already enables `MinionSpawner`.

### IBoss: a scoped exception to "no interfaces"

`LevelSequencer.cs`/`PlayerController.cs`/`AIController.cs`/
`PartySetupBootstrap.cs` are all reused verbatim across level scenes and
called boss-specific methods on a `MarauderBoss`-typed field. With
`HalcyonBoss` an unrelated class (no inheritance in this codebase), a new
`IBoss` interface (`SetVisible(bool)`, `ApplyContactDamage(GameObject)`) -
the only two methods those orchestrators actually call - lets both boss
types be driven identically. Since Unity can't serialize an interface-typed
field, every consuming field stays `MonoBehaviour`-typed (`bossObject`) and
is cast to a cached `IBoss` once in `Awake()`/`Start()`; plain
`.transform`/`.enabled` access needs no cast at all. Full writeup:
`architecture.md`'s "Boss-type-agnostic orchestration: IBoss".

Retyping `PlayerController.boss`/`AIController.boss` (renamed `bossObject`)
turned out to touch far more than `LevelSequencer` alone: `AIController`'s
positioning helpers (`AIControllerAttacker`/`Medic`/`Positioning`) all read
`owner.boss.transform.position` for boss-avoidance/patrol math (pure
`Transform` access, no cast needed), `PartyFrameUI`'s DPS line read
`playerController.boss.GetDamageDealt(...)` (Marauder-only - fixed with an
`as MarauderBoss` cast, so a Halcyon-side ship's DPS line just reads 0
instead of needing special-casing), and `AIController.UpdateAbilityUsage()`'s
Tank heuristic read `boss.CurrentTarget` (also Marauder-only aggro API -
fixed the same way, so a Halcyon-side Tank simply never auto-taunts,
matching the designed no-op). All caught at compile time, not discovered
live - Unity's compiler immediately flagged every broken reference once the
field types changed.

### Renaming a public field drops its serialized value

Renaming `PlayerController.boss`/`AIController.boss`/
`LevelSequencer.marauderBoss`/`PartySetupBootstrap.boss` to `bossObject`
orphaned every existing Inspector reference in `Level1.unity` (Unity keys
serialized values by field name, not type) - confirmed live via the Unity
MCP bridge (`bossObject: null` on every ship after recompiling). Re-wired
all of them (4 ships' `PlayerController`/`AIController`, `LevelSequencer`,
`PartySetupBootstrap`) back to the same `MarauderBoss` instance and
re-verified end-to-end before touching `Level2.unity` at all, per the
plan's explicit "confirm Marauder's behavior is unchanged" step.

### HalcyonBoss.prefab / Level2.unity wiring

Built in place on `Level2.unity`'s existing `Boss` GameObject (removed
`MarauderBoss`/`MinionSpawner`, added `HalcyonBoss`/`HalcyonRoam`/
`HalcyonSurge`/`HalcyonStaticField`) rather than a separate prefab asset -
same one-off-per-level treatment `MarauderBoss.prefab` gets. Caught one
real bug live: a freshly-added `HalcyonBoss` component defaults to
`enabled: true`, so without explicitly unchecking it the boss (and its
three siblings, cascaded via `OnEnable()`) was active from scene start
instead of only at `BossCombat` - confirmed via `execute_code` showing all
three mechanics `enabled: true` during `FreeMovement`, fixed by setting
`HalcyonBoss.enabled = false` to match `MarauderBoss`'s own established
convention, re-verified disabled through `Intro`/`FreeMovement`/
`MinionPhase1` and correctly enabled at `BossCombat`. `BossPanel`'s script
swapped from `BossPanelUI` to a new `HalcyonBossPanelUI`, reusing the
existing HP/phase text objects by content and repurposing
`BossWarningText`/`BossShockwaveCooldownText`/`BossGuidedMissileCooldownText`
for Surge/Static Field's own text rather than adding new UI objects;
`BossTargetText`/`BossPatternBarrageWarningText`/
`BossPatternBarrageCooldownText` (nothing left to drive them) were
deactivated rather than left showing stale text.

### Verified

Unity MCP bridge, both edit-mode (compile checks after every script change)
and Play-mode. `HalcyonRoam`/`HalcyonSurge`/`HalcyonStaticField` confirmed
disabled and the boss hidden/uncollidable throughout `Intro`/
`FreeMovement`/`MinionPhase1`, correctly enabled and visible at
`BossCombat` (accelerated via `Time.timeScale` rather than waiting through
the full ~30s pre-boss sequence at 1x). Live in `BossCombat`: the boss's
position had actually moved off its home point (confirming `HalcyonRoam`
is really roaming), `HalcyonSurge`/`HalcyonStaticField`'s cooldowns were
counting down from fresh values (confirming `OnEnable()`'s reset), and the
party's own AI auto-fire had already landed real damage on the boss (90 ->
86 HP) with zero extra code - confirming `Bullet.cs`'s new `HalcyonBoss`
check works end-to-end, not just in isolation. Directly verified
`HalcyonStaticField`'s pairwise proximity logic via
`ApplyPulseDamage()` (reflection-invoked, matching this project's
established fallback style whenever live timing is unreliable): two ships
both moved onto the boss's position each took exactly 3 damage (3x
`bulletDamage`); a lone ship on the boss's position took nothing. Confirmed
`HalcyonBoss.TakeDamage` reduces `CurrentHealth` by exactly the rounded
amount. No console errors or warnings across any of the above, including a
full compile pass after every file touched (10 existing scripts edited
alongside the 6 new ones).

**Environment note**: hit the same `playmode_transition`-stuck issue
documented in Session 29 (`editor.play_mode.is_changing` stuck `true`,
`Time.time` frozen at `0.00` across repeated real-time waits) more
persistently than that session described - stopping/re-entering Play mode
plus one `refresh_unity` call eventually cleared it. Once clear, real
elapsed time behaved normally (including one run that, left alone at
`Time.timeScale = 50` for a few real seconds, compressed enough game time
for the whole party to wipe against the boss - a legitimate outcome, not a
bug, just a reminder this environment's Play-mode timing is still not
fully reliable).

### Follow-up: MarauderBoss.cs over this session's new file-size cap

A style hook flagged `MarauderBoss.cs` at 289 lines against a newly-enforced
~200/250-line cap the instant this session touched it (adding `, IBoss` to
the class declaration) - pre-existing length, but the hook holds any file a
session edits to the cap regardless of who wrote the excess. Split out a
fourth helper, `MarauderBossAggro` (the threat table, `PickTarget`/
`TauntedBy`/damage-tracking - see "Aggro / targeting" above), matching the
existing `MarauderBossMovement`/`Shockwave`/`Attacks` shape exactly. That
alone wasn't enough margin, so Shockwave's and guided missile's tunable
fields were also grouped into two small `[System.Serializable]` classes
(`MarauderBossShockwaveSettings`, `MarauderBossGuidedMissileSettings`) -
still fully Inspector-editable as a foldout, just declared in their own
files instead of as ~18/~7 top-level fields on `MarauderBoss` itself.
Pattern Barrage's fields were left flat; the first two extractions alone
brought the file to 241 lines (from 289), comfortably under the cap.

Confirmed zero tuning drift before touching anything: read `Level1.unity`'s
live `Boss` component values via the Unity MCP bridge first, and every
value being moved (`shockwaveRadius` 1.7, `damageMultiplier` 3, ...,
`guidedMissileInterval` 5, ...) already matched the field defaults already
declared in code - so re-declaring them as the new settings classes'
defaults reproduces the scene exactly with no Inspector re-wiring needed,
unlike the `bossObject` field renames above (which did orphan their
serialized references). Verified this held after the refactor: read the
live component back and confirmed every nested value matched the
pre-refactor numbers exactly, then re-exercised the shockwave path
live (`CheckShockwave()`/`ApplyShockwaveEffect()`, both reflection-invoked)
- shield dropped by exactly 3 (`bulletDamage x shockwaveSettings.damageMultiplier`),
matching pre-refactor behavior. No console errors across any step.

### Follow-up: dangling OnTaunt listener in Level2, and an unrelated pre-existing gap found alongside it

A docs-accuracy pass after this session's main work surfaced a real side
effect of swapping `Level2.unity`'s `Boss` GameObject from `MarauderBoss`
to `HalcyonBoss` in place: each of the 4 ships' `PlayerAbility.OnTaunt`
still carried a persistent listener to the now-deleted `MarauderBoss`
component's `TauntedBy` method. Unity doesn't error on this (a
null-target persistent listener is silently skipped), so it was invisible
functionally — Taunt already does nothing against Halcyon by design either
way — but it's a dangling reference, not the clean absence the design
called for. Removed via the Unity MCP bridge
(`UnityEditor.Events.UnityEventTools.RemovePersistentListener`) on all 4
ships, confirmed via `UnityEventBase.GetPersistentEventCount()`/
`GetPersistentTarget()` before and after.

Found something unrelated while inspecting these listeners: in **both**
`Level1.unity` and `Level2.unity` (so this predates this session entirely,
not something introduced by it), `Teammate_Tank`/`Teammate_Medic`/
`Teammate_Support`'s `OnTaunt` are missing the `CameraShake.Shake()`
listener that `Player`'s has — only a human-controlled Taunt shakes the
camera; an AI teammate's Taunt doesn't. Left as-is (out of scope for this
pass), flagged here for a future session.

### Still open

- **Real human playtest** - same as every prior session's boss/mechanic
  work, this fight has only been exercised via the Unity MCP bridge.
- Halcyon's own numeric tuning (roam speed, Surge timing, Static Field
  range/cooldown/damage, HP) are first-pass placeholders like every other
  balance value in this project, not validated against real play.
- **Pre-existing gap, not caused by this session**: `Teammate_*`'s
  `OnTaunt` is missing a `CameraShake.Shake()` listener in every level
  scene — see the follow-up note above.
