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

Key public fields: `bulletPrefab`, `firePoint`, `shotsPerSecond` — shots per
second, higher = faster (renamed from the old, misleadingly-inverted
`fireRate`, which stored *seconds between shots*; both `shotsPerSecond` and
`fireDamage` below are fixed per-role values, overwritten at `Start()` — see
[player-roles.md](player-roles.md)'s "Fixed per-role stats") — `bulletSpeed`
(default 12), `fireDamage` (a fixed per-role value, base script default
`0.6`), `recoilDamping` (default 8, higher = faster decay). Two
non-destructive runtime-only buff multipliers, both default `1`, never
serialized-meaningful: `speedBuffMultiplier` (see [movement.md](movement.md))
and `fireRateBuffMultiplier` — set only by Support's party-wide Speed Boost
(see [player-roles.md](player-roles.md)), read via a computed `FireInterval
=> 1f / (shotsPerSecond * fireRateBuffMultiplier)` wherever the fire-cooldown
gate needs seconds-until-next-shot, rather than ever mutating
`shotsPerSecond` itself.

`Fire()` (regular Space/click fire) and `FireBigShot(float widthMultiplier,
float damageAmount)` (Attacker's ability — see [player-roles.md](player-roles.md))
both route through a shared private `SpawnBullet(widthMultiplier,
damageAmount)` so there's one instantiation/`Init()` call site; `Fire()` is
just `SpawnBullet(1f, fireDamage)` — the caster's own live, role-fixed
damage value, e.g. `2.0` for Attacker, `0.7` for Medic (see
[player-roles.md](player-roles.md)'s table). `SpawnBullet()` passes
`gameObject` into `Bullet.Init(..., ownerObject)` (see Bullet.cs below) so
the boss can attribute damage back to the shooter for aggro. `AddRecoil(Vector2 impulse)`
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
right after `Instantiate()`, before `Init()` runs. **`float`, not `int`**
(changed in the boss HP/damage tuning pass — see [boss.md](boss.md)'s
"Tuning" — since player fire damage is no longer a whole number); defaults
to `1`, so anything that doesn't set it (enemy/boss bullets) is unaffected.
`PlayerController.SpawnBullet()` sets it explicitly for both regular fire
(the caster's fixed-per-role `fireDamage`, e.g. `2.0` for Attacker) and
Attacker's big shot (`fireDamage × bigShotDamageMultiplier`, a live `2x` —
see [player-roles.md](player-roles.md)); the collider scales automatically
with the bullet's `transform.localScale` (Unity `BoxCollider2D` behavior),
so a wider bullet doesn't need any collider-size code.

`OnTriggerEnter2D`'s enemy-bullet-vs-`Player`-tag branch resolves the hit
target via `other.GetComponentInParent<PlayerHealth>()`, **not**
`other.GetComponent<PlayerHealth>()` (changed 2026-08-21) — lets a *child*
collider without its own `PlayerHealth` (e.g. Tank's Shield Arc, a wider
blocking trigger on a child GameObject — see [player-roles.md](player-roles.md))
still route the hit into its parent ship's own health/shield pool, exactly
like a direct hit on the ship's own body collider. Backward-compatible: a
ship's own collider still resolves to its own `PlayerHealth` first, since
`GetComponentInParent` checks the object itself before ascending.

`Init()` gained a 4th, optional param: `Init(Vector2 dir, float spd, string
ownerTag, GameObject ownerObject = null)`. The default keeps every existing
call (e.g. `Enemy.cs`'s `Init(Vector2.down, bulletSpeed, "Enemy")`)
compiling unchanged. Player-fired bullets now pass their shooter as
`ownerObject` so `OnTriggerEnter2D`'s player-bullet-vs-`Enemy`-tag branch —
in addition to its existing `Enemy.TakeDamage(damage)` call — also checks
for a `Boss` component and calls `boss.TakeDamage(damage, ownerObject)`,
attributing the hit to its shooter for the boss's aggro system. See
[boss.md](boss.md). Both `Enemy.TakeDamage`/`Boss.TakeDamage` now take a
`float amount` for the same reason as `damage` above — each rounds
(`Mathf.RoundToInt`) only at the point it subtracts from its own `int`
health pool, so no fractional HP appears anywhere. The enemy-bullet-vs-
`PlayerHealth` branch does the same rounding at its call site, since
`PlayerHealth.TakeDamage(int)` stays `int` (see below).

Key public fields: `lifeTime` (default 3s), `damage` (default `1f`).

**Planned, not yet decided** (see [boss.md](boss.md)'s "Boss combat
dynamism"): future boss/minion attacks may want bullets that re-aim at a
moving target over their lifetime, or curve, rather than the fixed
straight-line direction set once at `Init()` today. Flagged specifically
because it interacts with the Tank's physical-blocking behavior (see
[boss.md](boss.md)'s "Tank guard-point positioning" and
[player-roles.md](player-roles.md)) — blocking relies on bullets traveling
in a predictable straight line.

## PlayerHealth.cs

**Attached to:** `Player` GameObject (alongside `PlayerController` and
`PlayerRoleComponent` — see [player-roles.md](player-roles.md)).
**Requires:** nothing external.

Tracks player HP and shield. `TakeDamage(int)` is called by `Bullet.cs` on
enemy-bullet collision: **shield absorbs first** — deducted from
`currentShield` down to 0, only the remainder subtracts from
`currentHealth` (see [player-roles.md](player-roles.md)'s "Shield stat").
Fatal hits (`currentHealth <= 0`) call `Die()`, non-fatal hits invoke
`OnDamaged` instead — the two are mutually exclusive, a killing blow does
not also fire `OnDamaged`, since `Die()` deactivates the `Player` GameObject
(which would cut off an in-flight flash/shake coroutine) and `GameOverUI`
takes the screen immediately anyway. A hit fully absorbed by shield still
fires `OnDamaged` (the player should still feel it), it just doesn't touch
`currentHealth`. `Die()` disables the GameObject, then invokes the
`OnDeath` `UnityEvent` so other systems can react (game-over UI, HUD)
without `PlayerHealth` knowing who's listening. `CurrentHealth`/
`CurrentShield` properties expose current values for HUD hookup. `Heal(int)`
is the symmetric inverse of `TakeDamage(int)` — adds HP, clamped at
`maxHealth`; `RestoreShield(int)` is the shield equivalent (clamps at
`maxShield`). Both are called every tick, on any ally within range, by
Medic's passive proximity aura (`PlayerAbility.TickAura()` — see
[player-roles.md](player-roles.md) and [boss.md](boss.md)'s "Medic
positioning + proximity aura"), not on self — Medic's old self-targeted
instant heal was replaced by the aura entirely. No event fires on either
call since `PartyFrameUI` already polls `CurrentHealth`/`CurrentShield`
every frame, so a heal/shield-restore shows up live for free. **No passive
shield regen anywhere** — shield only ever goes up via `RestoreShield(int)`,
deliberately, to keep Tank dependent on Medic.

Key public fields: `maxHealth`/`maxShield` — both fixed per-role values
(script defaults `5`/`3`, overwritten by the role's own number at `Awake()`
— see [player-roles.md](player-roles.md)'s "Fixed per-role stats"),
`OnDeath` (`UnityEvent`, fires only on the killing blow —
see Game Over / Restart below), `OnDamaged` (`UnityEvent`, fires on any
non-fatal hit, shield-absorbed or not — see Damage Feedback below). Key
public methods: `TakeDamage(int)`, `Heal(int)`, `RestoreShield(int)`.

## GameOverUI.cs

**Attached to:** `GameOverPanel` (child of `HUDCanvas` — see
[hud-layout.md](hud-layout.md)).
**Requires:** `panelRoot` (Inspector-dragged, set to `GameOverPanel` itself).

Purely event-driven, no `Update()`. `Awake()` hides `panelRoot` so it's
invisible during normal play. `Show()` is wired as a listener on `Player`'s
`PlayerHealth.OnDeath` — reveals the panel. `Restart()` is wired to the
panel's Restart `Button.OnClick()` — calls
`SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)`, reloading
`Gameplay` from scratch so every stateful script (`PlayerHealth`,
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
via random initial delay), takes damage via `TakeDamage(float)` (was `int`
— see Bullet.cs above), self-destructs at 0 HP or when off-screen.

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
| **PlayerController.cs** | bulletPrefab: PlayerBullet prefab, firePoint: FirePoint child, bulletSpeed: 12, recoilDamping: 8 (shotsPerSecond/fireDamage/moveSpeed all overwritten by role at `Start()` — see [player-roles.md](player-roles.md)) |
| **PlayerHealth.cs**   | OnDeath: `GameOverPanel/GameOverUI.Show()` + `PartyFrame_1/PartyFrameUI.OnPlayerDied()`, OnDamaged: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` (maxHealth/maxShield overwritten by role at `Awake()`) |
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
