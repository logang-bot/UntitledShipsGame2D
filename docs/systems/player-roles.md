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

- **Tank — Taunt**: `public UnityEvent OnTaunt`, invoked on activation. Now
  has a **real** effect — see "Aggro/targeting" below and
  [boss.md](boss.md) — a persistent listener redirects the boss's target to
  the taunter (`Boss.TauntedBy(GameObject)`). The Session 9 placeholder
  feedback (`Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()`)
  was kept alongside it, additive, not replaced.
- **Medic — Heal**: calls the new `PlayerHealth.Heal(int)` (symmetric to
  `TakeDamage(int)`, clamps at `maxHealth`) on **self**. Ally-targeting
  isn't built yet, even though allies (the AI teammates — see
  [boss.md](boss.md)) now exist to target.
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

`OnAbility(InputValue)` (the `Player Input`-driven entry point above) is now
a thin wrapper around a public, non-input entry point — `TryUseAbility()` —
extracted so `AIController.cs` (see [boss.md](boss.md)) can trigger a CPU
teammate's ability directly, going through the exact same cooldown gate and
role-dispatch switch as the human player. The four `Trigger*` methods stay
private/unchanged.

## Aggro / targeting (implemented — on `Boss`, not `Enemy`)

**Targeting** is how an enemy AI decides which player to attack when
multiple are available. **Aggro** ("aggression"/threat) is the per-target
value that decision is based on — an enemy tracks how much attention each
player has drawn and attacks whoever currently has the highest aggro
against it. **Taunt** is an ability that artificially spikes the caster's
aggro to the top, forcing the enemy to switch targets — the classic
MMO-raid "tank and spank" mechanic this project is explicitly modeled on
(see `../overview.md`).

`Enemy.cs` still has **no targeting concept at all** — regular wave enemies
move in a fixed sine-wave and fire on a timer regardless of who or where any
player is; that was a deliberate scope decision, not an oversight, since
adding a guessed-at targeting shape to the disposable wave-enemy script
would've risked being the wrong shape once the real boss AI design
happened. The real threat-table aggro system was instead built directly on
the new `Boss.cs` once the boss prototype gave it something concrete to
target — see [boss.md](boss.md) for the full design (a plain
`Dictionary<GameObject, float>` of damage-dealt-per-target, no decay,
`TauntedBy(GameObject)` spiking the caster above everyone else).

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
| **PlayerAbility.cs**     | defaults as listed above; `OnTaunt`: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` + `Boss/Boss.TauntedBy(Player)` (real aggro redirect, see [boss.md](boss.md); same 3 listeners wired on each `Teammate_*`'s `PlayerAbility`, each pointing `TauntedBy` at itself) |

Confirmed attached and working: verified live via the Unity MCP bridge —
entering Play mode with the default `Attacker` role showed `maxHealth` 5→4,
`fireRate` 0.35→0.2625 (base fire interval as of the boss-fight tuning
pass, [boss.md](boss.md); was 0.2→0.15 before that pass), and the sprite
tinted red, matching the table above.
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

- Local co-op with multiple **human** players isn't wired up — the 3 extra
  ships fighting alongside `Player` (`Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support`) are CPU-controlled via `AIController.cs`, not real
  players; see [boss.md](boss.md).
- Medic heal only targets self — mechanically complete
  (`PlayerHealth.Heal(int)`), just not extended to target an ally yet, even
  though allies (the AI teammates) now exist to target.

Role display on the HUD (name/role text + tinted health bar) is now live
for all 4 party members (`PartyFrame_1..4`, one per `Player`/`Teammate_*`)
— see [hud-layout.md](hud-layout.md)'s `PartyFrameUI.cs`/`PartyFrameManager.cs`
entries.
