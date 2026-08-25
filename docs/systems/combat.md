# Combat

Shooting, projectiles, damage, health, and enemy waves. See
[movement.md](movement.md) for ship movement and the fixed-orientation
design decision that firing direction depends on, and [level1-boss.md](level1-boss.md)
for the boss encounter built on top of this system (boss HP/phases, aggro,
CPU teammates).

## PlayerController.cs — shooting

**Attached to:** `Player` GameObject (same script that handles movement —
see [movement.md](movement.md)).
**Requires:** a `FirePoint` child transform, a bullet prefab.

Fire input arrives via `OnFire(InputValue)`, called automatically by the
`Player Input` component (see [input.md](input.md)). Auto-fires on an
interval while held. Fire direction is hardcoded to `Vector2.up` — the ship
has a fixed orientation by design (see [movement.md](movement.md)).

Key public fields: `bulletPrefab`, `firePoint`, `shotsPerSecond` — shots
per second, higher = faster (both `shotsPerSecond` and `fireDamage` below
are fixed per-role values, overwritten at `Start()` — see
[player-roles.md](player-roles.md)'s "Fixed per-role stats") —
`bulletSpeed` (default 12), `fireDamage` (a fixed per-role value, base
script default `0.6`), `recoilDamping` (default 8, higher = faster decay).
Two non-destructive runtime-only buff multipliers, both default `1`:
`speedBuffMultiplier` (see [movement.md](movement.md)) and
`fireRateBuffMultiplier` — set only by Support's party-wide Speed Boost
(see [player-roles.md](player-roles.md)), read via a computed `FireInterval
=> 1f / (shotsPerSecond * fireRateBuffMultiplier)` wherever the
fire-cooldown gate needs seconds-until-next-shot, rather than ever mutating
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
adds into its position calculation every `FixedUpdate`. This has to work
this way: `HandleMovement()` recomputes position from `moveInput` and calls
`rb.MovePosition()` unconditionally every `FixedUpdate` — a plain
`Rigidbody2D.AddForce()` impulse would be silently overwritten the very
next step, since `MovePosition` never reads back accumulated velocity.
Recoil respects the existing viewport-edge clamp automatically, since it's
folded into the same position formula before clamping runs.

`OnMove(InputValue)`/`OnFire(InputValue)` are thin wrappers around public,
non-input entry points — `SetMoveDirection(Vector2)` and `SetFiring(bool)`
— so `AIController.cs` (see [level1-boss.md](level1-boss.md)) can drive a CPU-controlled
teammate's movement/firing directly, without constructing a fake
`InputValue` (which is only valid inside a real input callback).

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
right after `Instantiate()`, before `Init()` runs. `float`, default `1`, so
anything that doesn't set it (enemy/boss bullets that don't need a specific
value) is unaffected. `PlayerController.SpawnBullet()` sets it explicitly
for both regular fire (the caster's fixed-per-role `fireDamage`, e.g. `2.0`
for Attacker) and Attacker's big shot (`fireDamage × bigShotDamageMultiplier`,
a live `2x` — see [player-roles.md](player-roles.md)); the collider scales
automatically with the bullet's `transform.localScale` (Unity
`BoxCollider2D` behavior), so a wider bullet doesn't need any collider-size
code.

`InitHoming(Transform target, float turnRate, float spd, string ownerTag,
GameObject ownerObj = null)` is an alternate init path used by the boss's
guided missile (see [level1-boss.md](level1-boss.md)): it re-aims `direction` toward the
target's current position every frame via `Vector3.RotateTowards`, capped
by `turnRate` (degrees/second), instead of the fixed-at-spawn direction
`Init()` uses. If the target dies/deactivates mid-flight, the bullet just
stops re-aiming and continues straight on its last heading.

`OnTriggerEnter2D`'s enemy-bullet-vs-`Player`-tag branch resolves the hit
target via `other.GetComponentInParent<PlayerHealth>()`, not
`other.GetComponent<PlayerHealth>()` — lets a *child* collider without its
own `PlayerHealth` (e.g. Tank's Shield Arc — see
[player-roles.md](player-roles.md)) still route the hit into its parent
ship's own health/shield pool, exactly like a direct hit. A ship's own
collider still resolves to its own `PlayerHealth` first, since
`GetComponentInParent` checks the object itself before ascending.

`Init()`'s 4th, optional param — `Init(Vector2 dir, float spd, string
ownerTag, GameObject ownerObject = null)` — lets player-fired bullets pass
their shooter as `ownerObject`, so `OnTriggerEnter2D`'s
player-bullet-vs-`Enemy`-tag branch — in addition to its
`Enemy.TakeDamage(damage)` call — also checks for a `Level1Boss` component and
calls `boss.TakeDamage(damage, ownerObject)`, attributing the hit to its
shooter for the boss's aggro system. See [level1-boss.md](level1-boss.md). Both
`Enemy.TakeDamage`/`Level1Boss.TakeDamage` take a `float amount` — each rounds
(`Mathf.RoundToInt`) only at the point it subtracts from its own `int`
health pool, so no fractional HP appears anywhere. The enemy-bullet-vs-
`PlayerHealth` branch does the same rounding at its call site, since
`PlayerHealth.TakeDamage(int)` stays `int`.

Key public fields: `lifeTime` (default 3s), `damage` (default `1f`).

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
[player-roles.md](player-roles.md) and [level1-boss.md](level1-boss.md)), not on self. No
event fires on either call since `PartyFrameUI` already polls
`CurrentHealth`/`CurrentShield` every frame, so a heal/shield-restore shows
up live for free. **No passive shield regen anywhere** — shield only ever
goes up via `RestoreShield(int)`, deliberately, to keep Tank dependent on
Medic.

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
`PlayerHealth.OnDeath` — reveals the panel, unless `VictoryPanel` is already
showing (`victoryPanelRoot` guard — the 3 CPU teammates can still defeat the
boss after the human dies, see [level1-boss.md](level1-boss.md)'s "Death handling" for the
mutual-exclusion guard with `VictoryUI.cs`). `Restart()` is wired to the
panel's Restart `Button.OnClick()` — calls
`SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)`, reloading
`Gameplay` from scratch so every stateful script (`PlayerHealth`,
`EnemySpawner`, `PartyFrameManager`, ...) resets itself via its own
`Awake`/`Start`, with no hand-written reset logic needed. `ChangeRoles()`
loads `RoleSelect` instead (see [player-roles.md](player-roles.md)'s "Role
Select scene").

Key public fields: `panelRoot`, `victoryPanelRoot`. Key public methods:
`Show()`, `Restart()`, `ChangeRoles()`.

## PlayerDamageFlash.cs

**Attached to:** `Player` GameObject.
**Requires:** a `SpriteRenderer` and a `PlayerRoleComponent` on the same
GameObject (see [player-roles.md](player-roles.md)).

Flashes the ship's sprite on a non-fatal hit. Wired as a listener on
`PlayerHealth.OnDamaged`, and also on `PlayerAbility.OnTaunt` as Tank-ability
feedback (see [player-roles.md](player-roles.md)), alongside the real
`Level1Boss.TauntedBy()` aggro-redirect listener (see [level1-boss.md](level1-boss.md)).
`Flash()` restarts the coroutine (`StopCoroutine` + `StartCoroutine`) so
rapid hits re-flash at full brightness instead of stacking or blending. A
`Flash(Color)` overload lets other systems (Medic's heal) use a distinct
flash color. **Critical detail:** the routine reverts `SpriteRenderer.color`
to `PlayerRoleComponent.Stats.tintColor`, not `Color.white` —
`PlayerRoleComponent.Awake()` only tints the sprite once and never
re-applies it, so reverting to white would permanently erase the role tint
on the very first hit.

Key public fields: `flashColor` (default white), `flashDuration` (default
0.12s). Key public methods: `Flash()`, `Flash(Color)`.

## CameraShake.cs

**Attached to:** `Main Camera` GameObject (alongside `AspectRatioFitter.cs` —
see [hud-layout.md](hud-layout.md)).
**Requires:** nothing external.

Shakes the camera on a non-fatal player hit. Wired as a listener on
`PlayerHealth.OnDamaged`, and also on `PlayerAbility.OnTaunt` (same
pattern as `PlayerDamageFlash.cs` above). Caches `transform.localPosition`
once in `Awake()` as the base to return to; `Shake()` restarts the
coroutine the same way as `PlayerDamageFlash.Flash()`. Offsets
`transform.localPosition` by a linearly-decaying random offset each frame,
then **explicitly** resets to the cached base position when done rather
than relying on the decay to land at exactly zero. Safe alongside
`AspectRatioFitter`, which only ever touches `camera.rect` (the pillarbox
viewport), never `transform`.

Key public fields: `shakeDuration` (default 0.2s), `shakeMagnitude` (default
0.15). Key public method: `Shake()`.

## Enemy.cs

**Attached to:** `Enemy` prefab.
**Requires:** `Rigidbody2D`, `Collider2D` (trigger — see "Scene wiring"
below for why), tag `Enemy`, an `EnemyBullet` prefab reference.

Periodic downward fire (staggered per-instance via random initial delay),
takes damage via `TakeDamage(float)`, self-destructs at 0 HP or when
off-screen. **Kamikaze contact damage** — same shape as
`Minion.ApplyContactDamage` (see [level1-boss.md](level1-boss.md)): a ship
overlapping an `Enemy` (via `PlayerController.ResolveShipCollisions()`,
same manual-overlap idiom used for allies/boss/minions, not a Unity
trigger callback) takes `contactDamage` (1) once and the `Enemy` dies
immediately, funneled through a private `Die()` guarded by `isDead` (same
double-kill guard as `Minion.cs`, needed since `Destroy()` is deferred to
end-of-frame — a bullet kill and a ship contact landing in the same
physics step could otherwise double-fire). `TakeDamage(float)` and the
off-screen self-destruct both route through the same `Die()`. A new
`HalfExtents` (`Vector2`, cached from the `BoxCollider2D` in `Awake()`,
same as `Minion.HalfExtents`) is what `ResolveShipCollisions()` uses for
the overlap math, since runtime-spawned colliders can't be cached ship-side
in `Start()`. Movement is one of three shapes selected by the public
`movementPattern` field (nested `MovementPattern` enum: `SineWave`, `ZigZag`,
`StraightDive`), set externally by `EnemySpawner.cs` right after
`Instantiate()` — before `Start()` runs next frame, the same safe
assign-before-`Start()` ordering `Level1Boss.SpawnBullet()` already relies on for
`Bullet.damage`. Defaults to `SineWave`, so any stray direct-prefab spawn
that skips the spawner behaves exactly as it always has:

- **SineWave** (original, unchanged) — Galaga-style: Y descends at
  `moveSpeed`, X drifts as `startX + sin(Time.time * sineFrequency) *
  sineAmplitude`.
- **ZigZag** — Y still descends at `moveSpeed`; X accumulates by
  `zigzagSpeed * Time.deltaTime` in a direction that flips every
  `zigzagInterval` seconds — a real alternating step, not a smoother sine,
  reads distinctly more erratic.
- **StraightDive** — X locked to `startX` (no horizontal movement at all);
  Y descends at `moveSpeed * diveSpeedMultiplier` — faster, no dodging via
  horizontal reads.

**Static registry** — `public static readonly List<Enemy> Active`,
populated in `Awake()`/depopulated in `OnDestroy()`, same pattern as
`Bullet.Active`/`Minion.Active` (see [level1-boss.md](level1-boss.md)).
Added so `LevelSequencer` can detect "zero enemies on screen" before
starting the boss's entrance — see
[level-sequencing.md](level-sequencing.md).

**Explosive death** — ported from `Minion.cs`'s `MinionType`/fragment-burst
mechanic (see [level1-boss.md](level1-boss.md)'s "Minion.cs /
MinionSpawner.cs"), same idiom: a nested `EnemyType` enum (`Standard`,
`Explosive`), and a public `Init(MovementPattern, EnemyType)` — replacing
`EnemySpawner`'s old bare `movementPattern = ...` field assignment — that
sets both fields and, for `Explosive`, tints the `SpriteRenderer` to
`explosiveTintColor` (safe to call right after `Instantiate()`, since Unity
runs `Awake()` synchronously during `Instantiate` — the tint doesn't need to
wait for `Start()`). `Die()` calls a new private `SpawnFragments()` before
`Destroy()` whenever `type == Explosive` — an evenly-spaced ring of
`fragmentCount` (8) `Bullet`s, a single shared random start-offset per
burst (not per-fragment), `fragmentDamage` (1) each, owner `"Enemy"`, using
`fragmentPrefab` if assigned or falling back to the enemy's own
`bulletPrefab` otherwise. Since both the gunfire (`TakeDamage`) and
kamikaze (`ApplyContactDamage`) paths already funnel through this same
`Die()`, either kill triggers the burst — no changes needed to either
method. Each fragment's `SpriteRenderer` is also tinted to
`explosiveTintColor` right after `Init()`, so it visually reads as a piece
of the enemy that exploded rather than a plain bullet (same tweak applied
to `Minion.cs`'s fragments — see [level1-boss.md](level1-boss.md)). A `Standard` enemy's `Die()` is unaffected (no burst).

Key public fields: `moveSpeed`, `sineAmplitude`, `sineFrequency`,
`movementPattern`, `zigzagInterval`, `zigzagSpeed`, `diveSpeedMultiplier`,
`health`, `bulletPrefab`, `fireInterval`, `bulletSpeed`, `contactDamage`
(1), `type` (`EnemyType`, default `Standard`), `fragmentPrefab`,
`fragmentCount` (8), `fragmentSpeed` (5), `fragmentDamage` (1),
`explosiveTintColor`. Key public methods: `ApplyContactDamage(GameObject)`,
`Init(MovementPattern, EnemyType)`.

## EnemySpawner.cs

**Attached to:** `Spawner` GameObject (positioned above camera view).
**Requires:** an `Enemy` prefab reference.

Spawns waves of enemies on a repeating interval — this is Level 1's minion
system: `LevelSequencer` runs it before the boss appears and again once the
boss reaches phase 2 (see [level-sequencing.md](level-sequencing.md)).
Doesn't self-start: `public void StartSpawning()` (re-arms
`InvokeRepeating(nameof(SpawnWave), 0f, waveInterval)`) and `public void
StopSpawning()` (`CancelInvoke` — only halts *future* waves; a wave already
in flight via `SpawnWaveRoutine` finishes naturally so a formation doesn't
spawn half its enemies) are called externally by `LevelSequencer` instead.

Each wave picks a **formation** (nested `WaveFormation` enum: `Random`,
`Line`, `Cluster`, `VFormation`) from the public `formationOrder` array, at
random (`formationOrder[Random.Range(0, formationOrder.Length)]`) — no
no-repeat guard, unlike `Level1Boss.cs`'s Pattern Barrage random-no-repeat
pick; this is the only caller of `formationOrder` today, so the plain
random pick was the simplest change that satisfies "random order." Each
formation pairs one spawn shape with one `Enemy.MovementPattern`
(`MovementPatternFor()`); each spawned `Enemy` also independently rolls an
`Enemy.EnemyType` (`Random.value < explosiveEnemyChance ? Explosive :
Standard`, default chance `0.3`, same idiom as `MinionSpawner`'s
`explosiveMinionChance`) — both are passed together into the enemy's new
`Init(movementPattern, enemyType)` call right after `Instantiate()` (see
`Enemy.cs` above):

- **Random** (original behavior, unchanged) — uniform-random X per enemy,
  `spawnInterval` stagger, `SineWave` movement.
- **Line** — evenly spaced across `spawnWidth`, spawned with no stagger so
  they read as a line, `SineWave` movement.
- **Cluster** — one random center X rolled **once per wave** (not per
  enemy — a wave-scoped `clusterCenterX` local, passed into the private
  `PositionFor()` helper), each enemy jittered by `clusterJitter`, `ZigZag`
  movement.
- **VFormation** — symmetric X offsets around center plus a Y offset
  (`vFormationYStep` per position from center) so the wave visibly forms a
  V as it descends, `StraightDive` movement — the hardest tier.

Key public fields: `enemyPrefab`, `enemiesPerWave`, `spawnInterval`,
`waveInterval`, `spawnWidth`, `formationOrder`, `clusterJitter`,
`vFormationYStep`, `explosiveEnemyChance` (0.3). Key public methods:
`StartSpawning()`, `StopSpawning()`.

## Scene wiring

### Player (combat-relevant components)

| Component            | Key inspector values                                                 |
| --------------------- | ---------------------------------------------------------------------- |
| **PlayerController.cs** | bulletPrefab: PlayerBullet prefab, firePoint: FirePoint child, bulletSpeed: 12, recoilDamping: 8 (shotsPerSecond/fireDamage/moveSpeed all overwritten by role at `Start()` — see [player-roles.md](player-roles.md)) |
| **PlayerHealth.cs**   | OnDeath: `GameOverPanel/GameOverUI.Show()` + `PartyFrame_1/PartyFrameUI.OnPlayerDied()`, OnDamaged: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` (maxHealth/maxShield overwritten by role at `Awake()`) |
| **PlayerDamageFlash.cs** | flashColor: white, flashDuration: 0.12 |

### Main Camera (combat-relevant components)

| Component          | Key inspector values                  |
| ------------------- | ---------------------------------------- |
| **CameraShake.cs**  | shakeDuration: 0.2, shakeMagnitude: 0.15 |

### GameOverPanel

Child of `HUDCanvas` (see [hud-layout.md](hud-layout.md) for why it lives
there instead of `GameplayCanvas`). Full-rect dark overlay (`Image`, ~75%
alpha black), a centered "Game Over" `TextMeshProUGUI`, and Restart +
Change Roles `Button`s. Starts active in the saved scene; `GameOverUI.Awake()`
force-hides it at Play-mode start.

### Spawner

Empty GameObject positioned **above** the visible camera area (y ≈ 8).

| Component           | Key inspector values                                                     |
| -------------------- | ---------------------------------------------------------------------------- |
| **EnemySpawner.cs**  | enemyPrefab: Enemy prefab, enemiesPerWave: 5, spawnInterval: 0.5, waveInterval: 4, spawnWidth: 6, formationOrder: [Random, Line, Cluster, VFormation], clusterJitter: 0.5, vFormationYStep: 0.4, explosiveEnemyChance: 0.3 |

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
| Transform          | scale (0.6, 0.6, 1) — matches every ship's scale (see [movement.md](movement.md)); was 1.0 ("too big" relative to the party) before the Level 1 rework |
| Sprite Renderer    | Any placeholder sprite                                                        |
| Collider2D         | Is Trigger: **ON** — was `OFF` until the Level 1 rework; a solid collider on a `Dynamic` `Rigidbody2D` let real Box2D physics push enemies and the boss around on contact (see [level-sequencing.md](level-sequencing.md)'s "Boss visibility/collision"). Trigger-vs-trigger and trigger-vs-solid pairs still fire `OnTriggerEnter2D` normally, so `Bullet.cs`'s hit detection is unaffected. Ship-vs-`Enemy` kamikaze contact damage (see `Enemy.cs` above) is a separate, unrelated mechanism — manual overlap math in `PlayerController.ResolveShipCollisions()`, not a Unity trigger/physics callback — so it isn't affected by this setting either. |
| **Enemy.cs**       | moveSpeed: 2, sineAmplitude: 1.5, sineFrequency: 1, movementPattern: SineWave (default, set per-instance by EnemySpawner), zigzagInterval: 0.4, zigzagSpeed: 3, diveSpeedMultiplier: 1.6, health: 3, bulletPrefab: EnemyBullet prefab, fireInterval: 1.5, bulletSpeed: 6, contactDamage: 1, type: Standard (default, set per-instance by EnemySpawner), fragmentPrefab: unassigned (falls back to bulletPrefab), fragmentCount: 8, fragmentSpeed: 5, fragmentDamage: 1, explosiveTintColor: orange |
