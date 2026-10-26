# Architecture

This documents the current codebase's concrete conventions — script
organization, communication patterns, and deliberate omissions. For
product-level architecture principles (build order, server-authoritative
philosophy), see `overview.md`'s "Architecture Principles" section.

## Script Organization

Every file holds exactly one class — no exceptions. This is deliberate,
not sloppy: Unity's script serialization depends on the file's
*matching-name* class being the `MonoBehaviour`/`ScriptableObject` (see
`unity-notes.md`'s "Script serialization" section). The project got bitten
by this once — `PlayerRoleComponent` was originally bundled into
`PlayerRole.cs` (whose matching class is the `PlayerRole` enum), which
produced a component with a silently broken script reference — and the
one-class-per-file rule has been followed strictly ever since.

`Assets/Scripts/` itself is *mostly* flat, with one exception: a
`FeatureName/` subfolder for a `MonoBehaviour` that outgrew a single file
and needed owned helper classes split out (originally to stay readable,
now also to stay under this session's file-size cap — see
`docs/superpowers/specs/2026-09-04-halcyon-boss-design.md`). Not
`Managers/`, `Controllers/`, or `UI/` — those would group by *kind*, which
this project deliberately avoids; a feature folder groups by *owner*
instead, holding only that one `MonoBehaviour` and the plain helper
classes it alone constructs (e.g. `MarauderBoss/` holds `MarauderBoss.cs`
plus `MarauderBossMovement`/`Shockwave`/`Attacks`/`Aggro`/its two
`[System.Serializable]` settings classes; `HalcyonBoss/` holds
`HalcyonBoss.cs` plus its three sibling `MonoBehaviour`s;
`AIController/`/`PlayerAbility/` follow the same shape). At 58 scripts
across a handful of these feature folders plus the flat majority, this is
still easy to scan; a `Managers/`/`Controllers/`/`UI/`-style regrouping
remains something to revisit only if that stops being true.

## Component Model: Plain MonoBehaviours Only

Nearly every class is a plain `MonoBehaviour`. The only exceptions are
`PlayerRole.cs`'s static lookup (`PlayerRoleStats`) and
`PartyRoleAssignment`'s static nullable field — no interfaces, no abstract
base classes, no component hierarchies anywhere in the codebase.

## Boss-type-agnostic orchestration: IBoss

The one deliberate exception to "Component Model: Plain MonoBehaviours
Only" above. `LevelSequencer.cs`/`PlayerController.cs` are reused verbatim
across every level scene and need to drive whichever boss a level uses
(`MarauderBoss`, `HalcyonBoss`, ...) without knowing its concrete type —
with no inheritance hierarchy in this codebase, a shared type was needed.
`IBoss.cs` is a small interface (`SetVisible(bool)`,
`ApplyContactDamage(GameObject)`) covering exactly the two methods those
two orchestrator scripts call on a boss; both boss classes implement it.

Unity can't serialize an interface-typed field directly, so the actual
Inspector fields (`LevelSequencer.bossObject`, `PlayerController.bossObject`,
`AIController.bossObject`, `PartySetupBootstrap.bossObject`) stay
`MonoBehaviour`-typed (drag either boss prefab/instance in) and are cast to
a cached `private IBoss boss` field once, in `Awake()`/`Start()`. Plain
`.transform`/`.enabled` access (needed by `LevelSequencer`'s entrance glide
and `AIController`'s boss-avoidance positioning) stays on the
`MonoBehaviour` reference directly — no interface member needed for those,
since every `Component` already exposes them. See
`docs/systems/bosses/halcyon-boss.md`'s "Code architecture: sibling
MonoBehaviours + IBoss" for the full context this was introduced under.

## Primary Wiring: Inspector-Wired Public Fields

Direct references dragged in the Inspector are the primary way components
find each other: `PlayerController.bossObject`, `AIController.teammates[]`/
`bossObject`, `PartyFrameManager.players[]`/`partyFrames[]`. This is used where
the relationship is fixed at design time — a teammate always has the same
boss reference, a party frame always tracks the same ship.

## Decoupled Notification: UnityEvent Persistent Listeners

Where a component needs to notify others without knowing who's listening,
the pattern is a `UnityEvent` wired as an Inspector **persistent**
listener — not a code-added `AddListener` — matching how `Button.OnClick()`
is normally wired. Examples: `PlayerHealth.OnDeath`/`OnDamaged`,
`PlayerAbility.OnTaunt`, `MarauderBoss.OnPhase2`/`OnDefeated`.

**One documented exception**: `PartyFrameUI`'s ability-click handler is
wired via code (`onClick.AddListener(...)`), because a `PartyFrame` prefab
instance only learns which ship's `PlayerAbility` it owns at runtime
(inside `Initialize()`) — there's no concrete target to drag into an
Inspector slot at prefab-authoring time. Every other event listener in the
project uses the persistent-listener convention.

## Deliberately Absent: Singletons / Service Locator / FindObjectOfType

No singleton, no service locator, and no `FindObjectOfType`/
`GameObject.Find` appear anywhere in game code. Inspector wiring keeps
dependencies visible and explicit at the cost of the failure mode
described in "Known Limitations" below — a deliberate trade, not an
oversight.

## Deliberately Absent: Dependency Injection

No DI container or framework. Inspector wiring plays that role at this
project's scale (a handful of GameObjects with a handful of cross-references
each); a DI framework would be infrastructure the project hasn't earned yet,
matching `overview.md`'s "prove gameplay before infrastructure" principle.

## Deliberately Absent: ScriptableObjects

No `ScriptableObject`/`CreateAssetMenu` usage anywhere. Role balance data
(`PlayerRoleStats` in `PlayerRole.cs`) is a static in-code dictionary
instead — explicitly chosen to match "the project's plain-`MonoBehaviour`,
low-infra style" rather than introducing an asset-based data workflow for
values that are still being hand-tuned pre-playtesting.

## Static Registries Instead of Scene Scans

`Bullet.Active` and `Minion.Active` are static `List<T>`s, populated in
each instance's `Awake()` and removed in `OnDestroy()`. Other systems (AI
dodge logic, collision resolution) enumerate these instead of running a
`FindObjectsByType` scan every frame — chosen specifically over the
scan-based alternative for the per-frame cost across multiple AI
teammates.

## Static Cross-Scene State

`PartyRoleAssignment` is a plain `public static class` holding a nullable
`PlayerRole? HumanRole`, used to carry the human player's role choice from
`RoleSelect.unity` into whichever level scene gets picked, across a
`SceneManager.LoadScene` call. It's explicitly a plain static rather than a singleton
`MonoBehaviour` with `DontDestroyOnLoad` — no persistent GameObject to
manage, and it resets cleanly to `null` on a domain reload.

## Dual Entry-Point Pattern: AI vs. Human Control

The project uses Unity's New Input System exclusively for human input, but
every input-driven component also exposes parallel public, non-input entry
points: `PlayerController.SetMoveDirection()`/`SetFiring()`,
`PlayerAbility.TryUseAbility()`. `AIController` calls these same methods
directly to drive an AI teammate, so a teammate is mechanically identical
to a human-controlled ship in every way except how its input is produced.
This "same component, two callers" approach was chosen over an
`IController`/strategy abstraction — simpler at the current scale of one
input path plus one AI path.

## Non-Destructive Buff Pattern

Temporary effects (Support's Speed Boost) are applied via separate
runtime-only multiplier fields (`speedBuffMultiplier`,
`fireRateBuffMultiplier` on `PlayerController`) that are read at the point
of use, rather than ever being multiplied into the base stat and later
divided back out. This removes the revert-by-division arithmetic entirely,
along with the "cooldown must stay ≥ duration" constraint that arithmetic
used to require to avoid double-applying a buff.

## Execution Order

`[DefaultExecutionOrder(-1000)]` is used exactly once in the codebase, on
`PartySetupBootstrap`, to guarantee it assigns each ship's role before any
other script's default-order `Awake()` (which reads that role to set
health, tint, or build role-specific structures) runs. This is a
deliberately rare tool — reach for it only when `Awake()` ordering
correctness genuinely can't be achieved another way, not as a general
pattern.

## Sequencing: One Top-Level Orchestrator Per Level

`LevelSequencer` (see `systems/level-sequencing.md`) owns a level's whole
pre-fight-to-boss timeline as one linear coroutine, calling public methods
on other components (`EnemySpawner.StartSpawning()`/`StopSpawning()`,
toggling `MarauderBoss`'s `enabled`/`GameObject.SetActive`) to gate when each
system runs. This is new *coordination-shape* — nothing else in the
codebase owns a multi-phase timeline spanning several other components —
but it doesn't violate any convention above: it's still a plain
`MonoBehaviour`, still Inspector-wired (`ships[]`/`enemySpawner`/
`bossObject` are dragged in, not resolved via `FindObjectOfType`), and
still holds no singleton/static instance. Kept deliberately minimal — one
script, no generic "level framework" — and named generically (not
`Level1`-prefixed) since it's meant to be reused by future levels' own
scenes with their own boss/timing values.

## No Assembly Definitions

Until the test-framework setup, every script compiled into the implicit
default `Assembly-CSharp` assembly — no `.asmdef` files existed anywhere in
the project. `Assets/Scripts/UntitledShips.Runtime.asmdef` is the first.

## Known Limitations / Things to Watch

These are observed costs of the conventions above, not a call to redesign
them — each is noted with where it already showed up in practice.

- **Inspector-wiring fails silently, not at compile time.** A forgotten or
  broken reference produces a null at runtime with no compiler warning.
  This already caused the `PlayerRoleComponent` broken-script-reference bug
  (see "Script Organization" above) and the prefab-drift issue below.
- **Prefab drift risk — resolved.** `Player`/`Teammate_Tank`/`Teammate_Medic`/
  `Teammate_Support` used to be a mix of plain duplicated GameObjects and one
  real `Teammate.prefab` instance (see `unity-notes.md`'s "Duplicating a
  GameObject before it's a prefab instance") — a prefab-default edit didn't
  propagate to the duplicates automatically, and each needed the same edit
  applied by hand repeatedly across the project's history. Fixed as part of
  local co-op / dynamic player count (see `systems/player-roles.md`'s "Local
  co-op / dynamic player count"): all 4 are now real instances of one
  unified `Ship.prefab`. A related gotcha surfaced during that same fix,
  documented in `unity-notes.md`'s "Prefab-instance overrides" section: an
  instance value that happens to match the prefab's default at the moment
  it's set isn't recorded as a real override, so changing the prefab's
  default later can silently flip that instance too.
- **Float/int inconsistency in damage types.** `Bullet.damage`/
  `Enemy.TakeDamage`/`MarauderBoss.TakeDamage` are `float`, but
  `PlayerHealth.TakeDamage(int)` stayed `int`. This already caused a real
  bug: `Minion`'s first-pass fractional damage defaults (0.4/0.5) silently
  rounded to zero via `Mathf.RoundToInt`'s round-half-to-even behavior,
  until switched to whole numbers.
- **No data-driven balance.** Role stats and other tuning numbers live in
  static in-code tables, so every balance pass requires a recompile rather
  than an asset edit. Acceptable while values are still pre-playtesting
  placeholders; worth reconsidering once real tuning cadence increases.
- **Networking incompatibility — the significant one.** `overview.md`
  commits to eventual server-authoritative multiplayer. Drag-and-drop
  Inspector references and static cross-scene classes (`PartyRoleAssignment`)
  fundamentally don't work across a network boundary — this part of the
  current architecture will need real rework, not just extension, once
  networking work starts.
