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
