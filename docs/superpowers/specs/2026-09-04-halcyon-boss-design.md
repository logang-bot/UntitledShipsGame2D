# Halcyon Boss — Design Spec

Resolves the open design questions in `docs/systems/bosses/halcyon-boss.md`
(Level 2's boss, previously a design pitch with no code). Produced via a
brainstorming session with the user on 2026-09-04. This spec is the input
to the implementation plan; see that plan for file-by-file steps and
verification details.

## Identity

A pure positioning fight — the opposite of Marauder's bullet-heavy style.
Halcyon never fires a bullet during normal play. The only damage sources
are body contact (kept from Marauder, unchanged mechanic) and a new Static
Field proximity pulse. There is no aggro/threat table: Tank's Taunt still
fires its own feedback (flash/shake) but has no boss-side listener here, so
it's a genuine, intentional no-op against Halcyon.

## Mechanics carried over from Marauder

Only body contact damage. Everything else (shockwave, guided missile,
Pattern Barrage, minions, phase-based fire, aggro/taunt) is dropped
entirely — Halcyon's own three mechanics (Roam, Surge, Static Field) are
its full repertoire.

## HalcyonBoss.cs — HP, phases, contact damage

- `maxHealth`: 110 (higher than Marauder's 90/150-tuned values — Static
  Field's frequent-but-moderate damage needs the extra HP so the fight
  doesn't end before both phases are felt)
- Phase 2 at ≤50% HP, same threshold convention as Marauder
- `bulletDamage`: 1 — never spent on an actual shot, kept purely as the
  reference unit that contact damage and Static Field damage multiply
  against, exactly like Marauder's `bulletDamage` field
- Body contact: `bodyContactDamageMultiplier` 2×, per-target cooldown gate
  (`contactDamageCooldown`, 1s) — same shape and numbers as
  `MarauderBoss.ApplyContactDamage`
- `OnPhase2`, `OnDefeated` UnityEvents (persistent Inspector listeners,
  matching project convention)
- Implements `IBoss` (see below): `SetVisible(bool)`,
  `ApplyContactDamage(GameObject)`

## HalcyonRoam.cs — full-arena waypoint movement

Sibling component on the same GameObject as `HalcyonBoss`.

- Picks a random point within the playable viewport bounds (same clamp
  idiom `PlayerController`/`MarauderBoss` already use), glides there at
  `roamSpeed` (2.5 u/s in phase 1 — matches the middle of the party's own
  move-speed range, 1.5–4.5 u/s per `player-roles.md`), pauses briefly
  (`Random.Range(0.3, 0.8)`s), then picks the next point. Same "sit, hop,
  sit, hop" idiom as Marauder's M-pattern hops, just arena-wide with random
  destinations instead of a fixed vertex set.
- Re-rolls (bounded retries) if a freshly-picked point is too close to the
  current one, so a hop always goes somewhere meaningfully different.
- Phase 2: `roamSpeed` increases to 3.5 u/s.
- Freezes in place (skips its movement step) whenever
  `HalcyonSurge.IsTelegraphing || HalcyonSurge.IsActive` is true — read via
  `GetComponent<HalcyonSurge>()` cached in `Awake()`.

## HalcyonSurge.cs — the vulnerability window

Sibling component. State cycle, identical timing in both phases:

1. **Idle** — counts down `cooldown` (8s).
2. **Telegraph** (1s) — a visible tell (e.g. a brightening ring or flash on
   the boss sprite, mirroring Marauder's shockwave-ring telegraph idiom);
   boss is already stationary here (via `HalcyonRoam`'s freeze check).
3. **Active** (2s) — the actual vulnerable window. "Vulnerable" means only
   that the boss is reliably hittable (it's otherwise always moving) — no
   damage-taken multiplier is applied.
4. Returns to **Idle**, cooldown restarts.

Public API: `IsTelegraphing`, `IsActive`, `CooldownRemaining` (float) — read
by `HalcyonRoam` (to freeze) and `HalcyonBossPanelUI` (to show a warning).

## HalcyonStaticField.cs — the damage source

Sibling component. Every `pulseCooldown` (6s phase 1, 4s phase 2 — the
phase-2 escalation for this mechanic), pulses from the boss's current
position:

- Scans all active ships. Any ship within `bossRange` (1.8 units, ~3
  ship-widths) of the boss is a candidate.
- For every pair of candidates that are also within `clusterRange` (0.6
  units, ~1 ship-width) of *each other*, both ships take
  `staticFieldDamageMultiplier × bulletDamage` (3 × 1 = 3) — reusing
  Marauder's shockwave damage convention (3× bullet damage) exactly.
- A short telegraph (matching Marauder's shockwave idiom: dim-always-on,
  brightens briefly before the pulse, flashes on impact) on a
  `LineRenderer` ring at `bossRange`, built once in `Awake()`, re-centered
  on the boss's live position every `Update()`.

Phase 2 only tightens `pulseCooldown` (6s → 4s); `bossRange`/`clusterRange`/
damage are unchanged across phases.

## Cross-boss contract: IBoss

`LevelSequencer.cs` is the same script instance reused verbatim across all
3 level scenes, and currently calls `.SetVisible(bool)` /`.enabled = true`
on a field typed specifically as `MarauderBoss`. `HalcyonBoss` is an
unrelated class (this project has no inheritance hierarchies — plain
MonoBehaviours only), so a shared contract is required for
`LevelSequencer` to keep working across both:

```csharp
public interface IBoss
{
    void SetVisible(bool visible);
    void ApplyContactDamage(GameObject ship);
}
```

Both `MarauderBoss` and `HalcyonBoss` implement it. This is a deliberate,
scoped exception to `architecture.md`'s "no interfaces" convention — made
because two (soon three, with Warden) unrelated boss classes now share
exactly this one contract point with the orchestrator, not a general shift
toward interface-based design elsewhere in the codebase.

Unity can't serialize an interface-typed field directly, so both
`LevelSequencer` and `PlayerController` keep a `[SerializeField] private
MonoBehaviour` field (dragged in the Inspector, either concrete boss type)
and cache a cast to `IBoss` once in `Awake()`. `.transform.position` and
the boss's collider-derived half-extents (used by `ShipCollisionUtil`) stay
reachable directly off the `MonoBehaviour` reference — no interface member
needed for those.

## HalcyonBossPanelUI.cs

Level 2's `BossPanel` script — same "HUD only reads, never owns game
state" pattern as `BossPanelUI.cs`, against `HalcyonBoss`'s different
public API:

- HP bar + `"HP: x/y"` text
- Phase text (`"Phase 1"`/`"Phase 2"`, `"DEFEATED"` on `OnDefeated`)
- Surge warning text (shown while `IsTelegraphing || IsActive`) + a
  `"Surge: {n}s"` / `"Ready"` cooldown text
- Static Field cooldown text only (`"Static Field: {n}s"` / `"Ready"`) — no
  named warning text, same choice Marauder's own Shockwave already makes,
  since the ring itself is the positional tell, not a role callout

No target text, no guided-missile/pattern-barrage text — none of that
exists for this boss.

## Scene wiring

- New `HalcyonBoss.prefab` (with `HalcyonRoam`/`HalcyonSurge`/
  `HalcyonStaticField` as sibling components on the same GameObject)
  replaces `Level2BossPlaceholder.prefab` in `Level2.unity`, same
  position/role Marauder occupies in `Level1.unity`.
- `Level2.unity`'s `LevelSequencer` gets this prefab dragged into its boss
  field.
- `Level2.unity`'s `BossPanel` swaps its script from `BossPanelUI` to
  `HalcyonBossPanelUI`, wired to the new boss.
- `PartySetupBootstrap` needs no changes — Halcyon has no `targets[]`/aggro
  roster to seed, unlike Marauder's fix in `roadmap.md`'s "Aggro roster
  fix".

## Out of scope

Everything Marauder has that isn't listed above as "carried over" (phase-
based fire, shockwave, guided missile, Pattern Barrage, minions,
aggro/taunt) is deliberately absent from Halcyon, not deferred — this
boss's identity is Roam + Surge + Static Field, full stop.
