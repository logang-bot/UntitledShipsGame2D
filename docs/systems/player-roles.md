# Player Roles

## PlayerRole.cs

**Defines:** `PlayerRole` enum (`Attacker`, `Tank`, `Medic`, `Support`),
`RoleStats` struct, and the static `PlayerRoleStats` lookup table (one
`RoleStats` per role).

`RoleStats` holds **fixed, absolute per-role values** — `maxHealth`,
`maxShield`, `fireDamage`, `shotsPerSecond`, `moveSpeed`, `tintColor` — not
multipliers on a shared base. See "Fixed per-role stats" below for the full
table. Temporary effects (buffs) layer on **non-destructively** at the
point of use instead — see `PlayerController.speedBuffMultiplier`/
`fireRateBuffMultiplier` below — never by mutating these base values.

No `ScriptableObject` asset workflow — role data is a static in-code table,
matching the project's plain-`MonoBehaviour`, low-infra style.

## PlayerRoleComponent.cs

Its own file, separate from `PlayerRole.cs` — Unity requires a
`MonoBehaviour`/`ScriptableObject` class to be the filename-matching class
in its file for reliable script serialization. See
[../unity-notes.md](../unity-notes.md) for the general gotcha.

**Attached to:** `Player` GameObject (alongside `PlayerController` and
`PlayerHealth` — see [combat.md](combat.md) and [movement.md](movement.md)).
**Requires:** nothing external; tints its own `SpriteRenderer` if one is
present.

Holds the `role` field for this player instance and exposes `Stats`
(computed on access via `PlayerRoleStats.Get(role)` — not cached in `Awake`,
so it's safe regardless of Unity's unordered `Awake`/`Start` execution
across sibling components).

- `PlayerController.Start()` assigns `moveSpeed`/`shotsPerSecond`/`fireDamage`
  directly from `Stats.moveSpeed`/`Stats.shotsPerSecond`/`Stats.fireDamage`.
- `PlayerHealth.Awake()` assigns `maxHealth`/`maxShield` directly from
  `Stats.maxHealth`/`Stats.maxShield`.
- Both null-check `GetComponent<PlayerRoleComponent>()`, keeping the
  script's own inspector-set default as a fallback when the component is
  missing.

Key public fields: `role` (default `Attacker`).

## PlayerAbility.cs

**Attached to:** `Player` GameObject.
**Requires:** `PlayerRoleComponent`, `PlayerController`, `PlayerHealth` on
the same GameObject (cached in `Awake()`).

One script, not four — branches on `PlayerRoleComponent.role` in a single
`OnAbility(InputValue)` handler (auto-called by `Player Input`'s Send
Messages behavior on the `Ability` action, bound to `E` — see
[input.md](input.md)). A single `Time.time`-based cooldown gate
(`nextAbilityTime`, same pattern as `PlayerController`'s `nextFireTime`)
blocks re-activation until the current role's cooldown elapses.

- **Tank — Taunt**: `public UnityEvent OnTaunt`, invoked on activation. A
  persistent listener redirects the boss's target to the taunter
  (`Boss.TauntedBy(GameObject)` — see [boss.md](boss.md)'s "Aggro /
  targeting"), alongside `PlayerDamageFlash.Flash()` + `CameraShake.Shake()`
  feedback.
- **Tank — Shield Arc** (passive/always-on, independent of Taunt, not
  `E`-triggered): a wide, curved shield in front of Tank, both visual and
  **functional**. Built procedurally in `Awake()` only for `role == Tank`:
  a child `ShieldArc` GameObject, tagged `Player`, with a local-space
  `EdgeCollider2D` (`isTrigger`) and matching `LineRenderer`, both sampling
  the same shallow-parabola point set — `shieldArcWidthMultiplier` (3x
  Tank's own `BoxCollider2D` width) wide, `shieldArcHeight` (0.4) tall,
  offset `shieldArcYOffset` (0.3) above the body. Local-space and built
  once, so it tracks Tank's movement with no per-frame `Update()` needed.
  Relies on `Bullet.cs`'s `other.GetComponentInParent<PlayerHealth>()` (not
  `GetComponent`), so a hit on this child collider (which has no
  `PlayerHealth` of its own) routes into Tank's own shield/health pool,
  exactly like a direct hit — a bullet that would've missed Tank's own body
  but crossed the arc's wider span gets blocked **and** costs Tank
  shield/health, not a free block. Player-owned bullets pass through
  untouched (`Bullet.cs`'s player-bullet branch only checks the `Enemy`
  tag, and the arc is tagged `Player`).
- **Medic — Aura Boost**: Medic passively regenerates health *and* shield
  of every ally in `allies[]` within `auraRadius` every `auraTickInterval`,
  whether human- or AI-controlled. The default aura is **deliberately
  tiny** (`auraRadius` 0.5 — allies must nearly touch the Medic); pressing
  `E` (`TriggerAuraBoost()`) temporarily swaps to a larger
  `auraBoostRadius` (1.5) and a much faster `auraBoostTickInterval` (0.25s
  vs. 1s) for `auraBoostDuration` (4s), via a `StopCoroutine`/
  `StartCoroutine` restart-safety pattern — `auraBoostCooldown` (10s) must
  stay ≥ `auraBoostDuration` to avoid double-applying. A `LineRenderer`
  ring around the Medic (dim/thin by default, bright/thick while boosted)
  shows the live radius, and allies actually healed by a tick get a
  distinct green flash (`PlayerDamageFlash.Flash(Color)`) — both purely
  visual.
- **Support — Speed Boost**: a **party-wide**, non-destructive multiplier
  on both move speed and fire rate, not a self-only effect.
  `TriggerSpeedBoost()` loops over `allies[]` (all 4 ships, self-included)
  and sets each ally's `PlayerController.speedBuffMultiplier`/
  `fireRateBuffMultiplier` to `speedBoostMultiplier` (1.5) for
  `speedBoostDuration` (4s), then resets both to `1f` when it ends — plain
  assignment on both ends, no revert arithmetic. `speedBoostCooldown` is
  15s (round placeholder, tunable — flagged as a strong effect given it's
  party-wide). Every ship has its own initially-hidden `PartyBuffRing`,
  toggled by the caster's `SetPartyBuffVisual(bool, Color)` call on each
  ally — all 4 rings light up in the caster's tint color the instant the
  boost starts and disappear together the instant it ends.
- **Attacker — Big Shot**: calls `PlayerController.FireBigShot(widthMultiplier,
  damageAmount)` (`3x` width) and `PlayerController.AddRecoil(Vector2.down
  * recoilForce)`. Damage is a live multiplier of the caster's current
  `fireDamage` (`bigShotDamageMultiplier`, `2x`), computed at cast time —
  so it automatically stays proportional if `fireDamage` is ever retuned.
  See [combat.md](combat.md) for why recoil has to be a decaying velocity
  blended into `HandleMovement()` rather than a physics impulse
  (`MovePosition` overwrites plain `AddForce` every `FixedUpdate`).

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

`OnAbility(InputValue)` is a thin wrapper around a public, non-input entry
point — `TryUseAbility()` — so `AIController.cs` (see [boss.md](boss.md))
can trigger a CPU teammate's ability directly, through the exact same
cooldown gate and role-dispatch switch as the human player. The `Trigger*`
methods stay private.

**Manual teammate-ability triggering from the party frame**: each
`PartyFrame_N`'s `AbilityText` line (see [hud-layout.md](hud-layout.md))
doubles as a clickable button — a `Button` component added directly to the
existing text element (reusing it as its own click surface rather than
adding a separate button, since it already shows exactly the state a
manual trigger needs, e.g. `"Taunt: Ready"`) — wired to call that
teammate's `TryUseAbility()` on click. This is a third caller of the same
method alongside `AIController`'s auto-retry and the human's own `E`
binding, so it needed **no changes to `PlayerAbility.cs` itself**. The
click deliberately bypasses `AIController`'s extra Tank-specific condition
(`if (boss.CurrentTarget != gameObject)`) — a manual click can force a
Tank to re-taunt even while it already holds aggro, since the player may
want to refresh threat deliberately. `PartyFrameUI.Initialize()` hides/
disables this button on the human's own frame (`isHuman`, threaded in from
`PartyFrameManager`'s existing `AIController`-presence check) since the
human already has their own ability input, and drives its `interactable`
state every `Update()` off the same `CooldownRemaining` the status text
already reads. Wiring the click listener in code
(`abilityButton.onClick.AddListener(...)` inside `Initialize()`) is a
deliberate, narrow exception to this codebase's usual "Inspector
persistent listeners only" convention (see `GameOverUI.cs`/
`RoleSelectUI.cs`) — each `PartyFrame` prefab instance only learns which
ship's `PlayerAbility` it owns at runtime, so there's no concrete target
to drag into an Inspector slot at prefab-authoring time.

## Shield stat

A second, health-like pool per role (`PlayerHealth.maxShield`/
`CurrentShield`), a fixed per-role value alongside `maxHealth` (see "Fixed
per-role stats" below):

- **Absorbs damage before health** — `PlayerHealth.TakeDamage(int)` deducts
  from `currentShield` first, down to 0; only the remainder subtracts from
  `currentHealth`. A hit fully absorbed by shield still fires `OnDamaged`
  (flash/shake feedback), same mutual-exclusivity-with-`Die()` rule as
  before (see [combat.md](combat.md)).
- **No passive regen of its own** — `PlayerHealth.RestoreShield(int)`
  (symmetric to `Heal(int)`, clamps at `maxShield`) is only ever called by
  Medic's proximity aura, never on its own over time. Keeps Tank
  meaningfully dependent on Medic, matching the MMO-raid "tank and healer"
  coupling this project is modeled on (`../overview.md`).
- **Shield bar**: a fixed shield-blue bar on the party frame, not
  role-tinted — see [hud-layout.md](hud-layout.md).

## Aggro / targeting

**Targeting** is how an enemy AI decides which player to attack when
multiple are available. **Aggro** ("aggression"/threat) is the per-target
value that decision is based on — an enemy tracks how much attention each
player has drawn and attacks whoever currently has the highest aggro
against it. **Taunt** is an ability that artificially spikes the caster's
aggro to the top, forcing the enemy to switch targets — the classic
MMO-raid "tank and spank" mechanic this project is explicitly modeled on
(see `../overview.md`).

`Enemy.cs` has **no targeting concept at all** — regular wave enemies move
in a fixed sine-wave and fire on a timer regardless of who or where any
player is. The real threat-table aggro system lives on `Boss.cs` — see
[boss.md](boss.md) for the full design (a plain
`Dictionary<GameObject, float>` of damage-dealt-per-target, no decay,
`TauntedBy(GameObject)` spiking the caster above everyone else).

## Fixed per-role stats

`RoleStats` (see `PlayerRole.cs` above) holds one fixed, absolute number
per stat per role — no multipliers, no shared base. This is the entire
source of truth for a role's numbers; nothing else in the codebase
independently defines health, shield, fire damage, fire rate, or move
speed.

| Role     | Health | Shield | Fire damage | Fire rate | Move speed |
| -------- | ------ | ------ | ------------ | --------- | ---------- |
| Attacker | 6      | 5      | 2.0          | 2.5/s     | 3.0 u/s    |
| Tank     | 8      | 20     | 1.0          | 1/s       | 1.5 u/s    |
| Medic    | 4      | 3      | 0.7          | 1.5/s     | 3.0 u/s    |
| Support  | 5      | 3      | 1.0          | 2/s       | 4.5 u/s    |

Units: **Fire rate** is shots/second (higher = faster) —
`PlayerController.shotsPerSecond`. **Move speed** is world units/second
(`PlayerController.moveSpeed`). All values are placeholders, tunable until
real playtesting lands.

**Buffs are layered on non-destructively, not by mutating these values.**
`PlayerController` has two runtime-only multiplier fields —
`speedBuffMultiplier`, `fireRateBuffMultiplier` (both default `1f`) — read
at the point of use (`HandleMovement()`'s move vector, and a computed
`FireInterval => 1f / (shotsPerSecond * fireRateBuffMultiplier)` for the
fire-cooldown gate) rather than ever being multiplied into `moveSpeed`/
`shotsPerSecond` themselves. Only `PlayerAbility` (Support's Speed Boost,
see above) ever sets them, and only ever via plain assignment.

## Role Select scene

An in-game role picker: `RoleSelect.unity` (Build Settings index 0, entry
point) and `Gameplay.unity` (gameplay, index 1) — a real second scene, not
a same-scene overlay.

**`RoleSelect.unity` contents**: a single Screen Space - Overlay `Canvas`
(title text, 4 role buttons, a Start button that stays non-interactable
until a role's picked) plus an `EventSystem`
(`InputSystemUIInputModule`, matching the project's New-Input-System-only
setup — see `input.md`) and a plain `Main Camera` (tagged `MainCamera`,
background color matched to the dark HUD panel tone — needed because
Unity's Game view shows a "No cameras rendering" diagnostic if a scene has
zero cameras, even though the Overlay canvas itself doesn't need one to
render).

`PlayerRoleComponent.Awake()` (tints the sprite), `PlayerHealth.Awake()`
(sets `maxHealth`/`maxShield`), and `PlayerAbility.Awake()` (builds Medic's
aura ring / Tank's shield arc — structural, only happens once) each read
`role`/`Stats` exactly once, at their own startup, and never re-apply it
later — so role has to be set *before* any of those run:

- **`PartyRoleAssignment.cs`** (static class) — `public static PlayerRole?
  HumanRole`. Carries the human's chosen role from `RoleSelect` across the
  `SceneManager.LoadScene` into `Gameplay` (a plain static survives a scene
  load within one Play session but resets to `null` on domain reload).
- **`RoleSelectUI.cs`** (lives only in `RoleSelect`) — 4 role buttons each
  call `SelectRole(PlayerRole)`; a Start button stays non-interactable
  until one is picked. `StartGame()` sets `PartyRoleAssignment.HumanRole`
  then loads `Gameplay`.
- **`PartySetupBootstrap.cs`** (`[DefaultExecutionOrder(-1000)]`, so it
  runs before every default-order script's `Awake()`) on a `PartySetup`
  GameObject in `Gameplay`. If `PartyRoleAssignment.HumanRole` has a value,
  assigns it to `Player`'s `PlayerRoleComponent.role`, then assigns the
  remaining 3 `PlayerRole` enum values (in declaration order, skipping the
  human's pick) to `Teammate_Tank`/`Teammate_Medic`/`Teammate_Support`'s
  `PlayerRoleComponent.role` — covers all 4 roles exactly once by
  construction. If `HumanRole` is null (e.g. `Gameplay` opened directly,
  bypassing `RoleSelect`), it no-ops, preserving the Inspector-only manual
  role assignment.
- **Cosmetic note**: `Teammate_Tank`/`Teammate_Medic`/`Teammate_Support`
  (the GameObject *names*) frequently no longer play the role their name
  suggests once a human picks something other than Attacker. Purely a
  Hierarchy-panel label mismatch — `AIController`/`PartyFrameManager` are
  fully role-agnostic, keyed by GameObject reference, never by name.
- **`VictoryUI.cs`** (mirrors `GameOverUI.cs`) — a `VictoryPanel` under
  `HUDCanvas`, shown as a listener on `Boss.OnDefeated` (alongside
  `BossPanelUI.ShowDefeated()`) unless `GameOverPanel` is already showing
  (`gameOverPanelRoot` guard — the 3 CPU teammates can still defeat the
  boss after the human `Player` has already died, since only the human's
  own death ends the test; see [boss.md](boss.md)'s "Death handling").
  `PlayAgain()` reloads `Gameplay` (roles preserved via
  `PartyRoleAssignment.HumanRole`); `ChangeRoles()` loads `RoleSelect`.
  `GameOverPanel` has a matching "Change Roles" button
  (`GameOverUI.ChangeRoles()`) alongside its Restart, which also doubles
  as "play again, same party."

## Scene wiring — Player

| Component               | Key inspector values                            |
| ------------------------ | -------------------------------------------------- |
| **PlayerRoleComponent**  | role: Attacker (Inspector default — overwritten at runtime by the Role Select flow, see "Role Select scene" above; used as-is when `Gameplay` is opened directly) |
| **PlayerAbility.cs**     | defaults as listed above; `OnTaunt`: `Player/PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` + `Boss/Boss.TauntedBy(Player)` (real aggro redirect, see [boss.md](boss.md); same 3 listeners wired on each `Teammate_*`'s `PlayerAbility`, each pointing `TauntedBy` at itself) |

## Not yet built

- Local co-op with multiple **human** players isn't wired up — the 3 extra
  ships fighting alongside `Player` (`Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support`) are CPU-controlled via `AIController.cs`, not real
  players; see [boss.md](boss.md).
