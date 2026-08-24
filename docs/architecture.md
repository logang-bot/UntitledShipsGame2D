# Architecture

This documents the current codebase's concrete conventions — script
organization, communication patterns, and deliberate omissions. For
product-level architecture principles (build order, server-authoritative
philosophy), see `overview.md`'s "Architecture Principles" section.

## Script Organization

`Assets/Scripts/` is flat — no `Managers/`, `Controllers/`, `UI/`, or
per-feature subfolders — and every file holds exactly one class. This is
deliberate, not sloppy: Unity's script serialization depends on the file's
*matching-name* class being the `MonoBehaviour`/`ScriptableObject` (see
`unity-notes.md`'s "Script serialization" section). The project got bitten
by this once — `PlayerRoleComponent` was originally bundled into
`PlayerRole.cs` (whose matching class is the `PlayerRole` enum), which
produced a component with a silently broken script reference — and the
one-class-per-file rule has been followed strictly ever since.

At ~25 scripts the flat structure is still easy to scan; it's a convention
worth revisiting if the script count grows substantially.

## Component Model: Plain MonoBehaviours Only

Nearly every class is a plain `MonoBehaviour`. The only exceptions are
`PlayerRole.cs`'s static lookup (`PlayerRoleStats`) and
`PartyRoleAssignment`'s static nullable field — no interfaces, no abstract
base classes, no component hierarchies anywhere in the codebase.

## Primary Wiring: Inspector-Wired Public Fields

Direct references dragged in the Inspector are the primary way components
find each other: `PlayerController.boss`, `AIController.teammates[]`/
`boss`, `PartyFrameManager.players[]`/`partyFrames[]`. This is used where
the relationship is fixed at design time — a teammate always has the same
boss reference, a party frame always tracks the same ship.

## Decoupled Notification: UnityEvent Persistent Listeners

Where a component needs to notify others without knowing who's listening,
the pattern is a `UnityEvent` wired as an Inspector **persistent**
listener — not a code-added `AddListener` — matching how `Button.OnClick()`
is normally wired. Examples: `PlayerHealth.OnDeath`/`OnDamaged`,
`PlayerAbility.OnTaunt`, `Boss.OnPhase2`/`OnDefeated`.

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
`RoleSelect.unity` into `Gameplay.unity` across a `SceneManager.LoadScene`
call. It's explicitly a plain static rather than a singleton
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
- **Prefab drift risk.** `Teammate_Medic`/`Teammate_Support` are plain
  duplicated GameObjects, not real `Teammate.prefab` instances (only
  `Teammate_Tank` is) — see `unity-notes.md`'s "Duplicating a GameObject
  before it's a prefab instance." A prefab-default edit doesn't propagate
  to the other two automatically; each has needed the same edit applied by
  hand repeatedly across the project's history.
- **Float/int inconsistency in damage types.** `Bullet.damage`/
  `Enemy.TakeDamage`/`Boss.TakeDamage` are `float`, but
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
