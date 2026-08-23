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

## Session 10 — Boss Encounter Prototype

The roadmap's next item and the project's core design bet
(`overview.md`): prove MMO-raid-style role coordination is fun with one
human player plus CPU-controlled AI teammates, before any networking
exists. Full reference for everything below: `systems/boss.md`.

### Scope and design decisions

Kept deliberately prototype-simple, matching the project's "prove fun
before infra" style already established in Sessions 4/7:

- **Boss doesn't chase** — sine-drifts near the top of the screen (same
  pattern as `Enemy.cs`) and aims at whichever target holds highest aggro.
  No pathfinding needed to prove the aggro mechanic.
- **Aggro is a plain threat table** (`Dictionary<GameObject, float>`,
  damage-dealt-per-target, no decay) rather than a fuller MMO-style threat
  system — this is a prototype pass, not the final design.
- **2 phases on one HP bar**, not two separate encounters: Phase 1 (100%→50%
  HP) fires a single aimed shot; crossing 50% flips to Phase 2 (fire
  interval halved, 3-bullet spread). Reaching 0 HP in either phase ends the
  fight — there's no third phase after Phase 2 by design.
- **AI teammates reuse the human `Player`'s exact component set**
  (`PlayerController`, `PlayerHealth`, `PlayerRoleComponent`,
  `PlayerAbility`) with `PlayerInput` swapped for a new `AIController.cs`,
  rather than writing separate AI-specific movement/combat logic — keeps
  the AI teammates mechanically identical to a human player in every way
  except how their input is produced.
- Only the human `Player`'s death shows `GameOverPanel`; a teammate dying
  just grays its own party frame and keeps fighting inactive.
- Role assignment is Inspector-only (matches the existing single-player
  pattern) — no role-select UI, that's still scene-scaffolding scope,
  deferred per the roadmap's build order.

### New scripts

- `Boss.cs` — health/phases/aggro/firing, `TakeDamage(int, GameObject)`,
  `TauntedBy(GameObject)`, `OnPhase2`/`OnDefeated` events.
- `AIController.cs` — drives a teammate's movement (sine-weave strafe),
  firing (continuous auto-fire), and ability use (per-role heuristic: Tank
  taunts when it doesn't hold aggro, Medic heals below a threshold,
  Support/Attacker just retry every frame since `TryUseAbility()`'s own
  cooldown gate makes that safe).
- `BossPanelUI.cs` — reads `Boss`'s state into the rebuilt `BossPanel` HP
  bar/phase/target text.

### Minimal-diff changes to existing scripts

Rather than duplicating movement/fire/ability logic for AI, extracted
non-input public entry points from the existing input-driven ones:
`PlayerController.OnMove`/`OnFire` now wrap new `SetMoveDirection(Vector2)`/
`SetFiring(bool)`; `PlayerAbility.OnAbility` now wraps a new
`TryUseAbility()`. No behavior change for the human `Player`. Also:
`Bullet.Init()` gained an optional `GameObject ownerObject` param (default
`null` keeps every existing call site compiling) so player bullets can
attribute damage to their shooter, and `Bullet.OnTriggerEnter2D`'s
player-bullet-vs-`Enemy`-tag branch now also checks for a `Boss` component
and routes damage to it — previously only `Enemy.TakeDamage` was reachable.

### Bug found during testing: unsafe dictionary indexer

`Boss.PickTarget()` originally indexed the `aggro` dictionary directly
(`aggro[t]`) assuming every active `targets[]` entry was always a populated
key. Live Play-mode testing hit a `KeyNotFoundException` on `Player`
specifically, thrown every `Update()` — since the exception aborted the
rest of `Update()` before reaching `Fire()`, this silently stopped the boss
from firing at all once it started. Root cause wasn't fully pinned down
(the `targets[]`/`aggro` population from `Awake()` checked out correctly in
isolated re-tests), but the fix is correct regardless: switched to
`Dictionary.TryGetValue`, which can't throw and costs nothing extra. Not
caught by compilation or an initial quick Play-mode smoke test — only
surfaced during sustained live testing, a reminder that MonoBehaviour
`Update()` exceptions fail silent-ish (logged, not crashing) and can hide
inside otherwise-working systems.

### Bug found during testing: boss placed outside camera view

The `Boss` GameObject was initially placed at world `y=6`, but Main
Camera is orthographic with size 5 (visible Y range roughly `[-5, 5]`) — the
boss was completely invisible in Play mode despite every script and event
wiring working correctly. Caught by actually looking at a screenshot, not
by inspecting field values (which all looked fine). Moved to `y=4.2`.
**Lesson reinforced**: numeric/logical verification isn't a substitute for
a visual check when a bug could be purely spatial/visual.

### Unity MCP bridge quirks hit this session

- `manage_prefabs`'s `component_properties` and `manage_components`'s
  `set_property` both failed to resolve the type name `"PlayerController"`
  (ambiguous — a `VariableExamples+PlayerController` sample type also
  exists somewhere in the loaded assemblies; `manage_components` separately
  reported "not found" for the same name). Worked around by using
  `execute_code` with a direct, compile-time-unambiguous
  `GetComponent<PlayerController>()` call instead of the reflection-based
  tools, for every edit that needed to touch this specific component type.
- Object-reference component properties need to be passed as `{"instanceID":
  N}` objects in an array, not bare integers — a bare-int array silently
  produced an array of `null`s (`Boss.targets` came back `[null, null,
  null, null]` on the first attempt, only caught by reading the value back
  afterward).
- `create_from_gameobject` (prefab-izing an existing scene GameObject) can
  disconnect mid-call (likely from the asset-import domain reload it
  triggers) — retrying the same call after checking `editor/state` for
  `ready_for_tools` succeeded cleanly, with the scene GameObject's data
  intact.
- Duplicating a GameObject (`Teammate_Medic`/`Teammate_Support`, both
  duplicated from `Teammate_Tank` *before* `Teammate_Tank` was converted
  into `Teammate.prefab`) does **not** retroactively make the duplicates
  prefab instances — they stayed independent GameObjects with matching
  values, so a later edit to `Teammate.prefab`'s defaults (see Session 11)
  only affected `Teammate_Tank`, not the other two, and had to be applied
  to all three individually. Documented in `systems/boss.md`'s scene-wiring
  section so this doesn't get assumed away later.
- Reconfirmed the Session 6/8 environment quirk (this Editor instance
  doesn't reliably tick Play-mode `Update()` while unfocused/idle, then can
  jump substantially once refocused) — it showed up here as the boss
  appearing to take almost no damage across several tool calls and then
  being defeated between the next two. Not a gameplay bug; `manage_camera`
  screenshot calls (each forces one manual frame step) remain the reliable
  way to pump deterministic frames for testing.

### Verification

All done live via the Unity MCP bridge in Play mode: phase transition
flips `IsPhase2` exactly at the 50%-HP boundary and fires `OnPhase2`
exactly once; aggro correctly tracks the highest damage-dealer and
`TauntedBy()` redirects `CurrentTarget`, with a second immediate taunt
blocked by the existing cooldown gate; AI teammates were observed
autonomously moving, firing, and triggering role abilities (Tank's taunt
firing for real the moment it didn't hold aggro, Support's buff
auto-activating); boss defeat fires `OnDefeated`, flips `BossPanelUI` to
"DEFEATED", and destroys the `Boss` GameObject cleanly; all 4
`PartyFrameUI` instances and `BossPanelUI` read live values with no drift
from the underlying `Boss`/`PlayerHealth` state.

### Still open

- No minions around the boss yet (motivates Session 11's ship-shrink).
- Local co-op / a dynamic player count — the party is 4 fixed, hand-placed
  scene objects, not a runtime spawner.
- Medic heal still only targets self, even though allies (the AI
  teammates) now exist to target.

## Session 11 — Boss Fight Tuning

Follow-up requested after Session 10's playtest: the fight was too easy,
and ship sprites need to be smaller to leave room for minions planned
around the boss later.

### Changes

- **Fire cadence**: `PlayerController.fireRate`'s base value went from
  `0.2` to `0.35` (script default, `Teammate.prefab`, and all 4 scene
  ships), making the fight take more sustained effort while preserving
  each role's relative fire-rate balance (multipliers apply on top,
  unchanged).
- **Ship scale**: `Player`/`Teammate_*` `Transform.localScale` went from
  `1.0` to `0.6`. The `Boss` was deliberately left at its existing `1.6`
  scale (user's explicit choice) so it still reads as the big, central
  target once smaller minions are added around it later.

Neither change touched `FirePoint` (a child transform, so its effective
world offset scales automatically with the parent) or `BoxCollider2D`
(size scales with the transform automatically too) — confirmed no
additional edits were needed there.

### Reconfirmed the prefab-instance gotcha from Session 10

`Teammate_Tank` picked up the new `fireRate`/`scale` defaults automatically
from `Teammate.prefab` once it was edited (no per-instance override
existed to block inheritance). `Teammate_Medic`/`Teammate_Support` did
**not** — as flagged in Session 10, they're independent GameObjects, not
prefab instances — so they needed the same two values set directly, same
as `Player` (which was never part of the prefab to begin with).

### Verified

Read the 4 ships' live `fireRate`/`localScale` values in Play mode:
role-multiplied effective fire intervals matched expectations exactly
(Attacker 0.2625s, Support 0.35s baseline — briefly lower mid-buff, which
is correct, not a bug — Medic 0.35s, Tank 0.42s). A screenshot confirmed
visually: `Boss` unchanged and clearly larger, `Player`/teammates visibly
smaller and still firing correctly.

## Session 12 — Shield Stat, Tank AI Positioning, Boss HP/Damage Tuning

Design work (role-differentiated AI behavior, a new shield stat, a
manual-ability-trigger mechanic) had already been agreed and written up in
`docs/systems/*.md` as "planned, not yet implemented." This session
implemented the first slice — shield + Tank — then a follow-up tuning
request came in for boss HP and player damage. Full technical detail lives
in `systems/boss.md`, `systems/player-roles.md`, `systems/combat.md`,
`systems/hud-layout.md`; this is the narrative version.

### Shield stat

Added `RoleStats.shieldMultiplier` (Tank `2.0`, highest; Attacker `1.0`,
medium; Medic/Support `1.0`, placeholder — only two were specified by
design) and a `maxShield`/`CurrentShield` pool on `PlayerHealth`, scaled by
role the same way `maxHealth` already was. `TakeDamage(int)` now absorbs
into shield first, only the overflow touching health — a hit fully absorbed
by shield still fires `OnDamaged` (flash/shake), matching how a real hit
should feel. Added `RestoreShield(int)` (symmetric to `Heal(int)`) even
though nothing calls it yet — Medic's proximity aura is a separate,
still-planned follow-up — same "build the real method before the consumer
exists" precedent as Session 7's `Heal(int)`. Deliberately **no** passive
regen anywhere: shield only ever goes up via `RestoreShield`, keeping Tank
dependent on Medic by design.

### Tank guard-point positioning

`AIController.Update()` now branches on role: Tank calls a new private
`GuardPointDirection()` instead of the shared sine-weave. It averages the
positions of a new `teammates[]` array (Inspector-wired to the 3
`Teammate_*` transforms, self excluded at runtime), lerps from that toward
the boss by `guardBias` (0.65), and steers there (with a small deadzone to
stop jitter on arrival). Physically blocking bullets needed **zero changes
to `Bullet.cs`**: bullets already just travel in a straight line and damage
whichever `Player`-tagged collider they hit first, so a Tank standing in
the way already "blocks" an ally for free via the existing trigger
collision — this was purely a positioning problem once that was confirmed
by re-reading `Bullet.cs` rather than assumed. "Ignore the human player"
was achieved for free too: `teammates[]` simply never includes `Player`, no
runtime human-detection check needed, since the human always plays `Player`
specifically (see `current-state.md`) and the 3 `Teammate_*` are always
AI-controlled regardless of which role each currently has.

### Gotcha: scene wiring didn't survive a save, twice

First attempt at wiring `teammates[]` via `execute_code` +
`EditorUtility.SetDirty()` reported success and the scene save reported
success, but a fresh Play-mode check showed the array empty. Root cause:
`Teammate_Tank` is a `Teammate.prefab` instance (Session 10), and an
instance-level override on an object-reference field needs
`PrefabUtility.RecordPrefabInstancePropertyModifications()` called on the
component in addition to `SetDirty()`, or it silently doesn't serialize.
Confirmed the fix by forcing a full scene reload from disk (not just
trusting the in-memory value) — this became the standard verification
method for the rest of the session, and caught the exact same class of
issue again later (see below). `Teammate_Medic`/`Teammate_Support` (not
prefab instances) never needed the extra call.

### Verification

All via the Unity MCP bridge in Play mode: forced `TakeDamage` calls
confirmed shield absorbs first and only the overflow hits health, with
`OnDamaged` firing even on a shield-only hit; `RestoreShield` clamps at
`maxShield` correctly; read all 4 ships' live `maxShield` values and
confirmed the role multipliers applied (Tank 6, Attacker/Medic/Support 3).
Called the private `GuardPointDirection()` directly via reflection and
confirmed it matched the hand-computed expected direction exactly (dot
product 1.0), then let Play mode run and sampled positions over time: Tank
converged toward the guard point (both X and Y changing) while
Medic/Support kept moving only in X — confirming the non-Tank code path is
genuinely unchanged, not just visually similar. A screenshot during a live
fight showed Tank sitting between the boss and the other two teammates,
and the boss's live aggro target had already become Tank, confirming the
pre-existing taunt heuristic still works alongside the new positioning. The
party frame's new shield bar tracked live shield values correctly for all
4 frames once enough Play-mode frames had ticked (same Editor-idle quirk
as prior sessions — a value can look stale for a beat after a forced
`TakeDamage` call until the next pumped frame).

### Boss HP / player damage tuning (follow-up request)

Separate ask, same session: increase boss health, decrease all roles' fire
damage by 40%. No specific numbers were given, so picked round ones and
flagged them for the user to correct: `Boss.maxHealth` doubled (`30` →
`60`); regular fire damage `1` → `0.6`; Attacker's Big Shot `3` → `1.8`
(both hit values scale by the exact same 0.6× factor, so their 3:1 ratio is
preserved). Enemy/boss-dealt damage was explicitly out of scope — only
player-dealt damage.

A flat 40% cut on a baseline of `1` isn't representable as a whole number,
so `Bullet.damage` changed `int` → `float`, which rippled into
`Enemy.TakeDamage`/`Boss.TakeDamage` (also `int` → `float`) — each still
rounds (`Mathf.RoundToInt`) only at the point it subtracts from its own
`int` health pool, so no fractional HP shows up anywhere; the
enemy-bullet-vs-`PlayerHealth` path does the same rounding at its call
site, since `PlayerHealth.TakeDamage(int)` deliberately stayed `int`.

Hit the exact same "script default doesn't retroactively update an
already-serialized scene/prefab-instance value" gotcha from Session 11,
twice more: `Boss.maxHealth` and `PlayerAbility.bigShotDamage` both had to
be set explicitly on the live scene instances (all 4 ships, for
`bigShotDamage`) **and** on `Boss.prefab`/`Teammate.prefab`'s defaults, with
`Teammate_Tank` again needing `RecordPrefabInstancePropertyModifications()`.
Both caught immediately by the same "force a full disk reload, don't trust
the in-memory value" verification habit established earlier this session —
without it, both would have silently reverted to their old values.

### Verified

End-to-end in Play mode: all 4 ships' `Fire()` produced bullets with
`damage == 0.6` (confirmed via `FindObjectsByType<Bullet>`); Attacker's Big
Shot produced a `damage == 1.8` bullet; `Boss.maxHealth`/`CurrentHealth`
read `60/60` after a full scene reload from disk. No compile errors or
console warnings from the type changes.

## Session 13 — Medic AI Positioning + Proximity Aura + Visual Feedback

The roadmap's "Recommended next" item, second slice after Tank (Session
12): Medic AI positioning (hang back from the boss) plus the proximity
heal/shield aura design that had been sitting as "planned, not yet
implemented" in `boss.md`/`player-roles.md` since Session 12.

### Design refinement before implementation

The originally-written design (a single always-large aura radius) got
revised in conversation before any code was touched: the aura is **tiny by
default** — allies need to almost touch the Medic to be healed — and
pressing **E drastically expands the radius and heal rate for a limited
duration**, replacing Medic's old instant self-heal ability entirely rather
than being additive to it (explicitly confirmed with the user — "Replace
with aura boost", not "do both"). This changes what `boss.md`/
`player-roles.md` had already described, so both docs needed updating
alongside the code, not just appending.

### Architecture decision: aura lives on `PlayerAbility`, not `AIController`

The aura and its boost ability were built on `PlayerAbility.cs`, **not**
`AIController.cs`, even though the positioning half of this session's work
*does* live on `AIController.cs`. Reasoning: `AIController` only exists on
the 3 `Teammate_*` GameObjects; `PlayerAbility` exists identically on
`Player` too. Per Session 10's stated principle that AI teammates are
"mechanically identical to a human player in every way except how input is
produced," the aura has to work the same way regardless of whether Medic is
currently human- or AI-controlled — so it couldn't live in a teammate-only
script. Positioning stays AI-only in `AIController.cs` since a human Medic
just moves via WASD.

### Positioning: generalized `GuardPointDirection()` instead of duplicating it

Rather than writing a second near-identical method for Medic, Tank's
existing `GuardPointDirection()` was generalized into `BiasedPositionDirection(bias,
deadzone)`, parameterized on the Lerp bias — Tank keeps `guardBias = 0.65`
(toward the boss, unchanged behavior), Medic gets a new `medicBias = -0.3`
(away from the boss). `AIController.Update()`'s movement switch grew a
third case instead of staying a binary Tank/everyone-else ternary.

**Bug caught before it shipped**: `Vector2.Lerp` clamps its `t` parameter to
`[0, 1]` in Unity — a negative `medicBias` would have silently clamped to
`0` (landing exactly on ally center, not extrapolating past it) rather than
actually pulling Medic away from the boss. Caught by reasoning about the
API, not by testing a broken result. Fixed by switching both Tank's and
Medic's calls to `Vector2.LerpUnclamped`, which lets `t` go outside `[0, 1]`
and extrapolate.

### Aura mechanics

New fields/methods on `PlayerAbility.cs`: passive `TickAura()` runs every
`auraTickInterval` (1s default) while `role == Medic`, healing/shielding
(`Heal(int)`/`RestoreShield(int)`, both pre-existing) every ally in
`allies[]` within `auraRadius` (0.5 — tiny by design). `TriggerAuraBoost()`
(replacing the old `TriggerHeal()` in `TryUseAbility()`'s switch) is a
coroutine flipping `auraBoosted` on for `auraBoostDuration` (4s), during
which `TickAura()` uses `auraBoostRadius` (3) and a much shorter
`auraBoostTickInterval` (0.25s) instead — same `StopCoroutine`/
`StartCoroutine` restart-safety pattern as Support's `TriggerBuff()`, and
the same "cooldown must stay ≥ duration" constraint Session 7 documented
for that buff (`auraBoostCooldown` 10s ≥ `auraBoostDuration` 4s).

**New wiring needed**: `allies[]`, a `Transform[]` of all 4 ships
(self-included, filtered at runtime), had to be added fresh — the existing
`AIController.teammates[]` array deliberately excludes `Player` (see
Session 12), so it can't be reused for something that must also heal the
human player. Wired identically on all 4 ships' `PlayerAbility` via
`execute_code`, hitting the now-familiar prefab-instance gotcha once more:
`Teammate_Tank` needed `RecordPrefabInstancePropertyModifications()`,
`Teammate_Medic`/`Teammate_Support` didn't (not prefab instances, per
Session 10/11). Verified by forcing a full scene reload from disk, same
habit as every prior session that's hit this gotcha.

### Follow-up: visual feedback

Playtesting the mechanic surfaced the obvious gap immediately: nothing in
the world shows the aura exists. Two additions, both requested together:

- **Radius ring** — a `LineRenderer` circle (32 segments, `Sprites/Default`
  shader, world-space so it isn't distorted by the ship's `0.6` transform
  scale) built procedurally as a child of the Medic's `PlayerAbility` in
  `Awake()` (only when `role == Medic`, so other roles don't pay for an
  unused GameObject). Dim/thin by default, brighter/thicker while boosted —
  updated every frame in `Update()` independent of the tick-gated
  `TickAura()` call, so the ring's size/brightness reflects boost state
  immediately even between heal ticks.
- **Heal flash** — `PlayerDamageFlash.Flash()` gained a `Flash(Color)`
  overload (existing parameterless `Flash()` now just calls it with the
  component's own `flashColor` field, so `OnDamaged`/`OnTaunt`'s existing
  wiring is unchanged) so `TickAura()` can flash a healed ally green
  (`healFlashColor`) distinctly from the white damage flash — only on
  allies that actually had missing health/shield that tick, not every ally
  in range regardless of whether they needed healing.

### Verified

All via the Unity MCP bridge. Play mode, reflection-called `TickAura()`
directly (same technique as Session 7-9's private-method verification):
healed an ally at distance 0 (in range), confirmed no change to a
subsequent hit while 20 units away (out of range), triggered the boost via
`TryUseAbility()` and confirmed an ally 2 units away — outside the default
radius but inside the boosted one — got healed. Confirmed the boost
reverts automatically (`IsAuraBoosted` false again) after its duration
using the same "temporarily shrink the duration for a fast test" technique
Session 7 used for Support's buff. Confirmed via `BiasedPositionDirection()`
reflection calls that Tank's direction dot-products ~+1 with "toward the
boss" (matching Session 12's finding) while Medic's dot-products negative
(away from the boss). **Swapped which `Teammate_*` GameObject played Medic
mid-session and confirmed both the aura and the positioning followed the
role, not the GameObject** — the real test of the `allies[]`/prefab-instance
wiring. Screenshots confirmed the ring renders and visibly expands/brightens
during the boost, and the party frame's ability line correctly shows "Aura
Boost: Ready" / "Aura Boost: Boosted (Ns)" (no leftover "Heal" text
anywhere — `PartyFrameUI.cs` reads `PlayerAbility.AbilityName`/`StatusText`
generically, so it needed no changes itself). No console errors or warnings
at any point.

### Still open

- Attacker/Support AI positioning — still planned, see `boss.md`'s "AI
  teammate behavior". Medic and Tank are now both implemented.
- Bullet-dodging, teammate separation, manual teammate-ability triggering
  from the party frame — unchanged from Session 12, still designed but not
  built.

## Session 14 — Medic AI Trigger/Positioning Rework

Playtesting Session 13 surfaced a real problem: the Medic AI's aura boost
never fired in practice, not even once across a full test session. Root
cause was the trigger heuristic itself — `medicBoostThreshold` gated the
boost on the *Medic's own* HP dropping below 60%, but Medic's positioning
(hanging back, away from the boss) means it rarely takes damage, so the
gate almost never opened. The heuristic was checking the wrong ship's
health entirely — the boost is meant to help *allies*, not itself.

### New design

Agreed replacement, in two independent parts:

- **Ability trigger — temporary, explicitly flagged for rework**: Medic now
  fires the aura boost the instant it's off cooldown, identical to
  Support/Attacker's existing "retry every frame, let the cooldown gate
  sort it out" pattern. No need-awareness at all for now — marked with an
  explicit `TEMPORARY` comment in `AIController.cs` pointing back to this
  doc, since a smarter trigger (e.g. "boost when an ally is hurt," now that
  hurt-detection exists for positioning below) is an obvious near-term
  follow-up once this dumb version is validated.
- **Positioning — real, not temporary**: Medic's default is still hanging
  back (Session 13's `BiasedPositionDirection(medicBias, ...)`), but it now
  actively breaks from that position to approach whichever ally is hurt.
  "Hurt" is decided per-ally: below `medicApproachThreshold` (55%) in
  *either* health or shield fraction counts (mirrors `TickAura()`'s own
  health-or-shield check, so positioning and healing agree on what "needs
  help" means) — of potentially several hurt allies, Medic approaches
  whichever has the single lowest fraction. Checked every frame, so Medic
  re-targets immediately as the situation changes (an ally recovers, a
  different ally drops lower, everyone's fine again and it returns to
  hanging back).

### Why `PlayerAbility.allies`, not `AIController.teammates[]`

The hurt-ally check (`FindHurtAlly()`, new private method) iterates
`ability.allies` — the array Session 13 added to `PlayerAbility` for the
aura itself — rather than `AIController.teammates[]`, which was already
wired and would have been the "obvious" reuse. `teammates[]` deliberately
excludes `Player` (Tank's guard point is only supposed to average
AI-controlled allies' positions, see Session 12), but the Medic should
approach the human player if *they're* the one who's hurt just as readily
as a CPU teammate — `allies[]` already covers all 4 ships for exactly this
reason. No new wiring needed; it reuses Session 13's existing array as-is.

### Cleanup

`AIController`'s cached `PlayerHealth health` field became dead code once
the ability-trigger heuristic stopped reading it (the new trigger doesn't
check anyone's health) — removed rather than left unused.

### Verified

Unity MCP, Play mode: with the whole party at full health, a couple of
frames in, `PlayerAbility.CooldownRemaining`/`IsAuraBoosted` on the Medic
already showed the boost had fired (confirms the "as soon as available"
trigger actually fires, unlike the old heuristic). Reflection-called
`FindHurtAlly()` directly: returned `null` while everyone was healthy;
after damaging Support down to 40% health / 0% shield, returned
`Teammate_Support`, and `ApproachDirection()`'s returned direction
dot-producted `1.00` against the exact hand-computed direction to Support
(same verification style as Session 12's guard-point check). No console
errors or warnings.

### Still open

- The "temporary" ability trigger is still just "fire on cooldown" — see
  above for the flagged follow-up once this is validated as an improvement
  over the old (broken) behavior.
- Attacker/Support AI positioning, bullet-dodging, teammate separation,
  manual teammate-ability triggering — unchanged, still not built.

## Session 15 — Support AI Positioning + Fire-Cadence/Damage Catch-up

The roadmap's "Recommended next" item: `AIController.cs`'s Support role
still just weaved in X with no awareness of the boss, allies, or screen
space, unlike Tank (guard-point) and Medic (hang-back + approach-hurt-ally)
from Sessions 12-13. `docs/systems/boss.md`'s "Future work" section already
had a decided design for Support (agreed 2026-08-20, never implemented),
bundling two things together: AI positioning ("roams the available screen
freely rather than holding a zone") and combat stats ("the same fire
cadence as Attacker, the same fire damage as Tank"). Confirmed with the
user upfront to implement both halves in this session, not positioning
only.

### Positioning: random-waypoint wander, not a biased point

Tank/Medic's existing `BiasedPositionDirection()` steers toward a point
derived from the ally center and the boss's position, then holds there —
wrong shape for Support, which has no "zone" at all by design. Instead,
`AIController.cs` got a new `WanderDirection()`: steers toward a private
`roamTarget`, picking a new random point (`RandomRoamPoint()`, uniformly
sampled within the same viewport bounds `PlayerController.HandleMovement()`
already clamps to, reusing its public `screenPadding` field rather than
duplicating the inset constant) whenever the current one is reached (within
`roamDeadzone`, 0.3) or after `roamInterval` (3s) elapses, whichever comes
first. Deliberately does **not** return `Vector2.zero` inside the deadzone
like `ApproachDirection()`/`BiasedPositionDirection()` do — those correctly
hold position once arrived (Tank's guard point, Medic hanging back), but
Support should keep moving continuously, so arriving immediately triggers
picking the next point instead.

Added a `case PlayerRole.Support:` to `AIController.Update()`'s movement
switch, previously grouped under the shared `default:` with Attacker — the
`default:` case (and its comment) now covers Attacker only, the last role
still on the original sine-weave.

No new-field scene-wiring gotcha applied here, unlike most of this
project's prior tuning passes: `roamDeadzone`/`roamInterval` are brand-new
fields, not edits to already-serialized existing ones, so every
`Teammate_*` instance picked up the script defaults automatically with no
per-instance override needed.

### Stats: a new `damageMultiplier`, and a side effect on Tank

"The same fire damage as Tank" turned out to require more than a lookup
change: **no role had ever had elevated fire damage** — `PlayerController.Fire()`
hardcoded `SpawnBullet(1f, 0.6f)` for every role alike (the `0.6` itself was
a flat 40% cut applied uniformly in Session 12's tuning, not a per-role
value). Giving Support "Tank's damage" meant introducing a new
`RoleStats.damageMultiplier` stat and deciding what Tank's own value should
be, not just Support's — a small balance change to Tank as a side effect
of implementing Support's design faithfully, flagged to the user rather
than silently expanded scope. Picked `1.5x` for both (round placeholder,
tunable like every other not-yet-playtested balance value in this
project) — Attacker/Medic stay at the `1.0x` baseline, since Attacker's
high damage already comes from Big Shot, untouched by this stat.

Implementation followed the existing `moveSpeed`/`fireRate` pattern
exactly: new `PlayerController.fireDamage` field (base `0.6`), multiplied
by `Stats.damageMultiplier` once in `Start()` alongside the existing two
multiplications, then `Fire()`'s hardcoded literal became `SpawnBullet(1f,
fireDamage)`. Also bumped Support's `fireRateMultiplier` `1.0` → `0.75` to
match Attacker's cadence, completing the decided design. Tank's
`fireRateMultiplier` (1.2, slower) was deliberately left unchanged — only
the fire-damage side of Tank's stats was part of Support's design, not its
cadence.

### Verified

Unity MCP bridge, Play mode. Read all 4 ships' live `fireRate`/`fireDamage`:
Support showed `fireDamage = 0.9` (`0.6 × 1.5`, matching Tank, which also
read `0.9`) and a `fireRate` consistent with its buffed state at the moment
of sampling (Support's own buff ability multiplies `fireRate` further while
active — confirmed this was the AI's buff having already auto-fired, not a
bug, by cross-checking the math); Attacker/Medic stayed at `fireDamage =
0.6`, unchanged. `FindObjectsByType<Bullet>` confirmed live bullets in
flight carried `damage == 0.9` for Support/Tank and `0.6` for
Attacker/Medic (boss/enemy bullets, out of scope, stayed at their own
unrelated value). Reflection-called `WanderDirection()` on `Teammate_Support`
directly: returned a normalized direction with a non-trivial Y component,
and `roamTarget` landed within viewport bounds. Sampled its transform
position over several pumped frames (screenshot-forced frame-stepping, same
technique as every prior session) and confirmed both X and Y changed
over time, cross-checked against `Teammate_Tank`/`Teammate_Medic` (both
still moved in both axes as before, confirming their code paths were
unaffected by the new `case PlayerRole.Support` branch). No console errors
or warnings throughout.

### Still open

- Attacker AI positioning, bullet-dodging, teammate separation, manual
  teammate-ability triggering — unchanged, still not built. Attacker is now
  the only role without real AI positioning.
- Support's shield multiplier is still the placeholder `1.0x` baseline —
  only its fire-rate/damage were part of the decided design implemented
  this session; shield was never specified for it.

## Session 16 — Fixed Per-Role Stats + Ability Rework

User feedback after reviewing Session 15's multiplier-based stats: managing
health/shield/fire-rate/damage as `base × role multiplier` (e.g. Tank
health `5 × 1.6`) was confusing to reason about and hand-tune, especially
with fire rate stored *inverted* (`fireRate` meant seconds between shots —
lower was faster — despite reading like a rate). Requested a clear
single source of truth instead: fixed, absolute values per role, with
multipliers reserved strictly for temporary buffs/abilities, applied
non-destructively rather than mutated into a field and divided back out
later (the exact mechanism that made the old Support buff need
`buffCooldown ≥ buffDuration` to avoid double-applying).

### Architecture: `RoleStats` becomes fixed values

`PlayerRole.cs`'s `RoleStats` struct dropped every multiplier field
(`healthMultiplier`, `shieldMultiplier`, `fireRateMultiplier`,
`damageMultiplier`, `moveSpeedMultiplier`) in favor of direct values
(`maxHealth`, `maxShield`, `fireDamage`, `shotsPerSecond`, `moveSpeed`).
`PlayerHealth.Awake()`/`PlayerController.Start()` now just assign these
straight from `Stats`, no multiplication, no `Mathf.RoundToInt` needed
(the user's given numbers were already whole where it mattered).
`PlayerController.fireRate` was renamed `shotsPerSecond` and its meaning
flipped to match — higher is now faster, matching how the user specified
the design ("2.5 bullets/second") rather than the old inverted-interval
field. Final table (all user-specified, not derived):

| Role     | Health | Shield | Fire damage | Fire rate | Move speed |
| -------- | ------ | ------ | ------------ | --------- | ---------- |
| Attacker | 6      | 5      | 2.0          | 2.5/s     | 3.0 u/s    |
| Tank     | 8      | 20     | 1.0          | 1/s       | 1.5 u/s    |
| Medic    | 4      | 3      | 0.7          | 1.5/s     | 3.0 u/s    |
| Support  | 5      | 3      | 1.0          | 2/s       | 4.5 u/s    |

**Sanity-checked the numbers before implementing**: flagged that Attacker
ends up with both the highest DPS (damage × rate = 5.0/s, 2.5–5x every
other role) *and* the second-best survivability (health + shield = 11,
ahead of Support's 8 and Medic's 7) — a real shift from the role's
original "glass cannon" framing (health used to be Attacker's *lowest*
stat). Noted as worth confirming deliberate, not blocking — user's numbers
were used as given.

### Non-destructive buff layer

`PlayerController` gained two runtime-only fields, `speedBuffMultiplier`/
`fireRateBuffMultiplier` (both default `1f`), read at the point of use —
`HandleMovement()`'s move vector, and a computed `FireInterval => 1f /
(shotsPerSecond * fireRateBuffMultiplier)` for the fire-cooldown gate —
rather than ever being multiplied into `moveSpeed`/`shotsPerSecond`
themselves. Only `PlayerAbility` sets them (Support's redesigned ability,
below), always via plain assignment. This eliminates the old buff's
revert-by-dividing-back-out entirely — there's no arithmetic to get wrong,
so the "cooldown must stay ≥ duration" constraint that applied to every
prior buff/boost in this project (Support's old buff, Medic's aura boost)
no longer applies to Support's ability at all.

### Bullet.cs — one-line fix enabling Tank's new mechanic

`Bullet.cs`'s enemy-bullet-vs-`Player` branch changed
`other.GetComponent<PlayerHealth>()` → `other.GetComponentInParent<PlayerHealth>()`
— a one-line, backward-compatible change (a ship's own collider still
resolves to its own `PlayerHealth` exactly as before) that lets a *child*
collider without its own `PlayerHealth` route a hit to its parent ship's
health pool. Existed specifically to make Tank's Shield Arc (below)
possible; without it, a bullet touching the arc would have been destroyed
but dealt no damage — a "free" block, not what shield-draining absorption
should feel like.

### Four ability changes, requested alongside the stats overhaul

- **Attacker — Big Shot**: damage changed from a separately hand-tuned
  flat number (`1.8`) to a live `2x` multiplier of the caster's *current*
  `fireDamage` (`bigShotDamageMultiplier`), computed at cast time — `2.0 ×
  2 = 4.0` at today's values. Stays proportional automatically if
  `fireDamage` is ever retuned again, rather than needing a second manual
  update.
- **Support — Speed Boost** (renamed from "Buff", fully redesigned):
  became **party-wide** instead of self-only — `TriggerSpeedBoost()` loops
  over `allies[]` (all 4 ships, the same array Medic's aura already uses)
  setting each ally's `speedBuffMultiplier`/`fireRateBuffMultiplier` to
  `speedBoostMultiplier` (1.5, one shared value for both stats now,
  replacing the old two separate move-speed/fire-rate multipliers) for
  `speedBoostDuration` (4s). Cooldown bumped `8s → 15s` — flagged
  overpowered once it started affecting the whole party, round
  placeholder. New party-wide visual: every ship (any role, not just
  Support — built unconditionally, since any of the 4 could receive the
  boost) got an initially-hidden `PartyBuffRing`, toggled via a new
  `SetPartyBuffVisual(bool, Color)` call in the same `allies[]` loop — all
  4 rings light up in the caster's tint (Support's gold) together and
  disappear together, giving the buff a clear, readable tell instead of
  just feeling arbitrarily strong.
- **Medic — Aura Boost radius**: halved, `3 → 1.5` — flagged overpowered
  at the original size. Nothing else about the aura changed.
- **Tank — Shield Arc** (new mechanic, not an `E`-triggered ability —
  passive and always-on, independent of Taunt): a wide, curved shield in
  front of Tank, both visual and **functionally blocking**. Built
  procedurally in `PlayerAbility.Awake()` only for `role == Tank` (same
  "only build what this role needs" precedent as Medic's ring): a child
  `ShieldArc` GameObject, tagged `Player`, with a local-space
  `EdgeCollider2D` (`isTrigger`) and matching `LineRenderer` sampling a
  shallow parabola, `shieldArcWidthMultiplier` (3x Tank's own collider
  width, read live from `BoxCollider2D.bounds.size.x`) wide. Local-space
  and built once — unlike Medic's ring (which resizes on boost and needs
  per-frame updates), the arc never changes shape, so it needs **no
  `Update()` at all**; being a child of Tank's transform, it tracks
  Tank's movement automatically. Relies on the `Bullet.cs` fix above to
  route absorbed hits into Tank's own shield/health, not a free block.
  **Known edge case, flagged not solved**: if the arc's collider region
  vertically overlaps Tank's own body collider, a bullet could in rare
  cases enter both in one physics step and double-hit — mitigated by the
  arc's Y-offset placing it above the body, not defended against with
  extra code, matching this project's established "flag it, don't
  over-engineer for a rare edge case" style.

### Boss HP tuning

`Boss.maxHealth` ×1.5'd (`60 → 90`), purely to give this larger rework
enough runway in a full playthrough to actually be observed, rather than
the fight ending before the new stats/abilities' effects are visible.

### Gotcha, hit twice (same class as every prior tuning pass)

Changing a script *default* doesn't retroactively update an
already-serialized value. `Boss.maxHealth` (60 on the live scene instance
and `Boss.prefab`) and `PlayerAbility.auraBoostRadius` (3 on all 4 ships
except, unexplainedly, `Teammate_Tank` which already read `1.5` — never
fully root-caused, possibly a quirk of the field having been added to
`PlayerAbility.cs` after `Teammate.prefab`'s initial save, but the fix
(explicitly setting all 4 instances plus `Teammate.prefab`'s and
`Boss.prefab`'s defaults, verified via a full scene reload) is correct
regardless of the exact cause) both needed the same explicit-set-on-every-instance
treatment as every prior HP/damage tuning session. Every genuinely *new*
field this pass (`speedBuffMultiplier`/`fireRateBuffMultiplier`, the
Shield Arc's fields, the party-buff ring's fields) did **not** hit this —
new fields just pick up the script default, since there's no prior
serialized value to conflict with.

### Verified

Unity MCP bridge, Play mode. Read all 4 ships' live `maxHealth`/
`maxShield`/`fireDamage`/`shotsPerSecond`/`moveSpeed` right after
`Awake()`/`Start()`: matched the table above exactly for every role, no
rounding drift. Triggered Big Shot via reflection: spawned bullet carried
`damage == 4.0`, bullet width 3x normal. Confirmed Support's Speed Boost
(already auto-fired by the AI within the first frames of Play, expected
behavior) set all 4 ships' `speedBuffMultiplier`/`fireRateBuffMultiplier`
to `1.5` and activated all 4 party-buff rings in Support's gold tint.
**Tank's Shield Arc verified functionally, not just structurally**:
inspected the arc's `EdgeCollider2D` points (spanned ±0.9 local, matching
Tank's `0.6`-wide body × the `3x` multiplier, with the correct parabola
shape); spawned a fake enemy bullet positioned within the arc's width but
outside Tank's own body collider, pumped physics frames, and confirmed the
bullet was destroyed **and** Tank's `CurrentShield` dropped by the
bullet's exact damage (`20 → 18` for a 2-damage bullet) — the critical
check that the `Bullet.cs` fix actually routes the hit to Tank's own
health pool rather than silently no-oping. A same-position player-owned
bullet was confirmed to pass through untouched, Tank's health/shield
unchanged — no friendly-fire interaction. `Boss.maxHealth`/`CurrentHealth`
confirmed `90/90` after a full scene reload from disk. No console errors
or warnings throughout.

**Testing note**: hit the well-documented Editor-idle `Time.time` jump
quirk again mid-session (a gap between tool calls let real time jump to
`Time.time = 62s`, during which the human `Player` died from ongoing boss
fire and a test bullet's `lifeTime` naturally expired, initially looking
like a collision bug before the cause was traced) — resolved by keeping
the friendly-fire re-test's calls tight together and giving the test
bullet a long `lifeTime` override, consistent with every prior session's
handling of this same quirk.

### Still open

- Attacker AI positioning, bullet-dodging, teammate separation, manual
  teammate-ability triggering — unchanged, still not built.
- The Attacker survivability/DPS balance question flagged during design
  (highest damage *and* second-best survivability) wasn't revisited after
  the user confirmed the given numbers — worth another look once real
  playtesting happens.
- Every role's shield value is now a deliberately-chosen fixed number
  (no more "undecided 1.0x placeholder" framing), but all values across
  the board remain placeholder/tunable pending real playtesting, same as
  every prior balance pass in this project.

## Session 17 — Attacker AI Positioning (hybrid patrol + boss-tracking)

The roadmap's explicitly recommended next item: finish AI teammate
positioning by giving Attacker its own behavior — Tank, Medic, and Support
all already had it (Sessions 12/13/15); Attacker was still on the original
prototype-era placeholder, a pure X-only sine weave with zero boss/ally
awareness.

### Design revision, mid-conversation

`docs/systems/boss.md` already had a "decided design" for this dated
2026-08-20 (the previous session): patrol to cover the available screen
width for spread/DPS coverage, staying clear of the boss and the top edge.
Discussing the actual implementation surfaced a mechanical problem with
that plan before any code was written: ships never rotate and bullets only
ever fire straight up (`Vector2.up`, no homing, see `Bullet.cs`) — an
Attacker patrolling a fixed, boss-independent center would frequently drift
out of the boss's current lane as it sine-drifts, and just miss regardless
of how good its coverage looked. The user proposed tracking the boss's X
directly instead, holding a balanced mid-distance (not Tank-close, not
Medic-far). Resolved as a **hybrid**, the user's choice among three
options offered: keep the independent side-to-side patrol motion for
spread/coverage/visual variety, but anchor its *center* to the boss's live
X instead of a fixed point. This supersedes the prior session's decided
design outright — `boss.md` was updated to match, not left describing the
old plan alongside the new code.

The other half of the original ask — "fire the ability the instant it's
ready" — turned out to already be exactly how Attacker's
`TryUseAbility()` heuristic worked (`AIController`'s ability-triggering
switch already retries every frame for Attacker, relying on
`PlayerAbility`'s own cooldown gate). No code change was needed there —
confirmed by reading the existing switch before writing anything new,
avoiding a redundant "fix" for something that wasn't broken.

### New code: `AIController.AttackerPositionDirection()`

Same "compute a target point, seek it, zero inside a deadzone" shape
already used by `BiasedPositionDirection()`/`ApproachDirection()`, so it
reads as one more case in the same family rather than a bespoke one-off:

- `targetY`: `Mathf.LerpUnclamped(GetAllyCenter().y, boss.transform.position.y, attackerBias)`
  — the same ally-center/boss blend Tank and Medic use, applied to Y only.
  New field `attackerBias` (0.45) sits between Medic's `-0.3` and Tank's
  `0.65`. Since the boss sits near the top of the screen (world Y fixed at
  `4.2`) and ally center is naturally lower/mid-screen, this blend
  incidentally keeps Attacker clear of the top edge too — satisfying that
  part of the original design intent without a dedicated check.
- `targetX`: `boss.transform.position.x + Mathf.Sin(Time.time * weaveFrequency) * attackerPatrolAmplitude`
  — patrols around the boss's *current* X rather than an independent
  center, reusing the existing `weaveFrequency` field instead of adding a
  second oscillation-speed constant. New field `attackerPatrolAmplitude`
  (1.5) controls the swing width.
- Returns the normalized direction to `(targetX, targetY)`, or
  `Vector2.zero` inside new field `attackerDeadzone` (0.2, matching
  `guardDeadzone`'s default).

`Update()`'s movement switch gained an explicit `case PlayerRole.Attacker:`
(previously Attacker fell through to `default`); `default` now stays only
as a dead safety fallback for any future unhandled role, with the original
weave code left there unused.

**Small refactor alongside**: the ally-center averaging loop, previously
inlined only inside `BiasedPositionDirection()`, was extracted into a
shared private `GetAllyCenter()` so `AttackerPositionDirection()` doesn't
duplicate the same liveness-filtered average a second time —
`BiasedPositionDirection()` now calls it too, no behavior change. Same
"extract instead of duplicate" precedent as Session 13 generalizing
`GuardPointDirection()` into `BiasedPositionDirection()` itself.

### Verification

Reimported/compiled via the Unity MCP bridge — no console errors. New
fields, being brand-new rather than edits to already-serialized ones,
picked up their script defaults automatically on all three `Teammate_*`
instances (including the `Teammate_Tank` prefab instance) with no
prefab-instance-override gotcha, confirmed by reading them back live.

Since the default scene has the human `Player` on Attacker (per
`current-state.md`'s testing instructions, no AI teammate normally plays
it), temporarily reassigned `Player` → Support and `Teammate_Support` →
Attacker in Edit mode so an AI teammate actually exercised the new code
path, entered Play mode, and sampled `Teammate_Support`'s position against
`Boss.transform.position.x` over several pumped frames (same
screenshot-forces-a-frame-step technique as every prior session). X stayed
within `attackerPatrolAmplitude` of the boss's live X throughout rather
than drifting to an independent center; Y climbed from near the back of
the party toward the mid-distance blend as expected. The boss was actually
defeated mid-test (~18s of continuous 4-ship fire, Attacker contributing
real DPS the whole time), with zero console errors/warnings across the
whole fight. Reverted the temporary role reassignment afterward and
confirmed via a full scene reload from disk (the established habit for
this class of change) that `Player` = Attacker was restored correctly.

**Degenerate case observed, not a new bug**: once Tank and Medic had both
died mid-test, `GetAllyCenter()`'s existing "fall back to the caller's own
position when no allies are alive" behavior (shared by Tank/Medic already)
meant Attacker's Y target kept re-lerping from its own just-updated
position toward the boss's Y each frame, asymptotically converging onto
the boss's height rather than holding a mid-distance stand-off. Only
matters in the "down to one or two teammates" endgame, not normal play;
documented in `boss.md` rather than treated as something to fix this
session, since it's inherited from a pattern already accepted for Tank and
Medic.

### Docs updated

`boss.md` (new "Attacker patrol + boss-tracking positioning" subsection,
replacing the superseded "patrol screen width" design note; "Future work"
trimmed since Attacker positioning is no longer open), `roadmap.md`
(Attacker item moved from "Planned" to "Implemented"; "Boss combat
dynamism" now explicitly the recommended-next item), `current-state.md`
(boss-encounter bullet and "How to test it" step 5 updated to describe the
new behavior), `player-roles.md` (Attacker positioning removed from the
"not yet implemented" list).

### Still open

- Bullet-dodging, teammate separation, manual teammate-ability triggering
  — unchanged, still not built (see `boss.md`'s "Future work" and "Manual
  teammate ability triggering").
- Boss combat dynamism (static movement, flat-timer attacks) — now the
  explicitly recommended next item, see `roadmap.md`.
- The Y-convergence-onto-boss degenerate case (above) when few AI allies
  remain alive — not fixed, just documented.

## Session 18 — Role Select Scene + Victory Screen

Testing the 4-role AI behavior (Sessions 12-17) required hand-editing
`PlayerRoleComponent.role` in the Inspector on `Player`, plus swapping
whichever `Teammate_*` currently held that role — slow and error-prone, and
a blocker on iterating the boss-dynamism work recommended next. Requested
out of band from the roadmap's stated build order (which had "Scene
scaffolding" deferred until right before Nakama networking), because fast
role-switching was needed now for testing, not because the full scaffolding
timeline moved up.

### Design: a real second scene, not a same-scene overlay

Considered both a same-scene overlay panel (matching the existing
`GameOverPanel` pattern, avoiding a second scene per Session 5's precedent)
and a real separate `RoleSelect.unity` scene. **User explicitly chose the
real scene** — Role Select was always going to become a real scene per the
roadmap's deferred "Scene scaffolding" item; this just builds it earlier
than that item's original timeline, for testing purposes specifically (Main
Menu and Lobby remain unbuilt).

### The core technical problem: role has to be set before Awake()

`PlayerRoleComponent.Awake()` (tints the sprite), `PlayerHealth.Awake()`
(sets `maxHealth`/`maxShield`), and `PlayerAbility.Awake()` (builds Medic's
aura ring / Tank's shield arc — **structural**, only happens once) each
read `role`/`Stats` exactly once, at their own startup, and never re-react
to a later change. Unity doesn't guarantee `Awake()` order between
different GameObjects' default-order scripts. Fixed with
`[DefaultExecutionOrder(-1000)]` on a new bootstrap script — a first for
this project — guaranteed to run before every default-order script's
`Awake()`.

### New scripts

- `PartyRoleAssignment.cs` — static class, `PlayerRole? HumanRole`, carries
  the human's pick from `RoleSelect` into `Gameplay` across
  `SceneManager.LoadScene` (survives within a Play session, resets to
  `null` on domain reload). Same "static table over extra infra" precedent
  as `PlayerRoleStats` (Session 4).
- `RoleSelectUI.cs` — 4 role buttons, non-interactable Start button until a
  role's picked, `StartGame()` sets the static and loads `Gameplay`.
- `PartySetupBootstrap.cs` — the `DefaultExecutionOrder(-1000)` script, on a
  new `PartySetup` GameObject in `Gameplay`. Assigns the human's role to
  `Player`, then the 3 remaining `PlayerRole` values (enum declaration
  order, skipping the human's pick) to `Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support` — covers all 4 roles exactly once by construction. If
  `PartyRoleAssignment.HumanRole` is null (scene opened directly), no-ops,
  preserving the original Inspector-testing workflow.
- `VictoryUI.cs` — mirrors `GameOverUI.cs` exactly. `Show()` wired as a
  **second** listener on `Boss.OnDefeated` (`OnDefeated` already supported
  multiple listeners, same as `OnDamaged` — no `Boss.cs` change needed).
  `PlayAgain()` reloads `Gameplay` (roles preserved); `ChangeRoles()`
  loads `RoleSelect`.
- `GameOverUI.cs` — added `ChangeRoles()` + a new button; existing
  `Restart()` needed no change, since it now doubles as "play again, same
  party" for free (it just reloads the scene, and `PartyRoleAssignment` is
  never cleared by that path).

### Bug caught during verification: prefab-instance listener didn't persist

Wired `VictoryUI.Show` onto `Boss.OnDefeated` via `execute_code` +
`EditorUtility.SetDirty()`, same technique as every prior UnityEvent wiring
in this project. `GetPersistentEventCount()` read back `2` immediately, and
the scene saved successfully — but in Play mode, only `BossPanelUI.ShowDefeated()`
fired; `VictoryPanel` never appeared. Root cause: `Boss` is a `Boss.prefab`
instance (confirmed via `PrefabUtility.GetPrefabInstanceStatus`) — exactly
the Session 12/13 gotcha (an instance-level override on an existing
component needs `PrefabUtility.RecordPrefabInstancePropertyModifications()`
in addition to `SetDirty()`, or it silently doesn't serialize), just hit
against a UnityEvent listener list instead of a plain field this time.
Caught only because verification followed this project's established habit
of forcing a full scene reload from disk rather than trusting the
in-memory `GetPersistentEventCount()` read — which had reported the
correct count the whole time, since the in-memory mutation was real, just
not persisted. Fixed by calling `RecordPrefabInstancePropertyModifications(boss)`
after re-adding the listener; re-verified via a full disk reload before
moving on. `GameOverPanel`'s own new "Change Roles" button needed no such
fix, since `GameOverPanel` is a plain scene object, not a prefab instance.

### Editor-idle quirk reconfirmed, this time on Play-mode transition itself

Immediately after entering Play mode (Editor unfocused), one specific
teammate's `PlayerRoleComponent.Awake()` sprite tint read as white
(default) instead of its correct role color, even though the *same*
GameObject's `PlayerHealth.Awake()` had already read the correct
post-bootstrap stats. `editor/state` showed `play_mode.is_changing: true`
and a stalled `playmode_transition` phase (not advancing across repeated
reads while unfocused). Not a bug in the new code — a `manage_camera`
screenshot (forces one manual frame step, the project's established
technique since Session 6) let the transition complete, after which all 4
ships' tints read correctly and reproducibly. Reconfirms the Session 6/8/9/
10/12 finding generalizes to the Play-mode transition itself, not just
in-Play `Update()`/coroutines.

### Verification

All via the Unity MCP bridge, mirroring this project's established
technique (forced `Boss.TakeDamage(9999f, null)`/`PlayerHealth.TakeDamage(999)`
instead of waiting out real combat, screenshot-forced frame steps,
full-disk-reload checks for anything prefab/serialization-adjacent): role
assignment verified correct for 3 different human picks (Medic, Tank,
Support) across separate Play sessions, including the structural checks
(aura ring / shield arc present on whichever ship actually landed that
role); Victory panel appears on a forced boss kill with both buttons
working; Game Over's Change Roles button works; "Play Again" preserves the
exact prior role assignment across a reload via both the Victory path and
the Game Over Restart path; opening `Gameplay` directly with no prior
`RoleSelect` visit correctly falls back to the scene's hand-authored
Inspector defaults, confirming `PartyRoleAssignment.HumanRole` resets to
null on domain reload as designed. Zero console errors/warnings across
every phase.

### Docs updated

`roadmap.md` (new "Role Select scene + Victory screen" Implemented item;
"Scene scaffolding" note updated — Role Select shipped early, Main
Menu/Lobby still deferred), `current-state.md` ("What's playable" bullet,
"What's NOT there yet" scene count, "How to test it" steps 1-2 and 6-7
rewritten for the new boot flow), `player-roles.md` (new "Role Select
scene" section, `PlayerRoleComponent`'s scene-wiring table note updated).

### Addendum: gameplay scene renamed `SampleScene` → `Gameplay`

Immediate same-session follow-up: `SampleScene` was a leftover Unity
template name from Session 1, and now that a second scene (`RoleSelect`)
exists alongside it, the generic name read as unfinished rather than
intentional. Renamed via `manage_asset(action:"rename"/"move")` (preserves
the `.meta`/GUID, so Build Settings and all GUID-based references updated
automatically with no broken links) to `Assets/Scenes/Gameplay.unity`. Two
string-literal `SceneManager.LoadScene("SampleScene")` call sites
(`VictoryUI.PlayAgain()`, `RoleSelectUI.StartGame()`) needed a matching
code fix, plus a full docs sweep. **Historical session entries above
(1-17) keep saying `SampleScene`, deliberately** — they're an accurate
record of what the scene was actually called at the time; only this
session's own references and the forward-looking docs (`roadmap.md`,
`current-state.md`, `systems/*.md`) were updated to `Gameplay`. Re-verified
the full Role Select → Gameplay → Victory → Play Again loop end-to-end
post-rename via the Unity MCP bridge; zero console errors.

### Addendum: `RoleSelect` was missing a Camera

Playtesting surfaced Unity's "Display 1 No cameras rendering" diagnostic
text over the role-picker screen. Root cause: `RoleSelect` was built as a
UI-only scene (Canvas + EventSystem only) on the reasoning that a Screen
Space - Overlay canvas doesn't need a camera reference to render its UI —
true, but Unity's Game view still shows that warning whenever a scene has
**zero** `Camera` components at all, independent of whether any UI actually
needs one. Fixed by adding a plain `Main Camera` (tagged `MainCamera`,
matching this project's stated convention that `Camera.main` requires that
tag — see `progress-log.md` Session 1's troubleshooting notes), background
color set to match the dark HUD panel tone (`RGBA(0.05, 0.05, 0.08, 1)`) so
it's consistent even where the UI doesn't fully cover the screen. Verified
via screenshot in Play mode: warning gone, no console errors.

### Still open

- Boss combat dynamism — still the recommended next item, unchanged by this
  session.
- Main Menu / Lobby scenes — still not built; `RoleSelect` is a standalone
  picker, not yet part of a Main Menu flow.
- Bullet-dodging, teammate separation, manual teammate-ability triggering —
  unchanged, still not built.

## Session 19 — Boss Combat Dynamism (Erratic Movement, Body Hazard, Guided Missile, Shockwave)

The roadmap's long-recommended next item, requested directly this session
with four concrete mechanics: erratic movement bounded to a limited advance
toward the ships, a damaging body hitbox, homing bullets that call out a
specific role (with a HUD warning), and a close-range shockwave. Full
technical write-up: `systems/boss.md`.

### Clarifying two genuine forks before writing any code

Two requests were ambiguous in a way that would have meant rewriting core
systems if guessed wrong, so both were confirmed with the user before
implementation:

- **"2/5 of the screen" for boss movement** — could have meant a horizontal
  roam cap or a vertical advance-toward-the-ships limit. Confirmed:
  vertical. The boss's erratic left/right dashing is a separate, roughly
  full-width behavior; the 2/5 fraction only bounds how far down (toward the
  ships) it can push from its home row.
- **"Guided bullets aiming the medic or attacker"** — could have meant a
  bullet aimed at the target's position at fire time (straight line
  afterward, fully Tank-blockable, no `Bullet.cs` changes) or true homing
  (continuously re-aims in flight). Confirmed: true homing, at a capped turn
  rate so it stays dodgeable. This knowingly loosens (not breaks) Tank's
  straight-line-blocking guarantee — already flagged as an open question in
  `systems/boss.md`'s "Future work" from an earlier session, now a real,
  confirmed trade-off rather than a hypothetical one.

Also confirmed: geometric bullet-pattern variety is explicitly deferred to
a later pass, not bundled into this one — added as its own item under
`roadmap.md`'s "Player-vs-boss dynamics" rather than silently dropped.

### World-unit research before committing to numbers

Since the user was explicit that they didn't know what unit scale the
project uses, three parallel research passes (boss code + camera/viewport
math, AI positioning + player health/collision, bullet system + HUD panel)
established concrete numbers before any code was written: the playable
viewport is **5.625 × 10 units** (orthographic size 5, forced 9:16 via
`AspectRatioFitter`), ship collider footprint is **0.6 × 0.6**, the boss is
**1.6 × 1.6** with a non-trigger `BoxCollider2D` (ship colliders are
triggers, so Unity fires `OnTriggerEnter2D`/`OnTriggerStay2D` on **both**
sides on overlap — confirmed this meant body-contact damage needed no new
collider). This turned "1.5 ships around the boss" into a concrete
`shockwaveRadius` of 1.7 (boss half-extent 0.8 + 1.5 ship-widths 0.9) and
"2/5 of the screen" into a concrete vertical clamp, both stated as reasoning
in the plan rather than picked blind.

### Implementation

- **`Boss.cs`** — replaced the `Update()` sine-drift block with a
  dash-or-hold decision every `dashDecisionInterval` (1.5s,
  `dashProbability` 0.35), clamped by a new `ClampToBounds()` (X via the
  same `ViewportToWorldPoint`/`screenPadding` idiom
  `PlayerController.HandleMovement()` already uses; Y clamped to
  `[homeY - maxAdvanceFraction * viewportHeight, homeY]`). Added a new
  `bulletDamage` field (1f) making the boss's own bullet damage explicit —
  previously an implicit default from `Bullet.damage`, since `SpawnBullet()`
  never set it — as the single source of truth the two new damage
  mechanics multiply against. Body contact: `OnTriggerStay2D` on `Boss.cs`
  itself (reusing its existing solid collider), per-target cooldown-gated,
  `2x bulletDamage`. Shockwave: `CheckShockwave()`/`ShockwaveRoutine()`,
  telegraphed, `3x bulletDamage` plus knockback via the *existing*
  `PlayerController.AddRecoil()` (built for Attacker's Big Shot) rather than
  a new knockback mechanism. Guided missile:
  `CheckGuidedMissile()`/`GuidedMissileRoutine()` picks a random active
  Medic/Attacker, sets a new public `GuidedMissileTargetRole` property
  immediately (during the telegraph, not just during flight, so Tank gets
  real reaction time), fires via `Bullet.InitHoming()`, holds the property a
  couple seconds into flight before clearing.
- **`Bullet.cs`** — added `InitHoming(...)` as an alternate init path
  alongside the untouched existing `Init(...)`, so every straight-line
  bullet (player and enemy) is unaffected. Re-aims `direction` each frame
  toward the target's live position via `Vector3.RotateTowards` (**hit a
  real compile error here** — `Vector2.RotateTowards` doesn't exist, only
  `Vector3`'s overload does; Unity implicitly converts between the two, so
  the fix was a one-line type change, caught immediately by
  `refresh_unity`'s compile step), capped by a turn rate so it's dodgeable.
- **`AIController.cs`** — new `minDistanceFromBoss` (1.9, just outside the
  shockwave radius) and a new `EnforceBossDistance()` helper, applied to
  `BiasedPositionDirection()` (Tank/Medic), `AttackerPositionDirection()`,
  and `RandomRoamPoint()` (Support) — all four roles' default positioning
  now has a floor distance from the boss. This incidentally fixed the
  already-documented `GetAllyCenter()` collapse-toward-boss degenerate case
  from an earlier session, for free.
- **`BossPanelUI.cs`** — new `warningText` field, polled the same "HUD
  reads, never owns state" way as the existing health/phase/target text.

### Scene wiring

New `BossWarningText` child added under `BossPanel` via the Unity MCP
bridge, duplicated from `BossTargetText` as a styling template (same
approach `AbilityText` used on `PartyFrame.prefab` in Session 8), wired to
`BossPanelUI.warningText`, verified via a full scene reload from disk. Every
other new field is either a fresh script default (no prefab-instance gotcha
— confirmed on all 4 ships/`Teammate_Tank`'s prefab instance) or computed at
runtime, so none of the prior sessions' `RecordPrefabInstancePropertyModifications()`
gotcha applied this time.

### Testing notes

Unlike the "Editor doesn't tick Play-mode `Update()` while unfocused" quirk
documented in Sessions 6-10, **this session's Editor instance ticked Play
mode in real time on its own** between tool calls — `Time.time` advanced
freely without needing screenshot-forced frame steps. This cut both ways:
made end-to-end coroutine testing (shockwave, guided missile) much easier
once discovered, but also meant an unattended party (no human input driving
`Player`) died for real to the now-harder boss partway through testing —
not a bug, just the fight actually being harder now, confirmed via a clean
Play-mode restart. Where a coroutine still needed a real-time wait to
resume (`WaitForSeconds`), telegraph/linger fields were temporarily lowered
via direct field writes for the test only (same technique as Session 7's
temporary `buffDuration` shortening) — confirmed discarded automatically on
Stop, restoring the real serialized defaults.

Verified via the Unity MCP bridge: reflection-driven stress test of
`HandleMovement()` (300 forced decisions, bypassing the `Time.time` gate)
matched the configured dash probability and stayed within clamp bounds
every time; body contact damage confirmed exact math and cooldown gating
via both a direct `OnTriggerStay2D` call and real physics-driven overlap
(the latter correctly killed an overexposed test ship over repeated ticks);
shockwave confirmed exact math, telegraph, and knockback, and was observed
combining correctly with a simultaneous body-contact hit through the shared
shield-first `PlayerHealth.TakeDamage` path; guided missile confirmed
correct target-role restriction, HUD warning timing, and ran to completion
multiple times with zero console errors/warnings across the whole session.

### Follow-up: shockwave had no visible danger zone

Immediate playtest feedback after the above: the shockwave was a complete
surprise — nothing on screen indicated its radius before it hit. Added a
world-space ring at `shockwaveRadius`, built the same procedural
`LineRenderer` way `PlayerAbility.cs`'s Medic aura ring already is (dim and
always visible, brightens/pulses during the telegraph, flashes on impact) —
`CreateShockwaveRing()`/`UpdateShockwaveRing()` on `Boss.cs`, re-centered on
the boss's live position every frame since it now moves erratically.
Confirmed visually via a Play-mode screenshot (also incidentally caught the
guided-missile HUD warning firing live in the same shot). No new gotchas —
straightforward reuse of an already-proven visual pattern.

### Follow-up: shockwave knockback too weak, no cooldown visibility

Immediate playtest feedback after the above two follow-ups: the shockwave's
knockback (`shockwaveKnockback = 6`) was barely noticeable, and `BossPanel`
had no way to see whether the shockwave or guided missile were about to be
available again.

**Knockback math, derived from an existing precedent, not guessed**:
`shockwaveKnockback` is an impulse fed into `PlayerController.AddRecoil()`,
which decays exponentially every `FixedUpdate`
(`recoilDamping` 8, `Fixed Timestep` 0.02, confirmed by reading
`ProjectSettings/TimeManager.asset`). Session 8 already derived and verified
the closed-form total displacement for this exact system (Attacker's Big
Shot recoil: impulse 6 → measured 0.63 units). Re-deriving the same formula
here gave `displacement ≈ impulse × 0.105`, which exactly reproduces
Session 8's number — confirming the formula still holds rather than
assuming it does. This turned "how far should the wave push ships" into a
concrete question answerable in world units/ship-widths: the user was asked
to pick a target displacement with the actual math shown (playable area is
5.625 × 10 units, a ship is 0.6 × 0.6), and chose "very strong" (~3.5 units,
~5.8 ship-widths) — `shockwaveKnockback` raised `6 → 33`.

`Boss.cs` gained two pure derived-getter properties
(`ShockwaveCooldownRemaining`, `GuidedMissileCooldownRemaining`) off
already-existing private timer fields, no new state; `BossPanelUI.cs`
polls them the same way as every other boss stat. Body contact damage's
cooldown was deliberately left off `BossPanel` — it's per-target/reactive,
not a single global cooldown like the other two, so it doesn't fit one HUD
line the same way.

**Testing hit the "already-serialized value doesn't pick up a new script
default" gotcha again** (same class of issue as Sessions 11/12/16): after
compiling, the *live scene instance's* `shockwaveKnockback` still read `6`
even though the script default was now `33`, since it had been explicitly
serialized at `6` in an earlier session. Fixed by setting it explicitly on
both the scene instance and `Boss.prefab`'s default, verified via a full
scene reload from disk.

**Testing hit real noise from this session's free-running Play mode**: this
Editor instance ticked Play mode continuously in the background between
tool calls (same as observed at the end of the original Session 19 pass),
which repeatedly wiped the unattended AI-only party mid-test and made a
naive before/after position comparison too noisy to trust (AI healing,
wandering, and the boss's own erratic movement all overwrote the signal
within a few real seconds). Switched to a deterministic, instantaneous
check instead: read `PlayerController`'s private `recoilVelocity` field via
reflection immediately after manually calling `AddRecoil(pushDir * 33)` —
confirmed it lands at exactly magnitude 33, which combined with the
already-reproduced Session 8 decay formula is sufficient confirmation
without racing the simulation. Cooldown text was confirmed the reliable
way instead: a live screenshot showing `BossPanel` correctly reading
`"Shockwave: Ready"` / `"Guided Missile: 0.7s"` after real combat had
already exercised both.

### Still open

- Rapid-fire burst attack and geometric bullet spread patterns — deferred,
  see `roadmap.md`'s "Player-vs-boss dynamics" (new "Geometric bullet spread
  patterns" item).
- Bullet-dodging, teammate separation, manual teammate-ability triggering —
  unchanged, still not built.
- Main Menu / Lobby scenes — still not built.

## Session 20 — Solid-Body Ship/Boss Collision

Requested directly: ships (human and AI) should have a solid shape that no
other ship can overlap, and the boss's body should be equally solid against
every ship — `AIController.minDistanceFromBoss` only biases AI teammates'
*chosen target point* away from the boss, and nothing at all prevented
ship-vs-ship stacking or a ship physically passing through the boss. Full
technical write-up: `systems/boss.md`'s "Solid-body collision (ships +
boss)".

### Design conversation before any code

Two genuine forks were talked through with the user before writing
anything, since guessing wrong on either would have meant a rewrite:

- **How to reconcile "prevent overlap" with the boss's existing "touching
  its body deals contact damage" hazard** — hard-preventing overlap means
  Unity's physics engine never actually sees two colliders intersect, so
  the existing `Boss.OnTriggerStay2D` (which relies on genuine overlap)
  would stop firing. The user's own proposed resolution, confirmed as the
  design: since ships/the boss move in small discrete steps every frame
  rather than teleporting, a momentary overlap is unavoidable for a step
  before it's corrected — so the same box-overlap math that computes the
  push-back doubles as the contact-damage detector, replacing reliance on
  Unity's trigger callback with one unified per-ship step.
- **Who gives way when the boss's erratic dash would move it into a ship's
  spot** — considered "boss gets blocked like a wall" (symmetric, but adds
  resolution code to `Boss.cs`'s movement and risks it stalling against a
  parked ship) versus "boss shoves the ship aside" (asymmetric, but needs
  zero changes to `Boss.cs`). The same discrete-step reasoning above
  resolved this for free: since the boss's dash is already incremental
  (`Vector3.MoveTowards` each `Update()`, not a teleport), each ship's own
  next `FixedUpdate` naturally catches and corrects "the boss moved into
  me" the same way it catches any other ship — no boss-side code needed at
  all. Confirmed via the actual `TimeManager.asset` Fixed Timestep (0.02s)
  and the boss's `dashSpeed`/collider sizes that this means at most one
  rendered frame of transient overlap, never a persistent one.

CPU cost of the added per-frame box checks was also raised directly — confirmed negligible (at most ~20 simple AABB comparisons across 4 ships +
the boss per physics tick, the same order of magnitude as the per-frame
distance loops `Boss.cs` already runs for aggro/shockwave/guided-missile
targeting).

### Research and validation before implementation

Two parallel research passes (current physics/collider setup for
ships/boss; docs + progress-log history on AI positioning and boss
hazards) established that **no physics-engine collision response exists
today at all** — every ship moves via `Rigidbody2D.MovePosition()` and the
boss via raw `transform.position` writes, both fully imperative, so
Unity's solver never resolves overlap between any of them regardless of
trigger flags. A Plan agent then validated the concrete implementation
live against the actual scene (not just static file reads): confirmed
exact collider `bounds`/`isTrigger` values, confirmed `PlayerAbility.allies`
(already wired on all 4 ships) and `Boss.targets` (already wired) were
reusable with zero new arrays needed, corrected a stale claim in
`systems/movement.md` (`Player`'s `Collider2D` was documented as `Is
Trigger: OFF`, live value is `ON`), and flagged a rare accepted edge case:
the boss's `screenPadding.x` (0.8) equals its own half-extent, so a ship
pinned in the same corner the boss dashes into could see the viewport
clamp momentarily fight the collision resolution — self-corrects the
instant either body moves, not worth solving given nothing in the game
deliberately drives a ship into that corner (`minDistanceFromBoss` already
keeps default AI positioning well clear of the boss).

### Implementation

- **New `Assets/Scripts/ShipCollisionUtil.cs`** — a plain static class (no
  `MonoBehaviour`, no Inspector wiring), one function:
  `ResolveBoxOverlap(candidateSelfPos, selfHalfExtents, otherPos,
  otherHalfExtents, out wasOverlapping)`. Exact axis-aligned box-vs-box
  minimum-translation-vector push-out along whichever axis has the
  shallower penetration — ships and the boss never rotate (the project's
  established fixed-orientation design), so this is exact, not a circle
  approximation. The `out bool` lets the one ship-vs-boss call site reuse
  the same math as the contact-damage trigger, while ship-vs-ship call
  sites just discard it (`out _`).
- **`PlayerController.cs`** — new `public Boss boss` field (mirrors
  `AIController.boss`, but needed here directly since this script also
  drives the human `Player`, which has no `AIController`). Caches its own
  `BoxCollider2D`/half-extents, `PlayerAbility` (for `.allies`), and the
  boss's collider/half-extents once in `Start()`. New
  `ResolveShipCollisions(Vector2)`, called from `HandleMovement()` between
  computing the raw candidate position and the existing viewport clamp
  (resolve-then-clamp, so a corrected position can never end up pushed
  outside the play area by the correction itself) — resolves against every
  other ship in `ability.allies` (push-apart only), then against `boss` if
  present (push-apart **and**, on overlap, calls the new
  `Boss.ApplyContactDamage(gameObject)`).
- **`Boss.cs`** — removed `OnTriggerStay2D` (no longer reachable once
  overlap is actively prevented) and replaced it with `public void
  ApplyContactDamage(GameObject ship)`, the exact same cooldown-gated math
  (`lastContactDamageTime`/`contactDamageCooldown`/`bulletDamage`/
  `bodyContactDamageMultiplier`) just invoked from `PlayerController`'s
  resolution step instead of a trigger callback. Uses `GetComponent`
  instead of the old `GetComponentInParent`, since the caller always passes
  a ship's own root GameObject now, never a child collider — this is a
  narrow, accepted behavior change: Tank's Shield Arc (a separate child
  trigger collider) could previously trigger contact damage on its own,
  independent of Tank's body box; now only the body box is checked. In
  practice both paths always led to the same cooldown-gated hit on the same
  ship, so this wasn't treated as a balance change worth re-litigating.
  `HandleMovement()`/dash logic needed no changes at all, per the design
  conversation above.

### Scene wiring

Set the new `boss` field on all 4 ships' `PlayerController` in
`Gameplay.unity` via the Unity MCP bridge. Hit the now-familiar
prefab-instance gotcha once more on `Teammate_Tank` (the only one of the 4
ships that's an actual `Teammate.prefab` instance) — needed
`PrefabUtility.RecordPrefabInstancePropertyModifications()` on top of
`SetDirty()`, same as every prior session that touched a field on this
GameObject. Verified by reloading the scene fresh from disk afterward and
re-reading the field on all 4 ships rather than trusting the in-memory
value.

### Verification

All done live via the Unity MCP bridge in Play mode. Hit real friction from
this session's free-running Play mode (same class of issue as Session 19's
tail): an unattended party with ambient boss fire kept killing ships
mid-test between tool-call round trips, including the human `Player`
itself twice — recovered each time by reactivating the GameObject
(`SetActive(true)`, inactive objects aren't found by `GameObject.Find`, so
this needed `Resources.FindObjectsOfTypeAll<Transform>()` instead) and
topping health/shield back up via the existing `Heal`/`RestoreShield`
methods, rather than restarting the whole session and losing test state.
Isolated the push-back-distance test cleanly by temporarily setting
`boss.enabled = false` (stops the boss's `Update()` — movement, firing,
shockwave — without affecting `ApplyContactDamage`, which is a directly
callable public method unrelated to the component's enabled state) so
ambient combat noise didn't contaminate the measurement.

Confirmed: two overlapping ships separate correctly; a ship forced onto the
boss (boss paused, health topped up first) gets pushed back to *exactly*
the combined half-extent distance (measured `1.1`, matching
`playerHalfExtent + bossHalfExtent` to the decimal) and takes contact
damage exactly once; calling `ApplyContactDamage` twice back-to-back in one
`execute_code` call (bypassing physics/frame-pump timing entirely) confirms
the cooldown gate blocks the second hit deterministically; forcing the
boss's position onto a stationary teammate (simulating a dash-into-ship)
resolves with the ship pushed well clear, no persistent overlap. Tank's
Shield Arc (`EdgeCollider2D` child) confirmed still intact and unaffected.
Zero console errors/warnings across the entire session, including an
organic boss defeat from sustained ambient fire mid-test — confirming
`Bullet.cs`'s damage path is completely unaffected by this change.

### Still open

- Bullet-dodging, manual teammate-ability triggering — unchanged, still not
  built (see `roadmap.md`).
- Main Menu / Lobby scenes — still not built.

## Session 21 — Game Over/Victory Race Fix + CPU Party Frame Names

Two independent playtest-reported bugs, fixed in one session since both
were small and unrelated: the Victory panel could pop on top of an
already-showing Game Over panel, and every party frame displayed the
identical hardcoded name `"Player 1"` regardless of which ship it
represented.

### Game Over vs. Victory: mutual exclusion, not boss immortality

The 3 CPU teammates keep fighting after the human `Player` dies (only the
human's own death shows `GameOverPanel`, by existing design — see
`systems/boss.md`'s "Death handling"), so if they go on to defeat the boss,
`Boss.Die()`'s unconditional `OnDefeated` pops `VictoryPanel` on top of the
already-showing `GameOverPanel`.

**Design conversation before writing anything**: the user's first instinct
was to make the boss invulnerable once Game Over fires, so it could never
reach 0 HP afterward. Talked through and rejected in favor of a
mutual-exclusion guard between the two panels instead — an invulnerable
boss would never die or get cleaned up (nothing else destroys it), so the
fight would run forever in the background for zero visible benefit, since
`GameOverPanel` already covers the full screen either way. The user then
added one more requirement while confirming this direction: the guard must
be a genuine no-op, not a "show then immediately hide" — a boss defeat that
happens while Game Over is already up must never register as a victory at
all, even momentarily. Both `GameOverUI.Show()` and `VictoryUI.Show()` were
already bare `panelRoot.SetActive(true)` calls with no existing check of
any kind (confirmed by reading both scripts in full), so the guard is a
plain early-return *before* that line, not a state that gets set and later
unset.

Added `GameOverUI.victoryPanelRoot`/`VictoryUI.gameOverPanelRoot` (each
pointing at the other's panel), and each `Show()` now returns immediately
if the other's panel `activeSelf` — implemented symmetrically (not just
the reported Game-Over-then-Victory direction) to also cover the mirror
race, where an enemy bullet already in flight when the boss dies could
still land on the Player a moment after Victory has already shown. No
changes to `Boss.cs`/`PlayerHealth.cs`/`BossPanelUI.cs` at all — the boss
still dies and gets destroyed normally regardless of which panel already
won; `BossPanelUI.ShowDefeated()` (the other `OnDefeated` listener) is left
unguarded since it's just HUD text sitting behind whichever full-screen
panel is up, harmless either order.

### CPU party frame names

`PartyFrameUI.cs` had no name field at all — the identical `"Player 1"`
every frame showed was a static default baked into
`Assets/Prefabs/PartyFrame.prefab`'s `PlayerName` text child, never bound
to any script (already flagged as a known gap in `systems/hud-layout.md`).
Added `PartyFrameUI.nameText`, changed `Initialize(GameObject)` to
`Initialize(GameObject, string displayName)`. `PartyFrameManager.Awake()`
computes the name per slot before calling it: whichever ship has no
`AIController` (attached to all 3 `Teammate_*`, absent from `Player` — the
same signal already used elsewhere to distinguish human from AI, see
`systems/boss.md`) is `"Player 1"`; every other slot is `"CPU " + n`,
numbered in `players[]`'s array order. Checked component presence rather
than a raw index (`i == 0`) so this stays correct even if the array's
wiring order ever changed — matches the codebase's existing convention.

Wired the prefab's pre-existing `PlayerName` `TextMeshProUGUI` child into
the new `nameText` field once, at the prefab level
(`PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`, same technique as
Session 8's party-frame contrast fix) — confirmed all 4 `PartyFrame_1..4`
are genuine prefab instances (unlike the `Teammate_*` split-prefab
situation), so the one edit propagated to all 4 automatically with no
per-instance wiring needed.

### Verification

All via the Unity MCP bridge in Play mode. Confirmed the primary reported
case: forcing the human `Player` to 0 HP shows `GameOverPanel`; forcing the
boss to 0 HP afterward leaves `VictoryPanel.activeSelf == false`. Reset and
confirmed the mirror case: defeating the boss first shows `VictoryPanel`;
forcing the Player to 0 HP afterward leaves `GameOverPanel.activeSelf ==
false`. Read all 4 party frames' live `nameText.text` in Play mode:
`"Player 1"` on the human's frame, `"CPU 1"`/`"CPU 2"`/`"CPU 3"` on the
three teammates', matching `players[]`'s wired order. Zero console
errors/warnings throughout.

### Still open

- Bullet-dodging, manual teammate-ability triggering — unchanged, still not
  built (see `roadmap.md`).
- Main Menu / Lobby scenes — still not built.

## Session 22 — Pattern Barrage (Geometric Bullet Spread Patterns)

The roadmap's next "Player-vs-boss dynamics" item: more varied geometric
bullet-pattern shapes (fan/ring/spiral) beyond the boss's existing single
aimed shot / fixed 3-bullet spread. Planned in a dedicated planning pass
before any code was touched (a Plan agent validated the design against
existing precedent — see below), then implemented and verified live via the
Unity MCP bridge in one session.

### Design decision: one attack, randomized shape, no-immediate-repeat

Explored two alternatives before settling: three fully separate standalone
attacks (one cooldown/telegraph/HUD stack per shape), or a fixed rotation
through shapes. Rejected both. Went with one new standalone attack, **Pattern
Barrage** — its own cooldown (`patternBarrageCooldown`, 7s) and telegraph
(`patternBarrageTelegraphTime`, 0.7s), layered on top of the existing Phase
1/2 fire exactly like Shockwave and Guided Missile already are, not a
replacement of it. On each activation it randomly picks one of `{ Fan, Ring,
Spiral }` to fire — the same "build eligible options, `Random.Range` pick
one" idiom `CheckGuidedMissile()` already uses for target selection, just
applied to shapes instead of targets. Justified against this project's own
established "prototype-simple, prove it's fun before adding infra" principle
(`overview.md`'s Architecture Principle 1, Session 10's explicit scoping) —
three parallel systems is infrastructure the fight hasn't earned yet.

Pure `Random.Range(0,3)` alone risked the same shape firing twice or three
times in a row, which reads as a lack of content rather than surprise — a
worse outcome than either rejected alternative. Fixed with one extra
`private BulletPattern? lastPatternBarragePattern` field: `PickPattern()`
excludes whichever shape fired last time from the pick pool. Cheap (one
field, a few lines), gets both properties (surprise + guaranteed variety)
that the pure-random and fixed-rotation options each only got one of.

### Shape math

All three reuse the existing private `Boss.SpawnBullet(Vector2 dir)` helper
(already used by `Fire()`) — no `Bullet.cs` changes, no object pooling, no
new damage/speed fields.

- **Fan** — generalizes the existing Phase 2 3-bullet spread
  (`Quaternion.Euler(0,0,angle) * dir`) to N bullets: `fanBulletCount` (5)
  evenly spread across `fanSpreadAngle` (50°, so ±25°), centered on the
  direction to `CurrentTarget`. Aim is recomputed *after* the telegraph wait
  completes, not at activation time — same re-check-after-telegraph idiom
  `ShockwaveRoutine()` already uses, since the target may have moved or died
  during the wind-up.
- **Ring** — deliberately not target-relative; the boss never rotates, so
  there's no "facing" to aim relative to, and it's meant to be an
  omnidirectional "screen-full-of-bullets" moment. `ringBulletCount` (12)
  bullets evenly spaced around 360°, with a randomized per-burst start-angle
  offset (a standard bullet-hell technique) so the gaps between bullets
  don't always land in the same screen position — otherwise the same "safe
  lane" would be memorizable every single time.
- **Spiral** — the shape that actually delivers "rapid-fire," since Fan/Ring
  both resolve in a single frame. `FireSpiralRoutine()` is a coroutine:
  starts aimed at `CurrentTarget` like Fan, then fires one bullet every
  `spiralShotInterval` (0.05s) for `spiralBulletCount` (20) shots, sweeping
  `spiralAngleStep` (25°) between each. `PatternBarrageRoutine()` awaits it
  via `yield return StartCoroutine(...)`, so the barrage (and
  `PatternBarrageActivePattern`) doesn't end until the full spiral has
  actually finished firing. 20 × 25° = 500°, intentionally past a full
  revolution so it reads as a genuine spin rather than stopping dead at
  360°.

### HUD wiring

`BossPanelUI` gained `patternBarrageWarningText` (`"Incoming: {Shape}
Barrage"` while `Boss.PatternBarrageActivePattern.HasValue`, else empty) and
`patternBarrageCooldownText` (`"Pattern Barrage: {n}s"` / `"...: Ready"`) —
same exact idiom as the existing warning/cooldown text pairs. Built via the
Unity MCP bridge by duplicating existing template text elements
(`BossWarningText`, `BossGuidedMissileCooldownText`) rather than building
`TextMeshProUGUI` from scratch, same technique as every prior HUD addition
back to Session 8. Both new fields are brand-new script fields, so — unlike
several past sessions' gotcha with *existing* serialized fields — no
`RecordPrefabInstancePropertyModifications()` step was needed on `Boss`;
they just took their C# defaults.

Forced a full scene reload from disk after wiring (the project's standard
verification habit since Session 12, after multiple past sessions where
in-memory wiring success silently didn't survive a reload) — confirmed both
new `BossPanel` children and their `BossPanelUI` field references persisted
correctly.

### Verification

All live via the Unity MCP bridge, mostly via reflection since
`FireFan`/`FireRing`/`FireSpiralRoutine`/`PickPattern`/`CheckPatternBarrage`
are private:

- **Bullet counts and angle math**: invoked `FireFan`/`FireRing` directly,
  diffed the scene's `Bullet` instances before/after (by reference, not by
  the now-obsolete `GetInstanceID()`) to isolate exactly the newly spawned
  ones from bullets the boss's own concurrent regular fire was also
  producing. Fan produced exactly 5 bullets at angles `-25, -12.5, 0, 12.5,
  25` relative to the aim direction — exact even spacing across the
  configured spread. Ring produced exactly 12 bullets with an exact 30° gap
  between every consecutive pair. For Spiral, rather than relying on
  real-time `WaitForSeconds` frame-pumping (this project's Editor has a
  long-documented history, Sessions 6/8/9/10, of not reliably ticking
  `Update()` while unfocused), got the coroutine's `IEnumerator` directly
  from the reflected method call and manually drove `MoveNext()` in a tight
  loop — deterministic and instant, since a manually-driven `WaitForSeconds`
  yield is a no-op rather than a real wait. Produced exactly 20 bullets in
  20 steps.
- **No-immediate-repeat rule**: reset `lastPatternBarragePattern` to `null`,
  called `PickPattern()` 30 times in a row (threading its own output back in
  as `lastPatternBarragePattern` each time, matching what
  `PatternBarrageRoutine()` does for real). All 3 shapes appeared across the
  run; zero consecutive repeats.
- **Cooldown/target gating**: force-set the private `nextPatternBarrageTime`
  into the past and called `CheckPatternBarrage()` — confirmed it started
  the coroutine for real (`PatternBarrageActivePattern` became non-null
  immediately, `PatternBarrageCooldownRemaining` jumped to the full 7s) and
  that `BossPanelUI`'s new warning/cooldown text reflected it live in the
  same Play session. Separately, temporarily nulled the private
  `CurrentTarget` backing field and called `CheckPatternBarrage()` again —
  confirmed a clean no-op (no exception, `PatternBarrageCooldownRemaining`
  stayed at 0, meaning it correctly didn't start a coroutine or advance the
  cooldown) rather than throwing on a null target.
- Also incidentally reconfirmed, unprompted, that the whole system runs
  correctly end-to-end with zero manual intervention: real wall-clock time
  elapsing between two separate tool calls was enough for the 7s cooldown to
  naturally lapse, and `Boss.Update()`'s own automatic `CheckPatternBarrage()`
  call picked a fresh shape on its own.
- Zero console errors/warnings across the entire test pass, including
  through a Play mode stop.

### Still open

- Bullet-dodging, manual teammate-ability triggering — unchanged, still not
  built (see `roadmap.md`).
- Minions around the boss — next in the roadmap's build order, now that both
  bullet-dodging/manual triggering and Pattern Barrage are the only items
  left ahead of it in "Player-vs-boss dynamics."
- Main Menu / Lobby scenes — still not built.
