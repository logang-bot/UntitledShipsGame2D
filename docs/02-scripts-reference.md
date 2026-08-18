# Scripts Reference

## PlayerController.cs

**Attached to:** `Player` GameObject
**Requires:** `Rigidbody2D`, `Player Input` component (Actions = PlayerControls,
Behavior = Send Messages), a `FirePoint` child transform, a bullet prefab.

Handles ship movement (clamped to camera viewport) and shooting. Movement/fire
input arrives via `OnMove(InputValue)` / `OnFire(InputValue)`, called automatically
by the `Player Input` component — not polled manually.

Key public fields: `moveSpeed`, `screenPadding`, `bulletPrefab`, `firePoint`,
`fireRate`, `bulletSpeed`.

## PlayerHealth.cs

**Attached to:** `Player` GameObject (alongside `PlayerController`).
**Requires:** nothing external.

Tracks player HP. `TakeDamage(int)` is called by `Bullet.cs` on enemy-bullet
collision. `Die()` disables the GameObject (placeholder — no game-over flow yet).
`CurrentHealth` property exposes current HP for HUD hookup.

Key public fields: `maxHealth` (default 5).

## Bullet.cs

**Attached to:** `PlayerBullet` and `EnemyBullet` prefabs (same script, reused).
**Requires:** `Collider2D` with `Is Trigger` ON.

Shared projectile behavior. Direction, speed, and owner ("Player" or "Enemy") are
set at spawn time via `Init()`. Owner determines which tag it can damage on
collision (`Player` bullets hit `Enemy` tag and vice versa). Self-destructs after
`lifeTime` seconds as a safety net if it never hits anything.

Note: player health on enemy-bullet collision is a stub (`Destroy(gameObject)` only)
— the health system is not yet implemented.

Key public fields: `lifeTime` (default 3s).

## Enemy.cs

**Attached to:** `Enemy` prefab.
**Requires:** `Rigidbody2D`, `Collider2D` (not trigger), tag `Enemy`, an
EnemyBullet prefab reference.

Sine-wave downward movement, periodic downward fire (staggered per-instance via
random initial delay), takes damage via `TakeDamage(int)`, self-destructs at 0 HP
or when off-screen.

Key public fields: `moveSpeed`, `sineAmplitude`, `sineFrequency`, `health`,
`bulletPrefab`, `fireInterval`, `bulletSpeed`.

## EnemySpawner.cs

**Attached to:** `Spawner` GameObject (positioned above camera view).
**Requires:** an Enemy prefab reference.

Spawns waves of enemies at a randomized X position within a configurable width,
staggered within each wave, repeating on an interval.

Key public fields: `enemyPrefab`, `enemiesPerWave`, `spawnInterval`,
`waveInterval`, `spawnWidth`.

## AspectRatioFitter.cs

**Attached to:** `Main Camera`.
**Requires:** nothing external — self-contained, reads `Screen.width`/`Screen.height`.

Keeps gameplay locked to a fixed portrait aspect ratio (default 9:16),
centered on screen. Handles two cases:
- **Pillarbox** (screen wider than target — the PC case): portrait game area centered,
  bars on left/right used as HUD space.
- **Letterbox** (screen narrower/taller than target): bars on top/bottom, game area
  centered vertically. Unlikely in practice given the 9:16 target and phone sizes,
  but handled automatically.

On phones already close to the target aspect, bars shrink to near-zero automatically.
No platform branching needed; it's purely aspect-driven. Marked `[ExecuteAlways]` so
it also runs in the Editor outside Play mode, letting Game view preview the
pillarbox/letterbox live. Recalculates only on screen resize (not every frame).

Key public fields: `targetAspectWidth`, `targetAspectHeight`.
Key public method: `GetViewportPixelRect()` — returns the current gameplay
viewport in screen pixels, used by `HUDSidebarFitter` to size the side HUD to
match exactly.

## HUDSidebarFitter.cs

**Attached to:** `HUDCanvas`.
**Requires:** a reference to the `AspectRatioFitter` on Main Camera, and
`RectTransform` references to the left/right sidebar panels.

Dynamically resizes the sidebar panels every frame (on screen resize) to
exactly match `AspectRatioFitter`'s computed pillarbox bar width, closing the
gap between the gameplay viewport and the HUD. Also `[ExecuteAlways]` for
Editor preview without Play.

Key public fields: `aspectFitter`, `leftSidebar`, `rightSidebar`.

## PlayerControls (Input Actions asset)

**Assigned to:** `Player Input` component on the `Player` GameObject.

Custom-created (not Unity's auto-generated default) to keep exact control over
action names, since `Player Input`'s Send Messages behavior matches methods by
name (`Move` action → `OnMove`, `Fire` action → `OnFire`).

- Action map: `Player`
  - `Move` — Value / Vector2, 2D Vector composite (WASD only — arrow keys are **not**
    currently bound, despite early notes saying otherwise)
  - `Fire` — Button (Space and left mouse button)
