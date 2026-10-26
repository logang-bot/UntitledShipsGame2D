# Warden

Level 3's boss: a dual/triple-lane coverage fight, the first boss where two
threats exist genuinely at once — Tank's Taunt and Shield Arc alone can't
cover both by standing in one spot. Two turret-arms (three in Phase 2) each
independently and randomly re-lock onto a living ship and fire a continuous
stream while locked, rather than routing everything through a single-target
aggro model like [Marauder](marauder-boss.md)'s. On top of the arms: body
contact damage and a proximity shockwave, both carried over unchanged from
Marauder. The fight's own signature mechanic, **Lockdown volley**, is a wide
wall of parallel bullets from a random arena edge, built specifically to
reward Shield Arc's *width* over ordinary dodging. There is no single-target
aggro/threat table at all — Taunt has a real effect here, just not the
redirect model Marauder uses. Design decisions were resolved via a
brainstorming session recorded in
[docs/superpowers/specs/2026-09-04-warden-boss-design.md](../../superpowers/specs/2026-09-04-warden-boss-design.md);
this doc describes the built system.

## At a Glance

- **Identity:** a coverage/coordination check — Marauder's aggro table
  always has exactly one `CurrentTarget`, and Halcyon has no target concept
  at all. Warden is the first fight where two (later three) independent
  threats exist at once, so the party has to physically cover multiple
  lanes instead of funneling everything onto Tank.
- **Movement:** erratic dash-or-hold, reimplementing
  [Marauder](marauder-boss.md)'s "M"-pattern idiom with the same field
  values — see "WardenMovement.cs" below.
- **Mechanics:** [dual/triple turret-arms](#wardenarmcs) (each independently
  re-picks a living ship to lock onto and fire a continuous stream at,
  biased — not overridden — by Tank's Taunt) · [body contact
  damage](#wardenbosscs) and [proximity shockwave](#wardenshockwavecs) (both
  kept from Marauder, unchanged mechanics/numbers) · [Lockdown
  volley](#wardenlockdownvolleycs) (the fight's signature mechanic — a wide
  wall of parallel bullets from a random arena edge, built to be blocked by
  Tank's Shield Arc, not dodged).
- **Level summary:** reached via `LevelSelect` → `Level3.unity`, which runs
  the same shared `LevelSequencer` timeline every level uses (see
  [level-sequencing.md](../level-sequencing.md)) — no sequencing changes,
  only the boss instance and its own components differ from Marauder.

## Code architecture: sibling MonoBehaviours + a reusable arm component

Like Halcyon (see [halcyon-boss.md](halcyon-boss.md)'s "Code architecture:
sibling MonoBehaviours + IBoss"), Warden's mechanics are separate sibling
`MonoBehaviour`s on the same `Boss` GameObject rather than helper classes
owned by one big component: `WardenBoss.cs` (HP/phases/contact
damage/taunt-window core only), `WardenMovement.cs`, `WardenShockwave.cs`,
`WardenLockdownVolley.cs`. `WardenArm.cs` is a variant of that same
convention: not one mechanic on the boss GameObject, but **one reusable
component with multiple instances** — two active from `BossCombat`
(`armA`/`armB`), a third (`armC`) that exists from scene setup, starts
disabled, and is permanently activated by `WardenBoss.OnPhase2` via
`gameObject.SetActive(true)` (not a component-`enabled` toggle, since the
whole child GameObject — sprite tint included — starts off). Each arm lives
on its own child GameObject tracking the boss's live position plus a fixed
offset, the same idiom `Minion.cs` uses for boss-flank tracking.

`WardenBoss` implements `IBoss` (`SetVisible(bool)`,
`ApplyContactDamage(GameObject)`) as this interface's third concrete type —
no changes needed to `IBoss` itself, `LevelSequencer`, `PlayerController`,
`AIController`, or `PartySetupBootstrap`, since all of that was already
generalized for Halcyon. See `docs/architecture.md`'s "Boss-type-agnostic
orchestration: IBoss".

## WardenBoss.cs

**Attached to:** `Boss` GameObject (`Level3.unity`'s boss instance — built
directly on the scene object rather than a separate reusable prefab, same
one-off-per-level treatment Marauder and Halcyon both get). `maxHealth`
**130** — the highest of the three bosses, since more simultaneous damage
sources need enough HP to survive both phases. `bulletDamage` (1) is a
reference unit only, never spent on an actual shot — the arms', shockwave's,
and body contact's damage all multiply it, the same convention
`MarauderBoss.bulletDamage`/`HalcyonBoss.bulletDamage` use. Body contact
damage is unchanged from Marauder/Halcyon:
`bodyContactDamageMultiplier` 2×, cooldown-gated per target
(`contactDamageCooldown`, 1s).

Phase 2 triggers at `CurrentHealth <= maxHealth / 2` (same threshold
convention as Marauder/Halcyon). `EnterPhase2()` permanently activates
`armC` (if assigned) and fires `OnPhase2` — `WardenLockdownVolley` reads
`WardenBoss.IsPhase2` itself to tighten its own cooldown, so no direct call
between the two is needed.

Starts with every sibling mechanic (`WardenMovement`, `WardenShockwave`,
`WardenLockdownVolley`, `armA`, `armB` — cached into one `Behaviour[]` array
in `Awake()`) force-disabled, matching `MarauderBoss`/`HalcyonBoss`'s own
"defensively disabled, `LevelSequencer` enables at `BossCombat`" convention.
`OnEnable()` re-enables all of them at once. `SetVisible(bool)` toggles the
sprite/collider plus the shockwave ring and both arms' sprites together.

`Bullet.cs`'s player-bullet-vs-`Enemy`-tag branch gained a fifth check
(alongside `Enemy`/`MarauderBoss`/`HalcyonBoss`/`Minion`): `other.GetComponent<WardenBoss>()`
→ `TakeDamage(damage)` — a single-`float`-param overload, not
`TakeDamage(float, GameObject)` like Marauder's, since Warden has no
single-target aggro to attribute the source to.

**Taunt biases re-picks, doesn't redirect.** Unlike `MarauderBoss.TauntedBy`
(an instant aggro-table overwrite) or Halcyon (no listener at all, a genuine
no-op), Warden's `TauntedBy(GameObject)` — still the persistent listener
target for every ship's `PlayerAbility.OnTaunt` — has no single-target aggro
to redirect. It just opens a `tauntWindowDuration` (3s) window
(`TauntedShip`/`TauntActiveUntil`) that `WardenArm.PickWeighted` reads on its
next re-pick to weight (not force) the draw toward the taunter — see
"WardenArm.cs" below.

## WardenMovement.cs

Sibling component, on the same `Boss` GameObject. Reimplements
[Marauder](marauder-boss.md)'s dash-or-hold "M"-pattern movement (snap to a
side, bounded advance toward the ships within roughly the top portion of the
playable height, retreat, return to center, pause, repeat mirrored) as an
independent `MonoBehaviour` — can't reuse `MarauderBossMovement` directly,
since that class is a helper owned by `MarauderBoss`, not a shared utility.
`MarauderBossMovement`'s exact field values (advance distance/speed, pause
duration ranges) were copied verbatim as the starting point; no numbers were
changed for Warden.

## WardenArm.cs

Reusable turret-arm component — two instances (`armA`/`armB`) active from
`BossCombat`, a third (`armC`) added permanently at Phase 2 (see
"WardenBoss.cs" above). State cycle, driven entirely by `Update()` polling
against `Time.time` deadlines (no coroutines):

1. **Idle** — counts down `idleCooldown` (4s) with `±idleJitter` (1s) random
   jitter per instance, so the arms don't visibly sync up.
2. **Telegraph** (`telegraphTime`, 0.5s) — a tell before the stream starts;
   `currentTarget` is picked at the *start* of this state, not when firing
   begins.
3. **Firing** (`firingDuration`, 3s) — a continuous stream at `fireInterval`
   (0.15s between shots), re-aiming every shot at the target's current
   position since it keeps moving. Reuses `Bullet.Init()`'s existing
   straight-line path — no new bullet type.
4. Returns to Idle, ready to re-pick next cycle.

**Target re-pick** (`PickWeightedTarget()` → the public static
`PickWeighted(GameObject[] ships, TauntBias bias, float randomDraw01)`,
split out this way so it's testable without RNG flakiness): builds a weight
per currently-living ship in `WardenBoss.ships` (1 each), and if
`WardenBoss.TauntActiveUntil` is still in the future, the taunting ship's
weight becomes `tauntWeightMultiplier` (3) instead of 1; `randomDraw01`
(normally `Random.value`) picks against the cumulative weighted total. A
bias, not a hard override — the only ship a Taunt window can *guarantee*
redirection to is Marauder's aggro target, not Warden's arms.

Public API: `CurrentTargetRole` (the locked target's `PlayerRole?`, `null`
while idle/inactive) — read by `WardenBossPanelUI` for a per-arm warning
line, the same "HUD only reads" convention every boss panel already
follows.

## WardenShockwave.cs

Sibling component: [Marauder](marauder-boss.md)'s telegraphed-ring/knockback
proximity mechanic, ported with unchanged numbers and formula — `radius`
1.7, `damageMultiplier` 3×, `knockback` 33, `cooldown` 3s, `telegraphTime`
0.3s — the same closed-form recoil-decay knockback math Marauder's own
shockwave already uses, and the same always-dim / brighter-during-telegraph
/ impact-flash-on-hit `LineRenderer` ring idiom. A direct port for
consistency, the same way `HalcyonStaticField` ported the shockwave's `3×
bulletDamage` convention.

## WardenLockdownVolley.cs

The fight's signature mechanic. On `lockdownCooldown` (9s Phase 1,
`lockdownCooldownPhase2` 6s Phase 2 — read live off `WardenBoss.IsPhase2`
each cycle) with a `telegraphTime` of 1s — longer than Pattern Barrage's
0.7s, since the wall is meant to be *seen coming and blocked*, not reacted
to on instinct:

- Picks a random arena edge (`Edge.Top`/`Left`/`Right`, equal probability).
- Spawns `wallBulletCount` (12) bullets evenly spaced across that edge, all
  travelling the same direction inward (`DirectionFor(edge)`: Top → down,
  Left → right, Right → left), with `wallGapCount` (2) evenly-distributed
  single-bullet-width gaps removed (`PickGapIndices`) so 10 bullets actually
  fire per volley, not fully unbroken.
- Reuses `Bullet.Init(Vector2 dir)`'s existing arbitrary-direction path
  (already used by Pattern Barrage's Fan/Ring/Spiral) — no new bullet
  capability needed for a left/right-originating wall.

A ship standing in the wall's path takes a hit per bullet that reaches it.
Tank's Shield Arc — already a wide trigger collider absorbing hits into
Tank's own shield/health, per [player-roles.md](../player-roles.md)'s
"PlayerAbility.cs" — blocks whichever lanes of the wall cross its width for
free, with zero changes needed to `PlayerAbility.cs` itself. This is the
mechanic that gives Shield Arc's width a reason to matter, mirroring how
Halcyon's Static Field gives Support's Speed Boost a reason to matter (see
[halcyon-boss.md](halcyon-boss.md)).

Public API: `CooldownRemaining`, `IsTelegraphing`, `IncomingEdge` (`Edge?`)
— read by `WardenBossPanelUI`.

## WardenBossPanelUI.cs

**Attached to:** `Level3.unity`'s `BossPanel` (replacing `BossPanelUI`, same
"each level's `BossPanel` script matches its own boss type" convention
Halcyon's build established). Same "HUD only reads, never owns game state"
pattern as `BossPanelUI.cs`/`HalcyonBossPanelUI.cs`:

- HP bar + `"HP: x/y"` text, phase text (`"Phase 1"`/`"Phase 2"`,
  `"DEFEATED"` via `ShowDefeated()` on `OnDefeated`).
- One warning line per arm (`armAWarningText`/`armBWarningText`/
  `armCWarningText`), with three distinct states — the two dashes are
  deliberately different glyphs, not inconsistent formatting: `"Arm A: —"`
  (em dash) while the arm's GameObject is inactive/disabled (relevant
  pre-Phase-2, when `armC` exists but hasn't been activated yet); `"Arm A:
  --"` (double hyphen) while the arm is active but currently idle, between
  cycles with no `currentTarget` locked yet — this is the common case, since
  `idleCooldown` (4s, ±jitter) is longer than `telegraphTime + firingDuration`
  (3.5s); `"Arm A: Tank"` (the target's role name) once a target is actually
  locked. Task 10's live verification observed exactly this progression:
  `"--" → "Attacker"`/`"Support"` as arms picked targets.
- Shockwave cooldown text (`"Shockwave: {n}s"` / `"Shockwave: Ready"`).
- Lockdown-volley warning (`"Incoming: {Edge} Lockdown"` during telegraph,
  blank otherwise) + its own cooldown text.

No single target/aggro text — there's no single-target concept in this
fight, same reasoning `HalcyonBossPanelUI` already applies.

## Scene wiring

### Boss

**Tag:** `Enemy`. `Level3.unity`'s `Boss` GameObject (previously
`Level3BossPlaceholder.prefab`, a `MarauderBoss`-typed duplicate of
Marauder, plus `MinionSpawner`) had both components removed and
`WardenBoss`/`WardenMovement`/`WardenShockwave`/`WardenLockdownVolley` added
in place — built directly on the same GameObject rather than a separate
prefab asset, same one-off-per-level treatment every other boss gets.
`WardenBoss.enabled` set to `false` (matching `MarauderBoss`/
`HalcyonBoss`'s convention); tag `Enemy`, `Rigidbody2D.gravityScale = 0`,
`BoxCollider2D.isTrigger = false` all unchanged from the placeholder.

Three child GameObjects were created under `Boss`, each with a
`SpriteRenderer` (`Square` sprite, distinct tint) and a `WardenArm`
(`bulletPrefab` = `EnemyBullet.prefab`):

| Child | Local position | Tint | Notes |
| --- | --- | --- | --- |
| `ArmA` | `(-1.2, 0, 0)` | cyan | active from scene start |
| `ArmB` | `(1.2, 0, 0)` | gold | active from scene start |
| `ArmC` | `(0, -1.2, 0)` | magenta | GameObject starts **inactive** — `WardenBoss.OnPhase2` calls `SetActive(true)` |

`WardenBoss`'s own fields: `armA`/`armB`/`armC` → the three components
above; `ships` → `[Player, Teammate_Tank, Teammate_Medic, Teammate_Support]`.
`WardenLockdownVolley.bulletPrefab` → `EnemyBullet.prefab`.

### LevelSequencer / PlayerController / AIController / PartySetupBootstrap

All four scripts' boss-reference fields are the shared `MonoBehaviour
bossObject` (cast to `IBoss` in code where needed) — see
`docs/architecture.md`'s "Boss-type-agnostic orchestration: IBoss".
`Level3.unity`'s `LevelSequencer`/`PartySetupBootstrap` and all 4 ships'
`PlayerController`/`AIController` have `bossObject` set to the new
`WardenBoss` instance. Unlike Halcyon's build, these fields were already
`null` going in — `Level3.unity` had never been fully wired even to its own
placeholder — so this was a first wiring, not a re-wiring.

`WardenBoss.ships` is fed the *live* spawned roster the same way
`MarauderBoss.targets`/`HalcyonStaticField.ships` need to be (see
`marauder-boss.md`'s "Aggro roster comes from `targets[]`"): rather than a
separate `PartySetupBootstrap` field, Warden reuses the single
`WardenBoss.ships` array directly (like `MarauderBoss.targets`) — a
deliberate deviation from the design spec's suggested `wardenArms` field on
`PartySetupBootstrap`, same effect with less wiring. Built in from the
start, not discovered live as a bug the way the equivalent fix was for both
prior bosses.

`AIController.UpdateAbilityUsage()`'s Tank heuristic gained a third branch
for Warden, alongside its existing `MarauderBoss`/no-op-on-Halcyon cases
(see [halcyon-boss.md](halcyon-boss.md)'s equivalent subsection): casts
`bossObject as WardenBoss` and, if that succeeds, calls
`ability.TryUseAbility()` unconditionally — no gate at all, unlike
Marauder's `marauderBoss.CurrentTarget != gameObject` check. Against Warden,
Taunt has a real, always-beneficial effect (biases arm re-picks toward the
Tank — see "WardenArm.cs" above) with no "already holding aggro" concept to
gate on, since there's no aggro to hold, so an AI-controlled Tank
auto-taunts against Warden purely on cooldown — the same "fire the instant
it's off cooldown" placeholder trigger every other first-pass AI ability in
this project already uses by default (e.g. Medic's aura boost), not a
need-aware heuristic.

### BossPanel

`BossPanel`'s script swapped from `BossPanelUI` to `WardenBossPanelUI`, and
its pre-existing text objects were repurposed by content (not by name),
same as Halcyon's build:

| `WardenBossPanelUI` field | Repurposed from |
| --- | --- |
| `armAWarningText` | `BossWarningText` |
| `armBWarningText` | `BossPatternBarrageWarningText` |
| `armCWarningText` | `BossTargetText` |
| `shockwaveCooldownText` | `BossShockwaveCooldownText` |
| `lockdownWarningText` | `BossPatternBarrageCooldownText` |
| `lockdownCooldownText` | `BossGuidedMissileCooldownText` |

`healthBarFill`/`healthText`/`phaseText` unchanged; `boss`/`armA`/`armB`/
`armC`/`shockwave`/`lockdownVolley` wired to the new components.
`WardenBoss.OnDefeated` → `WardenBossPanelUI.ShowDefeated()` added as a
persistent listener.

### Not shared with the placeholder

All 4 ships' `PlayerAbility.OnTaunt` had a stale `TauntedBy` persistent
listener still pointing at the removed `MarauderBoss` component (a dangling,
silently-inert null-target listener — same situation Halcyon's build hit).
Removed on all 4 ships and replaced with a new listener pointing at
`WardenBoss.TauntedBy`, same static-argument convention (the ship's own
GameObject) as before.

## Not yet built

- **Real human playtest** — the mechanics above are verified via the Unity
  MCP bridge: live component/field reads, reflection-invoked calls (an
  8000-trial tally of `WardenArm.PickWeightedTarget()` confirming a taunted
  ship is picked ~50.1% of the time against `tauntWeightMultiplier = 3`,
  versus ~25% uniform across all 4 ships with no active taunt), and a
  `EditorApplication.Step()`-driven deterministic Play-mode pass through
  `LevelSelect` → `Level3` confirming both arms/`armC`/Lockdown volley
  enable at the right sequencing checkpoints, Phase 2 triggers at the
  correct HP threshold and activates `armC` exactly once, the Lockdown
  wall's bullet count/directions match `DirectionFor`/`PickGapIndices`
  exactly on all 3 edges, and Tank's Shield Arc genuinely intercepts a wall
  bullet before it reaches the ship's body — but no real human has played
  this fight yet.
- **Numeric tuning** — HP, arm timing, taunt bias multiplier, Lockdown
  volley's geometry/cooldown are all first-pass placeholders, same as every
  other boss's numbers in this project, not validated against real play.
- **Out of scope by design**: minions, Pattern Barrage, phase-based ambient
  fire beyond the arms, and any single-target aggro/threat table —
  deliberately absent, not deferred. Warden's identity is the arms +
  Lockdown volley + carried-over contact damage/shockwave, full stop.
