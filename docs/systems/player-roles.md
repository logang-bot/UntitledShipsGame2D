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
- `PlayerHealth.Start()` assigns `maxHealth`/`maxShield` directly from
  `Stats.maxHealth`/`Stats.maxShield` (moved from `Awake()` to `Start()`
  alongside `PlayerRoleComponent`'s own sprite-tint assignment and
  `PlayerAbility`'s role-dependent setup — see the note below).
- Both null-check `GetComponent<PlayerRoleComponent>()`, keeping the
  script's own inspector-set default as a fallback when the component is
  missing.

**Why `Start()`, not `Awake()`, for role-dependent setup**: the co-op
spawner (`PartySetupBootstrap.SpawnDynamicParty()`, see "Role Select scene"
below) creates ships via `Instantiate()`/`PlayerInput.Instantiate()`, which
run the new object's `Awake()` **synchronously before returning** — before
the spawner's own code gets a chance to set `.role` on the result. Unity
does *not* run a freshly-instantiated object's `Start()` synchronously,
though, so every role-dependent side effect (`PlayerRoleComponent`'s sprite
tint, `PlayerHealth`'s `maxHealth`/`maxShield`, `PlayerAbility`'s aura
ring/shield arc construction and initial cooldown) was moved from `Awake()`
into `Start()` to guarantee it always sees the real, assigned role — a
behavior-preserving change for the legacy scene-placed path too, since
`PartySetupBootstrap`'s `[DefaultExecutionOrder(-1000)]` `Awake()` already
ran (and already set `.role`) well before any object's `Start()` fires
either way.

Key public fields: `role` (default `Attacker`).

## PlayerAbility.cs

**Attached to:** `Player` GameObject.
**Requires:** `PlayerRoleComponent`, `PlayerController`, `PlayerHealth` on
the same GameObject (references cached in `Awake()`; the role-dependent
construction below — Medic's aura ring, Tank's shield arc, the initial
cooldown — happens in `Start()` instead, see `PlayerRoleComponent.cs`'s note
above).

One script, not four — branches on `PlayerRoleComponent.role` in a single
`OnAbility(InputValue)` handler (auto-called by `Player Input`'s Send
Messages behavior on the `Ability` action, bound to `E` — see
[input.md](input.md)). A single `Time.time`-based cooldown gate
(`nextAbilityTime`, same pattern as `PlayerController`'s `nextFireTime`)
blocks re-activation until the current role's cooldown elapses.

- **Tank — Taunt**: `public UnityEvent OnTaunt`, invoked on activation. A
  persistent listener redirects the boss's target to the taunter
  (`Level1Boss.TauntedBy(GameObject)` — see [level1-boss.md](level1-boss.md)'s "Aggro /
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
- **Attacker — 3-attack combo**: separate from Big Shot above, bound to
  three dedicated inputs (`Combo1`/`Combo2`/`Combo3` — keyboard `1`/`2`/`3`,
  gamepad North/L1/R1). `PlayerAbilityAttacker` tracks a single
  `expectedSlot` (1→2→3→1…, the fixed "correct rotation"). Landing the
  expected slot fires `FireBigShot` with that step's damage/width
  multiplier (`comboStepDamageMultipliers`/`comboStepWidthMultipliers` —
  escalating toward a finisher bonus on step 3) and advances `expectedSlot`;
  landing the wrong slot ("bad execution") fires at
  `comboBreakDamageMultiplier` (0.5x) instead and resets `expectedSlot` back
  to 1, rather than dealing no damage at all. Each slot has its own short
  `comboAttackCooldown` (0.5s) — independent of `bigShotCooldown` — so the
  gate is "don't mash one key", not the sequencing itself; the sequencing is
  entirely on the player picking the right key. `AIController` always feeds
  `TryComboAttack()` the current `AttackerComboExpectedSlot`, so Attacker
  bots play a perfect rotation every time — there's no "bad execution" mode
  for bots yet, only for human input.

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
(6); `comboAttackCooldown` (0.5s), `comboStepDamageMultipliers` (`1, 1.3,
1.8`), `comboStepWidthMultipliers` (`1, 1.5, 2.5`),
`comboBreakDamageMultiplier` (0.5). Key public methods: `OnAbility(InputValue)`,
`OnCombo1/2/3(InputValue)`.

Also exposes read-only status for the HUD (see `PartyFrameUI.cs` in
[hud-layout.md](hud-layout.md)): `CooldownRemaining`, `IsSpeedBoostActive`,
`SpeedBoostRemaining`, `AbilityName` (per-role display name), and
`StatusText` (formatted cooldown/`Ready`/active-boost string) — these are
the single source of truth for ability state so the HUD never duplicates
cooldown math.

`OnAbility(InputValue)` is a thin wrapper around a public, non-input entry
point — `TryUseAbility()` — so `AIController.cs` (see [level1-boss.md](level1-boss.md))
can trigger a CPU teammate's ability directly, through the exact same
cooldown gate and role-dispatch switch as the human player. The `Trigger*`
methods stay private. `OnCombo1/2/3(InputValue)` follow the same pattern
via `TryComboAttack(int slot)`, which no-ops outside the Attacker role since
every ship's `PlayerAbility` builds all four role helpers regardless of its
actual role (see `CreateHelpers()`).

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
player is. The real threat-table aggro system lives on `Level1Boss.cs` — see
[level1-boss.md](level1-boss.md) for the full design (a plain
`Dictionary<GameObject, float>` of damage-dealt-per-target, no decay,
`TauntedBy(GameObject)` spiking the caster above everyone else).

## Fixed per-role stats

`RoleStats` (see `PlayerRole.cs` above) holds one fixed, absolute number
per stat per role — no multipliers, no shared base. This is the entire
source of truth for a role's numbers; nothing else in the codebase
independently defines health, shield, fire damage, fire rate, or move
speed.

| Role     | Health | Shield | Fire damage | Fire rate | **DPS** | Move speed |
| -------- | ------ | ------ | ------------ | --------- | ------- | ---------- |
| Attacker | 6      | 5      | 2.0          | 2.5/s     | **5.00** | 3.0 u/s   |
| Tank     | 8      | 20     | 1.0          | 1/s       | **1.00** | 1.5 u/s   |
| Medic    | 4      | 3      | 0.7          | 1.5/s     | **1.05** | 3.0 u/s   |
| Support  | 5      | 3      | 1.0          | 2/s       | **2.00** | 4.5 u/s   |

> **Move speed in this table is stale.** `PlayerRole.cs` actually holds
> 2.4 / 1.2 / 2.4 / 3.6 — every value exactly 0.8x the ones above, which reads
> as a deliberate global tuning pass that never reached the docs or
> `PlayerRoleStatsTests` (where it is currently the only failing assertion).
> Every other column here matches the code. Resolve before trusting the
> movement numbers.

**DPS is derived, not stored** — `RoleStats.Dps => fireDamage *
shotsPerSecond`. There is no third field to keep in sync, and asserting it in
`PlayerRoleStatsTests` is what catches a role's damage output moving when only
one of its two inputs was tuned.

This is a **ceiling, not a prediction**: ships fire straight up (`Vector2.up`)
at a boss that moves side to side, so real output is this minus everything that
missed. `DpsMeterUI` (see [hud-layout.md](hud-layout.md)) reports what actually
landed, and the gap between the two numbers is the positioning skill. Attacker's
Big Shot widens its bullet 3x on top of doubling its damage, so part of that
ability's value is accuracy against a moving target, not raw damage.

`PlayerController.CurrentDps` is the *live* equivalent —
`fireDamage * shotsPerSecond * fireRateBuffMultiplier` — so it reflects
Support's party-wide fire-rate boost while it is up. That is what the party
frame's DPS line displays.

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

An in-game role picker: `RoleSelect.unity` (Build Settings index 3, reached
via `MainMenu` → `Lobby` → `JoinLobby` → `RoleSelect` — see
[scene-flow.md](scene-flow.md)) and `Gameplay.unity` (gameplay, index 4) — a
real separate scene, not a same-scene overlay. `RoleSelect` also has a Back
button (→ `JoinLobby` if the co-op join flow was used, `Lobby` otherwise).

**`RoleSelect.unity` contents**: a single Screen Space - Overlay `Canvas`
plus an `EventSystem` (`InputSystemUIInputModule`, matching the project's
New-Input-System-only setup — see `input.md`) and a plain `Main Camera`
(tagged `MainCamera`, background color matched to the dark HUD panel tone —
needed because Unity's Game view shows a "No cameras rendering" diagnostic
if a scene has zero cameras, even though the Overlay canvas itself doesn't
need one to render). The Canvas holds two child panels,
`RoleSelectUI.Awake()` activating exactly one based on
`CoOpRoster.Players`' count (see "Local co-op / dynamic player count"
below):

- **`SinglePickerPanel`** — the original 4 role buttons + Start button,
  used whenever 0 or 1 players joined (direct scene open, or exactly one
  human via `JoinLobby`).
- **`MultiPickerPanel`** — `RoleSelectMultiUI` + one `RolePickerRow.prefab`
  instance per joined player, used whenever 2+ players joined.

`PlayerRoleComponent`/`PlayerHealth`/`PlayerAbility`'s role-dependent setup
(sprite tint, `maxHealth`/`maxShield`, Medic's aura ring / Tank's shield arc)
each read `role`/`Stats` exactly once, in `Start()`, and never re-apply it
later — so role has to be set *before* any `Start()` runs (see
`PlayerRoleComponent.cs` above for why `Start()`, not `Awake()`, matters
here specifically because of the co-op spawner below).

### Local co-op / dynamic player count

Up to 4 local humans can now join (1 via keyboard+mouse or gamepad, 2-4 via
gamepad — see [scene-flow.md](scene-flow.md)'s "JoinLobbyUI.cs" and
[input.md](input.md)'s control-scheme section), each picking a distinct
role; any of the 4 `PlayerRole` slots nobody picked is filled by AI, exactly
as before. The party is always exactly 4 ships — only the human/AI split of
those 4 fixed role slots varies, not the total count, which is why
`PlayerRoleStats`'s 4-entry table, `PartyFrameManager`'s 4 hand-placed HUD
frames, `LevelSequencer.ships`'s always-4-element array, and the boss's
aggro table all stay untouched by this feature.

- **`Ship.prefab`** (new, `Assets/Prefabs/Ship.prefab`) — a single unified
  ship prefab replacing the old, inconsistent setup (`Player` and 2 of the 3
  `Teammate_*` GameObjects were plain hand-placed duplicates, not real
  prefab instances — see `../unity-notes.md`'s "Duplicating a GameObject
  before it's a prefab instance"). Carries **both** `PlayerInput` (prefab
  default `enabled: false`) and `AIController` (prefab default
  `enabled: true`) — never adds/removes either at runtime, only toggles
  `.enabled`, extending the project's existing "same component, two
  callers" dual-entry-point pattern (`../architecture.md`) to "which driver
  is switched on" instead of "which driver exists." `Gameplay.unity`'s 4
  scene ships (`Player`/`Teammate_Tank`/`Teammate_Medic`/`Teammate_Support`)
  are now all real instances of this one prefab, with `Player`'s
  `PlayerInput`/`AIController` overridden to the human shape (`true`/
  `false`) and each `Teammate_*`'s left at the AI-slot prefab default.
- **`CoOpRoster.cs`** (new static carrier, same pattern as
  `PartyRoleAssignment.cs`/`GameModeSelection.cs`) — `public static
  List<JoinedPlayer> Players`, each entry holding `controlScheme`,
  `devices[]` (paired at `JoinLobby`, still valid in `Gameplay` since
  physical `InputDevice`s persist across scene loads), and `role` (filled
  in by `RoleSelect`). `null` means the co-op flow wasn't used.
- **`PartySetupBootstrap.cs`** (`[DefaultExecutionOrder(-1000)]`) —
  `Awake()` now checks `CoOpRoster.Players` **first**: if set and non-empty,
  runs `SpawnDynamicParty()` instead of the legacy branch below. The 4
  original scene ships are reused purely as position markers (read
  `.transform.position`, then `SetActive(false)`) so both branches share one
  authored set of spawn points. For each joined human:
  `PlayerInput.Instantiate(shipPrefab, controlScheme, pairWithDevices)`
  (which pairs the human's devices but does **not** itself flip the
  prefab's serialized `enabled: false` back on — that needs an explicit
  `pi.enabled = true` plus `GetComponent<AIController>().enabled = false`
  right after, a real gotcha hit and fixed live during this feature's
  implementation) at that ship's marker position, with its picked role.
  Every remaining unpicked `PlayerRole` (same "walk `Enum.GetValues`, skip
  taken ones" idiom the legacy branch already used) gets a plain
  `Instantiate(shipPrefab)` — left at the prefab's AI-slot default
  (`PlayerInput` disabled, `AIController` enabled), which also avoids a
  second gotcha: an AI-instantiated clone with `PlayerInput` still enabled
  by default would try to auto-pair itself to an already-claimed device the
  instant it's created, logging "Cannot find matching control scheme."
  Finally wires every spawned ship's `PlayerAbility.allies[]` (all 4) and
  every AI ship's `AIController.teammates[]` (AI-only subset — kept
  AI-only, exactly matching the legacy semantics: never includes a human
  ship, even now that "the human" can be any of several), and assigns the
  spawned set directly onto `LevelSequencer.ships`/`PartyFrameManager.players`
  (safe given this script's `-1000` execution order).
  
  **Legacy branch (unchanged)**: if `CoOpRoster.Players` is unset, falls
  back to the original single-human path — if `PartyRoleAssignment.HumanRole`
  has a value, assigns it to `Player`'s `PlayerRoleComponent.role`, then
  assigns the remaining 3 `PlayerRole` enum values (in declaration order,
  skipping the human's pick) to `Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support`'s `PlayerRoleComponent.role`. If `HumanRole` is also
  null (e.g. `Gameplay` opened directly, bypassing every menu scene), it
  no-ops, preserving the Inspector-only manual role assignment.
- **`LevelSequencer.cs` fix (required, not optional)**: `SetShipsFrozen()`'s
  unfreeze branch used to unconditionally re-enable both `PlayerInput` and
  `AIController` whenever each was non-null — safe only because exactly one
  of the two ever existed per ship before `Ship.prefab`. Now that every ship
  always carries both, that would hand AI control to a human ship (and vice
  versa) the instant the intro glide or boss entrance ends. Fixed by caching
  `shipIsHuman[i] = shipInputs[i].enabled` once in `Awake()` (after
  `PartySetupBootstrap`'s `-1000` `Awake()` has already configured each
  ship), then restoring each ship to *its own* real driver on unfreeze
  instead of both.
- **`PartyFrameManager.cs` fix**: the old humanness check
  (`GetComponent<AIController>() == null`) broke the same way once every
  ship always has an `AIController` — switched to checking
  `GetComponent<PlayerInput>().enabled` (the same signal the
  `LevelSequencer` fix above relies on), and the hardcoded `"Player 1"`
  label became a running counter (`"Player " + (++humanIndex)`) so multiple
  humans get distinct names.
- **`RoleSelectMultiUI.cs` / `RolePickerRow.cs`** (new, `MultiPickerPanel`) —
  one row per joined player; each row polls its own paired device directly
  (dpad/stick or WASD to move a highlight, South/Enter to confirm,
  West/Escape to unlock — see [input.md](input.md)'s "Local co-op join
  screen input"). A shared `roleTaken` check across rows enforces distinct
  picks; `Start` enables once every row has locked a role.
- **Cosmetic note**: `Teammate_Tank`/`Teammate_Medic`/`Teammate_Support`
  (the GameObject *names* in the legacy fallback scene content) frequently
  no longer play the role their name suggests once a human picks something
  other than Attacker (or, now, once co-op assigns roles dynamically).
  Purely a Hierarchy-panel label mismatch — `AIController`/
  `PartyFrameManager` are fully role-agnostic, keyed by GameObject
  reference, never by name.
- **`VictoryUI.cs`** (mirrors `GameOverUI.cs`) — a `VictoryPanel` under
  `HUDCanvas`, shown as a listener on `Level1Boss.OnDefeated` (alongside
  `BossPanelUI.ShowDefeated()`) unless `GameOverPanel` is already showing
  (`gameOverPanelRoot` guard — the AI/other-human teammates can still
  defeat the boss after one human `Player` has already died, since only a
  human's own death ends the test; see [level1-boss.md](level1-boss.md)'s
  "Death handling"). `PlayAgain()` reloads `Gameplay` (roles preserved via
  `PartyRoleAssignment.HumanRole`/`CoOpRoster.Players`, and co-op devices
  re-pair fresh on the reload); `ChangeRoles()` loads `RoleSelect`.
  `GameOverPanel` has a matching "Change Roles" button
  (`GameOverUI.ChangeRoles()`) alongside its Restart, which also doubles
  as "play again, same party."

## Scene wiring — Ship.prefab / Player

| Component               | Key inspector values                            |
| ------------------------ | -------------------------------------------------- |
| **PlayerRoleComponent**  | role: Attacker (`Ship.prefab` default — overwritten at runtime by the Role Select flow, see "Role Select scene" above; used as-is when `Gameplay` is opened directly) |
| **PlayerAbility.cs**     | defaults as listed above; `OnTaunt`: self `PlayerDamageFlash.Flash()` + `Main Camera/CameraShake.Shake()` + `Boss/Level1Boss.TauntedBy(self)` (real aggro redirect, see [level1-boss.md](level1-boss.md)) — baked into `Ship.prefab` itself (self-referencing, so every instance gets correct per-ship listeners for free, unlike the pre-`Ship.prefab` setup where each `Teammate_*` needed the same 3 listeners wired by hand) |

## Not yet built

- Networked/authoritative multiplayer (multiple humans across *separate*
  machines) — see `roadmap.md`'s "Nakama networking." Local co-op (multiple
  humans on one machine) is implemented — see "Local co-op / dynamic player
  count" above.
