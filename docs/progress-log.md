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
