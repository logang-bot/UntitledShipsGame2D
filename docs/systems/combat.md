# Combat

Shooting, projectiles, damage, health, and enemy waves. See
[movement.md](movement.md) for ship movement and the fixed-orientation design
decision that firing direction depends on, and [boss.md](boss.md) for the
boss encounter built on top of this system (boss HP/phases, aggro, CPU
teammates).

## PlayerController.cs — shooting

**Attached to:** `Player` GameObject (same script that handles movement —
see [movement.md](movement.md)).
**Requires:** a `FirePoint` child transform, a bullet prefab.

Fire input arrives via `OnFire(InputValue)`, called automatically by the
`Player Input` component (see [input.md](input.md)). Auto-fires on an
interval while held. Fire direction is hardcoded to `Vector2.up` — the ship
has a fixed orientation by design (see [movement.md](movement.md)).

Key public fields: `bulletPrefab`, `firePoint`, `fireRate` (default `0.35`
as of the boss-fight tuning pass — see [boss.md](boss.md); was `0.2`),
`bulletSpeed` (default 12), `recoilDamping` (default 8, higher = faster
decay).

`Fire()` (regular Space/click fire) and `FireBigShot(float widthMultiplier,
int damageAmount)` (Attacker's ability — see [player-roles.md](player-roles.md))
both route through a shared private `SpawnBullet(widthMultiplier,
damageAmount)` so there's one instantiation/`Init()` call site; `Fire()` is
just `SpawnBullet(1f, 1)`. `SpawnBullet()` passes `gameObject` into
`Bullet.Init(..., ownerObject)` (see Bullet.cs below) so the boss can
attribute damage back to the shooter for aggro. `AddRecoil(Vector2 impulse)`
accumulates into a private `recoilVelocity` field that `HandleMovement()`
itself decays (`Vector2.Lerp` toward zero, scaled by `recoilDamping`) and
adds into its position calculation every `FixedUpdate`. **This is required,
not a style choice**: `HandleMovement()` recomputes position from
`moveInput` and calls `rb.MovePosition()` unconditionally every
`FixedUpdate` — a plain `Rigidbody2D.AddForce()` impulse would be silently
overwritten the very next step, since `MovePosition` never reads back
accumulated velocity. Recoil respects the existing viewport-edge clamp
automatically, since it's folded into the same position formula before
clamping runs.

`OnMove(InputValue)`/`OnFire(InputValue)` (the `Player Input`-driven entry
points above) are now thin wrappers around public, non-input entry points —
`SetMoveDirection(Vector2)` and `SetFiring(bool)` — added so
`AIController.cs` (see [boss.md](boss.md)) can drive a CPU-controlled
teammate's movement/firing directly, without constructing a fake
`InputValue` (which is only valid inside a real input callback). No
behavior change for the human `Player`.

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

`damage` is a plain public field (not passed through `Init()`, which only
covers direction/speed/owner) — set directly on the instantiated bullet
right after `Instantiate()`, before `Init()` runs. Defaults to `1`, so
anything that doesn't touch it (enemy bullets, regular player fire) is
unaffected. `PlayerController.SpawnBullet()` sets it explicitly for both
regular fire (`1`) and Attacker's big shot (`3`, see
[player-roles.md](player-roles.md)); the collider scales automatically with
the bullet's `transform.localScale` (Unity `BoxCollider2D` behavior), so a
wider bullet doesn't need any collider-size code.

`Init()` gained a 4th, optional param: `Init(Vector2 dir, float spd, string
ownerTag, GameObject ownerObject = null)`. The default keeps every existing
call (e.g. `Enemy.cs`'s `Init(Vector2.down, bulletSpeed, "Enemy")`)
compiling unchanged. Player-fired bullets now pass their shooter as
`ownerObject` so `OnTriggerEnter2D`'s player-bullet-vs-`Enemy`-tag branch —
in addition to its existing `Enemy.TakeDamage(damage)` call — also checks
for a `Boss` component and calls `boss.TakeDamage(damage, ownerObject)`,
attributing the hit to its shooter for the boss's aggro system. See
[boss.md](boss.md).

Key public fields: `lifeTime` (default 3s), `damage` (default 1).

**Planned, not yet decided** (see [boss.md](boss.md)'s "Boss combat
dynamism"): future boss/minion attacks may want bullets that re-aim at a
moving target over their lifetime, or curve, rather than the fixed
straight-line direction set once at `Init()` today. Flagged specifically
because it interacts with the Tank's planned physical-blocking behavior
(see [boss.md](boss.md)'s "AI teammate behavior" and
[player-roles.md](player-roles.md)) — blocking relies on bullets traveling
in a predictable straight line.

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
`CurrentHealth` property exposes current HP for HUD hookup. `Heal(int)` is
the symmetric inverse of `TakeDamage(int)` — adds HP, clamped at
`maxHealth` — called by Medic's ability on self (see
[player-roles.md](player-roles.md)); no event fires on heal since
`PartyFrameUI` already polls `CurrentHealth` every frame, so a heal shows up
live for free.

Key public fields: `maxHealth` (default 5; scaled by role — see
[player-roles.md](player-roles.md)), `OnDeath` (`UnityEvent`, fires only on
the killing blow — see Game Over / Restart below), `OnDamaged` (`UnityEvent`,
fires only on non-fatal hits — see Damage Feedback below). Key public
methods: `TakeDamage(int)`, `Heal(int)`.

**Planned, not yet implemented**: a second, shield pool (see
[player-roles.md](player-roles.md)'s "Planned: Shield stat") that
`TakeDamage(int)` would check first — damage deducts from shield before
touching `currentHealth`, only overflowing once shield is at 0. Shield has
no passive regen of its own; it's only refilled by Medic's planned proximity
aura (see [boss.md](boss.md)'s "AI teammate behavior").

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
`PlayerHealth.OnDamaged`, and also on `PlayerAbility.OnTaunt` as Tank-ability
feedback (see [player-roles.md](player-roles.md)) — added in Session 9
before a real aggro system existed for taunt to affect, and kept alongside
the real `Boss.TauntedBy()` listener once one did (see [boss.md](boss.md)),
not replaced by it. `Flash()` restarts the coroutine (`StopCoroutine`
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
`PlayerHealth.OnDamaged`, and also on `PlayerAbility.OnTaunt` (same
history as `PlayerDamageFlash.cs` above — added before, kept alongside,
the real `Boss.TauntedBy()` listener). Caches `transform.localPosition`
once in `Awake()`
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
| **PlayerController.cs** | bulletPrefab: PlayerBullet prefab, firePoint: FirePoint child, fireRate: 0.35, bulletSpeed: 12, recoilDamping: 8 |
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
