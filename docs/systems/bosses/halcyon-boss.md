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

> **Session 51:** this boss is now its own prefab, `Assets/Prefabs/HalcyonBoss.prefab`
> (inactive root at its home position), spawned into the one shared
> `Level.unity` by `LevelSetup` only when its level is picked. Its panel is
> `HUDCanvas/BossPanel_Halcyon`, bound at spawn. Ship rosters, taunt and the
> defeat/Victory wiring are set at runtime (see
> [scene-flow.md](../scene-flow.md)'s "Level setup"). Notes below about
> per-level scenes describe how the boss was originally built.

## At a Glance

- **Identity:** a mobility check — the first fight where standing still
  (or clustering) is the losing strategy. Unlike Marauder (stationary-but-
  erratic, confined near its home position), Halcyon roams the *whole*
  arena continuously.
- **Movement:** full-arena waypoint-to-waypoint roam (`HalcyonRoam.cs`) at
  a randomized, constantly re-rolled speed, pausing briefly at each point
  before picking the next — see "HalcyonRoam.cs" below.
- **Mechanics:** [body contact damage](#halcyonbosscs) (touching it
  directly, kept from Marauder, unchanged mechanic) · [Surge
  window](#halcyonsurgecs) (a periodic stillness window — the only reliable
  time to land hits; shown to players as an **Opening**) · [Static Field
  pulse](#halcyonstaticfieldcs) (the fight's real damage source — chips any
  ship inside its ring, and punishes ships clustering near the boss and
  near each other much harder).
- **Level summary:** reached via `LevelSelect` → Level 2 (the shared `Level.unity`), which runs
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
glides there via `Vector3.MoveTowards` at `currentSpeed`, pauses briefly on
arrival, then picks the next point — re-rolling (bounded retries) if the
new point is too close to the current one (`minWaypointDistance`, 2). Same
"sit, hop" idiom as Marauder's M-pattern hops, just arena-wide with random
destinations instead of a fixed vertex set.

**Randomized speed** (playtest feedback: a fixed 2.5/3.5 u/s made its
arrival easy to predict). Speed, pause, and re-roll timing live in two
`HalcyonRoamPhaseSettings` fields, `phase1` and `phase2` (a
`[Serializable]` plain class, same grouping pattern as
`MarauderBossShockwaveSettings`). `currentSpeed` is re-rolled from
`[speedMin, speedMax]` on enable, on every waypoint arrival, **and**
mid-glide every `[speedChangeIntervalMin, speedChangeIntervalMax]`
seconds, so the boss visibly speeds up/slows down partway to a point:

| | Phase 1 | Phase 2 |
|---|---|---|
| speed (u/s) | 3.5–5.5 | 5–7 |
| pause on arrival (s) | 0.2–0.5 | 0.05–0.2 |
| mid-glide re-roll every (s) | 0.5–1.2 | 0.4–0.9 |

For reference, ship speeds are 1.2 (Tank) to 3.6 (Support) — Halcyon now
always outpaces every ship, so the party tracks it by positioning, not by
chasing it.

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
`HalcyonRoam` (to freeze), `HalcyonSurgeTint` and `HalcyonBossPanelUI`.

**Player-facing name: "Opening".** Playtesting found "Surge!" read as an
incoming attack, the opposite of what the window is. The C# names stay
`HalcyonSurge`/`surge*`; only what the player sees changed:

- `HalcyonSurgeTint.cs` (sibling component, purely visual) pulses the
  boss sprite toward `openingColor` (green) during the telegraph and holds
  it solid while active, back to the base sprite color otherwise.
- `HalcyonBossPanelUI` shows "Opening!" (yellow, `telegraphTextColor`)
  during the telegraph, "OPENING — FIRE!" (green, `openingTextColor`)
  while active, and an "Opening: Xs" cooldown.

## HalcyonStaticField.cs

The fight's actual damage source. Every `pulseCooldown` (6s phase 1, 4s
phase 2 — the phase-2 escalation for this mechanic), pulses from the
boss's live position and damages every ship within `bossRange` (1.8 units,
~3 ship-widths):

- **Lone ship** (no other in-range ship within `clusterRange`): takes
  `soloDamageMultiplier × bulletDamage` (1 × 1 = 1). Added after
  playtesting — previously a lone ship was completely safe inside the ring.
- **Clustered ship**: takes `damageMultiplier × bulletDamage` (3 × 1 = 3)
  **per** in-range partner within `clusterRange` (0.6 units, ~1
  ship-width) — 3 for a pair, 6 each for three stacked ships, 9 each for
  four. Same totals as the original pairwise rule, reusing
  `MarauderBoss`'s shockwave damage convention (3× bullet damage).

Covered by `Assets/Tests/EditMode/HalcyonStaticFieldTests.cs`. A short telegraph (`telegraphTime`, 0.3s, via a
coroutine — the one mechanic here that needs a wait rather than pure
`Update()` polling) precedes the actual damage application.

**Ships roster**: `public GameObject[] ships` — proximity data only, no
aggro (unlike Marauder's `targets[]`, this never feeds a threat table).
Still needs the *live* spawned roster on the normal co-op join route, for
exactly the reason `MarauderBoss.targets[]` does (see marauder-boss.md's
"Aggro roster comes from `targets[]`") — `PartySetupBootstrap`
deactivates the 4 scene ships and spawns fresh ones, so a stale
Inspector-wired `ships[]` would mean Static Field never finds a live
candidate. `BossEvents.AssignShips` (called by `PartyBossWiring`) sets
`ships` on the spawned boss's `HalcyonStaticField`, the same place
`MarauderBoss.targets` gets its equivalent fix.

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
| **HalcyonBoss.cs** | `maxHealth`: 110; component starts **disabled**; `OnDefeated` → `HalcyonBossPanelUI.ShowDefeated` + `VictoryUI.Show` |
| **HalcyonRoam.cs** | `phase1`/`phase2`: see the table under "HalcyonRoam.cs" |
| **HalcyonSurge.cs** | `cooldown`/`telegraphTime`/`activeTime`: 8/1/2 |
| **HalcyonSurgeTint.cs** | `openingColor`: green `(0.35, 1, 0.45)` |
| **HalcyonStaticField.cs** | `pulseCooldown`/`pulseCooldownPhase2`: 6/4; `bossRange`/`clusterRange`: 1.8/0.6; `damageMultiplier`/`soloDamageMultiplier`: 3/1; `ships`: `Player` + all 3 `Teammate_*` |

`OnDefeated` had **no** listeners before this was fixed — the persistent
listeners were lost when the scene's `Boss` swapped its `MarauderBoss`
component for `HalcyonBoss`, so beating Halcyon never showed the Victory
screen.

### LevelSequencer / PlayerController / AIController / PartySetupBootstrap

All four scripts' boss-reference fields are now `MonoBehaviour bossObject`
(cast to `IBoss` in code where needed) instead of `MarauderBoss`-typed —
see `docs/architecture.md`'s "Boss-type-agnostic orchestration: IBoss".
`Level2.unity`'s `LevelSequencer`/`PartySetupBootstrap` and all 4 ships'
`PlayerController`/`AIController` have `bossObject` dragged to the `Boss`
instance, same as `Level1.unity`'s do to `MarauderBoss.prefab`.
Since Session 51 the boss is spawned from `HalcyonBoss.prefab` into the
shared `Level.unity`, so these references are set at runtime by
`PartyBossWiring` rather than dragged in the Inspector.

AI teammates against Halcyon use the default `AIBossTactics`. That's the
per-boss AI strategy since Session 46; see `docs/architecture.md`'s
"Per-boss AI behavior". Its `ShouldTaunt` is false, so an AI Tank never
auto-taunts against Halcyon, matching Taunt's documented no-op there. Its
`FocusPoint` is the boss. No Halcyon-specific AI class is needed.

### Not shared with Marauder

The aggro-roster fix (`boss.targets = ships`) doesn't apply here —
Halcyon has no `targets[]`/aggro at all, so `BossEvents.AssignShips` sets
`HalcyonStaticField.ships` instead, as described above.

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

- **Further balance tuning** — the fight has now had a real human
  playtest (Session 38): the original version felt too predictable and
  "Surge!" read as an attack, which drove the randomized roam, the
  Opening cue, and solo Static Field damage above. The revised fight was
  reported to feel better. All numbers are still first-pass values open
  to further adjustment.
- **Out of scope by design**: everything Marauder has that isn't body
  contact damage — phase-based fire, shockwave, guided missile, Pattern
  Barrage, minions, aggro/taunt. This boss's identity is Roam + Surge +
  Static Field, full stop, not a deferred subset of Marauder's kit.
