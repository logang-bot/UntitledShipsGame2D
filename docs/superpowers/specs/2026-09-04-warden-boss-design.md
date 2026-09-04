# Warden Boss — Design Spec

Resolves the open design questions in `docs/systems/bosses/warden-boss.md`
(Level 3's boss, previously a design pitch with no code). Produced via a
brainstorming session with the user on 2026-09-04. This spec is the input to
the implementation plan; see that plan for file-by-file steps and
verification details.

## Identity

A dual/triple-lane coverage fight — the first boss where two threats exist
genuinely at once, so Tank's Taunt and Shield Arc alone can't cover both by
standing in one spot. Two turret-arms (three in Phase 2) each independently
and randomly re-lock onto a living ship and fire a continuous stream while
locked — not a threat table, a rotating random pick, re-weighted (not
overridden) by Tank's Taunt. On top of the arms: body contact damage and a
proximity shockwave, both carried over unchanged from Marauder. The fight's
own signature mechanic, **Lockdown volley**, is a wide wall of parallel
bullets from a random arena edge, built specifically to reward Shield Arc's
*width* over ordinary dodging. There is no single-target aggro/threat table
at all — Taunt has a real effect here (see below), just not the redirect
model Marauder uses.

## Mechanics carried over from Marauder

Erratic dash-or-hold movement, body contact damage, and the proximity
shockwave — all reused with unchanged numbers/formulas. Everything else
(phase-based ambient fire, Pattern Barrage, minions, single-target
aggro/taunt-redirect) is dropped entirely, same "not a deferred subset of
Marauder's kit" framing Halcyon's doc uses — Warden's own repertoire is the
arms + Lockdown volley, full stop.

## WardenBoss.cs — HP, phases, contact damage

- `maxHealth`: 130 (placeholder — highest of the three bosses; more
  simultaneous damage sources need enough HP to survive both phases)
- Phase 2 at ≤50% HP, same threshold convention as Marauder/Halcyon. On
  entry: permanently enables the third `WardenArm` and tightens Lockdown
  volley's cooldown (`lockdownCooldown` 9s → `lockdownCooldownPhase2` 6s)
- `bulletDamage`: 1 — reference unit only, multiplied by contact damage and
  shockwave damage, same convention as Marauder's/Halcyon's `bulletDamage`
- Body contact: `bodyContactDamageMultiplier` 2×, per-target cooldown gate
  (`contactDamageCooldown`, 1s) — same shape and numbers as
  `MarauderBoss.ApplyContactDamage`
- `OnPhase2`, `OnDefeated` UnityEvents (persistent Inspector listeners,
  matching project convention)
- Implements `IBoss` (already defined, see `architecture.md`'s
  "Boss-type-agnostic orchestration: IBoss"): `SetVisible(bool)`,
  `ApplyContactDamage(GameObject)`. Warden is this interface's third
  implementer — no changes to `IBoss` itself or its consumers
  (`LevelSequencer`/`PlayerController`/`AIController`/`PartySetupBootstrap`)
  are needed, since they're already boss-type-agnostic from Halcyon's work.

## WardenMovement.cs — erratic movement

Sibling component. Reimplements Marauder's dash-or-hold pattern (snap to a
side, bounded advance toward the ships within roughly the top portion of the
playable height, retreat, return to center, pause, repeat mirrored) — cannot
reuse `MarauderBossMovement` directly, since that class is a helper owned by
`MarauderBoss`, not a shared utility; this is an independent reimplementation
of the same idiom, with `MarauderBossMovement`'s exact field values (advance
distance/speed, pause duration) copied verbatim as the starting point. A
later tuning pass may slow it down if playtesting the arms/volley together
shows the fight is over-tuned at Marauder's exact pace, but that's a tuning
call for after first implementation, not part of this spec.

## WardenArm.cs — reusable turret-arm component

Sibling-of-a-different-kind: not one mechanic on the boss GameObject, but one
reusable component with multiple instances (two active from `BossCombat`, a
third added by Phase 2), each on its own child GameObject tracking the
boss's live position plus a fixed offset (same idiom as `Minion`'s
boss-flank tracking).

State cycle:

1. **Idle** — counts down `idleCooldown` (4s), rolled with ±1s random jitter
   per arm instance so the two/three arms don't fall into visible sync.
2. **Telegraph** (`telegraphTime`, 0.5s) — a visible tint/flash tell before
   the stream starts.
3. **Firing** (`firingDuration`, 3s) — continuous stream at `fireInterval`
   (0.15s between shots), aimed at its currently-locked target, re-aiming
   every shot since the target moves. Reuses the existing straight-line
   `Bullet.Init()` path, no new bullet type.
4. Re-picks its target: builds a weighted candidate list over currently-living
   ships (weight 1 each), then if `PlayerAbility.OnTaunt`'s effect window is
   active for some ship, that ship's weight becomes `tauntWeightMultiplier`
   (3) instead of 1, and `Random.Range` picks against the weighted total — a
   bias, not a hard override, since the only ship a Taunt window can
   guarantee redirection to is Marauder's aggro target, not Warden's.
   Returns to Idle.

Public API: `CurrentTargetRole` (the locked target's `PlayerRole`, or
`null`/inactive) — read by `WardenBossPanelUI` for a per-arm warning line,
same "HUD only reads" convention every boss panel already follows.

Two `WardenArm` child GameObjects are active from scene start (enabled at
`BossCombat` alongside the rest of Warden's siblings); a third exists from
scene setup, starts disabled, and `WardenBoss.OnPhase2` enables it
permanently — matching how every existing phase-2 escalation in this project
(Marauder's fire-rate/spread, Halcyon's roam-speed/pulse-cooldown) is a
one-way switch, not something that cycles on and off.

## WardenShockwave.cs — proximity shockwave

Sibling component, Marauder's telegraphed-ring/knockback mechanic reused
with the same numbers and formula (3× bullet damage, ~1.5 ship-widths
radius, same knockback-via-recoil-decay math Marauder's already uses) — a
direct port for consistency, the same way `HalcyonBoss.cs` ported body
contact damage.

## WardenLockdownVolley.cs — the signature mechanic

Sibling component. On `lockdownCooldown` (9s phase 1, `lockdownCooldownPhase2`
6s) with `telegraphTime` (1s — longer than Pattern Barrage's 0.7s, since the
wall is meant to be *seen coming and blocked*, not reacted to on instinct
like a barrage):

- Picks a random arena edge (top, left, or right), equal probability.
- Spawns `wallBulletCount` (12) bullets evenly spaced across that edge, all
  with the same direction of travel (inward, perpendicular to the chosen
  edge), with `wallGapCount` (2) evenly-distributed single-bullet-width gaps
  removed from the row so it isn't fully unbroken.
- Reuses the existing `Bullet.Init(Vector2 dir)` path — already used by
  Pattern Barrage's Fan/Ring/Spiral for non-vertical bullet travel, so a
  left/right-originating wall needs no new bullet capability.

A ship standing in the wall's path takes a hit per bullet that reaches it.
Tank's Shield Arc — already a wide trigger collider that absorbs hits into
Tank's own shield/health, per `player-roles.md`'s "PlayerAbility.cs" — blocks
whichever lanes of the wall cross its width for free, with zero changes
needed to `PlayerAbility.cs` itself. This is the mechanic that gives Shield
Arc's width a reason to matter, mirroring how Halcyon's Static Field gives
Support's Speed Boost a reason to matter.

## WardenBossPanelUI.cs

**Attached to:** `Level3.unity`'s `BossPanel`, replacing whatever currently
drives it for the placeholder boss. Same "HUD only reads, never owns game
state" pattern as `BossPanelUI.cs`/`HalcyonBossPanelUI.cs`:

- HP bar + `"HP: x/y"` text
- Phase text (`"Phase 1"`/`"Phase 2"`, `"DEFEATED"` on `OnDefeated`)
- One warning line per arm, naming its current target's role (`"Arm A:
  {role}"`, `"Arm A: —"` while that arm is inactive — relevant once the
  third arm exists pre-Phase-2) — same "name the threatened role" convention
  Marauder's Guided Missile warning already uses
- Shockwave cooldown text (`"Shockwave: {n}s"` / `"Ready"`)
- Lockdown-volley warning (names the incoming edge during telegraph) +
  cooldown text

No single target/aggro text — there's no single-target concept in this
fight, same reasoning `HalcyonBossPanelUI` already applies.

## Cross-boss contract: IBoss

Already defined (see `docs/architecture.md`'s "Boss-type-agnostic
orchestration: IBoss" and `halcyon-boss-design.md`'s equivalent section).
Warden implements it as its third concrete type. No changes to the
interface, `LevelSequencer`, `PlayerController`, `AIController`, or
`PartySetupBootstrap`'s existing `bossObject`/cast-to-`IBoss` plumbing are
needed — that generalization was already done for Halcyon.

`AIController.UpdateAbilityUsage()`'s Tank auto-taunt heuristic currently
casts `bossObject as MarauderBoss` and only taunts when not already holding
aggro, no-opping entirely when the cast is `null` (Halcyon's no-aggro case).
Against Warden, Taunt has a real, always-beneficial effect (biases arm
re-picks toward the Tank) with no "already holding aggro" concept to gate
on, so the heuristic gains a third branch: cast to `WardenBoss` and, if that
succeeds, taunt on the same "fire the instant it's off cooldown" placeholder
trigger every other AI ability already uses by default (e.g. Medic's aura
boost) — no need-awareness, consistent with this project's existing
first-pass AI triggers.

## Scene wiring

- `Level3.unity`'s existing `Boss` GameObject (currently carrying
  `Level3BossPlaceholder.prefab`'s `MarauderBoss`-typed components) gets
  `WardenBoss`/`WardenMovement`/`WardenShockwave`/`WardenLockdownVolley`
  built in place, plus two `WardenArm`-carrying child GameObjects (a third
  added disabled), same one-off-per-level treatment Marauder and Halcyon
  both get — built directly on the scene object, not a separate reusable
  prefab asset.
- `Level3.unity`'s `LevelSequencer`/`PartySetupBootstrap` and all 4 ships'
  `PlayerController`/`AIController` have `bossObject` dragged to this `Boss`
  instance, same as `Level1`/`Level2`.
- `Level3.unity`'s `BossPanel` swaps its script to `WardenBossPanelUI`,
  wired to the new boss.
- `PartySetupBootstrap` needs a new field (e.g. `wardenArms`, the 2-3
  `WardenArm` components) assigned the *live* spawned roster on
  `SpawnDynamicParty()`, the same fix `halcyonStaticField.ships` needed and
  `MarauderBoss.targets` needed before that (`marauder-boss.md`'s "Aggro
  roster comes from `targets[]`") — without it, arms would only ever see the
  4 legacy scene-placed ships (deactivated on the normal co-op join route),
  never a real target. This should be built in from the start, not
  discovered live as a bug the way it was for both prior bosses.
- Any `OnTaunt` persistent listeners left over from `Level3BossPlaceholder`
  (a `MarauderBoss`-typed prefab) pointing at the now-removed component
  should be checked for and removed on all 4 ships, same cleanup Halcyon's
  build required.

## Out of scope

Minions (`MinionSpawner`), Pattern Barrage, phase-based ambient fire beyond
the arms, and any single-target aggro/threat table — deliberately absent,
not deferred. Warden's identity is the arms + Lockdown volley + carried-over
contact damage/shockwave, full stop.
