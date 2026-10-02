# Halcyon

Level 2's boss: a pure positioning fight, the opposite of
[Marauder](marauder-boss.md)'s bullet-heavy style. No ambient bullets at
all — damage comes only from body contact and a new Static Field proximity
pulse. No aggro/threat table either: Tank's Taunt still fires its own
feedback (flash/shake) but has no boss-side listener here, so it's a
genuine, intentional no-op against Halcyon. Design decisions were resolved
via a brainstorming session recorded in
[docs/superpowers/specs/2026-09-04-halcyon-boss-design.md](../../superpowers/specs/2026-09-04-halcyon-boss-design.md);
this doc describes the built system.

## At a Glance

- **Identity:** a mobility check — the first fight where standing still
  (or clustering) is the losing strategy. Unlike Marauder (stationary-but-
  erratic, confined near its home position), Halcyon roams the *whole*
  arena continuously.
- **Movement:** full-arena waypoint-to-waypoint roam (`HalcyonRoam.cs`),
  pausing briefly at each point before picking the next — see
  "HalcyonRoam.cs" below.
- **Mechanics:** [body contact damage](#halcyonbosscs) (touching it
  directly, kept from Marauder, unchanged mechanic) · [Surge
  window](#halcyonsurgecs) (a periodic stillness window — the only reliable
  time to land hits) · [Static Field pulse](#halcyonstaticfieldcs) (the
  fight's real damage source — punishes ships clustering near the boss and
  near each other).
- **Level summary:** reached via `LevelSelect` → `Level2.unity`, which runs
  the same shared `LevelSequencer` timeline every level uses (see
  [level-sequencing.md](../level-sequencing.md)) — no sequencing changes,
  only the boss instance and its own components differ from Marauder.

## Code architecture: sibling MonoBehaviours + IBoss

Unlike Marauder (one `MarauderBoss` component driving several owned,
non-MonoBehaviour helper classes), Halcyon's three mechanics are each their
own sibling `MonoBehaviour` on the same `Boss` GameObject —
`HalcyonRoam.cs`, `HalcyonSurge.cs`, `HalcyonStaticField.cs` — each with
its own `Update()`/cooldown, reading `HalcyonBoss`'s small public API
(`IsPhase2`) via `GetComponent` where needed. This keeps each mechanic
self-contained and independently sized (see "Not yet built" in
`marauder-boss.md` for why Marauder's shape doesn't fit this project's
current file-size conventions as cleanly).

`LevelSequencer.cs` and `PlayerController.cs` are reused verbatim across
every level scene and need to drive whichever boss a level uses without
knowing its concrete type. Since this project's convention is plain
MonoBehaviours with no inheritance, a small `IBoss` interface
(`SetVisible(bool)`, `ApplyContactDamage(GameObject)`) is a deliberate,
scoped exception — both `MarauderBoss` and `HalcyonBoss` implement it. See
`docs/architecture.md`'s "Boss-type-agnostic orchestration: IBoss" for the
full writeup, including the Inspector-serialization workaround
(`MonoBehaviour`-typed fields cast to `IBoss` in code, since Unity can't
serialize an interface-typed field directly).

## HalcyonBoss.cs

**Attached to:** `Boss` GameObject (`Level2.unity`'s boss instance — built
directly on the scene object rather than a separate reusable prefab, same
one-off-per-level treatment Marauder gets). Same physical setup as
`MarauderBoss` (`SpriteRenderer`, `Rigidbody2D` at Gravity Scale 0,
non-trigger `BoxCollider2D`, tag `Enemy`) so `Bullet.cs`'s existing
trigger-based hit detection needs no new physics setup.

HP/phases/body-contact-damage core only — no aggro, no bullets fired.
`maxHealth` 110 (higher than Marauder's, since Static Field's damage is the
only source and needs enough HP to carry the fight across both phases).
`bulletDamage` (1) is a reference unit only, never spent on an actual shot
— `HalcyonStaticField`'s pulse damage and body contact damage both
multiply it, the same convention `MarauderBoss.bulletDamage` uses. Body
contact damage itself is unchanged from Marauder: `bodyContactDamageMultiplier`
2×, cooldown-gated per target (`contactDamageCooldown`, 1s).

Starts disabled (component `enabled: false` in the scene, matching
`MarauderBoss`'s own convention) — `LevelSequencer` enables it at
`BossCombat`, firing `OnEnable()`, which enables the three sibling
mechanics (`HalcyonRoam`/`HalcyonSurge`/`HalcyonStaticField`, all cached via
`GetComponent` in `Awake()` and force-disabled there too, defensively).
`SetVisible(bool)` hides/shows the sprite/collider together with
`HalcyonStaticField`'s ring (via `HalcyonStaticField.SetRingVisible`) —
same `LevelSequencer`-driven entrance-hiding shape `MarauderBoss.SetVisible`
uses, see [level-sequencing.md](../level-sequencing.md).

`Bullet.cs`'s player-bullet-vs-`Enemy`-tag branch gained a fourth check
(alongside `Enemy`/`MarauderBoss`/`Minion`): `other.GetComponent<HalcyonBoss>()`
→ `TakeDamage(damage)` — a single-`float`-param overload, not
`TakeDamage(float, GameObject)` like Marauder's, since Halcyon has no
aggro to attribute the source to.

## HalcyonRoam.cs

Full-arena waypoint-to-waypoint movement: picks a random point within the
viewport bounds (same clamp idiom `PlayerController`/`MarauderBoss` use),
glides there via `Vector3.MoveTowards` at `roamSpeed` (2.5 u/s phase 1, 3.5
u/s phase 2 — the phase-2 escalation for this mechanic), pauses briefly
(`Random.Range(pauseMin, pauseMax)`, 0.3–0.8s) on arrival, then picks the
next point — re-rolling (bounded retries) if the new point is too close to
the current one (`minWaypointDistance`, 2). Same "sit, hop" idiom as
Marauder's M-pattern hops, just arena-wide with random destinations instead
of a fixed vertex set, and speed-driven rather than duration-driven since
"matches ship speed" was the design brief.

Freezes in place (skips its movement step in `Update()`) whenever
`HalcyonSurge.IsTelegraphing || HalcyonSurge.IsActive` — read via a cached
`GetComponent<HalcyonSurge>()` reference, no event/callback needed since
both are plain polled booleans.

## HalcyonSurge.cs

A stillness/vulnerability window, identical timing in both phases (not
touched by the phase-2 escalation): idle (`cooldown`, 8s) → telegraph
(`telegraphTime`, 1s — the boss is already stationary here, via
`HalcyonRoam`'s freeze check) → active window (`activeTime`, 2s) → back to
idle. "Vulnerable" means only that the boss is reliably hittable — it's
otherwise always moving, so this is the one consistent window to land
hits — no damage-taken multiplier is applied.

Public API: `IsTelegraphing`, `IsActive`, `CooldownRemaining` — read by
`HalcyonRoam` (to freeze) and `HalcyonBossPanelUI` (a `"Surge!"` warning
text while either flag is true, plus a cooldown countdown).

## HalcyonStaticField.cs

The fight's actual damage source. Every `pulseCooldown` (6s phase 1, 4s
phase 2 — the phase-2 escalation for this mechanic), pulses from the
boss's live position: any two ships that are both within `bossRange` (1.8
units, ~3 ship-widths) of the boss AND within `clusterRange` (0.6 units,
~1 ship-width) of each other both take `damageMultiplier × bulletDamage`
(3 × 1 = 3) — reusing `MarauderBoss`'s shockwave damage convention (3×
bullet damage) exactly. A short telegraph (`telegraphTime`, 0.3s, via a
coroutine — the one mechanic here that needs a wait rather than pure
`Update()` polling) precedes the actual damage application.

**Ships roster**: `public GameObject[] ships` — proximity data only, no
aggro (unlike Marauder's `targets[]`, this never feeds a threat table).
Still needs the *live* spawned roster on the normal co-op join route, for
exactly the reason `MarauderBoss.targets[]` does (see marauder-boss.md's
"Aggro roster comes from `targets[]`") — `PartySetupBootstrap.SpawnDynamicParty()`
deactivates the 4 legacy scene ships and spawns fresh ones, so a stale
Inspector-wired `ships[]` would mean Static Field never finds a live
candidate. `PartySetupBootstrap` gained a `halcyonStaticField` field (only
set in `Level2.unity`) alongside its existing `bossObject` field, assigning
`halcyonStaticField.ships = spawned` the same place `MarauderBoss.targets`
gets its equivalent fix.

**Ring visual**: same always-dim / brighter-during-telegraph /
impact-flash-on-hit `LineRenderer` idiom as Marauder's shockwave ring, at
radius `bossRange`, built once in `Awake()` and toggled by
`HalcyonBoss.SetVisible` via `SetRingVisible(bool)`.

**Public API**: `CooldownRemaining`, `SetRingVisible(bool)`.

## HalcyonBossPanelUI.cs

**Attached to:** `Level2.unity`'s `BossPanel` (replacing `BossPanelUI` —
each level's `BossPanel` script matches its own boss type, not a shared
reusable component). Same "HUD only reads, never owns game state" pattern
as `BossPanelUI.cs`, against Halcyon's much smaller public surface: HP bar
+ text, phase text, a Surge warning + cooldown text, a Static Field
cooldown text only (no named warning — same "the ring itself is the tell"
choice Marauder's own Shockwave already makes). No target text, no
guided-missile/pattern-barrage text — none of that exists for this boss;
`Level2.unity`'s now-unused `BossTargetText`/`BossPatternBarrageWarningText`/
`BossPatternBarrageCooldownText` rows are deactivated rather than left
showing stale text. `BossWarningText` and `BossShockwaveCooldownText`/
`BossGuidedMissileCooldownText` are reused (by content, not by name) as
Halcyon's Surge warning/cooldown and Static Field cooldown rows.

## Scene wiring

### Boss

**Tag:** `Enemy`. `Level2.unity`'s `Boss` GameObject (previously
`Level2BossPlaceholder.prefab`, a `MarauderBoss`-typed duplicate of
Marauder) now carries `HalcyonBoss`/`HalcyonRoam`/`HalcyonSurge`/
`HalcyonStaticField` directly instead — built in place on the same
GameObject (physical setup, position `(0, 4.2, 0)`, scale `1.6` all
unchanged) rather than a separate prefab asset, since a boss is a one-off
per level, same treatment `MarauderBoss.prefab` gets in `Level1.unity`.

| Component | Key inspector values |
| --- | --- |
| **HalcyonBoss.cs** | `maxHealth`: 110; component starts **disabled** |
| **HalcyonRoam.cs** | `roamSpeed`/`roamSpeedPhase2`: 2.5/3.5 |
| **HalcyonSurge.cs** | `cooldown`/`telegraphTime`/`activeTime`: 8/1/2 |
| **HalcyonStaticField.cs** | `pulseCooldown`/`pulseCooldownPhase2`: 6/4; `bossRange`/`clusterRange`: 1.8/0.6; `ships`: `Player` + all 3 `Teammate_*` |

### LevelSequencer / PlayerController / AIController / PartySetupBootstrap

All four scripts' boss-reference fields are now `MonoBehaviour bossObject`
(cast to `IBoss` in code where needed) instead of `MarauderBoss`-typed —
see `docs/architecture.md`'s "Boss-type-agnostic orchestration: IBoss".
`Level2.unity`'s `LevelSequencer`/`PartySetupBootstrap` and all 4 ships'
`PlayerController`/`AIController` have `bossObject` dragged to the `Boss`
instance, same as `Level1.unity`'s do to `MarauderBoss.prefab`.
`PartySetupBootstrap` additionally has `halcyonStaticField` set (see
"HalcyonStaticField.cs" above) — a field that stays `null`/unused in
`Level1.unity`.

`AIController.UpdateAbilityUsage()`'s Tank heuristic (auto-taunt whenever
not holding the boss's aggro) casts `bossObject as MarauderBoss` and simply
skips the ability call when that's `null` — a Tank never auto-taunts
against Halcyon, matching its documented no-op there, without any
Halcyon-specific branching needed.

### Not shared with Marauder

`PartySetupBootstrap`'s aggro-roster fix (`boss.targets = spawned`) doesn't
apply here — Halcyon has no `targets[]`/aggro at all, so that assignment is
simply absent for this boss, replaced by the `halcyonStaticField.ships =
spawned` assignment described above.

`Level2.unity`'s 4 ships previously carried Marauder's `OnTaunt` →
`MarauderBoss.TauntedBy` persistent listener (inherited from when the scene
was duplicated from `Level1.unity`, back when `Level2`'s boss was still the
`MarauderBoss`-typed placeholder). Swapping the `Boss` GameObject's
component from `MarauderBoss` to `HalcyonBoss` in place deleted the
listener's target, leaving a dangling (`null`-target) persistent listener
on each ship's `PlayerAbility.OnTaunt` — silently inert (Unity skips a
null-target listener), same net effect as the intended no-op, but not the
clean absence the design called for. Removed on all 4 ships.

## Not yet built

- **Real human playtest** — the mechanics above are verified via the Unity
  MCP bridge (direct method calls confirming waypoint bounds, the Surge
  state machine's timing, Static Field's pairwise proximity damage
  including both negative cases — a lone ship near the boss, and two ships
  clustered but far from the boss — and phase-2 numbers actually changing)
  plus a live Play-mode pass through `LevelSelect` → `Level2` confirming
  the boss appears/roams/HUD updates correctly, but no real human has
  played the fight yet.
- **Out of scope by design**: everything Marauder has that isn't body
  contact damage — phase-based fire, shockwave, guided missile, Pattern
  Barrage, minions, aggro/taunt. This boss's identity is Roam + Surge +
  Static Field, full stop, not a deferred subset of Marauder's kit.
