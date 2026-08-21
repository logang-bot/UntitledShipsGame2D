# Player Roles

## PlayerRole.cs

**Defines:** `PlayerRole` enum (`Attacker`, `Tank`, `Medic`, `Support`),
`RoleStats` struct, and the static `PlayerRoleStats` lookup table (one
`RoleStats` per role).

`RoleStats` holds **fixed, absolute per-role values** — `maxHealth`,
`maxShield`, `fireDamage`, `shotsPerSecond`, `moveSpeed`, `tintColor` — not
multipliers on a shared base. This is a deliberate architecture change
(2026-08-21, replacing the original `base × multiplier` design from
Session 4 onward): multipliers made hand-tuning confusing (e.g. "Tank
health is `5 × 1.6`" instead of just "Tank health is `8`"), and fire rate
in particular was stored *inverted* (`fireRate` used to mean seconds
between shots — lower was faster — despite reading like a rate). See
"Fixed per-role stats" below for the full table and the source of truth
this establishes. Temporary effects (buffs) are layered on **non-destructively**
at the point of use instead — see `PlayerController.speedBuffMultiplier`/
`fireRateBuffMultiplier` below — never by mutating these base values.

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

- `PlayerController.Start()` assigns `moveSpeed`/`shotsPerSecond`/`fireDamage`
  directly from `Stats.moveSpeed`/`Stats.shotsPerSecond`/`Stats.fireDamage`
  (a straight overwrite now, not a multiplication).
- `PlayerHealth.Awake()` assigns `maxHealth`/`maxShield` directly from
  `Stats.maxHealth`/`Stats.maxShield`.
- Both do a null-check on `GetComponent<PlayerRoleComponent>()`, keeping the
  script's own inspector-set default as a fallback when the component is
  missing.

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

- **Tank — Taunt**: `public UnityEvent OnTaunt`, invoked on activation. Has
  a **real** effect — see "Aggro/targeting" below and [boss.md](boss.md) —
  a persistent listener redirects the boss's target to the taunter
  (`Boss.TauntedBy(GameObject)`). The Session 9 placeholder feedback
  (`Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()`)
  is kept alongside it, additive, not replaced.
- **Tank — Shield Arc** (new, 2026-08-21, passive/always-on — independent
  of Taunt, not an `E`-triggered ability): a wide, curved shield in front
  of Tank, both visual and **functional**. Built procedurally in `Awake()`
  only for `role == Tank` (same "only build what this role needs"
  precedent as Medic's aura ring): a child `ShieldArc` GameObject, tagged
  `Player`, with a local-space `EdgeCollider2D` (`isTrigger`) and matching
  `LineRenderer`, both sampling the same shallow-parabola point set —
  `shieldArcWidthMultiplier` (3x Tank's own `BoxCollider2D` width) wide,
  `shieldArcHeight` (0.4) tall, offset `shieldArcYOffset` (0.3) above the
  body. Local-space and built once, so it tracks Tank's movement and needs
  **no per-frame `Update()`** (unlike Medic's ring, which resizes on boost
  and therefore needs one). Relies on a one-line `Bullet.cs` fix —
  `other.GetComponentInParent<PlayerHealth>()` instead of
  `other.GetComponent<PlayerHealth>()` — so a hit on this child collider
  (which has no `PlayerHealth` of its own) still routes into Tank's own
  shield/health pool, exactly like a direct hit. This means a bullet that
  would've missed Tank's own body but crossed the arc's wider span gets
  blocked **and** costs Tank shield/health — not a free block. Player-owned
  bullets pass through untouched (`Bullet.cs`'s player-bullet branch only
  checks the `Enemy` tag, and the arc is tagged `Player`). **Known edge
  case, not defended against**: if the arc's collider region vertically
  overlaps Tank's own body collider, a bullet could in rare cases enter
  both in the same physics step and double-hit — mitigated in practice by
  `shieldArcYOffset` placing the arc above the body, consistent with this
  project's "flag it, don't over-engineer for a rare edge case" style (see
  the documented lack of bullet-dodging).
- **Medic — Aura Boost** (implemented Session 13, replacing the original
  instant self-heal entirely — see [boss.md](boss.md)'s "Medic positioning
  + proximity aura"): Medic passively regenerates health *and* shield of
  every ally in `allies[]` within `auraRadius` every `auraTickInterval`,
  whether human- or AI-controlled — this is what finally resolves the old
  "Medic heal only targets self" gap, as a proximity aura rather than
  manual ally-targeting. The default aura is **deliberately tiny**
  (`auraRadius` 0.5 — allies must nearly touch the Medic); pressing `E`
  (`TriggerAuraBoost()`) temporarily swaps to a larger `auraBoostRadius`
  and a much faster `auraBoostTickInterval` (0.25s vs. 1s) for
  `auraBoostDuration` (4s), via the same `StopCoroutine`/`StartCoroutine`
  restart-safety pattern used elsewhere — same "cooldown must stay ≥
  duration" constraint applies (`auraBoostCooldown` 10s ≥
  `auraBoostDuration` 4s). **`auraBoostRadius` was halved, 2026-08-21** (`3`
  → `1.5`) — flagged overpowered at the original size. A `LineRenderer`
  ring around the Medic (dim/thin by default, bright/thick while boosted)
  shows the live radius, and allies actually healed by a tick get a
  distinct green flash (`PlayerDamageFlash.Flash(Color)`, a new overload of
  the existing damage-flash mechanism) — both purely visual, no gameplay
  effect.
- **Support — Speed Boost** (renamed from "Buff" and fully redesigned,
  2026-08-21): a **party-wide**, non-destructive multiplier on both move
  speed and fire rate, not a self-only effect. `TriggerSpeedBoost()` loops
  over `allies[]` (all 4 ships, self-included — the same array Medic's aura
  already uses) and sets each ally's `PlayerController.speedBuffMultiplier`/
  `fireRateBuffMultiplier` to `speedBoostMultiplier` (1.5) for
  `speedBoostDuration` (4s), then resets both to `1f` when it ends — plain
  assignment on both ends, so unlike the old self-only buff (which
  multiplied `moveSpeed`/`fireRate` in place and divided back out, needing
  `buffCooldown ≥ buffDuration` to avoid double-applying), there's no
  revert arithmetic and nothing to double-apply. **`speedBoostCooldown`
  bumped 8s → 15s** — flagged overpowered once it became party-wide, round
  placeholder, tunable. **New party-wide visual**: every ship (any role,
  built unconditionally in `Awake()` — not role-gated like Medic's ring or
  Tank's arc, since any of the 4 could receive the boost) has its own
  initially-hidden `PartyBuffRing`, toggled by the caster's new
  `SetPartyBuffVisual(bool, Color)` call on each ally — all 4 rings light
  up in the caster's tint color (Support's gold) the instant the boost
  starts and disappear together the instant it ends.
- **Attacker — Big Shot**: calls `PlayerController.FireBigShot(widthMultiplier,
  damageAmount)` (`3x` width) and `PlayerController.AddRecoil(Vector2.down
  * recoilForce)`. **Damage is now a live multiplier of the caster's
  current `fireDamage`** (`bigShotDamageMultiplier`, `2x`), computed at
  cast time — `2.0 × 2 = 4.0` at today's values — rather than a
  separately hand-tuned flat number, so it automatically stays proportional
  if `fireDamage` is ever retuned again. See [combat.md](combat.md) for why
  recoil has to be a decaying velocity blended into `HandleMovement()`
  rather than a physics impulse (`MovePosition` overwrites plain
  `AddForce` every `FixedUpdate`).

Key public fields: `tauntCooldown` (5s), `OnTaunt`; `shieldArcWidthMultiplier`
(3), `shieldArcHeight` (0.4), `shieldArcYOffset` (0.3),
`shieldArcColor`/`shieldArcLineWidth`; `allies[]`, `auraRadius` (0.5),
`auraTickInterval` (1s), `auraHealPerTick`/`auraShieldPerTick` (1 each),
`auraBoostRadius` (1.5), `auraBoostTickInterval` (0.25s), `auraBoostDuration`
(4s), `auraBoostCooldown` (10s), `auraRingColor`/`auraRingBoostedColor`/
`auraRingWidth`/`auraRingBoostedWidth`, `healFlashColor`; `speedBoostCooldown`
(15s), `speedBoostDuration` (4s), `speedBoostMultiplier` (1.5),
`partyBuffRingRadius`/`partyBuffRingWidth`; `bigShotCooldown` (3s),
`bigShotWidthMultiplier` (3), `bigShotDamageMultiplier` (2), `recoilForce`
(6). Key public method: `OnAbility(InputValue)`.

Also exposes read-only status for the HUD (see `PartyFrameUI.cs` in
[hud-layout.md](hud-layout.md)): `CooldownRemaining`, `IsSpeedBoostActive`,
`SpeedBoostRemaining`, `AbilityName` (per-role display name), and
`StatusText` (formatted cooldown/`Ready`/active-boost string) — these are
the single source of truth for ability state so the HUD never duplicates
cooldown math.

`OnAbility(InputValue)` (the `Player Input`-driven entry point above) is now
a thin wrapper around a public, non-input entry point — `TryUseAbility()` —
extracted so `AIController.cs` (see [boss.md](boss.md)) can trigger a CPU
teammate's ability directly, going through the exact same cooldown gate and
role-dispatch switch as the human player. The `Trigger*` methods stay
private/unchanged. **Planned** (see [boss.md](boss.md)'s "Manual teammate
ability triggering"): this same `TryUseAbility()` entry point is also meant
to be called from a click/tap on that teammate's party frame, letting the
human player force a specific teammate's ability to fire on demand.

## Shield stat (implemented)

Agreed design, built 2026-08-20, see [boss.md](boss.md)'s "AI teammate
behavior" for the motivating context (Tank physically blocking bullets,
since extended by the Shield Arc above). A second, health-like pool per
role (`PlayerHealth.maxShield`/`CurrentShield`), a fixed per-role value
alongside `maxHealth` (see "Fixed per-role stats" below):

- **Absorbs damage before health** — `PlayerHealth.TakeDamage(int)` deducts
  from `currentShield` first, down to 0; only the remainder subtracts from
  `currentHealth`. A hit fully absorbed by shield still fires `OnDamaged`
  (flash/shake feedback), same mutual-exclusivity-with-`Die()` rule as
  before (see [combat.md](combat.md)).
- **No passive regen of its own** — `PlayerHealth.RestoreShield(int)`
  (symmetric to `Heal(int)`, clamps at `maxShield`) is only ever called by
  Medic's proximity aura (see the Medic ability entry above and
  [boss.md](boss.md)), never on its own over time. Deliberate: keeps Tank
  meaningfully dependent on Medic rather than being self-sufficient,
  matching the MMO-raid "tank and healer" coupling this project is modeled
  on (`../overview.md`).
- **Shield bar**: a fixed shield-blue bar on the party frame, not
  role-tinted — see [hud-layout.md](hud-layout.md).

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

## Fixed per-role stats (single source of truth)

**Architecture change, 2026-08-21** — replaces every `base × multiplier`
table this doc previously had. `RoleStats` (see `PlayerRole.cs` above) now
holds one fixed, absolute number per stat per role — no multipliers, no
shared base, no rounding. This is the entire source of truth for a role's
numbers; nothing else in the codebase independently defines health, shield,
fire damage, fire rate, or move speed.

| Role     | Health | Shield | Fire damage | Fire rate | Move speed |
| -------- | ------ | ------ | ------------ | --------- | ---------- |
| Attacker | 6      | 5      | 2.0          | 2.5/s     | 3.0 u/s    |
| Tank     | 8      | 20     | 1.0          | 1/s       | 1.5 u/s    |
| Medic    | 4      | 3      | 0.7          | 1.5/s     | 3.0 u/s    |
| Support  | 5      | 3      | 1.0          | 2/s       | 4.5 u/s    |

Units: **Fire rate** is shots/second (higher = faster) — `PlayerController.shotsPerSecond`,
replacing the old, misleadingly-named `fireRate` field that actually stored
*seconds between shots* (lower was faster). **Move speed** is world
units/second (`PlayerController.moveSpeed`), already unambiguous, no change
in kind. All values are placeholders, tunable until real playtesting lands
— every role now has a deliberately-chosen number for every stat (no more
"left at the undecided 1.0x baseline" placeholders).

**Buffs are layered on non-destructively, not by mutating these values.**
`PlayerController` has two runtime-only multiplier fields —
`speedBuffMultiplier`, `fireRateBuffMultiplier` (both default `1f`) — read
at the point of use (`HandleMovement()`'s move vector, and a computed
`FireInterval => 1f / (shotsPerSecond * fireRateBuffMultiplier)` for the
fire-cooldown gate) rather than ever being multiplied into `moveSpeed`/
`shotsPerSecond` themselves. Only `PlayerAbility` (Support's Speed Boost,
see above) ever sets them, and only ever via plain assignment — there is no
revert-by-dividing-back-out anywhere in this system anymore, which is what
made the old self-only buff need `buffCooldown ≥ buffDuration` to avoid
double-applying.

## Scene wiring — Player

| Component               | Key inspector values                            |
| ------------------------ | -------------------------------------------------- |
| **PlayerRoleComponent**  | role: Attacker (change in Inspector to test other roles) |
| **PlayerAbility.cs**     | defaults as listed above; `OnTaunt`: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` + `Boss/Boss.TauntedBy(Player)` (real aggro redirect, see [boss.md](boss.md); same 3 listeners wired on each `Teammate_*`'s `PlayerAbility`, each pointing `TauntedBy` at itself) |

Confirmed attached and working: verified live via the Unity MCP bridge —
entering Play mode with the default `Attacker` role showed `maxHealth = 6`,
`maxShield = 5`, `fireDamage = 2.0`, `shotsPerSecond = 2.5`, `moveSpeed =
3.0`, matching the table above exactly, and the sprite tinted red.
`PlayerAbility` was verified the same way per-role: Medic's aura heals/
shields allies within its (tiny, default) radius and not outside it, and
`TryUseAbility()`'s boost expands the radius/tick rate for its duration
before reverting automatically; Tank taunt's `OnTaunt` event fires and is
cooldown-gated; **Tank's Shield Arc** was verified functionally, not just
visually — a fake enemy bullet placed within the arc's width but outside
Tank's own body collider was destroyed and Tank's `CurrentShield` dropped
by the bullet's exact damage (confirming the `Bullet.cs`
`GetComponentInParent` fix correctly routes the hit into Tank's own health
pool, not a silent no-op), while a same-position player-owned bullet passed
through untouched; **Support's Speed Boost** was confirmed to set all 4
ships' buff multipliers and activate all 4 party-buff rings together, then
clear both together when the boost ended; Attacker's big shot spawns a
bullet with `localScale.x` 3x normal and `damage` equal to the caster's
live `fireDamage × bigShotDamageMultiplier` (`4.0` at today's values), and
the recoil impulse visibly moves the ship and decays back to a stable,
non-drifting stop.

**Known gotcha hit again this pass**: same class of issue as every prior
tuning session — changing a field's *script* default (`auraBoostRadius`,
`Boss.maxHealth`) does not retroactively update an already-serialized
value on an existing scene GameObject or prefab. Both had to be set
explicitly on all 4 live scene instances (and `Teammate.prefab`'s/
`Boss.prefab`'s defaults), verified by a full scene reload from disk. Newly
*added* fields (e.g. `speedBuffMultiplier`, the Shield Arc's fields) don't
have this problem — they pick up the script default automatically since
there's no prior serialized value to conflict with.

## Not yet built

- Local co-op with multiple **human** players isn't wired up — the 3 extra
  ships fighting alongside `Player` (`Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support`) are CPU-controlled via `AIController.cs`, not real
  players; see [boss.md](boss.md).
- All four AI teammate roles now have real positioning (see
  [boss.md](boss.md)'s "Tank guard-point positioning" / "Medic positioning
  + proximity aura" / "Support roaming positioning" / "Attacker patrol +
  boss-tracking positioning"). Bullet-dodging, teammate separation, and
  manual teammate-ability triggering from the party frame are all designed
  (see [boss.md](boss.md)'s "AI teammate behavior" / "Manual teammate
  ability triggering") but not yet implemented.

Role display on the HUD (name/role text + tinted health bar) is now live
for all 4 party members (`PartyFrame_1..4`, one per `Player`/`Teammate_*`)
— see [hud-layout.md](hud-layout.md)'s `PartyFrameUI.cs`/`PartyFrameManager.cs`
entries.
