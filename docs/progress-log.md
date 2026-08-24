# Progress Log

Session-by-session narrative history (the "why" behind decisions,
troubleshooting notes). Sessions 1-9 (pre-boss, single-player
fundamentals) were archived to
[progress-log-archive.md](progress-log-archive.md) to keep this file
scoped to the boss-encounter arc onward — cross-session references to
Session 1-9 below point there. A few Unity/MCP environment gotchas that
kept getting re-explained across many sessions (Editor-idle Play-mode
ticking, prefab-instance property recording, stale serialized defaults)
were also consolidated into single canonical write-ups in
[unity-notes.md](unity-notes.md), with the sessions below now pointing at
those instead of re-stating the mechanism each time.

## Session 10 — Boss Encounter Prototype

The roadmap's next item and the project's core design bet
(`overview.md`): prove MMO-raid-style role coordination is fun with one
human player plus CPU-controlled AI teammates, before any networking
exists. Full reference for everything below: `systems/boss.md`.

### Scope and design decisions

Kept deliberately prototype-simple, matching the project's "prove fun
before infra" style already established in Sessions 4/7 (see
progress-log-archive.md):

- **Boss doesn't chase** — sine-drifts near the top of the screen (same
  pattern as `Enemy.cs`) and aims at whichever target holds highest aggro.
  No pathfinding needed to prove the aggro mechanic.
- **Aggro is a plain threat table** (`Dictionary<GameObject, float>`,
  damage-dealt-per-target, no decay) rather than a fuller MMO-style threat
  system — this is a prototype pass, not the final design.
- **2 phases on one HP bar**, not two separate encounters: Phase 1 (100%→50%
  HP) fires a single aimed shot; crossing 50% flips to Phase 2 (fire
  interval halved, 3-bullet spread). Reaching 0 HP in either phase ends the
  fight — there's no third phase after Phase 2 by design.
- **AI teammates reuse the human `Player`'s exact component set**
  (`PlayerController`, `PlayerHealth`, `PlayerRoleComponent`,
  `PlayerAbility`) with `PlayerInput` swapped for a new `AIController.cs`,
  rather than writing separate AI-specific movement/combat logic — keeps
  the AI teammates mechanically identical to a human player in every way
  except how their input is produced.
- Only the human `Player`'s death shows `GameOverPanel`; a teammate dying
  just grays its own party frame and keeps fighting inactive.
- Role assignment is Inspector-only (matches the existing single-player
  pattern) — no role-select UI, that's still scene-scaffolding scope,
  deferred per the roadmap's build order.

### New scripts

- `Boss.cs` — health/phases/aggro/firing, `TakeDamage(int, GameObject)`,
  `TauntedBy(GameObject)`, `OnPhase2`/`OnDefeated` events.
- `AIController.cs` — drives a teammate's movement (sine-weave strafe),
  firing (continuous auto-fire), and ability use (per-role heuristic: Tank
  taunts when it doesn't hold aggro, Medic heals below a threshold,
  Support/Attacker just retry every frame since `TryUseAbility()`'s own
  cooldown gate makes that safe).
- `BossPanelUI.cs` — reads `Boss`'s state into the rebuilt `BossPanel` HP
  bar/phase/target text.

### Minimal-diff changes to existing scripts

Rather than duplicating movement/fire/ability logic for AI, extracted
non-input public entry points from the existing input-driven ones:
`PlayerController.OnMove`/`OnFire` now wrap new `SetMoveDirection(Vector2)`/
`SetFiring(bool)`; `PlayerAbility.OnAbility` now wraps a new
`TryUseAbility()`. No behavior change for the human `Player`. Also:
`Bullet.Init()` gained an optional `GameObject ownerObject` param (default
`null` keeps every existing call site compiling) so player bullets can
attribute damage to their shooter, and `Bullet.OnTriggerEnter2D`'s
player-bullet-vs-`Enemy`-tag branch now also checks for a `Boss` component
and routes damage to it — previously only `Enemy.TakeDamage` was reachable.

### Bug found during testing: unsafe dictionary indexer

`Boss.PickTarget()` originally indexed the `aggro` dictionary directly
(`aggro[t]`) assuming every active `targets[]` entry was always a populated
key. Live Play-mode testing hit a `KeyNotFoundException` on `Player`
specifically, thrown every `Update()` — since the exception aborted the
rest of `Update()` before reaching `Fire()`, this silently stopped the boss
from firing at all once it started. Root cause wasn't fully pinned down
(the `targets[]`/`aggro` population from `Awake()` checked out correctly in
isolated re-tests), but the fix is correct regardless: switched to
`Dictionary.TryGetValue`, which can't throw and costs nothing extra. Not
caught by compilation or an initial quick Play-mode smoke test — only
surfaced during sustained live testing, a reminder that MonoBehaviour
`Update()` exceptions fail silent-ish (logged, not crashing) and can hide
inside otherwise-working systems.

### Bug found during testing: boss placed outside camera view

The `Boss` GameObject was initially placed at world `y=6`, but Main
Camera is orthographic with size 5 (visible Y range roughly `[-5, 5]`) — the
boss was completely invisible in Play mode despite every script and event
wiring working correctly. Caught by actually looking at a screenshot, not
by inspecting field values (which all looked fine). Moved to `y=4.2`.
**Lesson reinforced**: numeric/logical verification isn't a substitute for
a visual check when a bug could be purely spatial/visual.

### Unity MCP bridge quirks hit this session

- `manage_prefabs`'s `component_properties` and `manage_components`'s
  `set_property` both failed to resolve the type name `"PlayerController"`
  (ambiguous — a `VariableExamples+PlayerController` sample type also
  exists somewhere in the loaded assemblies; `manage_components` separately
  reported "not found" for the same name). Worked around by using
  `execute_code` with a direct, compile-time-unambiguous
  `GetComponent<PlayerController>()` call instead of the reflection-based
  tools, for every edit that needed to touch this specific component type.
- Object-reference component properties need to be passed as `{"instanceID":
  N}` objects in an array, not bare integers — a bare-int array silently
  produced an array of `null`s (`Boss.targets` came back `[null, null,
  null, null]` on the first attempt, only caught by reading the value back
  afterward).
- `create_from_gameobject` (prefab-izing an existing scene GameObject) can
  disconnect mid-call (likely from the asset-import domain reload it
  triggers) — retrying the same call after checking `editor/state` for
  `ready_for_tools` succeeded cleanly, with the scene GameObject's data
  intact.
- Duplicating a GameObject (`Teammate_Medic`/`Teammate_Support`, both
  duplicated from `Teammate_Tank` *before* `Teammate_Tank` was converted
  into `Teammate.prefab`) does **not** retroactively make the duplicates
  prefab instances — they stayed independent GameObjects with matching
  values, so a later edit to `Teammate.prefab`'s defaults (see Session 11)
  only affected `Teammate_Tank`, not the other two, and had to be applied
  to all three individually. Documented in `systems/boss.md`'s scene-wiring
  section so this doesn't get assumed away later.
- Hit the now-familiar Editor-idle gotcha again (see
  `docs/unity-notes.md#editor-doesnt-tick-play-mode-updatecoroutines-while-unfocused-and-the-inverse`)
  — it showed up here as the boss appearing to take almost no damage
  across several tool calls and then being defeated between the next two.
  Not a gameplay bug.

### Verification

All done live via the Unity MCP bridge in Play mode: phase transition
flips `IsPhase2` exactly at the 50%-HP boundary and fires `OnPhase2`
exactly once; aggro correctly tracks the highest damage-dealer and
`TauntedBy()` redirects `CurrentTarget`, with a second immediate taunt
blocked by the existing cooldown gate; AI teammates were observed
autonomously moving, firing, and triggering role abilities (Tank's taunt
firing for real the moment it didn't hold aggro, Support's buff
auto-activating); boss defeat fires `OnDefeated`, flips `BossPanelUI` to
"DEFEATED", and destroys the `Boss` GameObject cleanly; all 4
`PartyFrameUI` instances and `BossPanelUI` read live values with no drift
from the underlying `Boss`/`PlayerHealth` state.

### Still open

- No minions around the boss yet (motivates Session 11's ship-shrink).
- Local co-op / a dynamic player count — the party is 4 fixed, hand-placed
  scene objects, not a runtime spawner.
- Medic heal still only targets self, even though allies (the AI
  teammates) now exist to target.

## Session 11 — Boss Fight Tuning

Follow-up requested after Session 10's playtest: the fight was too easy,
and ship sprites need to be smaller to leave room for minions planned
around the boss later.

### Changes

- **Fire cadence**: `PlayerController.fireRate`'s base value went from
  `0.2` to `0.35` (script default, `Teammate.prefab`, and all 4 scene
  ships), making the fight take more sustained effort while preserving
  each role's relative fire-rate balance (multipliers apply on top,
  unchanged).
- **Ship scale**: `Player`/`Teammate_*` `Transform.localScale` went from
  `1.0` to `0.6`. The `Boss` was deliberately left at its existing `1.6`
  scale (user's explicit choice) so it still reads as the big, central
  target once smaller minions are added around it later.

Neither change touched `FirePoint` (a child transform, so its effective
world offset scales automatically with the parent) or `BoxCollider2D`
(size scales with the transform automatically too) — confirmed no
additional edits were needed there.

### Reconfirmed the prefab-instance gotcha from Session 10

`Teammate_Tank` picked up the new `fireRate`/`scale` defaults automatically
from `Teammate.prefab` once it was edited (no per-instance override
existed to block inheritance). `Teammate_Medic`/`Teammate_Support` did
**not** — as flagged in Session 10, they're independent GameObjects, not
prefab instances — so they needed the same two values set directly, same
as `Player` (which was never part of the prefab to begin with).

### Verified

Read the 4 ships' live `fireRate`/`localScale` values in Play mode:
role-multiplied effective fire intervals matched expectations exactly
(Attacker 0.2625s, Support 0.35s baseline — briefly lower mid-buff, which
is correct, not a bug — Medic 0.35s, Tank 0.42s). A screenshot confirmed
visually: `Boss` unchanged and clearly larger, `Player`/teammates visibly
smaller and still firing correctly.

## Session 12 — Shield Stat, Tank AI Positioning, Boss HP/Damage Tuning

Design work (role-differentiated AI behavior, a new shield stat, a
manual-ability-trigger mechanic) had already been agreed and written up in
`docs/systems/*.md` as "planned, not yet implemented." This session
implemented the first slice — shield + Tank — then a follow-up tuning
request came in for boss HP and player damage. Full technical detail lives
in `systems/boss.md`, `systems/player-roles.md`, `systems/combat.md`,
`systems/hud-layout.md`; this is the narrative version.

### Shield stat

Added `RoleStats.shieldMultiplier` (Tank `2.0`, highest; Attacker `1.0`,
medium; Medic/Support `1.0`, placeholder — only two were specified by
design) and a `maxShield`/`CurrentShield` pool on `PlayerHealth`, scaled by
role the same way `maxHealth` already was. `TakeDamage(int)` now absorbs
into shield first, only the overflow touching health — a hit fully absorbed
by shield still fires `OnDamaged` (flash/shake), matching how a real hit
should feel. Added `RestoreShield(int)` (symmetric to `Heal(int)`) even
though nothing calls it yet — Medic's proximity aura is a separate,
still-planned follow-up — same "build the real method before the consumer
exists" precedent as Session 7's `Heal(int)` (see progress-log-archive.md).
Deliberately **no** passive
regen anywhere: shield only ever goes up via `RestoreShield`, keeping Tank
dependent on Medic by design.

### Tank guard-point positioning

`AIController.Update()` now branches on role: Tank calls a new private
`GuardPointDirection()` instead of the shared sine-weave. It averages the
positions of a new `teammates[]` array (Inspector-wired to the 3
`Teammate_*` transforms, self excluded at runtime), lerps from that toward
the boss by `guardBias` (0.65), and steers there (with a small deadzone to
stop jitter on arrival). Physically blocking bullets needed **zero changes
to `Bullet.cs`**: bullets already just travel in a straight line and damage
whichever `Player`-tagged collider they hit first, so a Tank standing in
the way already "blocks" an ally for free via the existing trigger
collision — this was purely a positioning problem once that was confirmed
by re-reading `Bullet.cs` rather than assumed. "Ignore the human player"
was achieved for free too: `teammates[]` simply never includes `Player`, no
runtime human-detection check needed, since the human always plays `Player`
specifically (see `current-state.md`) and the 3 `Teammate_*` are always
AI-controlled regardless of which role each currently has.

### Gotcha: scene wiring didn't survive a save, twice

First attempt at wiring `teammates[]` via `execute_code` +
`EditorUtility.SetDirty()` reported success and the scene save reported
success, but a fresh Play-mode check showed the array empty. Root cause:
`Teammate_Tank` is a `Teammate.prefab` instance (Session 10) — hit the
now-familiar prefab-instance-override gotcha, see
`docs/unity-notes.md#prefab-instance-overrides-need-recordprefabinstancepropertymodifications-not-just-setdirty`.
Confirmed the fix by forcing a full scene reload from disk (not just
trusting the in-memory value) — this became the standard verification
method for the rest of the session, and caught the exact same class of
issue again later (see below). `Teammate_Medic`/`Teammate_Support` (not
prefab instances) never needed the extra call.

### Verification

All via the Unity MCP bridge in Play mode: forced `TakeDamage` calls
confirmed shield absorbs first and only the overflow hits health, with
`OnDamaged` firing even on a shield-only hit; `RestoreShield` clamps at
`maxShield` correctly; read all 4 ships' live `maxShield` values and
confirmed the role multipliers applied (Tank 6, Attacker/Medic/Support 3).
Called the private `GuardPointDirection()` directly via reflection and
confirmed it matched the hand-computed expected direction exactly (dot
product 1.0), then let Play mode run and sampled positions over time: Tank
converged toward the guard point (both X and Y changing) while
Medic/Support kept moving only in X — confirming the non-Tank code path is
genuinely unchanged, not just visually similar. A screenshot during a live
fight showed Tank sitting between the boss and the other two teammates,
and the boss's live aggro target had already become Tank, confirming the
pre-existing taunt heuristic still works alongside the new positioning. The
party frame's new shield bar tracked live shield values correctly for all
4 frames once enough Play-mode frames had ticked (same Editor-idle quirk
as prior sessions — a value can look stale for a beat after a forced
`TakeDamage` call until the next pumped frame).

### Boss HP / player damage tuning (follow-up request)

Separate ask, same session: increase boss health, decrease all roles' fire
damage by 40%. No specific numbers were given, so picked round ones and
flagged them for the user to correct: `Boss.maxHealth` doubled (`30` →
`60`); regular fire damage `1` → `0.6`; Attacker's Big Shot `3` → `1.8`
(both hit values scale by the exact same 0.6× factor, so their 3:1 ratio is
preserved). Enemy/boss-dealt damage was explicitly out of scope — only
player-dealt damage.

A flat 40% cut on a baseline of `1` isn't representable as a whole number,
so `Bullet.damage` changed `int` → `float`, which rippled into
`Enemy.TakeDamage`/`Boss.TakeDamage` (also `int` → `float`) — each still
rounds (`Mathf.RoundToInt`) only at the point it subtracts from its own
`int` health pool, so no fractional HP shows up anywhere; the
enemy-bullet-vs-`PlayerHealth` path does the same rounding at its call
site, since `PlayerHealth.TakeDamage(int)` deliberately stayed `int`.

Hit the now-familiar script-default gotcha again (see
`docs/unity-notes.md#changing-a-scripts-default-value-doesnt-retroactively-update-an-already-serialized-field`),
same as Session 11, twice more: `Boss.maxHealth` and
`PlayerAbility.bigShotDamage` both had to be set explicitly on the live
scene instances (all 4 ships, for `bigShotDamage`) **and** on
`Boss.prefab`/`Teammate.prefab`'s defaults, with `Teammate_Tank` again
needing the prefab-instance-override fix (see
`docs/unity-notes.md#prefab-instance-overrides-need-recordprefabinstancepropertymodifications-not-just-setdirty`).
Both caught immediately by the same "force a full disk reload, don't trust
the in-memory value" verification habit established earlier this session —
without it, both would have silently reverted to their old values.

### Verified

End-to-end in Play mode: all 4 ships' `Fire()` produced bullets with
`damage == 0.6` (confirmed via `FindObjectsByType<Bullet>`); Attacker's Big
Shot produced a `damage == 1.8` bullet; `Boss.maxHealth`/`CurrentHealth`
read `60/60` after a full scene reload from disk. No compile errors or
console warnings from the type changes.

## Session 13 — Medic AI Positioning + Proximity Aura + Visual Feedback

The roadmap's "Recommended next" item, second slice after Tank (Session
12): Medic AI positioning (hang back from the boss) plus the proximity
heal/shield aura design that had been sitting as "planned, not yet
implemented" in `boss.md`/`player-roles.md` since Session 12.

### Design refinement before implementation

The originally-written design (a single always-large aura radius) got
revised in conversation before any code was touched: the aura is **tiny by
default** — allies need to almost touch the Medic to be healed — and
pressing **E drastically expands the radius and heal rate for a limited
duration**, replacing Medic's old instant self-heal ability entirely rather
than being additive to it (explicitly confirmed with the user — "Replace
with aura boost", not "do both"). This changes what `boss.md`/
`player-roles.md` had already described, so both docs needed updating
alongside the code, not just appending.

### Architecture decision: aura lives on `PlayerAbility`, not `AIController`

The aura and its boost ability were built on `PlayerAbility.cs`, **not**
`AIController.cs`, even though the positioning half of this session's work
*does* live on `AIController.cs`. Reasoning: `AIController` only exists on
the 3 `Teammate_*` GameObjects; `PlayerAbility` exists identically on
`Player` too. Per Session 10's stated principle that AI teammates are
"mechanically identical to a human player in every way except how input is
produced," the aura has to work the same way regardless of whether Medic is
currently human- or AI-controlled — so it couldn't live in a teammate-only
script. Positioning stays AI-only in `AIController.cs` since a human Medic
just moves via WASD.

### Positioning: generalized `GuardPointDirection()` instead of duplicating it

Rather than writing a second near-identical method for Medic, Tank's
existing `GuardPointDirection()` was generalized into `BiasedPositionDirection(bias,
deadzone)`, parameterized on the Lerp bias — Tank keeps `guardBias = 0.65`
(toward the boss, unchanged behavior), Medic gets a new `medicBias = -0.3`
(away from the boss). `AIController.Update()`'s movement switch grew a
third case instead of staying a binary Tank/everyone-else ternary.

**Bug caught before it shipped**: `Vector2.Lerp` clamps its `t` parameter to
`[0, 1]` in Unity — a negative `medicBias` would have silently clamped to
`0` (landing exactly on ally center, not extrapolating past it) rather than
actually pulling Medic away from the boss. Caught by reasoning about the
API, not by testing a broken result. Fixed by switching both Tank's and
Medic's calls to `Vector2.LerpUnclamped`, which lets `t` go outside `[0, 1]`
and extrapolate.

### Aura mechanics

New fields/methods on `PlayerAbility.cs`: passive `TickAura()` runs every
`auraTickInterval` (1s default) while `role == Medic`, healing/shielding
(`Heal(int)`/`RestoreShield(int)`, both pre-existing) every ally in
`allies[]` within `auraRadius` (0.5 — tiny by design). `TriggerAuraBoost()`
(replacing the old `TriggerHeal()` in `TryUseAbility()`'s switch) is a
coroutine flipping `auraBoosted` on for `auraBoostDuration` (4s), during
which `TickAura()` uses `auraBoostRadius` (3) and a much shorter
`auraBoostTickInterval` (0.25s) instead — same `StopCoroutine`/
`StartCoroutine` restart-safety pattern as Support's `TriggerBuff()`, and
the same "cooldown must stay ≥ duration" constraint Session 7 (see
progress-log-archive.md) documented
for that buff (`auraBoostCooldown` 10s ≥ `auraBoostDuration` 4s).

**New wiring needed**: `allies[]`, a `Transform[]` of all 4 ships
(self-included, filtered at runtime), had to be added fresh — the existing
`AIController.teammates[]` array deliberately excludes `Player` (see
Session 12), so it can't be reused for something that must also heal the
human player. Wired identically on all 4 ships' `PlayerAbility` via
`execute_code`, hitting the now-familiar prefab-instance gotcha once more
(see
`docs/unity-notes.md#prefab-instance-overrides-need-recordprefabinstancepropertymodifications-not-just-setdirty`):
`Teammate_Tank` needed the fix, `Teammate_Medic`/`Teammate_Support` didn't
(not prefab instances, per Session 10/11). Verified by forcing a full
scene reload from disk, same habit as every prior session that's hit this
gotcha.

### Follow-up: visual feedback

Playtesting the mechanic surfaced the obvious gap immediately: nothing in
the world shows the aura exists. Two additions, both requested together:

- **Radius ring** — a `LineRenderer` circle (32 segments, `Sprites/Default`
  shader, world-space so it isn't distorted by the ship's `0.6` transform
  scale) built procedurally as a child of the Medic's `PlayerAbility` in
  `Awake()` (only when `role == Medic`, so other roles don't pay for an
  unused GameObject). Dim/thin by default, brighter/thicker while boosted —
  updated every frame in `Update()` independent of the tick-gated
  `TickAura()` call, so the ring's size/brightness reflects boost state
  immediately even between heal ticks.
- **Heal flash** — `PlayerDamageFlash.Flash()` gained a `Flash(Color)`
  overload (existing parameterless `Flash()` now just calls it with the
  component's own `flashColor` field, so `OnDamaged`/`OnTaunt`'s existing
  wiring is unchanged) so `TickAura()` can flash a healed ally green
  (`healFlashColor`) distinctly from the white damage flash — only on
  allies that actually had missing health/shield that tick, not every ally
  in range regardless of whether they needed healing.

### Verified

All via the Unity MCP bridge. Play mode, reflection-called `TickAura()`
directly (same technique as Session 7-9's private-method verification, see
progress-log-archive.md):
healed an ally at distance 0 (in range), confirmed no change to a
subsequent hit while 20 units away (out of range), triggered the boost via
`TryUseAbility()` and confirmed an ally 2 units away — outside the default
radius but inside the boosted one — got healed. Confirmed the boost
reverts automatically (`IsAuraBoosted` false again) after its duration
using the same "temporarily shrink the duration for a fast test" technique
Session 7 (see progress-log-archive.md) used for Support's buff. Confirmed
via `BiasedPositionDirection()`
reflection calls that Tank's direction dot-products ~+1 with "toward the
boss" (matching Session 12's finding) while Medic's dot-products negative
(away from the boss). **Swapped which `Teammate_*` GameObject played Medic
mid-session and confirmed both the aura and the positioning followed the
role, not the GameObject** — the real test of the `allies[]`/prefab-instance
wiring. Screenshots confirmed the ring renders and visibly expands/brightens
during the boost, and the party frame's ability line correctly shows "Aura
Boost: Ready" / "Aura Boost: Boosted (Ns)" (no leftover "Heal" text
anywhere — `PartyFrameUI.cs` reads `PlayerAbility.AbilityName`/`StatusText`
generically, so it needed no changes itself). No console errors or warnings
at any point.

### Still open

- Attacker/Support AI positioning — still planned, see `boss.md`'s "AI
  teammate behavior". Medic and Tank are now both implemented.
- Bullet-dodging, teammate separation, manual teammate-ability triggering
  from the party frame — unchanged from Session 12, still designed but not
  built.

## Session 14 — Medic AI Trigger/Positioning Rework

Playtesting Session 13 surfaced a real problem: the Medic AI's aura boost
never fired in practice, not even once across a full test session. Root
cause was the trigger heuristic itself — `medicBoostThreshold` gated the
boost on the *Medic's own* HP dropping below 60%, but Medic's positioning
(hanging back, away from the boss) means it rarely takes damage, so the
gate almost never opened. The heuristic was checking the wrong ship's
health entirely — the boost is meant to help *allies*, not itself.

### New design

Agreed replacement, in two independent parts:

- **Ability trigger — temporary, explicitly flagged for rework**: Medic now
  fires the aura boost the instant it's off cooldown, identical to
  Support/Attacker's existing "retry every frame, let the cooldown gate
  sort it out" pattern. No need-awareness at all for now — marked with an
  explicit `TEMPORARY` comment in `AIController.cs` pointing back to this
  doc, since a smarter trigger (e.g. "boost when an ally is hurt," now that
  hurt-detection exists for positioning below) is an obvious near-term
  follow-up once this dumb version is validated.
- **Positioning — real, not temporary**: Medic's default is still hanging
  back (Session 13's `BiasedPositionDirection(medicBias, ...)`), but it now
  actively breaks from that position to approach whichever ally is hurt.
  "Hurt" is decided per-ally: below `medicApproachThreshold` (55%) in
  *either* health or shield fraction counts (mirrors `TickAura()`'s own
  health-or-shield check, so positioning and healing agree on what "needs
  help" means) — of potentially several hurt allies, Medic approaches
  whichever has the single lowest fraction. Checked every frame, so Medic
  re-targets immediately as the situation changes (an ally recovers, a
  different ally drops lower, everyone's fine again and it returns to
  hanging back).

### Why `PlayerAbility.allies`, not `AIController.teammates[]`

The hurt-ally check (`FindHurtAlly()`, new private method) iterates
`ability.allies` — the array Session 13 added to `PlayerAbility` for the
aura itself — rather than `AIController.teammates[]`, which was already
wired and would have been the "obvious" reuse. `teammates[]` deliberately
excludes `Player` (Tank's guard point is only supposed to average
AI-controlled allies' positions, see Session 12), but the Medic should
approach the human player if *they're* the one who's hurt just as readily
as a CPU teammate — `allies[]` already covers all 4 ships for exactly this
reason. No new wiring needed; it reuses Session 13's existing array as-is.

### Cleanup

`AIController`'s cached `PlayerHealth health` field became dead code once
the ability-trigger heuristic stopped reading it (the new trigger doesn't
check anyone's health) — removed rather than left unused.

### Verified

Unity MCP, Play mode: with the whole party at full health, a couple of
frames in, `PlayerAbility.CooldownRemaining`/`IsAuraBoosted` on the Medic
already showed the boost had fired (confirms the "as soon as available"
trigger actually fires, unlike the old heuristic). Reflection-called
`FindHurtAlly()` directly: returned `null` while everyone was healthy;
after damaging Support down to 40% health / 0% shield, returned
`Teammate_Support`, and `ApproachDirection()`'s returned direction
dot-producted `1.00` against the exact hand-computed direction to Support
(same verification style as Session 12's guard-point check). No console
errors or warnings.

### Still open

- The "temporary" ability trigger is still just "fire on cooldown" — see
  above for the flagged follow-up once this is validated as an improvement
  over the old (broken) behavior.
- Attacker/Support AI positioning, bullet-dodging, teammate separation,
  manual teammate-ability triggering — unchanged, still not built.

## Session 15 — Support AI Positioning + Fire-Cadence/Damage Catch-up

The roadmap's "Recommended next" item: `AIController.cs`'s Support role
still just weaved in X with no awareness of the boss, allies, or screen
space, unlike Tank (guard-point) and Medic (hang-back + approach-hurt-ally)
from Sessions 12-13. `docs/systems/boss.md`'s "Future work" section already
had a decided design for Support (agreed 2026-08-20, never implemented),
bundling two things together: AI positioning ("roams the available screen
freely rather than holding a zone") and combat stats ("the same fire
cadence as Attacker, the same fire damage as Tank"). Confirmed with the
user upfront to implement both halves in this session, not positioning
only.

### Positioning: random-waypoint wander, not a biased point

Tank/Medic's existing `BiasedPositionDirection()` steers toward a point
derived from the ally center and the boss's position, then holds there —
wrong shape for Support, which has no "zone" at all by design. Instead,
`AIController.cs` got a new `WanderDirection()`: steers toward a private
`roamTarget`, picking a new random point (`RandomRoamPoint()`, uniformly
sampled within the same viewport bounds `PlayerController.HandleMovement()`
already clamps to, reusing its public `screenPadding` field rather than
duplicating the inset constant) whenever the current one is reached (within
`roamDeadzone`, 0.3) or after `roamInterval` (3s) elapses, whichever comes
first. Deliberately does **not** return `Vector2.zero` inside the deadzone
like `ApproachDirection()`/`BiasedPositionDirection()` do — those correctly
hold position once arrived (Tank's guard point, Medic hanging back), but
Support should keep moving continuously, so arriving immediately triggers
picking the next point instead.

Added a `case PlayerRole.Support:` to `AIController.Update()`'s movement
switch, previously grouped under the shared `default:` with Attacker — the
`default:` case (and its comment) now covers Attacker only, the last role
still on the original sine-weave.

No new-field scene-wiring gotcha applied here, unlike most of this
project's prior tuning passes: `roamDeadzone`/`roamInterval` are brand-new
fields, not edits to already-serialized existing ones, so every
`Teammate_*` instance picked up the script defaults automatically with no
per-instance override needed.

### Stats: a new `damageMultiplier`, and a side effect on Tank

"The same fire damage as Tank" turned out to require more than a lookup
change: **no role had ever had elevated fire damage** — `PlayerController.Fire()`
hardcoded `SpawnBullet(1f, 0.6f)` for every role alike (the `0.6` itself was
a flat 40% cut applied uniformly in Session 12's tuning, not a per-role
value). Giving Support "Tank's damage" meant introducing a new
`RoleStats.damageMultiplier` stat and deciding what Tank's own value should
be, not just Support's — a small balance change to Tank as a side effect
of implementing Support's design faithfully, flagged to the user rather
than silently expanded scope. Picked `1.5x` for both (round placeholder,
tunable like every other not-yet-playtested balance value in this
project) — Attacker/Medic stay at the `1.0x` baseline, since Attacker's
high damage already comes from Big Shot, untouched by this stat.

Implementation followed the existing `moveSpeed`/`fireRate` pattern
exactly: new `PlayerController.fireDamage` field (base `0.6`), multiplied
by `Stats.damageMultiplier` once in `Start()` alongside the existing two
multiplications, then `Fire()`'s hardcoded literal became `SpawnBullet(1f,
fireDamage)`. Also bumped Support's `fireRateMultiplier` `1.0` → `0.75` to
match Attacker's cadence, completing the decided design. Tank's
`fireRateMultiplier` (1.2, slower) was deliberately left unchanged — only
the fire-damage side of Tank's stats was part of Support's design, not its
cadence.

### Verified

Unity MCP bridge, Play mode. Read all 4 ships' live `fireRate`/`fireDamage`:
Support showed `fireDamage = 0.9` (`0.6 × 1.5`, matching Tank, which also
read `0.9`) and a `fireRate` consistent with its buffed state at the moment
of sampling (Support's own buff ability multiplies `fireRate` further while
active — confirmed this was the AI's buff having already auto-fired, not a
bug, by cross-checking the math); Attacker/Medic stayed at `fireDamage =
0.6`, unchanged. `FindObjectsByType<Bullet>` confirmed live bullets in
flight carried `damage == 0.9` for Support/Tank and `0.6` for
Attacker/Medic (boss/enemy bullets, out of scope, stayed at their own
unrelated value). Reflection-called `WanderDirection()` on `Teammate_Support`
directly: returned a normalized direction with a non-trivial Y component,
and `roamTarget` landed within viewport bounds. Sampled its transform
position over several pumped frames (screenshot-forced frame-stepping, same
technique as every prior session) and confirmed both X and Y changed
over time, cross-checked against `Teammate_Tank`/`Teammate_Medic` (both
still moved in both axes as before, confirming their code paths were
unaffected by the new `case PlayerRole.Support` branch). No console errors
or warnings throughout.

### Still open

- Attacker AI positioning, bullet-dodging, teammate separation, manual
  teammate-ability triggering — unchanged, still not built. Attacker is now
  the only role without real AI positioning.
- Support's shield multiplier is still the placeholder `1.0x` baseline —
  only its fire-rate/damage were part of the decided design implemented
  this session; shield was never specified for it.

## Session 16 — Fixed Per-Role Stats + Ability Rework

User feedback after reviewing Session 15's multiplier-based stats: managing
health/shield/fire-rate/damage as `base × role multiplier` (e.g. Tank
health `5 × 1.6`) was confusing to reason about and hand-tune, especially
with fire rate stored *inverted* (`fireRate` meant seconds between shots —
lower was faster — despite reading like a rate). Requested a clear
single source of truth instead: fixed, absolute values per role, with
multipliers reserved strictly for temporary buffs/abilities, applied
non-destructively rather than mutated into a field and divided back out
later (the exact mechanism that made the old Support buff need
`buffCooldown ≥ buffDuration` to avoid double-applying).

### Architecture: `RoleStats` becomes fixed values

`PlayerRole.cs`'s `RoleStats` struct dropped every multiplier field
(`healthMultiplier`, `shieldMultiplier`, `fireRateMultiplier`,
`damageMultiplier`, `moveSpeedMultiplier`) in favor of direct values
(`maxHealth`, `maxShield`, `fireDamage`, `shotsPerSecond`, `moveSpeed`).
`PlayerHealth.Awake()`/`PlayerController.Start()` now just assign these
straight from `Stats`, no multiplication, no `Mathf.RoundToInt` needed
(the user's given numbers were already whole where it mattered).
`PlayerController.fireRate` was renamed `shotsPerSecond` and its meaning
flipped to match — higher is now faster, matching how the user specified
the design ("2.5 bullets/second") rather than the old inverted-interval
field. Final table (all user-specified, not derived):

| Role     | Health | Shield | Fire damage | Fire rate | Move speed |
| -------- | ------ | ------ | ------------ | --------- | ---------- |
| Attacker | 6      | 5      | 2.0          | 2.5/s     | 3.0 u/s    |
| Tank     | 8      | 20     | 1.0          | 1/s       | 1.5 u/s    |
| Medic    | 4      | 3      | 0.7          | 1.5/s     | 3.0 u/s    |
| Support  | 5      | 3      | 1.0          | 2/s       | 4.5 u/s    |

**Sanity-checked the numbers before implementing**: flagged that Attacker
ends up with both the highest DPS (damage × rate = 5.0/s, 2.5–5x every
other role) *and* the second-best survivability (health + shield = 11,
ahead of Support's 8 and Medic's 7) — a real shift from the role's
original "glass cannon" framing (health used to be Attacker's *lowest*
stat). Noted as worth confirming deliberate, not blocking — user's numbers
were used as given.

### Non-destructive buff layer

`PlayerController` gained two runtime-only fields, `speedBuffMultiplier`/
`fireRateBuffMultiplier` (both default `1f`), read at the point of use —
`HandleMovement()`'s move vector, and a computed `FireInterval => 1f /
(shotsPerSecond * fireRateBuffMultiplier)` for the fire-cooldown gate —
rather than ever being multiplied into `moveSpeed`/`shotsPerSecond`
themselves. Only `PlayerAbility` sets them (Support's redesigned ability,
below), always via plain assignment. This eliminates the old buff's
revert-by-dividing-back-out entirely — there's no arithmetic to get wrong,
so the "cooldown must stay ≥ duration" constraint that applied to every
prior buff/boost in this project (Support's old buff, Medic's aura boost)
no longer applies to Support's ability at all.

### Bullet.cs — one-line fix enabling Tank's new mechanic

`Bullet.cs`'s enemy-bullet-vs-`Player` branch changed
`other.GetComponent<PlayerHealth>()` → `other.GetComponentInParent<PlayerHealth>()`
— a one-line, backward-compatible change (a ship's own collider still
resolves to its own `PlayerHealth` exactly as before) that lets a *child*
collider without its own `PlayerHealth` route a hit to its parent ship's
health pool. Existed specifically to make Tank's Shield Arc (below)
possible; without it, a bullet touching the arc would have been destroyed
but dealt no damage — a "free" block, not what shield-draining absorption
should feel like.

### Four ability changes, requested alongside the stats overhaul

- **Attacker — Big Shot**: damage changed from a separately hand-tuned
  flat number (`1.8`) to a live `2x` multiplier of the caster's *current*
  `fireDamage` (`bigShotDamageMultiplier`), computed at cast time — `2.0 ×
  2 = 4.0` at today's values. Stays proportional automatically if
  `fireDamage` is ever retuned again, rather than needing a second manual
  update.
- **Support — Speed Boost** (renamed from "Buff", fully redesigned):
  became **party-wide** instead of self-only — `TriggerSpeedBoost()` loops
  over `allies[]` (all 4 ships, the same array Medic's aura already uses)
  setting each ally's `speedBuffMultiplier`/`fireRateBuffMultiplier` to
  `speedBoostMultiplier` (1.5, one shared value for both stats now,
  replacing the old two separate move-speed/fire-rate multipliers) for
  `speedBoostDuration` (4s). Cooldown bumped `8s → 15s` — flagged
  overpowered once it started affecting the whole party, round
  placeholder. New party-wide visual: every ship (any role, not just
  Support — built unconditionally, since any of the 4 could receive the
  boost) got an initially-hidden `PartyBuffRing`, toggled via a new
  `SetPartyBuffVisual(bool, Color)` call in the same `allies[]` loop — all
  4 rings light up in the caster's tint (Support's gold) together and
  disappear together, giving the buff a clear, readable tell instead of
  just feeling arbitrarily strong.
- **Medic — Aura Boost radius**: halved, `3 → 1.5` — flagged overpowered
  at the original size. Nothing else about the aura changed.
- **Tank — Shield Arc** (new mechanic, not an `E`-triggered ability —
  passive and always-on, independent of Taunt): a wide, curved shield in
  front of Tank, both visual and **functionally blocking**. Built
  procedurally in `PlayerAbility.Awake()` only for `role == Tank` (same
  "only build what this role needs" precedent as Medic's ring): a child
  `ShieldArc` GameObject, tagged `Player`, with a local-space
  `EdgeCollider2D` (`isTrigger`) and matching `LineRenderer` sampling a
  shallow parabola, `shieldArcWidthMultiplier` (3x Tank's own collider
  width, read live from `BoxCollider2D.bounds.size.x`) wide. Local-space
  and built once — unlike Medic's ring (which resizes on boost and needs
  per-frame updates), the arc never changes shape, so it needs **no
  `Update()` at all**; being a child of Tank's transform, it tracks
  Tank's movement automatically. Relies on the `Bullet.cs` fix above to
  route absorbed hits into Tank's own shield/health, not a free block.
  **Known edge case, flagged not solved**: if the arc's collider region
  vertically overlaps Tank's own body collider, a bullet could in rare
  cases enter both in one physics step and double-hit — mitigated by the
  arc's Y-offset placing it above the body, not defended against with
  extra code, matching this project's established "flag it, don't
  over-engineer for a rare edge case" style.

### Boss HP tuning

`Boss.maxHealth` ×1.5'd (`60 → 90`), purely to give this larger rework
enough runway in a full playthrough to actually be observed, rather than
the fight ending before the new stats/abilities' effects are visible.

### Gotcha, hit twice (same class as every prior tuning pass)

Hit the now-familiar script-default gotcha again (see
`docs/unity-notes.md#changing-a-scripts-default-value-doesnt-retroactively-update-an-already-serialized-field`).
`Boss.maxHealth` (60 on the live scene instance
and `Boss.prefab`) and `PlayerAbility.auraBoostRadius` (3 on all 4 ships
except, unexplainedly, `Teammate_Tank` which already read `1.5` — never
fully root-caused, possibly a quirk of the field having been added to
`PlayerAbility.cs` after `Teammate.prefab`'s initial save, but the fix
(explicitly setting all 4 instances plus `Teammate.prefab`'s and
`Boss.prefab`'s defaults, verified via a full scene reload) is correct
regardless of the exact cause) both needed the same explicit-set-on-every-instance
treatment as every prior HP/damage tuning session. Every genuinely *new*
field this pass (`speedBuffMultiplier`/`fireRateBuffMultiplier`, the
Shield Arc's fields, the party-buff ring's fields) did **not** hit this —
new fields just pick up the script default, since there's no prior
serialized value to conflict with.

### Verified

Unity MCP bridge, Play mode. Read all 4 ships' live `maxHealth`/
`maxShield`/`fireDamage`/`shotsPerSecond`/`moveSpeed` right after
`Awake()`/`Start()`: matched the table above exactly for every role, no
rounding drift. Triggered Big Shot via reflection: spawned bullet carried
`damage == 4.0`, bullet width 3x normal. Confirmed Support's Speed Boost
(already auto-fired by the AI within the first frames of Play, expected
behavior) set all 4 ships' `speedBuffMultiplier`/`fireRateBuffMultiplier`
to `1.5` and activated all 4 party-buff rings in Support's gold tint.
**Tank's Shield Arc verified functionally, not just structurally**:
inspected the arc's `EdgeCollider2D` points (spanned ±0.9 local, matching
Tank's `0.6`-wide body × the `3x` multiplier, with the correct parabola
shape); spawned a fake enemy bullet positioned within the arc's width but
outside Tank's own body collider, pumped physics frames, and confirmed the
bullet was destroyed **and** Tank's `CurrentShield` dropped by the
bullet's exact damage (`20 → 18` for a 2-damage bullet) — the critical
check that the `Bullet.cs` fix actually routes the hit to Tank's own
health pool rather than silently no-oping. A same-position player-owned
bullet was confirmed to pass through untouched, Tank's health/shield
unchanged — no friendly-fire interaction. `Boss.maxHealth`/`CurrentHealth`
confirmed `90/90` after a full scene reload from disk. No console errors
or warnings throughout.

**Testing note**: hit the well-documented Editor-idle `Time.time` jump
quirk again mid-session (see
`docs/unity-notes.md#editor-doesnt-tick-play-mode-updatecoroutines-while-unfocused-and-the-inverse`)
— a gap between tool calls let real time jump to `Time.time = 62s`, during
which the human `Player` died from ongoing boss fire and a test bullet's
`lifeTime` naturally expired, initially looking like a collision bug
before the cause was traced. Resolved by keeping the friendly-fire
re-test's calls tight together and giving the test bullet a long
`lifeTime` override, consistent with every prior session's handling of
this same quirk.

### Still open

- Attacker AI positioning, bullet-dodging, teammate separation, manual
  teammate-ability triggering — unchanged, still not built.
- The Attacker survivability/DPS balance question flagged during design
  (highest damage *and* second-best survivability) wasn't revisited after
  the user confirmed the given numbers — worth another look once real
  playtesting happens.
- Every role's shield value is now a deliberately-chosen fixed number
  (no more "undecided 1.0x placeholder" framing), but all values across
  the board remain placeholder/tunable pending real playtesting, same as
  every prior balance pass in this project.

## Session 17 — Attacker AI Positioning (hybrid patrol + boss-tracking)

The roadmap's explicitly recommended next item: finish AI teammate
positioning by giving Attacker its own behavior — Tank, Medic, and Support
all already had it (Sessions 12/13/15); Attacker was still on the original
prototype-era placeholder, a pure X-only sine weave with zero boss/ally
awareness.

### Design revision, mid-conversation

`docs/systems/boss.md` already had a "decided design" for this dated
2026-08-20 (the previous session): patrol to cover the available screen
width for spread/DPS coverage, staying clear of the boss and the top edge.
Discussing the actual implementation surfaced a mechanical problem with
that plan before any code was written: ships never rotate and bullets only
ever fire straight up (`Vector2.up`, no homing, see `Bullet.cs`) — an
Attacker patrolling a fixed, boss-independent center would frequently drift
out of the boss's current lane as it sine-drifts, and just miss regardless
of how good its coverage looked. The user proposed tracking the boss's X
directly instead, holding a balanced mid-distance (not Tank-close, not
Medic-far). Resolved as a **hybrid**, the user's choice among three
options offered: keep the independent side-to-side patrol motion for
spread/coverage/visual variety, but anchor its *center* to the boss's live
X instead of a fixed point. This supersedes the prior session's decided
design outright — `boss.md` was updated to match, not left describing the
old plan alongside the new code.

The other half of the original ask — "fire the ability the instant it's
ready" — turned out to already be exactly how Attacker's
`TryUseAbility()` heuristic worked (`AIController`'s ability-triggering
switch already retries every frame for Attacker, relying on
`PlayerAbility`'s own cooldown gate). No code change was needed there —
confirmed by reading the existing switch before writing anything new,
avoiding a redundant "fix" for something that wasn't broken.

### New code: `AIController.AttackerPositionDirection()`

Same "compute a target point, seek it, zero inside a deadzone" shape
already used by `BiasedPositionDirection()`/`ApproachDirection()`, so it
reads as one more case in the same family rather than a bespoke one-off:

- `targetY`: `Mathf.LerpUnclamped(GetAllyCenter().y, boss.transform.position.y, attackerBias)`
  — the same ally-center/boss blend Tank and Medic use, applied to Y only.
  New field `attackerBias` (0.45) sits between Medic's `-0.3` and Tank's
  `0.65`. Since the boss sits near the top of the screen (world Y fixed at
  `4.2`) and ally center is naturally lower/mid-screen, this blend
  incidentally keeps Attacker clear of the top edge too — satisfying that
  part of the original design intent without a dedicated check.
- `targetX`: `boss.transform.position.x + Mathf.Sin(Time.time * weaveFrequency) * attackerPatrolAmplitude`
  — patrols around the boss's *current* X rather than an independent
  center, reusing the existing `weaveFrequency` field instead of adding a
  second oscillation-speed constant. New field `attackerPatrolAmplitude`
  (1.5) controls the swing width.
- Returns the normalized direction to `(targetX, targetY)`, or
  `Vector2.zero` inside new field `attackerDeadzone` (0.2, matching
  `guardDeadzone`'s default).

`Update()`'s movement switch gained an explicit `case PlayerRole.Attacker:`
(previously Attacker fell through to `default`); `default` now stays only
as a dead safety fallback for any future unhandled role, with the original
weave code left there unused.

**Small refactor alongside**: the ally-center averaging loop, previously
inlined only inside `BiasedPositionDirection()`, was extracted into a
shared private `GetAllyCenter()` so `AttackerPositionDirection()` doesn't
duplicate the same liveness-filtered average a second time —
`BiasedPositionDirection()` now calls it too, no behavior change. Same
"extract instead of duplicate" precedent as Session 13 generalizing
`GuardPointDirection()` into `BiasedPositionDirection()` itself.

### Verification

Reimported/compiled via the Unity MCP bridge — no console errors. New
fields, being brand-new rather than edits to already-serialized ones,
picked up their script defaults automatically on all three `Teammate_*`
instances (including the `Teammate_Tank` prefab instance) with no
prefab-instance-override gotcha, confirmed by reading them back live.

Since the default scene has the human `Player` on Attacker (per
`current-state.md`'s testing instructions, no AI teammate normally plays
it), temporarily reassigned `Player` → Support and `Teammate_Support` →
Attacker in Edit mode so an AI teammate actually exercised the new code
path, entered Play mode, and sampled `Teammate_Support`'s position against
`Boss.transform.position.x` over several pumped frames (same
screenshot-forces-a-frame-step technique as every prior session). X stayed
within `attackerPatrolAmplitude` of the boss's live X throughout rather
than drifting to an independent center; Y climbed from near the back of
the party toward the mid-distance blend as expected. The boss was actually
defeated mid-test (~18s of continuous 4-ship fire, Attacker contributing
real DPS the whole time), with zero console errors/warnings across the
whole fight. Reverted the temporary role reassignment afterward and
confirmed via a full scene reload from disk (the established habit for
this class of change) that `Player` = Attacker was restored correctly.

**Degenerate case observed, not a new bug**: once Tank and Medic had both
died mid-test, `GetAllyCenter()`'s existing "fall back to the caller's own
position when no allies are alive" behavior (shared by Tank/Medic already)
meant Attacker's Y target kept re-lerping from its own just-updated
position toward the boss's Y each frame, asymptotically converging onto
the boss's height rather than holding a mid-distance stand-off. Only
matters in the "down to one or two teammates" endgame, not normal play;
documented in `boss.md` rather than treated as something to fix this
session, since it's inherited from a pattern already accepted for Tank and
Medic.

### Docs updated

`boss.md` (new "Attacker patrol + boss-tracking positioning" subsection,
replacing the superseded "patrol screen width" design note; "Future work"
trimmed since Attacker positioning is no longer open), `roadmap.md`
(Attacker item moved from "Planned" to "Implemented"; "Boss combat
dynamism" now explicitly the recommended-next item), `current-state.md`
(boss-encounter bullet and "How to test it" step 5 updated to describe the
new behavior), `player-roles.md` (Attacker positioning removed from the
"not yet implemented" list).

### Still open

- Bullet-dodging, teammate separation, manual teammate-ability triggering
  — unchanged, still not built (see `boss.md`'s "Future work" and "Manual
  teammate ability triggering").
- Boss combat dynamism (static movement, flat-timer attacks) — now the
  explicitly recommended next item, see `roadmap.md`.
- The Y-convergence-onto-boss degenerate case (above) when few AI allies
  remain alive — not fixed, just documented.

## Session 18 — Role Select Scene + Victory Screen

Testing the 4-role AI behavior (Sessions 12-17) required hand-editing
`PlayerRoleComponent.role` in the Inspector on `Player`, plus swapping
whichever `Teammate_*` currently held that role — slow and error-prone, and
a blocker on iterating the boss-dynamism work recommended next. Requested
out of band from the roadmap's stated build order (which had "Scene
scaffolding" deferred until right before Nakama networking), because fast
role-switching was needed now for testing, not because the full scaffolding
timeline moved up.

### Design: a real second scene, not a same-scene overlay

Considered both a same-scene overlay panel (matching the existing
`GameOverPanel` pattern, avoiding a second scene per Session 5's precedent,
see progress-log-archive.md)
and a real separate `RoleSelect.unity` scene. **User explicitly chose the
real scene** — Role Select was always going to become a real scene per the
roadmap's deferred "Scene scaffolding" item; this just builds it earlier
than that item's original timeline, for testing purposes specifically (Main
Menu and Lobby remain unbuilt).

### The core technical problem: role has to be set before Awake()

`PlayerRoleComponent.Awake()` (tints the sprite), `PlayerHealth.Awake()`
(sets `maxHealth`/`maxShield`), and `PlayerAbility.Awake()` (builds Medic's
aura ring / Tank's shield arc — **structural**, only happens once) each
read `role`/`Stats` exactly once, at their own startup, and never re-react
to a later change. Unity doesn't guarantee `Awake()` order between
different GameObjects' default-order scripts. Fixed with
`[DefaultExecutionOrder(-1000)]` on a new bootstrap script — a first for
this project — guaranteed to run before every default-order script's
`Awake()`.

### New scripts

- `PartyRoleAssignment.cs` — static class, `PlayerRole? HumanRole`, carries
  the human's pick from `RoleSelect` into `Gameplay` across
  `SceneManager.LoadScene` (survives within a Play session, resets to
  `null` on domain reload). Same "static table over extra infra" precedent
  as `PlayerRoleStats` (Session 4, see progress-log-archive.md).
- `RoleSelectUI.cs` — 4 role buttons, non-interactable Start button until a
  role's picked, `StartGame()` sets the static and loads `Gameplay`.
- `PartySetupBootstrap.cs` — the `DefaultExecutionOrder(-1000)` script, on a
  new `PartySetup` GameObject in `Gameplay`. Assigns the human's role to
  `Player`, then the 3 remaining `PlayerRole` values (enum declaration
  order, skipping the human's pick) to `Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support` — covers all 4 roles exactly once by construction. If
  `PartyRoleAssignment.HumanRole` is null (scene opened directly), no-ops,
  preserving the original Inspector-testing workflow.
- `VictoryUI.cs` — mirrors `GameOverUI.cs` exactly. `Show()` wired as a
  **second** listener on `Boss.OnDefeated` (`OnDefeated` already supported
  multiple listeners, same as `OnDamaged` — no `Boss.cs` change needed).
  `PlayAgain()` reloads `Gameplay` (roles preserved); `ChangeRoles()`
  loads `RoleSelect`.
- `GameOverUI.cs` — added `ChangeRoles()` + a new button; existing
  `Restart()` needed no change, since it now doubles as "play again, same
  party" for free (it just reloads the scene, and `PartyRoleAssignment` is
  never cleared by that path).

### Bug caught during verification: prefab-instance listener didn't persist

Wired `VictoryUI.Show` onto `Boss.OnDefeated` via `execute_code` +
`EditorUtility.SetDirty()`, same technique as every prior UnityEvent wiring
in this project. `GetPersistentEventCount()` read back `2` immediately, and
the scene saved successfully — but in Play mode, only `BossPanelUI.ShowDefeated()`
fired; `VictoryPanel` never appeared. Root cause: `Boss` is a `Boss.prefab`
instance (confirmed via `PrefabUtility.GetPrefabInstanceStatus`) — the
now-familiar prefab-instance-override gotcha (see
`docs/unity-notes.md#prefab-instance-overrides-need-recordprefabinstancepropertymodifications-not-just-setdirty`),
just hit against a UnityEvent listener list instead of a plain field this
time. Caught only because verification followed this project's established habit
of forcing a full scene reload from disk rather than trusting the
in-memory `GetPersistentEventCount()` read — which had reported the
correct count the whole time, since the in-memory mutation was real, just
not persisted. Fixed by calling `RecordPrefabInstancePropertyModifications(boss)`
after re-adding the listener; re-verified via a full disk reload before
moving on. `GameOverPanel`'s own new "Change Roles" button needed no such
fix, since `GameOverPanel` is a plain scene object, not a prefab instance.

### Editor-idle quirk reconfirmed, this time on Play-mode transition itself

Hit the Editor-idle gotcha again (see
`docs/unity-notes.md#editor-doesnt-tick-play-mode-updatecoroutines-while-unfocused-and-the-inverse`),
this time on the Play-mode transition itself rather than in-Play
`Update()`/coroutines: immediately after entering Play mode (Editor
unfocused), one specific teammate's `PlayerRoleComponent.Awake()` sprite
tint read as white (default) instead of its correct role color, even
though the *same* GameObject's `PlayerHealth.Awake()` had already read the
correct post-bootstrap stats. `editor/state` showed `play_mode.is_changing:
true` and a stalled `playmode_transition` phase (not advancing across
repeated reads while unfocused). Not a bug in the new code — a
`manage_camera` screenshot (forces one manual frame step, the project's
established technique since Session 6, see progress-log-archive.md) let
the transition complete, after
which all 4 ships' tints read correctly and reproducibly.

### Verification

All via the Unity MCP bridge, mirroring this project's established
technique (forced `Boss.TakeDamage(9999f, null)`/`PlayerHealth.TakeDamage(999)`
instead of waiting out real combat, screenshot-forced frame steps,
full-disk-reload checks for anything prefab/serialization-adjacent): role
assignment verified correct for 3 different human picks (Medic, Tank,
Support) across separate Play sessions, including the structural checks
(aura ring / shield arc present on whichever ship actually landed that
role); Victory panel appears on a forced boss kill with both buttons
working; Game Over's Change Roles button works; "Play Again" preserves the
exact prior role assignment across a reload via both the Victory path and
the Game Over Restart path; opening `Gameplay` directly with no prior
`RoleSelect` visit correctly falls back to the scene's hand-authored
Inspector defaults, confirming `PartyRoleAssignment.HumanRole` resets to
null on domain reload as designed. Zero console errors/warnings across
every phase.

### Docs updated

`roadmap.md` (new "Role Select scene + Victory screen" Implemented item;
"Scene scaffolding" note updated — Role Select shipped early, Main
Menu/Lobby still deferred), `current-state.md` ("What's playable" bullet,
"What's NOT there yet" scene count, "How to test it" steps 1-2 and 6-7
rewritten for the new boot flow), `player-roles.md` (new "Role Select
scene" section, `PlayerRoleComponent`'s scene-wiring table note updated).

### Addendum: gameplay scene renamed `SampleScene` → `Gameplay`

Immediate same-session follow-up: `SampleScene` was a leftover Unity
template name from Session 1 (see progress-log-archive.md), and now that a
second scene (`RoleSelect`)
exists alongside it, the generic name read as unfinished rather than
intentional. Renamed via `manage_asset(action:"rename"/"move")` (preserves
the `.meta`/GUID, so Build Settings and all GUID-based references updated
automatically with no broken links) to `Assets/Scenes/Gameplay.unity`. Two
string-literal `SceneManager.LoadScene("SampleScene")` call sites
(`VictoryUI.PlayAgain()`, `RoleSelectUI.StartGame()`) needed a matching
code fix, plus a full docs sweep. **Historical session entries above
(1-17) keep saying `SampleScene`, deliberately** — they're an accurate
record of what the scene was actually called at the time; only this
session's own references and the forward-looking docs (`roadmap.md`,
`current-state.md`, `systems/*.md`) were updated to `Gameplay`. Re-verified
the full Role Select → Gameplay → Victory → Play Again loop end-to-end
post-rename via the Unity MCP bridge; zero console errors.

### Addendum: `RoleSelect` was missing a Camera

Playtesting surfaced Unity's "Display 1 No cameras rendering" diagnostic
text over the role-picker screen. Root cause: `RoleSelect` was built as a
UI-only scene (Canvas + EventSystem only) on the reasoning that a Screen
Space - Overlay canvas doesn't need a camera reference to render its UI —
true, but Unity's Game view still shows that warning whenever a scene has
**zero** `Camera` components at all, independent of whether any UI actually
needs one. Fixed by adding a plain `Main Camera` (tagged `MainCamera`,
matching this project's stated convention that `Camera.main` requires that
tag — see `progress-log-archive.md` Session 1's troubleshooting notes),
background
color set to match the dark HUD panel tone (`RGBA(0.05, 0.05, 0.08, 1)`) so
it's consistent even where the UI doesn't fully cover the screen. Verified
via screenshot in Play mode: warning gone, no console errors.

### Still open

- Boss combat dynamism — still the recommended next item, unchanged by this
  session.
- Main Menu / Lobby scenes — still not built; `RoleSelect` is a standalone
  picker, not yet part of a Main Menu flow.
- Bullet-dodging, teammate separation, manual teammate-ability triggering —
  unchanged, still not built.

## Session 19 — Boss Combat Dynamism (Erratic Movement, Body Hazard, Guided Missile, Shockwave)

The roadmap's long-recommended next item, requested directly this session
with four concrete mechanics: erratic movement bounded to a limited advance
toward the ships, a damaging body hitbox, homing bullets that call out a
specific role (with a HUD warning), and a close-range shockwave. Full
technical write-up: `systems/boss.md`.

### Clarifying two genuine forks before writing any code

Two requests were ambiguous in a way that would have meant rewriting core
systems if guessed wrong, so both were confirmed with the user before
implementation:

- **"2/5 of the screen" for boss movement** — could have meant a horizontal
  roam cap or a vertical advance-toward-the-ships limit. Confirmed:
  vertical. The boss's erratic left/right dashing is a separate, roughly
  full-width behavior; the 2/5 fraction only bounds how far down (toward the
  ships) it can push from its home row.
- **"Guided bullets aiming the medic or attacker"** — could have meant a
  bullet aimed at the target's position at fire time (straight line
  afterward, fully Tank-blockable, no `Bullet.cs` changes) or true homing
  (continuously re-aims in flight). Confirmed: true homing, at a capped turn
  rate so it stays dodgeable. This knowingly loosens (not breaks) Tank's
  straight-line-blocking guarantee — already flagged as an open question in
  `systems/boss.md`'s "Future work" from an earlier session, now a real,
  confirmed trade-off rather than a hypothetical one.

Also confirmed: geometric bullet-pattern variety is explicitly deferred to
a later pass, not bundled into this one — added as its own item under
`roadmap.md`'s "Player-vs-boss dynamics" rather than silently dropped.

### World-unit research before committing to numbers

Since the user was explicit that they didn't know what unit scale the
project uses, three parallel research passes (boss code + camera/viewport
math, AI positioning + player health/collision, bullet system + HUD panel)
established concrete numbers before any code was written: the playable
viewport is **5.625 × 10 units** (orthographic size 5, forced 9:16 via
`AspectRatioFitter`), ship collider footprint is **0.6 × 0.6**, the boss is
**1.6 × 1.6** with a non-trigger `BoxCollider2D` (ship colliders are
triggers, so Unity fires `OnTriggerEnter2D`/`OnTriggerStay2D` on **both**
sides on overlap — confirmed this meant body-contact damage needed no new
collider). This turned "1.5 ships around the boss" into a concrete
`shockwaveRadius` of 1.7 (boss half-extent 0.8 + 1.5 ship-widths 0.9) and
"2/5 of the screen" into a concrete vertical clamp, both stated as reasoning
in the plan rather than picked blind.

### Implementation

- **`Boss.cs`** — replaced the `Update()` sine-drift block with a
  dash-or-hold decision every `dashDecisionInterval` (1.5s,
  `dashProbability` 0.35), clamped by a new `ClampToBounds()` (X via the
  same `ViewportToWorldPoint`/`screenPadding` idiom
  `PlayerController.HandleMovement()` already uses; Y clamped to
  `[homeY - maxAdvanceFraction * viewportHeight, homeY]`). Added a new
  `bulletDamage` field (1f) making the boss's own bullet damage explicit —
  previously an implicit default from `Bullet.damage`, since `SpawnBullet()`
  never set it — as the single source of truth the two new damage
  mechanics multiply against. Body contact: `OnTriggerStay2D` on `Boss.cs`
  itself (reusing its existing solid collider), per-target cooldown-gated,
  `2x bulletDamage`. Shockwave: `CheckShockwave()`/`ShockwaveRoutine()`,
  telegraphed, `3x bulletDamage` plus knockback via the *existing*
  `PlayerController.AddRecoil()` (built for Attacker's Big Shot) rather than
  a new knockback mechanism. Guided missile:
  `CheckGuidedMissile()`/`GuidedMissileRoutine()` picks a random active
  Medic/Attacker, sets a new public `GuidedMissileTargetRole` property
  immediately (during the telegraph, not just during flight, so Tank gets
  real reaction time), fires via `Bullet.InitHoming()`, holds the property a
  couple seconds into flight before clearing.
- **`Bullet.cs`** — added `InitHoming(...)` as an alternate init path
  alongside the untouched existing `Init(...)`, so every straight-line
  bullet (player and enemy) is unaffected. Re-aims `direction` each frame
  toward the target's live position via `Vector3.RotateTowards` (**hit a
  real compile error here** — `Vector2.RotateTowards` doesn't exist, only
  `Vector3`'s overload does; Unity implicitly converts between the two, so
  the fix was a one-line type change, caught immediately by
  `refresh_unity`'s compile step), capped by a turn rate so it's dodgeable.
- **`AIController.cs`** — new `minDistanceFromBoss` (1.9, just outside the
  shockwave radius) and a new `EnforceBossDistance()` helper, applied to
  `BiasedPositionDirection()` (Tank/Medic), `AttackerPositionDirection()`,
  and `RandomRoamPoint()` (Support) — all four roles' default positioning
  now has a floor distance from the boss. This incidentally fixed the
  already-documented `GetAllyCenter()` collapse-toward-boss degenerate case
  from an earlier session, for free.
- **`BossPanelUI.cs`** — new `warningText` field, polled the same "HUD
  reads, never owns state" way as the existing health/phase/target text.

### Scene wiring

New `BossWarningText` child added under `BossPanel` via the Unity MCP
bridge, duplicated from `BossTargetText` as a styling template (same
approach `AbilityText` used on `PartyFrame.prefab` in Session 8, see
progress-log-archive.md), wired to
`BossPanelUI.warningText`, verified via a full scene reload from disk. Every
other new field is either a fresh script default (no prefab-instance gotcha
— confirmed on all 4 ships/`Teammate_Tank`'s prefab instance) or computed at
runtime, so none of the prior sessions' `RecordPrefabInstancePropertyModifications()`
gotcha applied this time.

### Testing notes

See
`docs/unity-notes.md#editor-doesnt-tick-play-mode-updatecoroutines-while-unfocused-and-the-inverse`
for the general quirk — **this session's Editor instance ticked Play mode
in real time on its own** between tool calls — `Time.time` advanced
freely without needing screenshot-forced frame steps. This cut both ways:
made end-to-end coroutine testing (shockwave, guided missile) much easier
once discovered, but also meant an unattended party (no human input driving
`Player`) died for real to the now-harder boss partway through testing —
not a bug, just the fight actually being harder now, confirmed via a clean
Play-mode restart. Where a coroutine still needed a real-time wait to
resume (`WaitForSeconds`), telegraph/linger fields were temporarily lowered
via direct field writes for the test only (same technique as Session 7's
temporary `buffDuration` shortening, see progress-log-archive.md) —
confirmed discarded automatically on
Stop, restoring the real serialized defaults.

Verified via the Unity MCP bridge: reflection-driven stress test of
`HandleMovement()` (300 forced decisions, bypassing the `Time.time` gate)
matched the configured dash probability and stayed within clamp bounds
every time; body contact damage confirmed exact math and cooldown gating
via both a direct `OnTriggerStay2D` call and real physics-driven overlap
(the latter correctly killed an overexposed test ship over repeated ticks);
shockwave confirmed exact math, telegraph, and knockback, and was observed
combining correctly with a simultaneous body-contact hit through the shared
shield-first `PlayerHealth.TakeDamage` path; guided missile confirmed
correct target-role restriction, HUD warning timing, and ran to completion
multiple times with zero console errors/warnings across the whole session.

### Follow-up: shockwave had no visible danger zone

Immediate playtest feedback after the above: the shockwave was a complete
surprise — nothing on screen indicated its radius before it hit. Added a
world-space ring at `shockwaveRadius`, built the same procedural
`LineRenderer` way `PlayerAbility.cs`'s Medic aura ring already is (dim and
always visible, brightens/pulses during the telegraph, flashes on impact) —
`CreateShockwaveRing()`/`UpdateShockwaveRing()` on `Boss.cs`, re-centered on
the boss's live position every frame since it now moves erratically.
Confirmed visually via a Play-mode screenshot (also incidentally caught the
guided-missile HUD warning firing live in the same shot). No new gotchas —
straightforward reuse of an already-proven visual pattern.

### Follow-up: shockwave knockback too weak, no cooldown visibility

Immediate playtest feedback after the above two follow-ups: the shockwave's
knockback (`shockwaveKnockback = 6`) was barely noticeable, and `BossPanel`
had no way to see whether the shockwave or guided missile were about to be
available again.

**Knockback math, derived from an existing precedent, not guessed**:
`shockwaveKnockback` is an impulse fed into `PlayerController.AddRecoil()`,
which decays exponentially every `FixedUpdate`
(`recoilDamping` 8, `Fixed Timestep` 0.02, confirmed by reading
`ProjectSettings/TimeManager.asset`). Session 8 (see
progress-log-archive.md) already derived and verified the closed-form
total displacement for this exact system (Attacker's Big Shot recoil:
impulse 6 → measured 0.63 units). Re-deriving the same formula here gave
`displacement ≈ impulse × 0.105`, which exactly reproduces Session 8's
number — confirming the formula still holds rather than
assuming it does. This turned "how far should the wave push ships" into a
concrete question answerable in world units/ship-widths: the user was asked
to pick a target displacement with the actual math shown (playable area is
5.625 × 10 units, a ship is 0.6 × 0.6), and chose "very strong" (~3.5 units,
~5.8 ship-widths) — `shockwaveKnockback` raised `6 → 33`.

`Boss.cs` gained two pure derived-getter properties
(`ShockwaveCooldownRemaining`, `GuidedMissileCooldownRemaining`) off
already-existing private timer fields, no new state; `BossPanelUI.cs`
polls them the same way as every other boss stat. Body contact damage's
cooldown was deliberately left off `BossPanel` — it's per-target/reactive,
not a single global cooldown like the other two, so it doesn't fit one HUD
line the same way.

**Testing hit the now-familiar script-default gotcha again** (see
`docs/unity-notes.md#changing-a-scripts-default-value-doesnt-retroactively-update-an-already-serialized-field`):
after compiling, the *live scene instance's* `shockwaveKnockback` still
read `6` even though the script default was now `33`, since it had been
explicitly serialized at `6` in an earlier session. Fixed by setting it
explicitly on
both the scene instance and `Boss.prefab`'s default, verified via a full
scene reload from disk.

**Testing hit real noise from this session's free-running Play mode**: this
Editor instance ticked Play mode continuously in the background between
tool calls (same as observed at the end of the original Session 19 pass),
which repeatedly wiped the unattended AI-only party mid-test and made a
naive before/after position comparison too noisy to trust (AI healing,
wandering, and the boss's own erratic movement all overwrote the signal
within a few real seconds). Switched to a deterministic, instantaneous
check instead: read `PlayerController`'s private `recoilVelocity` field via
reflection immediately after manually calling `AddRecoil(pushDir * 33)` —
confirmed it lands at exactly magnitude 33, which combined with the
already-reproduced Session 8 decay formula (see progress-log-archive.md)
is sufficient confirmation
without racing the simulation. Cooldown text was confirmed the reliable
way instead: a live screenshot showing `BossPanel` correctly reading
`"Shockwave: Ready"` / `"Guided Missile: 0.7s"` after real combat had
already exercised both.

### Still open

- Rapid-fire burst attack and geometric bullet spread patterns — deferred,
  see `roadmap.md`'s "Player-vs-boss dynamics" (new "Geometric bullet spread
  patterns" item).
- Bullet-dodging, teammate separation, manual teammate-ability triggering —
  unchanged, still not built.
- Main Menu / Lobby scenes — still not built.

## Session 20 — Solid-Body Ship/Boss Collision

Requested directly: ships (human and AI) should have a solid shape that no
other ship can overlap, and the boss's body should be equally solid against
every ship — `AIController.minDistanceFromBoss` only biases AI teammates'
*chosen target point* away from the boss, and nothing at all prevented
ship-vs-ship stacking or a ship physically passing through the boss. Full
technical write-up: `systems/boss.md`'s "Solid-body collision (ships +
boss)".

### Design conversation before any code

Two genuine forks were talked through with the user before writing
anything, since guessing wrong on either would have meant a rewrite:

- **How to reconcile "prevent overlap" with the boss's existing "touching
  its body deals contact damage" hazard** — hard-preventing overlap means
  Unity's physics engine never actually sees two colliders intersect, so
  the existing `Boss.OnTriggerStay2D` (which relies on genuine overlap)
  would stop firing. The user's own proposed resolution, confirmed as the
  design: since ships/the boss move in small discrete steps every frame
  rather than teleporting, a momentary overlap is unavoidable for a step
  before it's corrected — so the same box-overlap math that computes the
  push-back doubles as the contact-damage detector, replacing reliance on
  Unity's trigger callback with one unified per-ship step.
- **Who gives way when the boss's erratic dash would move it into a ship's
  spot** — considered "boss gets blocked like a wall" (symmetric, but adds
  resolution code to `Boss.cs`'s movement and risks it stalling against a
  parked ship) versus "boss shoves the ship aside" (asymmetric, but needs
  zero changes to `Boss.cs`). The same discrete-step reasoning above
  resolved this for free: since the boss's dash is already incremental
  (`Vector3.MoveTowards` each `Update()`, not a teleport), each ship's own
  next `FixedUpdate` naturally catches and corrects "the boss moved into
  me" the same way it catches any other ship — no boss-side code needed at
  all. Confirmed via the actual `TimeManager.asset` Fixed Timestep (0.02s)
  and the boss's `dashSpeed`/collider sizes that this means at most one
  rendered frame of transient overlap, never a persistent one.

CPU cost of the added per-frame box checks was also raised directly — confirmed negligible (at most ~20 simple AABB comparisons across 4 ships +
the boss per physics tick, the same order of magnitude as the per-frame
distance loops `Boss.cs` already runs for aggro/shockwave/guided-missile
targeting).

### Research and validation before implementation

Two parallel research passes (current physics/collider setup for
ships/boss; docs + progress-log history on AI positioning and boss
hazards) established that **no physics-engine collision response exists
today at all** — every ship moves via `Rigidbody2D.MovePosition()` and the
boss via raw `transform.position` writes, both fully imperative, so
Unity's solver never resolves overlap between any of them regardless of
trigger flags. A Plan agent then validated the concrete implementation
live against the actual scene (not just static file reads): confirmed
exact collider `bounds`/`isTrigger` values, confirmed `PlayerAbility.allies`
(already wired on all 4 ships) and `Boss.targets` (already wired) were
reusable with zero new arrays needed, corrected a stale claim in
`systems/movement.md` (`Player`'s `Collider2D` was documented as `Is
Trigger: OFF`, live value is `ON`), and flagged a rare accepted edge case:
the boss's `screenPadding.x` (0.8) equals its own half-extent, so a ship
pinned in the same corner the boss dashes into could see the viewport
clamp momentarily fight the collision resolution — self-corrects the
instant either body moves, not worth solving given nothing in the game
deliberately drives a ship into that corner (`minDistanceFromBoss` already
keeps default AI positioning well clear of the boss).

### Implementation

- **New `Assets/Scripts/ShipCollisionUtil.cs`** — a plain static class (no
  `MonoBehaviour`, no Inspector wiring), one function:
  `ResolveBoxOverlap(candidateSelfPos, selfHalfExtents, otherPos,
  otherHalfExtents, out wasOverlapping)`. Exact axis-aligned box-vs-box
  minimum-translation-vector push-out along whichever axis has the
  shallower penetration — ships and the boss never rotate (the project's
  established fixed-orientation design), so this is exact, not a circle
  approximation. The `out bool` lets the one ship-vs-boss call site reuse
  the same math as the contact-damage trigger, while ship-vs-ship call
  sites just discard it (`out _`).
- **`PlayerController.cs`** — new `public Boss boss` field (mirrors
  `AIController.boss`, but needed here directly since this script also
  drives the human `Player`, which has no `AIController`). Caches its own
  `BoxCollider2D`/half-extents, `PlayerAbility` (for `.allies`), and the
  boss's collider/half-extents once in `Start()`. New
  `ResolveShipCollisions(Vector2)`, called from `HandleMovement()` between
  computing the raw candidate position and the existing viewport clamp
  (resolve-then-clamp, so a corrected position can never end up pushed
  outside the play area by the correction itself) — resolves against every
  other ship in `ability.allies` (push-apart only), then against `boss` if
  present (push-apart **and**, on overlap, calls the new
  `Boss.ApplyContactDamage(gameObject)`).
- **`Boss.cs`** — removed `OnTriggerStay2D` (no longer reachable once
  overlap is actively prevented) and replaced it with `public void
  ApplyContactDamage(GameObject ship)`, the exact same cooldown-gated math
  (`lastContactDamageTime`/`contactDamageCooldown`/`bulletDamage`/
  `bodyContactDamageMultiplier`) just invoked from `PlayerController`'s
  resolution step instead of a trigger callback. Uses `GetComponent`
  instead of the old `GetComponentInParent`, since the caller always passes
  a ship's own root GameObject now, never a child collider — this is a
  narrow, accepted behavior change: Tank's Shield Arc (a separate child
  trigger collider) could previously trigger contact damage on its own,
  independent of Tank's body box; now only the body box is checked. In
  practice both paths always led to the same cooldown-gated hit on the same
  ship, so this wasn't treated as a balance change worth re-litigating.
  `HandleMovement()`/dash logic needed no changes at all, per the design
  conversation above.

### Scene wiring

Set the new `boss` field on all 4 ships' `PlayerController` in
`Gameplay.unity` via the Unity MCP bridge. Hit the now-familiar
prefab-instance gotcha once more on `Teammate_Tank` (the only one of the 4
ships that's an actual `Teammate.prefab` instance — see
`docs/unity-notes.md#prefab-instance-overrides-need-recordprefabinstancepropertymodifications-not-just-setdirty`),
same as every prior session that touched a field on this GameObject.
Verified by reloading the scene fresh from disk afterward and
re-reading the field on all 4 ships rather than trusting the in-memory
value.

### Verification

All done live via the Unity MCP bridge in Play mode. Hit real friction from
this session's free-running Play mode (same class of issue as Session 19's
tail): an unattended party with ambient boss fire kept killing ships
mid-test between tool-call round trips, including the human `Player`
itself twice — recovered each time by reactivating the GameObject
(`SetActive(true)`, inactive objects aren't found by `GameObject.Find`, so
this needed `Resources.FindObjectsOfTypeAll<Transform>()` instead) and
topping health/shield back up via the existing `Heal`/`RestoreShield`
methods, rather than restarting the whole session and losing test state.
Isolated the push-back-distance test cleanly by temporarily setting
`boss.enabled = false` (stops the boss's `Update()` — movement, firing,
shockwave — without affecting `ApplyContactDamage`, which is a directly
callable public method unrelated to the component's enabled state) so
ambient combat noise didn't contaminate the measurement.

Confirmed: two overlapping ships separate correctly; a ship forced onto the
boss (boss paused, health topped up first) gets pushed back to *exactly*
the combined half-extent distance (measured `1.1`, matching
`playerHalfExtent + bossHalfExtent` to the decimal) and takes contact
damage exactly once; calling `ApplyContactDamage` twice back-to-back in one
`execute_code` call (bypassing physics/frame-pump timing entirely) confirms
the cooldown gate blocks the second hit deterministically; forcing the
boss's position onto a stationary teammate (simulating a dash-into-ship)
resolves with the ship pushed well clear, no persistent overlap. Tank's
Shield Arc (`EdgeCollider2D` child) confirmed still intact and unaffected.
Zero console errors/warnings across the entire session, including an
organic boss defeat from sustained ambient fire mid-test — confirming
`Bullet.cs`'s damage path is completely unaffected by this change.

### Still open

- Bullet-dodging, manual teammate-ability triggering — unchanged, still not
  built (see `roadmap.md`).
- Main Menu / Lobby scenes — still not built.

## Session 21 — Game Over/Victory Race Fix + CPU Party Frame Names

Two independent playtest-reported bugs, fixed in one session since both
were small and unrelated: the Victory panel could pop on top of an
already-showing Game Over panel, and every party frame displayed the
identical hardcoded name `"Player 1"` regardless of which ship it
represented.

### Game Over vs. Victory: mutual exclusion, not boss immortality

The 3 CPU teammates keep fighting after the human `Player` dies (only the
human's own death shows `GameOverPanel`, by existing design — see
`systems/boss.md`'s "Death handling"), so if they go on to defeat the boss,
`Boss.Die()`'s unconditional `OnDefeated` pops `VictoryPanel` on top of the
already-showing `GameOverPanel`.

**Design conversation before writing anything**: the user's first instinct
was to make the boss invulnerable once Game Over fires, so it could never
reach 0 HP afterward. Talked through and rejected in favor of a
mutual-exclusion guard between the two panels instead — an invulnerable
boss would never die or get cleaned up (nothing else destroys it), so the
fight would run forever in the background for zero visible benefit, since
`GameOverPanel` already covers the full screen either way. The user then
added one more requirement while confirming this direction: the guard must
be a genuine no-op, not a "show then immediately hide" — a boss defeat that
happens while Game Over is already up must never register as a victory at
all, even momentarily. Both `GameOverUI.Show()` and `VictoryUI.Show()` were
already bare `panelRoot.SetActive(true)` calls with no existing check of
any kind (confirmed by reading both scripts in full), so the guard is a
plain early-return *before* that line, not a state that gets set and later
unset.

Added `GameOverUI.victoryPanelRoot`/`VictoryUI.gameOverPanelRoot` (each
pointing at the other's panel), and each `Show()` now returns immediately
if the other's panel `activeSelf` — implemented symmetrically (not just
the reported Game-Over-then-Victory direction) to also cover the mirror
race, where an enemy bullet already in flight when the boss dies could
still land on the Player a moment after Victory has already shown. No
changes to `Boss.cs`/`PlayerHealth.cs`/`BossPanelUI.cs` at all — the boss
still dies and gets destroyed normally regardless of which panel already
won; `BossPanelUI.ShowDefeated()` (the other `OnDefeated` listener) is left
unguarded since it's just HUD text sitting behind whichever full-screen
panel is up, harmless either order.

### CPU party frame names

`PartyFrameUI.cs` had no name field at all — the identical `"Player 1"`
every frame showed was a static default baked into
`Assets/Prefabs/PartyFrame.prefab`'s `PlayerName` text child, never bound
to any script (already flagged as a known gap in `systems/hud-layout.md`).
Added `PartyFrameUI.nameText`, changed `Initialize(GameObject)` to
`Initialize(GameObject, string displayName)`. `PartyFrameManager.Awake()`
computes the name per slot before calling it: whichever ship has no
`AIController` (attached to all 3 `Teammate_*`, absent from `Player` — the
same signal already used elsewhere to distinguish human from AI, see
`systems/boss.md`) is `"Player 1"`; every other slot is `"CPU " + n`,
numbered in `players[]`'s array order. Checked component presence rather
than a raw index (`i == 0`) so this stays correct even if the array's
wiring order ever changed — matches the codebase's existing convention.

Wired the prefab's pre-existing `PlayerName` `TextMeshProUGUI` child into
the new `nameText` field once, at the prefab level
(`PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`, same technique as
Session 8's party-frame contrast fix, see progress-log-archive.md) —
confirmed all 4 `PartyFrame_1..4`
are genuine prefab instances (unlike the `Teammate_*` split-prefab
situation), so the one edit propagated to all 4 automatically with no
per-instance wiring needed.

### Verification

All via the Unity MCP bridge in Play mode. Confirmed the primary reported
case: forcing the human `Player` to 0 HP shows `GameOverPanel`; forcing the
boss to 0 HP afterward leaves `VictoryPanel.activeSelf == false`. Reset and
confirmed the mirror case: defeating the boss first shows `VictoryPanel`;
forcing the Player to 0 HP afterward leaves `GameOverPanel.activeSelf ==
false`. Read all 4 party frames' live `nameText.text` in Play mode:
`"Player 1"` on the human's frame, `"CPU 1"`/`"CPU 2"`/`"CPU 3"` on the
three teammates', matching `players[]`'s wired order. Zero console
errors/warnings throughout.

### Still open

- Bullet-dodging, manual teammate-ability triggering — unchanged, still not
  built (see `roadmap.md`).
- Main Menu / Lobby scenes — still not built.

## Session 22 — Pattern Barrage (Geometric Bullet Spread Patterns)

The roadmap's next "Player-vs-boss dynamics" item: more varied geometric
bullet-pattern shapes (fan/ring/spiral) beyond the boss's existing single
aimed shot / fixed 3-bullet spread. Planned in a dedicated planning pass
before any code was touched (a Plan agent validated the design against
existing precedent — see below), then implemented and verified live via the
Unity MCP bridge in one session.

### Design decision: one attack, randomized shape, no-immediate-repeat

Explored two alternatives before settling: three fully separate standalone
attacks (one cooldown/telegraph/HUD stack per shape), or a fixed rotation
through shapes. Rejected both. Went with one new standalone attack, **Pattern
Barrage** — its own cooldown (`patternBarrageCooldown`, 7s) and telegraph
(`patternBarrageTelegraphTime`, 0.7s), layered on top of the existing Phase
1/2 fire exactly like Shockwave and Guided Missile already are, not a
replacement of it. On each activation it randomly picks one of `{ Fan, Ring,
Spiral }` to fire — the same "build eligible options, `Random.Range` pick
one" idiom `CheckGuidedMissile()` already uses for target selection, just
applied to shapes instead of targets. Justified against this project's own
established "prototype-simple, prove it's fun before adding infra" principle
(`overview.md`'s Architecture Principle 1, Session 10's explicit scoping) —
three parallel systems is infrastructure the fight hasn't earned yet.

Pure `Random.Range(0,3)` alone risked the same shape firing twice or three
times in a row, which reads as a lack of content rather than surprise — a
worse outcome than either rejected alternative. Fixed with one extra
`private BulletPattern? lastPatternBarragePattern` field: `PickPattern()`
excludes whichever shape fired last time from the pick pool. Cheap (one
field, a few lines), gets both properties (surprise + guaranteed variety)
that the pure-random and fixed-rotation options each only got one of.

### Shape math

All three reuse the existing private `Boss.SpawnBullet(Vector2 dir)` helper
(already used by `Fire()`) — no `Bullet.cs` changes, no object pooling, no
new damage/speed fields.

- **Fan** — generalizes the existing Phase 2 3-bullet spread
  (`Quaternion.Euler(0,0,angle) * dir`) to N bullets: `fanBulletCount` (5)
  evenly spread across `fanSpreadAngle` (50°, so ±25°), centered on the
  direction to `CurrentTarget`. Aim is recomputed *after* the telegraph wait
  completes, not at activation time — same re-check-after-telegraph idiom
  `ShockwaveRoutine()` already uses, since the target may have moved or died
  during the wind-up.
- **Ring** — deliberately not target-relative; the boss never rotates, so
  there's no "facing" to aim relative to, and it's meant to be an
  omnidirectional "screen-full-of-bullets" moment. `ringBulletCount` (12)
  bullets evenly spaced around 360°, with a randomized per-burst start-angle
  offset (a standard bullet-hell technique) so the gaps between bullets
  don't always land in the same screen position — otherwise the same "safe
  lane" would be memorizable every single time.
- **Spiral** — the shape that actually delivers "rapid-fire," since Fan/Ring
  both resolve in a single frame. `FireSpiralRoutine()` is a coroutine:
  starts aimed at `CurrentTarget` like Fan, then fires one bullet every
  `spiralShotInterval` (0.05s) for `spiralBulletCount` (20) shots, sweeping
  `spiralAngleStep` (25°) between each. `PatternBarrageRoutine()` awaits it
  via `yield return StartCoroutine(...)`, so the barrage (and
  `PatternBarrageActivePattern`) doesn't end until the full spiral has
  actually finished firing. 20 × 25° = 500°, intentionally past a full
  revolution so it reads as a genuine spin rather than stopping dead at
  360°.

### HUD wiring

`BossPanelUI` gained `patternBarrageWarningText` (`"Incoming: {Shape}
Barrage"` while `Boss.PatternBarrageActivePattern.HasValue`, else empty) and
`patternBarrageCooldownText` (`"Pattern Barrage: {n}s"` / `"...: Ready"`) —
same exact idiom as the existing warning/cooldown text pairs. Built via the
Unity MCP bridge by duplicating existing template text elements
(`BossWarningText`, `BossGuidedMissileCooldownText`) rather than building
`TextMeshProUGUI` from scratch, same technique as every prior HUD addition
back to Session 8 (see progress-log-archive.md). Both new fields are
brand-new script fields, so — unlike
several past sessions' gotcha with *existing* serialized fields — no
`RecordPrefabInstancePropertyModifications()` step was needed on `Boss`;
they just took their C# defaults.

Forced a full scene reload from disk after wiring (the project's standard
verification habit since Session 12, after multiple past sessions where
in-memory wiring success silently didn't survive a reload) — confirmed both
new `BossPanel` children and their `BossPanelUI` field references persisted
correctly.

### Verification

All live via the Unity MCP bridge, mostly via reflection since
`FireFan`/`FireRing`/`FireSpiralRoutine`/`PickPattern`/`CheckPatternBarrage`
are private:

- **Bullet counts and angle math**: invoked `FireFan`/`FireRing` directly,
  diffed the scene's `Bullet` instances before/after (by reference, not by
  the now-obsolete `GetInstanceID()`) to isolate exactly the newly spawned
  ones from bullets the boss's own concurrent regular fire was also
  producing. Fan produced exactly 5 bullets at angles `-25, -12.5, 0, 12.5,
  25` relative to the aim direction — exact even spacing across the
  configured spread. Ring produced exactly 12 bullets with an exact 30° gap
  between every consecutive pair. For Spiral, rather than relying on
  real-time `WaitForSeconds` frame-pumping (this project's Editor has a
  long-documented history, Sessions 6/8/9/10 (see progress-log-archive.md
for 6/8/9), of not reliably ticking
  `Update()` while unfocused), got the coroutine's `IEnumerator` directly
  from the reflected method call and manually drove `MoveNext()` in a tight
  loop — deterministic and instant, since a manually-driven `WaitForSeconds`
  yield is a no-op rather than a real wait. Produced exactly 20 bullets in
  20 steps.
- **No-immediate-repeat rule**: reset `lastPatternBarragePattern` to `null`,
  called `PickPattern()` 30 times in a row (threading its own output back in
  as `lastPatternBarragePattern` each time, matching what
  `PatternBarrageRoutine()` does for real). All 3 shapes appeared across the
  run; zero consecutive repeats.
- **Cooldown/target gating**: force-set the private `nextPatternBarrageTime`
  into the past and called `CheckPatternBarrage()` — confirmed it started
  the coroutine for real (`PatternBarrageActivePattern` became non-null
  immediately, `PatternBarrageCooldownRemaining` jumped to the full 7s) and
  that `BossPanelUI`'s new warning/cooldown text reflected it live in the
  same Play session. Separately, temporarily nulled the private
  `CurrentTarget` backing field and called `CheckPatternBarrage()` again —
  confirmed a clean no-op (no exception, `PatternBarrageCooldownRemaining`
  stayed at 0, meaning it correctly didn't start a coroutine or advance the
  cooldown) rather than throwing on a null target.
- Also incidentally reconfirmed, unprompted, that the whole system runs
  correctly end-to-end with zero manual intervention: real wall-clock time
  elapsing between two separate tool calls was enough for the 7s cooldown to
  naturally lapse, and `Boss.Update()`'s own automatic `CheckPatternBarrage()`
  call picked a fresh shape on its own.
- Zero console errors/warnings across the entire test pass, including
  through a Play mode stop.

### Still open

- Bullet-dodging, manual teammate-ability triggering — unchanged, still not
  built (see `roadmap.md`).
- Minions around the boss — next in the roadmap's build order, now that both
  bullet-dodging/manual triggering and Pattern Barrage are the only items
  left ahead of it in "Player-vs-boss dynamics."
- Main Menu / Lobby scenes — still not built.

## Session 23 — Bullet-Dodging + Manual Ability Triggering

The roadmap's next "Player-vs-boss dynamics" item, and the last one ahead
of minions (see Session 22's "Still open"). Two independent features
scoped together since the roadmap listed them as one item. Planned in a
dedicated research + design pass (three parallel Explore agents covering
the movement/bullet code, the ability/UI code, and the project's own
existing design notes; one Plan agent to turn that into a concrete
implementation plan) before any code was touched, then implemented and
verified live via the Unity MCP bridge.

### Manual ability triggering

Implemented first (smaller surface, validated MCP prefab-editing early).
The project's own design notes (`boss.md`'s old "Not yet built") were
fairly prescriptive here — a party-frame ability element calling the
already-public, already-cooldown-gated `PlayerAbility.TryUseAbility()` —
so this needed no new ability logic at all, only UI wiring.

Rather than adding a separate button element to `PartyFrame.prefab`, a
`Button` component was added directly to the existing `AbilityText`
`TextMeshProUGUI` child — it already displays exactly the state a manual
trigger needs (`"Taunt: Ready"` / `"Taunt: 3.2s"`), and `TextMeshProUGUI`
implements `Graphic`, so `Button` could use it as its own `targetGraphic`
with no wrapping `Image`. Done via `manage_prefabs`'s `open_prefab_stage`/
`save_prefab_stage` (interactive prefab-stage editing, not
`modify_contents`), since this needed both a component add and an
object-reference field wire-up (`PartyFrameUI.abilityButton`) on the
prefab's root — confirmed via `get_hierarchy` and a component-resource
read that the wiring landed correctly before saving.

`PartyFrameUI.Initialize()`'s signature grew a third parameter
(`bool isHumanPlayer`) — `PartyFrameManager.Awake()` already computed
`isHuman` via the existing `GetComponent<AIController>() == null` check
for the CPU display-name logic, so this was just threading an
already-computed value through, not new detection logic. The human's own
frame gets its button `SetActive(false)`; teammate frames get
`onClick.AddListener(() => playerAbility.TryUseAbility())` — a deliberate,
narrow exception to this codebase's otherwise-universal "Inspector
persistent listeners only" convention (confirmed by checking
`GameOverUI.cs`/`RoleSelectUI.cs`, both wired purely via the Inspector),
justified because each `PartyFrame` prefab instance only learns which
ship's `PlayerAbility` it owns at runtime inside `Initialize()` — there's
no concrete target to drag into an Inspector slot at prefab-authoring
time. `Update()` gained one line driving `abilityButton.interactable` off
the same `CooldownRemaining` the status text already reads, so the button
visibly greys out during cooldown instead of silently no-oping.

The manual click deliberately does **not** replicate `AIController`'s
extra Tank-specific gate (`if (boss.CurrentTarget != gameObject)`) — it
calls `TryUseAbility()` directly, so a player can force a Tank to
re-taunt even while it already holds aggro, on the reasoning that
refreshing threat deliberately is a legitimate player choice the AI
heuristic (which only cares about *not* holding aggro) has no way to
express.

**Verified live**: read back all 4 party frames' `abilityButton` active/
interactable state and each ship's live `CooldownRemaining` — confirmed
the human's frame has an inactive button while all 3 CPU frames have
active ones whose `interactable` tracks cooldown exactly. Invoked a
ready Tank frame's `onClick` directly and confirmed `PlayerAbility`'s
cooldown jumped from 0 to the full `tauntCooldown` (5s), proving the
click really drives `TryUseAbility()` and not just a UI-only state
change. Reloaded the scene from disk afterward and re-confirmed the
`abilityButton` wiring persisted (this project's standard prefab-edit
verification habit since Session 12's `RecordPrefabInstanceProperty
Modifications()` gotcha).

### Bullet-dodging

`Bullet.cs` gained a static `Active` registry (`List<Bullet>`, populated/
depopulated in new `Awake()`/`OnDestroy()` methods) and three public
read-only accessors (`Direction`, `Speed`, `Owner`) over its existing
private fields — chosen over a per-frame `FindObjectsByType<Bullet>()`
scan (needless cost across 3 AI teammates × every frame) or adding a new
Unity tag/layer for `Physics2D.OverlapCircleAll` (more moving parts for
no real win at this bullet count, ~20 concurrent max during a Spiral
barrage). No existing bullet-registry or tag mechanism existed anywhere in
the codebase before this (confirmed via grep before assuming otherwise).

**A first `script_apply_edits` attempt on `Bullet.cs` misplaced the new
members outside the class** — an `anchor_insert` anchored on the class
declaration line inserted its text *before* the anchor rather than after,
landing the new registry/accessors above `public class Bullet :
MonoBehaviour` entirely, which wouldn't compile. Caught immediately by
reading the file back rather than trusting the tool's success response;
fixed with a direct `Edit` (reordering the class declaration back above
the new members) followed by an explicit `refresh_unity` (`scope:
scripts`, `compile: request`) — the documented gotcha from Sessions 5/6
(see progress-log-archive.md)
that editing a script file outside the MCP script tools leaves Unity's
asset database unaware of the change. Confirmed zero compile errors
afterward. Lesson: `anchor_insert` inserts *before* its anchor match, not
after — for "insert right after this line" edits, anchor on the
*following* line instead, or use `insert_method`'s explicit
`position`/`afterMethodName` for method-level insertions (which needed
its own fallback — see below).

`AIController.cs` gained a new `[Header("Bullet dodging")]` tunable block
(`dodgeDetectionRadius` 3, `dodgeLookaheadTime` 0.6s, `dodgeMissDistance`
0.6, `dodgeWeight` 1 — first-pass placeholders, explicitly confirmed with
the user to ship as-is and tune after playtesting, same as every other
stat in this project) and a new private `ComputeDodgeVector()`, called
once per `Update()` right after the existing per-role switch computes its
positioning direction and before that direction reaches
`controller.SetMoveDirection()` — a single choke point all four roles
already pass through, so no per-role code needed changing.
`insert_method`'s `position: "after"`/`afterMethodName` failed to locate
`AttackerPositionDirection` as an anchor (a tool-side brace-matching
issue, cause not fully diagnosed); switched to `position: "end"` instead,
which appended the method inside the class cleanly.

**Algorithm** (confirmed with the user in advance: applies to all 4
roles, including Tank, as an additive blend rather than an override or a
Tank-specific exemption): for each bullet in `Bullet.Active` with `Owner
== "Enemy"` within `dodgeDetectionRadius`, projects the teammate's
position onto the bullet's current velocity (`Direction * Speed`) to find
the time and point of closest approach (clamped to
`dodgeLookaheadTime`) — re-evaluated fresh every frame, so a guided
missile's currently-re-aimed heading is handled reasonably without full
intercept prediction. If the resulting miss distance is within
`dodgeMissDistance`, the bullet counts as imminent and the teammate steers
**perpendicular** to the bullet's travel direction (a sideways step out of
its lane, not a radial push away from the bullet's current position) —
this reads as an actual dodge for a fast-moving projectile in a way a
naive "move directly away from it" wouldn't. Multiple imminent bullets'
escape vectors sum and normalize. The result blends additively into the
role's own positioning (`moveDirection + dodge * dodgeWeight`, then
renormalized) rather than overriding it — deliberately, so Tank doesn't
abandon its guard point outright the moment a bullet approaches, since
standing in the way is Tank's entire job. The boss's proximity shockwave
is out of scope (not a `Bullet` instance, so it never enters
`Bullet.Active`) — already floored by the existing `minDistanceFromBoss`.

**Verified live**, with `Time.timeScale` forced to `0` during the actual
test calls to sidestep the Editor-idle/focused-realtime hazard (see
`docs/unity-notes.md#editor-doesnt-tick-play-mode-updatecoroutines-while-unfocused-and-the-inverse`)
— it wiped this session's own first live-encounter attempt too — the
whole CPU party died mid-test before a screenshot could be taken,
confirmed by reading back `activeSelf`/`CurrentHealth` on all 4 ships, not
assumed): called the
private `ComputeDodgeVector()` on a live Tank teammate via reflection
across three constructed scenarios — a bullet 1.2 units away heading
straight at it (returned a nonzero perpendicular vector), a bullet 10
units away on the same heading (returned `Vector2.zero`, correctly
rejected by the detection-radius check), and a bullet 2.5 units away
(inside the radius) but traveling parallel to a line that misses the
teammate by 2.5 units (also returned `Vector2.zero`, correctly rejected
by the miss-distance/lane check, not just the radius check) — confirming
this is a genuine closing-lane dodge, not "flee anything nearby."
Separately confirmed `Bullet.Active.Count` increments for both
boss-fired and player-fired bullets (filtering to enemy-owned ones is
`ComputeDodgeVector()`'s job, not the registry's). A live full-encounter
screenshot (taken during one of the deliberately-brief unpaused windows)
showed the fight rendering normally with no visual regressions; a longer
live qualitative "does it look like dodging, not jitter" observation
wasn't completed after the real-time hazard repeatedly ended the test
party — left for the user to eyeball interactively, since this
environment's idle-tick behavior makes unattended live observation
unreliable.

### Docs

Both features moved from `roadmap.md`'s "Planned" to "Implemented" as one
combined entry (they shipped together); `boss.md`'s "Not yet built" lost
both bullet-dodging and manual-ability-triggering entries and gained a new
"Bullet-dodging" subsection alongside the existing positioning
subsections; `player-roles.md`'s "PlayerAbility.cs" section gained a
"Manual teammate-ability triggering from the party frame" paragraph;
`hud-layout.md`'s `PartyFrameUI.cs` section's old "planned, not yet
implemented" note was replaced with the real mechanics; `current-state.md`
gained a new "What's playable" bullet and a "How to test it" callout for
clicking a teammate's ability button and watching for dodge jukes.

### Still open

- Minions around the boss — the last remaining item ahead of it in
  "Player-vs-boss dynamics" is now done; this is next in the roadmap's
  build order.
- Dodge tuning numbers (`dodgeDetectionRadius`/`dodgeLookaheadTime`/
  `dodgeMissDistance`/`dodgeWeight`) are unplaytested placeholders,
  flagged for a follow-up pass once minions/further encounters give more
  material to tune against.
- A full live-encounter visual check of dodging (vs. the controlled,
  paused unit-style tests already done) is still outstanding — see
  "Verified live" above.
- Main Menu / Lobby scenes — still not built.

## Session 24 — Minions Around the Boss

The next item in "Player-vs-boss dynamics" per `roadmap.md`, and the last
thing Session 23 flagged as up next: smaller enemy ships flanking the boss,
a second distinct threat type layered on top of its own attacks.

### Design decisions (asked directly, not assumed)

Three real gameplay-feel questions were checked with the user before
writing any code, rather than guessed: minions aim at `boss.CurrentTarget`
(not straight down like wave `Enemy.cs`) so they're tied to the boss's
existing aggro system for free; minions spawn from the very start of the
fight at a small cap (2 concurrent), not gated to Phase 2 as an "adds"
escalation; and minions are solid, physically blocking ships and dealing
contact damage the same way the boss's own body already does, not just a
bullet-only pass-through hazard like wave `Enemy.cs`.

### New scripts, modeled on Enemy.cs + Boss.cs conventions

`Minion.cs` and `MinionSpawner.cs` are new
(`Assets/Scripts/Minion.cs`/`MinionSpawner.cs`). Positioning tracks the
boss's live, erratically-dashing transform (`boss.transform.position +`
a fixed per-minion flank offset `+` a small independent sine wobble) rather
than free sine-drift — the closest existing precedent for "position
relative to the boss's moving transform" was `AIController`'s
`BiasedPositionDirection()`/`AttackerPositionDirection()`, but those steer
*toward* a target point over time; a minion instead snaps directly onto its
anchor every frame, since it has no viewport-clamp or ally-avoidance
concerns a player ship does. `MinionSpawner` lives as a component **on the
`Boss` GameObject itself** (not a separately-referenced object like
`EnemySpawner`) specifically so it gets a free `GetComponent<Boss>()` in
`Awake()` with no Inspector wiring, and is destroyed automatically the
instant `Boss.Die()` destroys the GameObject — no explicit spawner cleanup
needed. It also calls `boss.OnDefeated.AddListener(DestroyAllMinions)`
directly in code in its own `Awake()` (not an Inspector wire-up, since it
already holds a direct `boss` reference) so no stray minions survive into
the Victory panel.

Two small, required changes to existing scripts: `Bullet.cs`'s
player-bullet-vs-`Enemy`-tag branch gained a third check
(`GetComponent<Minion>()`) alongside its existing `Enemy`/`Boss` checks — a
`Minion` isn't literally an `Enemy` component, so without this player fire
would pass straight through a minion with no effect.
`PlayerController.ResolveShipCollisions()` gained a loop over a new
`Minion.Active` static registry (mirroring `Bullet.Active`'s pattern)
after its existing ally/boss checks — minions are spawned/destroyed at
runtime, unlike the hand-placed ships/boss `ResolveShipCollisions` already
had cached colliders for, so each `Minion` caches its own `HalfExtents`
once in its own `Awake()` instead.

### Bug found during verification: fractional damage silently rounds to zero

`Minion`'s first-pass defaults were `bulletDamage = 0.4f` (intentionally
lower than the boss's own `bulletDamage`, for a "lesser threat" feel) and
`contactDamage = 0.5f`. Both are dead on arrival: `PlayerHealth.TakeDamage`
takes an `int`, and every caller rounds via `Mathf.RoundToInt` first — `0.4`
rounds down to `0`, and `0.5` rounds to the nearest *even* integer (Unity's
`Mathf.RoundToInt` uses round-half-to-even, not round-half-up), which is
also `0`. So at these defaults, minions would have dealt **zero** damage to
players on every single hit, silently, no error, no console warning. Every
other player-facing damage value already in the codebase
(`Boss.bulletDamage`, `Enemy`'s default bullet damage, the contact/shockwave
multipliers) happens to already be a whole number, so this specific footgun
had never surfaced before. Caught live, not by code review:
`ApplyContactDamage` produced no shield/health change at all when called
directly against the old defaults; after switching both values to whole
numbers (`1`), the identical call correctly dropped shield by 1 and was
correctly blocked by its own cooldown on an immediate second call. Since the
`Minion.prefab` had already been created from the script's old defaults,
fixing the script alone wasn't enough — the now-familiar script-default
gotcha (see
`docs/unity-notes.md#changing-a-scripts-default-value-doesnt-retroactively-update-an-already-serialized-field`)
— the prefab's serialized `Minion` component values needed an explicit
`manage_prefabs modify_contents` call too.

### Verification quirks hit this session

Hit the Editor-idle gotcha's domain-reload edge case (see
`docs/unity-notes.md#editor-doesnt-tick-play-mode-updatecoroutines-while-unfocused-and-the-inverse`):
entering Play mode while the Editor window is unfocused left `editor_state`
reporting `play_mode.is_changing: true` and a slightly-later
`last_domain_reload_after_unix_ms` than expected for a stretch of calls,
during which `Minion.Active.Count` briefly read `0` even though
`GameObject.FindObjectsByType<Minion>()` found a real, correctly-initialized
instance — the instance's own serialized fields, like `health`, survived
intact; only the non-serialized static `List<Minion>` was wiped. Exiting
and re-entering Play mode cleanly (confirmed via `is_changing: false`
before proceeding) made `Minion.Active` and `FindObjectsByType` agree
again for the rest of the session. Separately, two stationary test bullets
spawned exactly on top of a minion (`speed = 0`, to isolate the trigger
check from bullet travel) vanished within a frame or two without ever
registering a hit — not a collision bug, but their own `lifeTime`-based
`Destroy(gameObject, 3f)` safety cleanup firing, because a single forced
"frame step" in this idle/unfocused Editor can carry a real-world-clock-sized
`Time.deltaTime` large enough to blow past the 3-second lifetime in one
tick. Worked around by
testing `TakeDamage`/`ApplyContactDamage`/`ResolveShipCollisions` directly
rather than fighting bullet-travel timing, and by using the boss's own
already-proven contact-damage path as a control to confirm the *environment*
was the variable, not the new code — the control produced a real, correct
shield drop under the exact same conditions where a naive minion-only
reading looked broken.

### Verified

All via the Unity MCP bridge, live in Play mode: minion position tracks the
boss through a dash exactly (`boss.position + flankOffset`, wobble within
`wobbleAmplitude`); minion fire direction matches the vector to
`boss.CurrentTarget` exactly, checked for two different minions against the
same target; `TakeDamage` reduces health and destroys at 0, and
`MinionSpawner` immediately refills the freed slot on its next `Update()`
(cap never exceeded 2 across repeated forced spawns, including through a
kill/respawn cycle); `ApplyContactDamage` applies once and is blocked by its
own cooldown on an immediate second call (confirmed post-fix, shield 5→4);
`ResolveShipCollisions` (invoked directly, and confirmed via the raw
`ShipCollisionUtil.ResolveBoxOverlap` math independently) correctly resolves
a ship/minion overlap using `Minion.Active`; invoking `Boss.OnDefeated`
destroys every live minion immediately (`Minion.Active.Count` → 0, no stray
minions visible alongside the Victory panel). No console errors or warnings
at any point.

### Docs

`roadmap.md`'s "Minions around the boss" moved from "Planned" to
"Implemented"; `boss.md` lost its "Minions around the boss" line from "Not
yet built" and gained a full "Minion.cs / MinionSpawner.cs" section plus a
`MinionSpawner.cs` row in the Boss scene-wiring table; `current-state.md`
gained a new "What's playable" bullet.

### Still open

- No `BossPanel`/HUD minion-count display — deliberately deferred, matching
  the project's "prove fun before polish" pattern.
- No aggro/threat-table integration for minions — they always aim at the
  boss's own `CurrentTarget` by design, not tracked as a roadmap gap.
- Enemy spawn pattern variety — the other item still open under
  "Player-vs-boss dynamics", not started.
- Main Menu / Lobby scenes — still not built.

## Session 25 — Enemy Spawn Pattern Variety

The roadmap's last open item under "Player-vs-boss dynamics": `EnemySpawner.cs`
picked a uniform-random X per enemy every wave, and `Enemy.cs` always
sine-waved straight down, no variety at all. The roadmap explicitly framed
this as feeding into "the boss encounter's bullet-pattern design language,"
i.e. `Boss.cs`'s Pattern Barrage — the established precedent in this
codebase for "one system, several shapes."

### Scope, decided upfront

Two decisions made with the user before writing any code: (1) scope covers
**both** spawn formations (where/when enemies appear in a wave) **and**
movement behavior after spawning, not formations alone; (2) selection is
**sequential/escalating** — a fixed cycling order that gets harder — rather
than Pattern Barrage's random-no-repeat, since the goal here is ramping
difficulty over a session, not keeping the player guessing wave-to-wave.

### `EnemySpawner.cs` — `WaveFormation` enum, fixed cycle

New nested `enum WaveFormation { Random, Line, Cluster, VFormation }` and a
public `formationOrder` array (defaults to that exact order, easy → hard).
A new private `waveIndex`, incremented once per `SpawnWaveRoutine()` call,
picks `formationOrder[waveIndex % formationOrder.Length]` — cycles forever
rather than terminating, matching this project's "prove fun before infra"
style (no difficulty cap, no wave-count ending condition). Each formation
computes its own `(x, yOffset)` per enemy via a new private `PositionFor()`,
and maps to one `Enemy.MovementPattern` via a new private
`MovementPatternFor()` — kept as two small private helpers rather than
inlining the logic into `SpawnWaveRoutine()`, so the formation → shape and
formation → movement pairings each live in one place:

- **Random** (unchanged) — uniform-random X, `SineWave`.
- **Line** — evenly spaced across `spawnWidth`, spawned with no stagger
  (skips the `WaitForSeconds(spawnInterval)` yield only for this formation)
  so the wave actually reads as a line rather than trickling in, `SineWave`.
- **Cluster** — one random center X, `ZigZag` movement.
- **VFormation** — symmetric X offsets around center plus a Y offset
  (`vFormationYStep` scaled by distance from center) so the wave visibly
  forms a V as it descends, `StraightDive` — the hardest tier.

### `Enemy.cs` — `MovementPattern` enum, three shapes

New nested `enum MovementPattern { SineWave, ZigZag, StraightDive }` and a
public `movementPattern` field, defaulting to `SineWave` so a stray
direct-prefab spawn (bypassing the spawner entirely) behaves exactly as it
always has. Set externally by `EnemySpawner.cs` as a plain field assignment
right after `Instantiate()`, before `Start()` runs next frame — same safe
ordering `Boss.SpawnBullet()` already relies on for `Bullet.damage`.
`Update()`'s movement block became a switch: `SineWave` is the original
formula, untouched; `ZigZag` accumulates X by `zigzagSpeed * Time.deltaTime`
in a direction that flips every `zigzagInterval` seconds (a real alternating
step, not a smoother sine — reads distinctly more erratic); `StraightDive`
locks X to `startX` and multiplies `moveSpeed` by `diveSpeedMultiplier` for
the descent (faster, no horizontal dodging cue). No changes to `Fire()`,
`TakeDamage()`, or the off-screen cleanup.

### Bug caught live: Cluster's center was re-rolled per enemy, not per wave

First pass called `Random.Range(-spawnWidth/2, spawnWidth/2)` for the
cluster's center **inside** `PositionFor()`, which is invoked once per
enemy in the spawn loop — so every enemy in a "Cluster" wave got its own
independent random center instead of jittering around one shared point.
Caught immediately during live verification (see below), not by reading the
code back: reflection-invoking `PositionFor(Cluster, i)` for `i = 0..4`
directly looked fine in isolation (each call correctly returns *a* jittered
value), but sampling a real wave spawned via `SpawnWaveRoutine()` showed
positions spread almost the full `spawnWidth` (`-0.92` to `3.05`) instead of
a tight group — nowhere near `clusterJitter`'s ±0.5 range. Fixed by rolling
`clusterCenterX` **once** in `SpawnWaveRoutine()`, before the per-enemy
loop, and passing it into `PositionFor()` as a parameter rather than letting
the switch re-roll it. Re-verified: a full wave landed within ±0.5 of a
single shared center as designed.

### Testing wrinkle: the Spawner is currently unreachable in normal play

`Boss.Awake()` calls `enemySpawner.SetActive(false)`, and `Awake()` runs
before any `Start()` on the same frame — so in the current `Gameplay`
scene, `EnemySpawner.Start()` never actually fires and zero enemies spawn
during a normal playtest; this predates this session and isn't a
regression, just a pre-existing consequence of the boss always being
present. Verification worked around it by deactivating the `Boss`
GameObject in Edit mode before entering Play (keeping `Spawner` active),
then reactivating `Boss` afterward and confirming via `manage_scene
get_active`'s `isDirty: false` that the temporary toggle left no scene
diff — same "temporarily reassign, test, revert, confirm" pattern Session
17 used for exercising Attacker's AI code path. Flagged in
`current-state.md`'s testing instructions and `combat.md` so this doesn't
get assumed away later.

### Verified

All via the Unity MCP bridge, in Play mode, with `Boss` deactivated:
reflection-invoked `PositionFor()`/`MovementPatternFor()` directly for
every formation, confirming the exact expected `(x, yOffset)` shape and
movement-pattern pairing (Line: evenly spaced across ±3 with 0 yOffset;
VFormation: symmetric X offsets with `yOffset` increasing by distance from
center; Cluster: initially broken, see above, then confirmed tight after
the fix). Manually drained `SpawnWaveRoutine()`'s `IEnumerator` five times
in a row (`MoveNext()` in a loop, skipping the real `WaitForSeconds` waits
for determinism — same technique Session 22 used to drive Pattern Barrage's
`FireSpiralRoutine()`), confirming `waveIndex` advanced exactly one per
call and the formation cycled in the exact fixed order (`Random → Line →
Cluster → VFormation → Random`, verified across a full second lap too).
Spawned real `Enemy` instances at `y = 1000` (safe from the `y < -10`
off-screen cleanup) and reflection-invoked `Start()`/`Update()` directly
several times each to confirm the live per-frame movement trend for all
three patterns against the sampled `Time.deltaTime`: `ZigZag` moved in X at
`zigzagSpeed` while descending at `moveSpeed`; `StraightDive` held X exactly
constant while descending at `moveSpeed * diveSpeedMultiplier` (faster);
`SineWave` matched its original, unchanged formula. Confirmed the
`Random`-formation regression case is bit-for-bit unchanged from before
this session. No console errors or warnings at any point. `Boss` was
reactivated and the scene confirmed clean (`isDirty: false`) afterward.

### Still open

- Local co-op / dynamic player count — unchanged, still the only "In
  Progress" item on the roadmap.
- Scene scaffolding (Main Menu / Lobby) and Nakama networking — next up per
  the roadmap's build order, now that "Player-vs-boss dynamics" is fully
  implemented.

## Session 26 — Minion Kamikaze Contact + Explosive Minion Type

User-requested follow-up boss-combat tuning, chosen over moving on to scene
scaffolding/networking: minions (Session 24) had zero real risk/reward for
the player — touching one just cost cooldown-gated chip damage with no
consequence to the minion itself, so a ship could tank hits from it for
free indefinitely. Two changes requested together: make contact damage
"kamikaze" (cost the minion its life), and add a new minion type that
explodes into damaging fragments on death.

### Design decisions, confirmed upfront

Three open questions, resolved with the user before writing code:

- **Kamikaze scope**: applies to **every** minion, not just a new type —
  the existing cooldown-gated repeat-contact behavior is gone entirely.
  Touching a ship deals `contactDamage` once, then the minion dies
  immediately.
- **Explosion trigger**: the new Explosive type's fragment burst fires on
  **any** death — killed by a player bullet, not just by touching a ship —
  so players can't safely snipe one from a distance either.
- **Spawn mix**: `MinionSpawner` rolls an independent random chance
  (`explosiveMinionChance`, 30%) on every spawn, rather than pinning
  Explosive to a fixed flank slot.

### `Minion.cs` — shared `Die()`, `MinionType`, fragments

Both existing death paths (`TakeDamage` from a player bullet,
`ApplyContactDamage` from ship contact) now funnel through one new private
`Die()`, guarded by a new private `bool isDead`. The guard exists because
`Object.Destroy()` is deferred to end-of-frame — a minion hit by a bullet
and touched by a ship in the same physics step could otherwise double-fire
death logic (double contact damage, or two fragment bursts) before it
actually disappears; confirmed this scenario directly during verification
(see below), not just reasoned about.

`ApplyContactDamage` dropped its `lastContactDamageTime`
`Dictionary<GameObject, float>` and `contactDamageCooldown` field entirely
— dead code once a minion only ever takes one hit.

New `public enum MinionType { Standard, Explosive }` and a public `type`
field. `Init(Boss, Vector2)` gained a third `MinionType` parameter
(default `Standard`, so the only real call site — `MinionSpawner`, below —
is the one place that needs to pass it) — type has to flow in through
`Init()` rather than being set as a plain field post-`Instantiate`, since
`Awake()` (which the tint below depends on) already ran by the time the
spawner would get a reference back. When `Explosive`, `Init()` also tints
the minion's `SpriteRenderer` to a new `explosiveTintColor` (orange), so
the danger is visible the instant it spawns — same "always give a new
hazard a visible tell" precedent as the shockwave ring, Medic's aura ring,
and Support's party-buff ring.

`Die()` calls a new private `SpawnFragments()` only when `type ==
Explosive`: an evenly-spaced ring of `fragmentCount` (8) more `Bullet`
instances launched from the minion's position, reusing
`Boss.FireRing()`'s exact idiom bit-for-bit (`step = 360 / fragmentCount`,
random start offset, `Quaternion.Euler(0, 0, angle) * Vector2.up` per
direction). Each fragment is `Init(dir, fragmentSpeed, "Enemy")` with
`damage = fragmentDamage` — an ordinary enemy-owned `Bullet`, so
`Bullet.cs` needed **zero changes**: its existing enemy-bullet-vs-`Player`
routing already handles a fragment hitting any ship, including the one
that just killed the minion. `fragmentPrefab` is optional, falling back to
the minion's own `bulletPrefab` if left unassigned in the Inspector, so no
new prefab was required to ship this. `fragmentDamage` defaults to `1` —
kept as a whole number deliberately, the same footgun `bulletDamage`/
`contactDamage` already hit in Session 24 (`PlayerHealth.TakeDamage(int)`
round-half-to-even's a fractional value to zero).

### `MinionSpawner.cs`

New `[Range(0,1)] explosiveMinionChance` (0.3) field. `SpawnMinion()` rolls
`Random.value < explosiveMinionChance` and passes the resulting
`MinionType` into `Init()`. `PlayerController.cs`/`Bullet.cs` needed no
changes at all — `ResolveShipCollisions()` already called
`minion.ApplyContactDamage(gameObject)` on overlap, and the new
kamikaze/explosion behavior lives entirely inside `Minion.cs`'s own
`Die()`.

### Verified

Unity MCP bridge, Play mode, after a clean recompile with no console
errors/warnings. Ran four scenarios via `execute_code`:

1. A Standard minion's `ApplyContactDamage` dealt its shield/health hit
   exactly once (shield absorbed the 1-point hit first, matching
   `PlayerHealth.TakeDamage`'s existing shield-then-health order) and
   `isDead` flipped `true`; an immediate second call on the same
   still-alive-per-C#-reference instance (exploiting the end-of-frame
   `Destroy()` deferral to test the guard directly) was a confirmed no-op —
   health/shield unchanged, zero fragments.
2. An Explosive minion killed via `ApplyContactDamage` spawned exactly
   `fragmentCount` (8) new `Bullet`s, all `Owner == "Enemy"` and `damage ==
   fragmentDamage`.
3. A separate Explosive minion killed via `TakeDamage` (simulating a
   player bullet) also spawned exactly 8 fragments — confirming the "any
   death" trigger, not just kamikaze contact.
4. A Standard minion killed via `TakeDamage` spawned zero fragments.

Also confirmed an Explosive-`Init()`'d minion's `SpriteRenderer.color`
matched `explosiveTintColor` exactly.

**Incidental finding, not a bug**: partway through, `GameObject.Find
("Player")` started returning `null` — the human `Player`, sitting
unattended with no input while tool calls were in flight, had actually been
killed and deactivated by ongoing boss fire (`PlayerHealth.Die()` calls
`SetActive(false)`, and `GameObject.Find` doesn't search inactive
objects). Worked around by reactivating and fully healing all 4 ships via
`FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include, ...)` before
testing — a testing-only workaround, no game code changed for this.

### Still open

- Local co-op / dynamic player count — unchanged, still the only "In
  Progress" item on the roadmap.
- Scene scaffolding (Main Menu / Lobby) and Nakama networking — still the
  next roadmap milestones; this session was boss-combat tuning requested
  ahead of that, not a change to the build order.
- The Explosive tint/fragment-burst was verified programmatically
  (reflection/field checks), not yet screenshotted in a live fight — worth
  a visual pass next time the boss fight is played end-to-end.
