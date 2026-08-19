# Progress Log

## Session 1 — Base Gameplay Loop

### Project setup

- Created Unity 6.4 LTS project using the **Universal 2D (URP)** template.
- Confirmed the default 2D orthographic camera.
- Added two tags: `Player`, `Enemy`.

### Player ship

- Created `Player` GameObject: Sprite Renderer (placeholder square), Rigidbody2D
  (Gravity Scale 0, Freeze Rotation Z), Collider2D (not a trigger), tagged `Player`.
- Added child `FirePoint` object positioned at the ship's nose, used as the bullet
  spawn origin.
- Wrote `PlayerController.cs` — movement + shooting, clamped to stay within camera
  viewport bounds.

### Input System migration

- Initially used the legacy `Input.GetAxis`/`Input.GetButton` API — hit a runtime
  error: `Active Input Handling` was set to "Input System Package (New)" only, which
  breaks all legacy `Input.*` calls.
- **Decision**: switch fully to the New Input System rather than falling back to
  legacy or running "Both" — driven by the project's local co-op requirement
  (`PlayerInput`'s device-pairing is the correct foundation for multiple local
  players on separate gamepads later).
- Rewrote `PlayerController.cs` to use `OnMove(InputValue)` / `OnFire(InputValue)`,
  called automatically by a `Player Input` component set to **Send Messages**
  behavior (matches method name to action name: action `"Move"` → `OnMove`).
- Created a custom **PlayerControls** Input Actions asset (rather than relying on
  Unity's auto-generated default asset, which uses different action names like
  `Attack` instead of `Fire` and caused a silent mismatch bug):
  - Action map: `Player`
  - `Move` (Value / Vector2, 2D Vector composite bound to WASD only — arrow keys
    were not added)
  - `Fire` (Button, bound to Space and left mouse button)
- Added `Player Input` component to the Player GameObject: Actions = PlayerControls,
  Default Map = Player, Behavior = Send Messages.

### Bullets

- Wrote `Bullet.cs` — shared script for both player and enemy bullets, direction/
  speed/owner set via `Init()`. Trigger-based collision (`Is Trigger` ON), owner tag
  determines what it can damage.
- Created `PlayerBullet` prefab: Sprite Renderer, Collider2D (trigger), `Bullet.cs`.

### Enemies

- Wrote `Enemy.cs` — sine-wave downward movement, periodic downward fire, takes
  damage from player bullets, self-destructs off-screen or at 0 HP.
- Created `Enemy` GameObject (Sprite Renderer, Rigidbody2D, Collider2D non-trigger,
  tagged `Enemy`) + `EnemyBullet` prefab (reuses `Bullet.cs`). Turned into a prefab.

### Wave spawning

- Wrote `EnemySpawner.cs` — spawns waves of enemies at a randomized X position along
  a configurable width, staggered within a wave, repeating on an interval.
- Created `Spawner` GameObject positioned above the camera's visible area.

### Result

Base gameplay loop confirmed working: player movement, shooting, enemy waves with
sine-wave movement and return fire, collision/damage in both directions.

### Troubleshooting notes (for future reference)

- **"Nothing moves, no errors visible"** → check Console first; New Input System
  conflicts throw clear `InvalidOperationException`s.
- **Movement works but Fire doesn't** → check whether `Player Input`'s Actions field
  is pointing at a custom asset or Unity's auto-assigned default — default action
  names won't match `OnFire`/`OnMove` unless you created them yourself.
- **Rigidbody2D not moving despite no errors** → check Simulated is enabled and no
  Freeze Position constraints are accidentally checked.
- Camera must be tagged `MainCamera` or `Camera.main` returns null.

## Session 2 — Portrait/Crossplay Screen Layout + HUD Foundation

### Screen layout decision

- Clarified requirement: fixed portrait aspect ratio on **all** platforms, not
  just mobile. On PC, rather than stretching to widescreen, the portrait game
  area stays centered and the freed-up side space is used for HUD content. No
  separate project/build needed for this — a single dynamic camera viewport
  approach handles both cases automatically based on actual screen aspect at
  runtime.
- Wrote `AspectRatioFitter.cs` (attached to Main Camera) — computes a pillarboxed
  `camera.rect` at runtime by comparing target aspect (9:16) to actual screen
  aspect. Wide screens (PC) get centered bars; narrow screens (phones) get
  little to no bar automatically. Exposes `GetViewportPixelRect()` for other
  scripts to query the exact gameplay viewport bounds.
- Later added `[ExecuteAlways]` to this script so the pillarbox previews live in
  the **Game view** tab without pressing Play. Note: this can never preview in
  **Scene view** — `camera.rect` only affects the camera it's set on, and Scene
  view always uses its own separate, unrelated editor camera. This is a hard
  technical boundary, not a bug.

### HUD canvas structure

- Split UI into two Canvases, a standard pattern for this kind of layout:
  - `GameplayCanvas` — Render Mode: Screen Space - Camera, tied to Main Camera.
    Confined to the pillarboxed viewport. For in-game UI (health bars over
    ships, boss HP bar, etc.).
  - `HUDCanvas` — Render Mode: Screen Space - Overlay. Spans the full window
    regardless of pillarbox. For side-bar content (party frames, boss panel).
- Built placeholder `LeftSidebar` (party frames) and `BossPanel` (boss HP, cast
  bar, wave counter) inside `HUDCanvas`.
- Wrote `HUDSidebarFitter.cs` — dynamically resizes the sidebar panels to
  exactly match `AspectRatioFitter`'s computed bar width every frame (on
  resize), closing the gap between the gameplay viewport and HUD. Also given
  `[ExecuteAlways]` for the same Game-view-without-Play preview benefit.

### Unity UI layout learnings

Spent significant time debugging nested Layout Group behavior while building
`PartyFrame_1` (name/role/health-bar). Full detailed notes moved to
`unity-notes.md` since these are reusable lessons, not one-off bugs —
worth reading before building any further UI panels.

### Scene view canvas visualization

Diagnosed (not a bug): Screen Space - Overlay canvases render as an oversized
flat plane near world origin in **Scene view only** — a known Unity editor
quirk from visualizing screen-space UI and world-space gameplay objects in one
3D preview. Doesn't affect Game view or the actual build. Workflow fix: toggle
the eye icon next to `HUDCanvas` in the Hierarchy to hide it from Scene view
while doing world-space/gameplay work; toggle back on for UI work. Isolation
View (crosshair icon in Scene view toolbar) works too for one-off focus.

### Result

Portrait/crossplay screen layout working: pillarboxed gameplay area, dynamically
matched HUD sidebars, both previewable in Game view without pressing Play.
Placeholder party frame UI built and correctly laid out.

## Session 3 — Player Health

### Player health system

- Created `PlayerHealth.cs` — attached to the `Player` GameObject alongside
  `PlayerController`. Tracks `currentHealth` (initialised to `maxHealth` in `Awake`).
  `TakeDamage(int)` reduces HP; reaching 0 calls `Die()`, which disables the
  GameObject (placeholder — no game-over screen or respawn yet).
- Updated `Bullet.cs` — enemy-bullet collision now calls `health.TakeDamage(1)` via
  `PlayerHealth` instead of the previous stub (`Destroy` only, no damage).
- `CurrentHealth` property exposed on `PlayerHealth` for future HUD party-frame
  hookup.

### Still open from this session

- No visual feedback on taking damage (flash, screen shake, etc.) — not started.
- Game-over / respawn flow — not started.
- `maxHealth` is a flat inspector field; per-role health multipliers (Tank higher,
  Attacker lower) come when the role system is added.

### Still open

- **Resolved**: ship rotation approach — decided static (no rotation), Galaga-
  style. Ships strafe only and always fire straight up; matches the current
  `PlayerController.cs` behavior (`Vector2.up` fire direction), so no code
  change was needed.
- Omnidirectional enemy spawning (from any screen edge) — no longer planned,
  since it was only motivated by twin-stick rotation. Enemies continue to
  spawn from the top, matching current `EnemySpawner.cs` behavior.

## Session 4 — Role Architecture

### PlayerRole system

- Wrote `PlayerRole.cs`: `PlayerRole` enum (`Attacker`, `Tank`, `Medic`,
  `Support`), `RoleStats` struct (health/fire-rate/move-speed multipliers +
  sprite tint `Color`), a static `PlayerRoleStats` lookup table with one
  `RoleStats` per role (placeholder balancing values — Tank tankier/slower,
  Attacker squishier/faster-firing, Medic/Support close to baseline), and
  `PlayerRoleComponent` (holds the `role` field, exposes `Stats` computed on
  access, tints its own `SpriteRenderer` in `Awake`).
- Updated `PlayerController.cs` (`Start`) and `PlayerHealth.cs` (`Awake`) to
  multiply their base stats by the role's multipliers via
  `GetComponent<PlayerRoleComponent>()`, null-checked so behavior is unchanged
  if the component isn't present.
- Added `PlayerRoleComponent` to the `Player` GameObject in the scene, default
  `role = Attacker`.
- **Found while testing**: despite Session 3's notes, `PlayerHealth` had never
  actually been added as a component to the `Player` GameObject in the scene
  (script existed, scene wiring didn't) — verified live via the Unity MCP
  bridge (`GetComponent`/component list came back without it, both before and
  after entering/exiting Play mode). Added it now alongside
  `PlayerRoleComponent`, confirmed both persist correctly across Play mode,
  and re-saved the scene.
- No ScriptableObject asset workflow introduced — kept role data as a static
  in-code table to match the project's existing plain-`MonoBehaviour`,
  low-infra style. Easy to migrate to ScriptableObjects later if hand-tuning
  in the Inspector becomes worth the friction.

### Still open

- HUD does not yet display role (name/role text on `PartyFrame_1` is still
  placeholder) — tracked under "Finish the HUD" in `roadmap.md`.
- Only one `Player` instance exists in the scene; local co-op (multiple
  players/roles at once) isn't wired up yet.
- Role-specific abilities (Tank taunt, Medic heal, Support buffs) are not
  implemented — this session only covers passive stat multipliers + tint, per
  the roadmap's stated scope for this item.

## Session 5 — Game Over / Respawn Flow

### Scene scaffolding gap noticed first

Reviewing the docs surfaced that the project only has one scene
(`SampleScene`) with no Main Menu, Role Select, Lobby, or Game Over scene.
Decision: split this into two separate roadmap items rather than one —
a minimal Game Over/Restart flow (needed now, since `PlayerHealth.Die()`
had nowhere to send the player) versus full scene scaffolding (Main Menu,
Role Select, Lobby), which was deliberately deferred to right before the
Nakama networking phase since building it earlier would lock in UI/flow
decisions before role abilities and the boss prototype exist to inform what
those screens actually need to show.

### Approach: same-scene overlay, not a second scene

For the Game Over flow itself, considered three options: a genuinely
separate Game Over scene, a same-scene UI overlay with hand-written state
reset, or a same-scene overlay backed by `SceneManager.LoadScene` reloading
`SampleScene` itself. Went with the third: reloading the scene re-runs
every `Awake`/`Start` exactly as at boot, so `PlayerHealth`, `EnemySpawner`,
and `PartyFrameManager` all reset themselves for free with zero hand-written
reset code. A second scene would have been the project's first multi-scene
setup for no real content difference, and manual state reset would have
been strictly more code for the same result. This keeps the deferred
"Scene scaffolding" roadmap item consistent — no scenes added ahead of when
the roadmap actually calls for them.

### Death signal: first UnityEvent in the codebase

`PlayerHealth.Die()` was `private` and had no way to notify listeners.
Added `public UnityEvent OnDeath`, invoked at the end of `Die()` (after
`SetActive(false)`). Used `UnityEvent` rather than a C# `event`/`Action` to
match the project's existing "explicit Inspector-dragged references only,
no `FindObjectOfType`" convention — it's wired in the Inspector exactly like
any `Button.OnClick()`.

This surfaced a real (if minor) latent bug, not just a missing feature:
`PartyFrameUI.cs` lives on a separate GameObject from `Player` and its
`Update()` kept polling `playerHealth.CurrentHealth` every frame even after
`Player.SetActive(false)` — no exception, but frozen stale values with no
"this player is dead" signal. Fixed by adding `PartyFrameUI.OnPlayerDied()`
(grays out `healthBarFill`, sets an `isDead` guard checked in `Update()`),
wired as a second listener on the same `OnDeath` event.

### New script: GameOverUI.cs

`Assets/Scripts/GameOverUI.cs` — `panelRoot` field, `Awake()` hides it,
`Show()` reveals it (wired to `PlayerHealth.OnDeath`), `Restart()` calls
`SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)` (wired to
the Restart button's `OnClick()`). First `SceneManager`/`Button.onClick`
usage in the project, kept confined to this one file. No `GameManager`
singleton introduced — nothing here needs cross-scene persistence.

### Scene wiring via Unity MCP

Built `GameOverPanel` (dark full-rect overlay + "Game Over" text + Restart
button) under `HUDCanvas` — not `GameplayCanvas`, since Overlay canvas is
needed to cover the pillarbox bars too, not just the 9:16 viewport — and
wired both `OnDeath` listeners live through the Unity MCP bridge using
`execute_code` (arbitrary Editor C# handles multi-object creation +
`UnityEditor.Events.UnityEventTools.AddPersistentListener` wiring in one
shot, versus dozens of individual `manage_gameobject`/`manage_components`
calls for a UnityEvent that has no simple single-property representation).

**Troubleshooting note:** `execute_code`'s C# runs as a method body, not a
full file — `using` directives at the top threw "Unexpected symbol" errors.
Fix: drop the `using`s and fully-qualify types instead (e.g.
`UnityEngine.UI.Image`, `TMPro.TextMeshProUGUI`,
`UnityEditor.Events.UnityEventTools`). Also: HTML-entity-encoded angle
brackets (`&lt;`/`&gt;`) sent through the tool arrive literally rather than
decoding to `<`/`>` — use real `<`/`>` in generic type params.

### Verification

Confirmed end-to-end in Play mode via the MCP bridge (no manual Editor
interaction needed): called `PlayerHealth.TakeDamage(999)` directly →
`GameOverPanel` became active, `PartyFrame_1`'s health bar turned gray.
Invoked the Restart button's `onClick` → scene reloaded cleanly, `Player`
active again with HP back to 4/4 (Attacker role multiplier), no console
errors. Screenshot confirmed the overlay visually covers the full window
including both pillarbox bars, not just the gameplay viewport.

### Still open

- Scene scaffolding (Main Menu, Role Select, Lobby) — deliberately deferred,
  see above.

## Session 6 — Damage Feedback (Sprite Flash + Screen Shake)

### Scope and event design

Completed the last item under "Basic mechanics (remaining)". Went with both
sprite flash and camera shake in one pass rather than flash-only: all the
prerequisite infra already existed (a cached-camera-reference pattern in
`PlayerController.cs`, a coroutine precedent in `EnemySpawner.SpawnWaveRoutine()`),
both are small single-purpose scripts, and shake defaults were kept
conservative (0.2s duration, 0.15 magnitude) to avoid juice-creep — reverting
to flash-only later would just be removing one Inspector listener, not a
code change.

Added `PlayerHealth.OnDamaged` (`UnityEvent`), mirroring `OnDeath`.
**Decision: a fatal hit does not also fire `OnDamaged`**, only `OnDeath` —
`Die()` deactivates the `Player` GameObject, which would cut off an
in-flight flash coroutine before it could revert the sprite color, and
`GameOverUI` takes the screen immediately anyway, so a flash on the killing
blow would be pointless and added a revert-race risk for no benefit.

### Critical constraint: don't clobber the role tint

`PlayerRoleComponent.Awake()` tints the Player's `SpriteRenderer.color` to
the role's color **once** and never re-applies it. `PlayerDamageFlash.cs`'s
`FlashRoutine()` therefore reverts to `PlayerRoleComponent.Stats.tintColor`,
not `Color.white` — reverting to white would have permanently erased the
role tint the first time a player took non-fatal damage. Caught during
planning (research phase), not as a live bug.

### New scripts

- `Assets/Scripts/PlayerDamageFlash.cs` (on `Player`) — `Flash()` restarts a
  coroutine (`StopCoroutine` + `StartCoroutine`) so rapid hits re-flash at
  full brightness instead of stacking or blending; reverts to the role tint
  as above.
- `Assets/Scripts/CameraShake.cs` (on `Main Camera`) — caches
  `transform.localPosition` once in `Awake()` as the base position;
  `Shake()` offsets it by a linearly-decaying random offset per frame, then
  **explicitly** resets to the cached base when done rather than trusting
  the decay to land at exactly zero. Confirmed safe alongside
  `AspectRatioFitter.cs` by re-reading it in full: it only ever touches
  `camera.rect` (the pillarbox viewport), never `transform` — the two
  properties can't conflict.

### Scene wiring via Unity MCP

Same approach as Session 5: attached both components via
`manage_gameobject`, then wired `PlayerHealth.OnDamaged`'s two listeners
(`PlayerDamageFlash.Flash()`, `CameraShake.Shake()`) via `execute_code` +
`UnityEditor.Events.UnityEventTools.AddPersistentListener`.

**New troubleshooting note:** editing script files directly on disk (via
the Write/Edit tools, not through an MCP asset-write action) left Unity's
asset database unaware of the changes — `manage_gameobject`'s
`components_to_add` failed with "Component type not found" even though the
files existed and had no syntax errors. Fixed by explicitly calling
`UnityEditor.AssetDatabase.ImportAsset(...)` per new file (or
`refresh_unity` with `scope: "scripts"`, `compile: "request"`) before
attaching components — needed once per newly-created script file, not
needed for edits to already-imported files.

### Verification

Confirmed in Play mode via the MCP bridge: a single non-fatal
`PlayerHealth.TakeDamage(1)` immediately set `SpriteRenderer.color` to
`flashColor` and offset `Main Camera.transform.localPosition`; after enough
elapsed time both reverted exactly — sprite to `PlayerRoleComponent.Stats.tintColor`
(not white), camera to the exact cached base position with no drift. Two
rapid non-fatal hits in immediate succession re-triggered cleanly with no
stacking bugs. A lethal `TakeDamage` (and, separately, an organic death from
live enemy fire during background simulation) correctly triggered only
`OnDeath`/`GameOverPanel`, never `OnDamaged`, with no console errors.

**Testing note:** discovered mid-session that this Unity Editor instance
does not tick Play-mode `Update()`/coroutines at all while its window is
unfocused and idle — `Time.time` stayed frozen across tool calls and even a
real 3-second wall-clock sleep. Each `manage_camera` screenshot call with
`include_image: true` forces exactly one manual frame step (~0.02s), which
was used to pump enough frames for the flash/shake timers to complete
deterministically. Calling `EditorApplication.QueuePlayerLoopUpdate()`
manually from `execute_code` (attempted before finding the screenshot-step
technique) caused benign "PlayerLoop called recursively" console warnings —
harmless but avoid combining it with Unity's own automatic pump.

### Still open

- Role abilities beyond stat multipliers (Tank taunt, Medic heal, Support
  buffs) — next roadmap item, prerequisite for the boss encounter prototype.
- Scene scaffolding (Main Menu, Role Select, Lobby) — deliberately deferred,
  see Session 5.

## Session 7 — Role Abilities (Tank Taunt / Medic Heal / Support Buff)

### The target-less-ability problem

The scene has exactly one `Player` GameObject — no boss, no AI teammates, no
local co-op. Re-reading `Enemy.cs` confirmed it has **zero** targeting/aggro
concept (its sine-wave movement is computed purely from `startX`/`Time.time`,
never references the player), so Tank taunt has no boss to redirect and
Medic heal has no ally to heal. Only Support's buff has an obvious,
non-contrived single-player test path.

**Decision: no targeting/aggro concept was added to `Enemy.cs`.** Taunt's
entire point is changing *boss* AI behavior — inventing a `currentTarget`
field now would mean guessing at a data shape the boss prototype hasn't
earned yet, and a wrong guess becomes dead code to delete later. Matches the
project's "prove gameplay is fun before investing in infrastructure"
principle and the Session 4 precedent of keeping role data a static
dictionary instead of ScriptableObjects. Instead, built the full ability
**framework** with every ability's mechanics fully real: Tank taunt is a
real, cooldown-gated `UnityEvent` broadcast with no listener yet (the boss
prototype adds one later — a one-line Inspector wire-up, which is the whole
point of building the framework now); Medic heal is a real, symmetric
`Heal(int)` targeting self; Support buff is a real temporary stat multiplier,
fully self-testable today. Attacker gets no ability this pass.

### Architecture: one script, not four

`PlayerAbility.cs` branches on `PlayerRoleComponent.role` in a single
`OnAbility(InputValue)` handler rather than four per-role scripts or an
`IAbility` interface — exactly one role is ever active per `Player`
GameObject today, the same constraint that already justified `RoleStats`
being a flat struct+dictionary rather than a class hierarchy. An `IAbility`
abstraction only pays for itself once something needs a polymorphic
collection (iterating "each teammate's ability") — that's boss-prototype/
AI-teammate scope, not this one.

### New input action

Added `Ability` (Button, bound to `E` — Space is already Fire) to
`Assets/Input/PlayerControls.inputactions` by hand-editing the JSON (same
structure as the existing `Fire` action/binding, new GUIDs). `PlayerInput`'s
existing Send Messages behavior auto-matches `Ability` → `OnAbility` by
name — no other plumbing needed, confirmed by reading `PlayerController.cs`'s
existing `OnMove`/`OnFire` pattern first.

### New/changed scripts

- `Assets/Scripts/PlayerAbility.cs` (new, on `Player`) — `Time.time`-based
  cooldown gate (`nextAbilityTime`, same pattern as `PlayerController`'s
  `nextFireTime`) shared across all three abilities. Support's buff uses the
  same coroutine-restart pattern as `PlayerDamageFlash.cs`/`CameraShake.cs`.
- `PlayerHealth.cs` — added `Heal(int)`, symmetric to `TakeDamage(int)`,
  clamped at `maxHealth`. No new `UnityEvent` — `PartyFrameUI.cs` already
  polls `CurrentHealth` every frame, so a heal shows up live for free.

**Correctness constraint worth documenting** (found while testing, not a
shipped bug): the buff's revert *divides out* a fixed multiplier rather than
restoring a cached base value, so `buffCooldown` must stay ≥ `buffDuration`
(shipped defaults: 8s ≥ 4s) — re-triggering before the previous buff has
reverted would double-apply the multiplier. The cooldown gate already
enforces this under shipped defaults; noted in `systems/player-roles.md` so
future tuning doesn't break it silently.

### Scene wiring via Unity MCP

Same approach as Sessions 5-6: edited the `.inputactions` JSON and both
scripts on disk, reimported via `AssetDatabase.ImportAsset` (Session 6's
gotcha still applies to new files), waited for compile, attached
`PlayerAbility` via `manage_gameobject`. `OnTaunt` was left with zero
persistent listeners — nothing exists to listen yet, and a placeholder
listener would have been scope creep.

### Verification

Confirmed per-role in Play mode via the MCP bridge, using reflection to call
each ability's private trigger method directly (`TriggerHeal`/`TriggerTaunt`/
`TriggerBuff`) since constructing a real `InputValue` outside an actual input
callback isn't practical:
- **Medic**: damaged self, healed, confirmed `CurrentHealth` increased and
  clamped at `maxHealth`; confirmed the cooldown gate's condition would
  block an immediate re-press.
- **Tank**: temporarily attached a throwaway listener to `OnTaunt`, confirmed
  it fires on activation and the cooldown gate blocks a second immediate
  trigger; removed the listener again afterward (verification-only, not left
  in the saved scene).
- **Support**: confirmed the buff immediately multiplies `moveSpeed`/
  `fireRate` correctly, and reverts to the exact pre-buff baseline with no
  drift once `buffDuration` elapses.

**Testing hazard hit twice this session:** with the Editor window focused,
Play mode runs in real time in the background — enemy fire killed the test
player mid-verification more than once (confirming the game loop itself
still works correctly, but interrupting scripted tests). Recovered by
restarting via the same `RestartButton.onClick.Invoke()` technique from
Session 5. Also re-confirmed Session 6's frame-stepping technique
(`manage_camera` screenshots with `include_image: true`) still works when
unfocused, but pumping ~200 individual frames for a 4-second coroutine
wasn't practical — instead, temporarily lowered `buffDuration` to a
fraction of a second for the revert test only. This is safe because Play
mode changes to serialized fields are automatically discarded when exiting
Play mode; the saved scene keeps the real default (4s).

### Still open

- Tank taunt's listener and Medic heal's ally-targeting — both deferred
  until the boss prototype / AI teammates exist to target.
- Scene scaffolding (Main Menu, Role Select, Lobby) — deliberately deferred,
  see Session 5.
- Shrink ship sprites, enemy spawn pattern variety — newly added roadmap
  items, not started.

## Session 8 — Ability Feedback + Contrast Fix + Attacker "Big Shot"

Playtesting Session 7's abilities surfaced three follow-up requests: ability
activation was invisible (especially Tank taunt, with nothing to affect,
and Support's buff, with no on/cooldown indicator), the party frame's text
was hard to read, and Attacker still had no ability.

### Contrast root cause (found via Unity MCP, not guessed)

Read `PartyFrame_1`'s live component values before touching anything: the
background `Image` was white at 39% alpha (`RGBA(1,1,1,0.392)`), not an
actual grey — it only *read* as a washed-out light-grey box because it's
blended over `HUDCanvas`'s dark backdrop. All 5 `TextMeshProUGUI` children
were opaque white. So the real bug was white-on-near-white, not
white-on-mid-grey. Fixed by darkening the *prefab's* root `Image` to
`RGBA(0.05, 0.05, 0.08, 0.85)` — a real dark panel, matching the project's
stated cyberpunk aesthetic — rather than changing text color, since the
text was never the problem.

### Ability status + cooldown display

Added `CooldownRemaining`, `IsBuffActive`, `BuffRemaining`, `AbilityName`,
and `StatusText` as public read-only getters on `PlayerAbility.cs` — the
single source of truth for ability state, so `PartyFrameUI` only *formats*
what `PlayerAbility` already knows (same "HUD reads, never owns state"
pattern as health/movement stats) rather than duplicating cooldown math.
Added a sixth party-frame stat line, `abilityText`, showing e.g. `"Buff:
+30% Spd +30% Rate (2.1s)"` while Support's buff is active, or `"Taunt:
Ready"` / `"Taunt: 3.2s"` otherwise. Discovered live: all four abilities
share **one** cooldown gate (`nextAbilityTime`), not per-ability cooldowns —
switching role mid-cooldown correctly shows the leftover time from whatever
ability last fired, which is the intended shared-gate design, not a bug.

### Attacker — "Big Shot" ability

Gave `Bullet.cs` a real `damage` field (previously hardcoded as the literal
`1` in both `OnTriggerEnter2D` branches) — defaults to `1`, so enemy bullets
and regular player fire are unaffected. Attacker's `E` now fires a bullet at
3x width (`transform.localScale.x`) and 3x damage (3, vs. a regular
bullet's 1) via a new `PlayerController.FireBigShot()`, sharing a
`SpawnBullet()` helper with the regular `Fire()` path so there's one
instantiation/`Init()` call site.

**Recoil — the key technical constraint of this session:** re-reading
`HandleMovement()` showed it recomputes position from `moveInput` and calls
`rb.MovePosition()` unconditionally every `FixedUpdate`, with no term for
accumulated velocity. A plain `Rigidbody2D.AddForce()` impulse — the
obvious first approach — would have been silently overwritten the very next
`FixedUpdate`, making recoil invisible. Instead, recoil is a `recoilVelocity`
field that `HandleMovement()` itself decays (`Vector2.Lerp` toward zero,
scaled by `recoilDamping`) and folds directly into its existing position
formula, so it automatically respects the viewport-edge clamp too.

### Scene/prefab wiring via Unity MCP

Same reimport-before-attach approach as prior sessions for the script
changes. The prefab edits (darkened background, new `AbilityText` child)
used `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset` via
`execute_code` — duplicated the existing `FireRateText` child as a styling
template (same font/size/color) rather than building a `TextMeshProUGUI`
from scratch, then renamed and rewired it. Confirmed via MCP that the live
scene instance (`PartyFrame_1`) picked up both prefab changes cleanly with
no stale per-instance override.

### Verification

Confirmed in Play mode via the MCP bridge: the party frame renders with
genuine contrast at full resolution (a small/heavily-downscaled screenshot
misleadingly still looked washed-out — always verify UI contrast at a
reasonably large capture resolution, not a thumbnail). Support's buff shows
the live `+30% Spd +30% Rate (Ns)` countdown; Tank/Medic show `Ready`/
cooldown seconds correctly. Attacker's big shot: bullet `localScale.x`
and `damage` both confirmed 3x normal; recoil visibly moved the ship and
decayed to a stable stop. **The recoil's total displacement (-0.63 units)
was verified against the closed-form sum of the decaying-velocity series**
(`recoilForce × fixedDeltaTime × (1-k)/k` where `k = recoilDamping ×
fixedDeltaTime`) rather than assumed correct from a single before/after
position check — it matched exactly, confirming smooth convergence rather
than a runaway or stuck value. No console errors throughout, including
after an organic in-Play death from live enemy fire (same recurring testing
hazard as Session 7 — Play mode runs in real time once the Editor window is
focused).

### Still open

- Tank taunt's listener and Medic heal's ally-targeting — still deferred,
  see Session 7.
- Scene scaffolding, shrink ship sprites, enemy spawn pattern variety — not
  started, see Session 5/7.

## Session 9 — Tank Taunt Placeholder Feedback

Playtesting Session 8 surfaced a fair complaint: Tank taunt has zero visible
effect (by design — no boss/aggro system exists yet, see the new
"Aggro/targeting" explainer in `systems/player-roles.md`), which reads as
"is this broken?" rather than "there's just nothing to affect yet."

Rather than inventing a fake targeting system on `Enemy.cs` to give taunt
something to do (explicitly out of scope — that's the boss prototype's
job), wired `PlayerAbility.OnTaunt` to the two feedback effects already
built for `PlayerHealth.OnDamaged`: `PlayerDamageFlash.Flash()` and
`CameraShake.Shake()`. Zero new code — this is purely two more
`AddPersistentListener` calls via the Unity MCP bridge (same technique as
every prior session), reusing infrastructure exactly as its event-driven
design intended. Gives `E`-as-Tank an immediate "something happened" cue
(flash + shake) without pretending it does anything mechanically yet.

Verified via MCP: `OnTaunt.GetPersistentEventCount()` went from 0 to 2;
triggering taunt flashed the sprite white as expected. Camera shake fired
too (no errors, listener count confirms it ran) but its visible offset was
swallowed by a large first-frame `Time.deltaTime` in this specific
Editor-idle test snapshot — the same environment quirk documented in
Session 6/8, not a wiring problem; the underlying `CameraShake.Shake()` code
is unchanged from its already-verified `OnDamaged` usage.

### Still open

- Real aggro/targeting system and taunt's actual gameplay effect — boss
  prototype scope, not started.
