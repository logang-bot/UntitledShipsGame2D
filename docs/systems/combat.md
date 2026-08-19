# Combat

Shooting, projectiles, damage, health, and enemy waves. See
[movement.md](movement.md) for ship movement and the fixed-orientation design
decision that firing direction depends on.

## PlayerController.cs — shooting

**Attached to:** `Player` GameObject (same script that handles movement —
see [movement.md](movement.md)).
**Requires:** a `FirePoint` child transform, a bullet prefab.

Fire input arrives via `OnFire(InputValue)`, called automatically by the
`Player Input` component (see [input.md](input.md)). Auto-fires on an
interval while held. Fire direction is hardcoded to `Vector2.up` — the ship
has a fixed orientation by design (see [movement.md](movement.md)).

Key public fields: `bulletPrefab`, `firePoint`, `fireRate` (default 0.2),
`bulletSpeed` (default 12).

### Child: FirePoint

Empty GameObject, child of `Player`. Position at the ship's nose (top edge of
the square placeholder sprite). This is the bullet spawn origin passed to
`PlayerController`'s `firePoint` field.

## Bullet.cs

**Attached to:** `PlayerBullet` and `EnemyBullet` prefabs (same script,
reused).
**Requires:** `Collider2D` with `Is Trigger` ON.

Shared projectile behavior. Direction, speed, and owner (`"Player"` or
`"Enemy"`) are set at spawn time via `Init()`. Owner determines which tag it
can damage on collision (`Player` bullets hit `Enemy` tag and vice versa).
Self-destructs after `lifeTime` seconds as a safety net if it never hits
anything.

Key public fields: `lifeTime` (default 3s).

## PlayerHealth.cs

**Attached to:** `Player` GameObject (alongside `PlayerController` and
`PlayerRoleComponent` — see [player-roles.md](player-roles.md)).
**Requires:** nothing external.

Tracks player HP. `TakeDamage(int)` is called by `Bullet.cs` on enemy-bullet
collision: fatal hits (`currentHealth <= 0`) call `Die()`, non-fatal hits
invoke `OnDamaged` instead — the two are mutually exclusive, a killing blow
does not also fire `OnDamaged`, since `Die()` deactivates the `Player`
GameObject (which would cut off an in-flight flash/shake coroutine) and
`GameOverUI` takes the screen immediately anyway. `Die()` disables the
GameObject, then invokes the `OnDeath` `UnityEvent` so other systems can
react (game-over UI, HUD) without `PlayerHealth` knowing who's listening.
`CurrentHealth` property exposes current HP for HUD hookup.

Key public fields: `maxHealth` (default 5; scaled by role — see
[player-roles.md](player-roles.md)), `OnDeath` (`UnityEvent`, fires only on
the killing blow — see Game Over / Restart below), `OnDamaged` (`UnityEvent`,
fires only on non-fatal hits — see Damage Feedback below).

## GameOverUI.cs

**Attached to:** `GameOverPanel` (child of `HUDCanvas` — see
[hud-layout.md](hud-layout.md)).
**Requires:** `panelRoot` (Inspector-dragged, set to `GameOverPanel` itself).

Purely event-driven, no `Update()`. `Awake()` hides `panelRoot` so it's
invisible during normal play. `Show()` is wired as a listener on `Player`'s
`PlayerHealth.OnDeath` — reveals the panel. `Restart()` is wired to the
panel's Restart `Button.OnClick()` — calls
`SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)`, reloading
`SampleScene` from scratch so every stateful script (`PlayerHealth`,
`EnemySpawner`, `PartyFrameManager`, ...) resets itself via its own
`Awake`/`Start`, with no hand-written reset logic needed.

Key public field: `panelRoot`. Key public methods: `Show()`, `Restart()`.

## PlayerDamageFlash.cs

**Attached to:** `Player` GameObject.
**Requires:** a `SpriteRenderer` and a `PlayerRoleComponent` on the same
GameObject (see [player-roles.md](player-roles.md)).

Flashes the ship's sprite on a non-fatal hit. Wired as a listener on
`PlayerHealth.OnDamaged`. `Flash()` restarts the coroutine (`StopCoroutine`
+ `StartCoroutine`) so rapid hits re-flash at full brightness instead of
stacking or blending. **Critical detail:** the routine reverts
`SpriteRenderer.color` to `PlayerRoleComponent.Stats.tintColor`, not
`Color.white` — `PlayerRoleComponent.Awake()` only tints the sprite once and
never re-applies it, so reverting to white would permanently erase the role
tint on the very first hit.

Key public fields: `flashColor` (default white), `flashDuration` (default
0.12s). Key public method: `Flash()`.

## CameraShake.cs

**Attached to:** `Main Camera` GameObject (alongside `AspectRatioFitter.cs` —
see [hud-layout.md](hud-layout.md)).
**Requires:** nothing external.

Shakes the camera on a non-fatal player hit. Wired as a listener on
`PlayerHealth.OnDamaged`. Caches `transform.localPosition` once in `Awake()`
as the base to return to; `Shake()` restarts the coroutine the same way as
`PlayerDamageFlash.Flash()`. Offsets `transform.localPosition` by a
linearly-decaying random offset each frame, then **explicitly** resets to
the cached base position when done rather than relying on the decay to land
at exactly zero — confirmed safe alongside `AspectRatioFitter`, which only
ever touches `camera.rect` (the pillarbox viewport), never `transform`, so
the two cannot conflict.

Key public fields: `shakeDuration` (default 0.2s), `shakeMagnitude` (default
0.15). Key public method: `Shake()`.

## Enemy.cs

**Attached to:** `Enemy` prefab.
**Requires:** `Rigidbody2D`, `Collider2D` (not trigger), tag `Enemy`, an
`EnemyBullet` prefab reference.

Sine-wave downward movement, periodic downward fire (staggered per-instance
via random initial delay), takes damage via `TakeDamage(int)`,
self-destructs at 0 HP or when off-screen.

Key public fields: `moveSpeed`, `sineAmplitude`, `sineFrequency`, `health`,
`bulletPrefab`, `fireInterval`, `bulletSpeed`.

## EnemySpawner.cs

**Attached to:** `Spawner` GameObject (positioned above camera view).
**Requires:** an `Enemy` prefab reference.

Spawns waves of enemies at a randomized X position within a configurable
width, staggered within each wave, repeating on an interval.

Key public fields: `enemyPrefab`, `enemiesPerWave`, `spawnInterval`,
`waveInterval`, `spawnWidth`.

## Scene wiring

### Player (combat-relevant components)

| Component            | Key inspector values                                                 |
| --------------------- | ---------------------------------------------------------------------- |
| **PlayerController.cs** | bulletPrefab: PlayerBullet prefab, firePoint: FirePoint child, fireRate: 0.2, bulletSpeed: 12 |
| **PlayerHealth.cs**   | maxHealth: 5, OnDeath: `GameOverPanel/GameOverUI.Show()` + `PartyFrame_1/PartyFrameUI.OnPlayerDied()`, OnDamaged: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` |
| **PlayerDamageFlash.cs** | flashColor: white, flashDuration: 0.12 |

Both `PlayerHealth.cs` and `PlayerRoleComponent` are confirmed attached and
verified working via the Unity MCP bridge (component values checked live in
Play mode, tint/health/fire-rate all applied correctly). The `OnDeath` and
`OnDamaged` event listeners were both wired live via the MCP bridge and
verified end-to-end in Play mode: forcing 0 HP shows `GameOverPanel` and
grays the party frame with Restart cleanly reloading the scene; a non-fatal
hit flashes the sprite and shakes the camera, both reverting exactly to
their pre-hit state (role tint color, base camera position) with no drift,
and rapid repeated hits re-trigger cleanly with no stacking.

### Main Camera (combat-relevant components)

| Component          | Key inspector values                  |
| ------------------- | ---------------------------------------- |
| **CameraShake.cs**  | shakeDuration: 0.2, shakeMagnitude: 0.15 |

### GameOverPanel

Child of `HUDCanvas` (see [hud-layout.md](hud-layout.md) for why it lives
there instead of `GameplayCanvas`). Full-rect dark overlay (`Image`, ~75%
alpha black), a centered "Game Over" `TextMeshProUGUI`, and a centered
Restart `Button` (+ `TextMeshProUGUI` label). Starts active in the saved
scene; `GameOverUI.Awake()` force-hides it at Play-mode start.

### Spawner

Empty GameObject positioned **above** the visible camera area (y ≈ 8).

| Component           | Key inspector values                                                     |
| -------------------- | ---------------------------------------------------------------------------- |
| **EnemySpawner.cs**  | enemyPrefab: Enemy prefab, enemiesPerWave: 5, spawnInterval: 0.5, waveInterval: 4, spawnWidth: 6 |

### Prefabs

**PlayerBullet**

| Component       | Setting            |
| ----------------- | ------------------- |
| Sprite Renderer   | Any small sprite    |
| Collider2D        | Is Trigger: **ON**  |
| **Bullet.cs**     | lifeTime: 3          |

`Bullet.cs` is initialized at spawn via `Init(direction, speed, ownerTag)` —
no inspector values for direction/speed/owner, those come from the spawning
script.

**EnemyBullet** — identical setup to PlayerBullet. Reuses `Bullet.cs`.

**Enemy**

**Tag:** `Enemy`
**Rigidbody2D:** Gravity Scale: 0

| Component        | Key inspector values                                                        |
| ------------------ | ------------------------------------------------------------------------------ |
| Sprite Renderer    | Any placeholder sprite                                                        |
| Collider2D         | Is Trigger: **OFF**                                                            |
| **Enemy.cs**       | moveSpeed: 2, sineAmplitude: 1.5, sineFrequency: 1, health: 3, bulletPrefab: EnemyBullet prefab, fireInterval: 1.5, bulletSpeed: 6 |
