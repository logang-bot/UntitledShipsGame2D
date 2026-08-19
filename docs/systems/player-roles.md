# Player Roles

## PlayerRole.cs

**Defines:** `PlayerRole` enum (`Attacker`, `Tank`, `Medic`, `Support`),
`RoleStats` struct (health/fire-rate/move-speed multipliers + sprite tint
`Color`), and the static `PlayerRoleStats` lookup table (one `RoleStats` per
role, placeholder balancing values).

No `ScriptableObject` asset workflow — role data is a static in-code table,
matching the project's existing plain-`MonoBehaviour`, low-infra style. Easy
to migrate to `ScriptableObject`s later if hand-tuning in the Inspector
becomes worth the friction.

## PlayerRoleComponent.cs

Deliberately its own file, separate from `PlayerRole.cs` — Unity requires a
`MonoBehaviour`/`ScriptableObject` class to be the filename-matching class
in its file for reliable script serialization. `PlayerRoleComponent` was
originally bundled into `PlayerRole.cs` (whose matching class is the enum);
that produced a broken, non-asset-backed script reference on the component
(silently, no compile error) — Unity logged "referenced script is missing"
only once something tried to actually use the component at runtime. See
[../unity-notes.md](../unity-notes.md) for the general gotcha. Fixed by
moving it to its own filename-matching file, which is also consistent with
every other script in this project (one class per file).

**Attached to:** `Player` GameObject (alongside `PlayerController` and
`PlayerHealth` — see [combat.md](combat.md) and [movement.md](movement.md)).
**Requires:** nothing external; tints its own `SpriteRenderer` if one is
present.

Holds the `role` field for this player instance and exposes `Stats`
(computed on access via `PlayerRoleStats.Get(role)` — not cached in `Awake`,
so it's safe regardless of Unity's unordered `Awake`/`Start` execution
across sibling components).

- `PlayerController.Start()` multiplies `moveSpeed`/`fireRate` by
  `Stats.moveSpeedMultiplier`/`Stats.fireRateMultiplier`.
- `PlayerHealth.Awake()` multiplies `maxHealth` by
  `Stats.healthMultiplier`.
- Both do a null-check on `GetComponent<PlayerRoleComponent>()` so behavior
  is unchanged if the component is missing.

Key public fields: `role` (default `Attacker`).

## PlayerAbility.cs

**Attached to:** `Player` GameObject.
**Requires:** `PlayerRoleComponent`, `PlayerController`, `PlayerHealth` on
the same GameObject (cached in `Awake()`).

One script, not four — branches on `PlayerRoleComponent.role` in a single
`OnAbility(InputValue)` handler (auto-called by `Player Input`'s Send
Messages behavior on the new `Ability` action, bound to `E` — see
[input.md](input.md)), matching the same "one component reads the role enum"
shape already used by `PlayerRoleComponent.Stats`. A single
`Time.time`-based cooldown gate (`nextAbilityTime`, same pattern as
`PlayerController`'s `nextFireTime`) blocks re-activation until the current
role's cooldown elapses.

- **Tank — Taunt**: `public UnityEvent OnTaunt`, invoked on activation.
  There's no boss to redirect aggro on yet — see "Aggro/targeting" below —
  so `OnTaunt` currently drives a **placeholder** feedback pair instead:
  `Player/PlayerDamageFlash.Flash()` and `Main Camera/CameraShake.Shake()`
  (the exact same effects `PlayerHealth.OnDamaged` already uses — see
  [combat.md](combat.md)), purely so pressing `E` as Tank visibly does
  *something* while no real target exists. The boss encounter prototype
  (`roadmap.md`) adds a real aggro-redirect listener once a boss exists;
  the placeholder listeners can stay alongside it or be removed then.
- **Medic — Heal**: calls the new `PlayerHealth.Heal(int)` (symmetric to
  `TakeDamage(int)`, clamps at `maxHealth`) on **self**. Ally-targeting is
  deferred until a second player/AI teammate exists to target.
- **Support — Buff**: temporarily multiplies `PlayerController.moveSpeed`
  and `fireRate` (both already role-scaled once at `Start()`), via a
  coroutine that reverts by dividing back out after `buffDuration` —
  identical restart-on-repeat pattern to `PlayerDamageFlash.cs`/
  `CameraShake.cs` (see [combat.md](combat.md)). **Constraint**:
  `buffCooldown` must stay ≥ `buffDuration` (defaults: 8s ≥ 4s) — the
  revert divides out a fixed multiplier, so re-triggering before the
  previous buff has reverted would double-apply it. The cooldown gate
  already prevents this under the shipped defaults; don't lower
  `buffCooldown` below `buffDuration` without changing the revert logic
  too.
- **Attacker — Big Shot**: calls `PlayerController.FireBigShot(widthMultiplier,
  damageAmount)` (`3x` width, `3` damage vs. a regular bullet's `1`) and
  `PlayerController.AddRecoil(Vector2.down * recoilForce)` — see
  [combat.md](combat.md) for `Bullet.damage` and why recoil has to be a
  decaying velocity blended into `HandleMovement()` rather than a physics
  impulse (`MovePosition` overwrites plain `AddForce` every `FixedUpdate`).

Key public fields: `tauntCooldown` (5s), `OnTaunt`; `healCooldown` (6s),
`healAmount` (2); `buffCooldown` (8s), `buffDuration` (4s),
`buffMoveSpeedMultiplier` (1.3), `buffFireRateMultiplier` (0.7, lower =
faster); `bigShotCooldown` (3s), `bigShotWidthMultiplier` (3),
`bigShotDamage` (3), `recoilForce` (6). Key public method:
`OnAbility(InputValue)`.

Also exposes read-only status for the HUD (see `PartyFrameUI.cs` in
[hud-layout.md](hud-layout.md)): `CooldownRemaining`, `IsBuffActive`,
`BuffRemaining`, `AbilityName` (per-role display name), and `StatusText`
(formatted cooldown/`Ready`/active-buff string) — these are the single
source of truth for ability state so the HUD never duplicates cooldown
math.

## Aggro / targeting (concept, not yet implemented)

**Targeting** is how an enemy AI decides which player to attack when
multiple are available. **Aggro** ("aggression"/threat) is the per-target
value that decision is based on — an enemy tracks how much attention each
player has drawn and attacks whoever currently has the highest aggro
against it. **Taunt** is an ability that artificially spikes the caster's
aggro to the top, forcing the enemy to switch targets — the classic
MMO-raid "tank and spank" mechanic this project is explicitly modeled on
(see `../overview.md`).

`Enemy.cs` currently has **no targeting concept at all** — enemies don't
track the player's position or any per-player value; they move in a fixed
sine-wave and fire on a timer regardless of who or where the player is. So
there's no aggro system for Tank taunt to hook into today. Building one is
explicitly boss-prototype scope (`../roadmap.md`'s "Boss encounter
prototype — ... Tank taunt forces boss aggro"), not this system — adding a
guessed-at targeting shape to `Enemy.cs` now would risk being the wrong
shape once the real boss AI design happens.

## Current balancing values (placeholders, tunable)

| Role     | Health ×   | Fire rate ×          | Move speed × | Tint            |
| -------- | ---------- | --------------------- | ------------- | ---------------- |
| Attacker | 0.8 (lower) | 0.75 (faster)          | 1.0            | red/orange        |
| Tank     | 1.6 (higher)| 1.2 (slower)           | 0.8 (slower)   | blue               |
| Medic    | 1.0         | 1.0                    | 1.0            | green              |
| Support  | 1.0         | 1.0                    | 1.15 (faster)  | yellow/gold        |

## Scene wiring — Player

| Component               | Key inspector values                            |
| ------------------------ | -------------------------------------------------- |
| **PlayerRoleComponent**  | role: Attacker (change in Inspector to test other roles) |
| **PlayerAbility.cs**     | defaults as listed above; `OnTaunt`: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` (placeholder feedback, no real target yet) |

Confirmed attached and working: verified live via the Unity MCP bridge —
entering Play mode with the default `Attacker` role showed `maxHealth` 5→4,
`fireRate` 0.2→0.15, and the sprite tinted red, matching the table above.
`PlayerAbility` was verified the same way per-role: Medic heal clamps
correctly at `maxHealth` and the cooldown gate blocks immediate re-use; Tank
taunt's `OnTaunt` event fires and is cooldown-gated; Support's buff applies
and reverts to the exact pre-buff baseline with no drift; Attacker's big
shot spawns a bullet with `localScale.x` and `damage` both 3x a regular
bullet's, and the recoil impulse visibly moves the ship and decays back to
a stable, non-drifting stop (confirmed the total displacement matches the
closed-form sum of the decaying-velocity series, not a runaway/broken
value).

## Not yet built

- Only one `Player` instance exists in the scene; local co-op (multiple
  players/roles at once) isn't wired up yet — `PartyFrame_2..4` have no data
  source until it is.
- Tank taunt has no boss to affect yet, and Medic heal only targets self —
  both are mechanically complete but await a real target (boss/AI teammate).

Role display on the HUD (name/role text + tinted health bar on
`PartyFrame_1`) is now live — see [hud-layout.md](hud-layout.md)'s
`PartyFrameUI.cs` entry.
