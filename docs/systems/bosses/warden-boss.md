# Warden

**Design spec, not yet built.** Level 3's boss. This doc records the design
pitch discussed before implementation starts, following the same "At a
Glance" shape as [marauder-boss.md](marauder-boss.md) — but where that doc
describes code that exists (exact field names, tuning values, verification
notes), this one describes *intent*. Treat every mechanic below as a
starting point for Warden's own design/implementation session, not a
finished spec ready to build blind. See the "Open design questions" section
for what that session still needs to decide.

Currently, `Level3.unity`'s boss is `Level3BossPlaceholder.prefab` — a
plain duplicate of [Marauder](marauder-boss.md), mechanically identical to
it. Nothing below exists in code yet.

## At a Glance

- **Identity:** a dual-threat, Tank-coordination check. Every existing boss
  mechanic ultimately reduces to "one target, everyone reacts to it" —
  Marauder's aggro table always has exactly one `CurrentTarget`, and its
  Guided Missile locks exactly one role. Warden is the first fight where two
  threats exist genuinely at once, so Tank's taunt and Shield Arc alone
  can't cover both.
- **Movement:** undecided (see Open design questions) — could stay
  stationary like Marauder, or move; not part of the core pitch either way.
- **Mechanics (pitch, not final):**
  - **Dual turret-arms** — two attack sources that independently lock onto
    and fire at two *different* ships at the same time, instead of both
    threats routing through the single-target aggro model Marauder uses.
    The party has to physically cover two lanes, not just funnel everything
    onto Tank.
  - **Lockdown volley** — a telegraphed, wide simultaneous multi-lane
    barrage that specifically rewards Shield Arc's width (see
    [player-roles.md](../player-roles.md)'s "PlayerAbility.cs") over ordinary
    dodging — blocking several lanes at once is the point, not evasion.
  - **Phase 2 escalation** — briefly adds a third arm, forcing the party to
    consciously split attention further rather than just repeating the
    Phase 1 pattern at a higher tempo.
- **Level summary (once built):** reached via `LevelSelect` → `Level3.unity`,
  which will run the same shared `LevelSequencer` timeline every level uses
  (see [level-sequencing.md](../level-sequencing.md)) — no sequencing changes
  anticipated, only the boss instance and its tuning differ from Marauder.

## Design rationale

Tank's kit (Taunt, Shield Arc) is currently only ever tested against a
single threat stream — Marauder's aggro table, its contact damage, and its
shockwave all resolve to "stand between the boss and whoever it's
targeting." Warden is meant to be the fight where that single-threat
assumption breaks: two simultaneous locked targets means Tank's positioning
and Taunt timing both have to be genuinely good, not just present. The
Lockdown volley gives Shield Arc's *width* (as opposed to Tank's body
alone) a reason to matter, mirroring how Halcyon is meant to give Support's
Speed Boost a reason to matter.

## Open design questions

- How each arm picks its target — its own independent aggro/threat table
  per arm, a role-targeted model like Marauder's Guided Missile, or
  something else?
- Does Warden keep Marauder's single-target aggro/taunt model running
  underneath the dual-arm system (e.g. Taunt still redirects *one* arm), or
  does the dual-arm system replace aggro/taunt entirely for this fight?
- Are the two (later three) arms separate sub-objects with their own
  timers/state — mirroring how `MinionSpawner` is a sibling component on
  the boss GameObject (see [marauder-boss.md](marauder-boss.md)'s
  "Minion.cs / MinionSpawner.cs") — or logic inside one main boss
  component?
- Movement — stationary, or does it move? Not decided; the core pitch
  doesn't depend on either answer.
- Lockdown volley's exact geometry, telegraph time, and cooldown.
- HP and other tuning numbers, and whether Marauder's other mechanics
  (contact damage, shockwave, minions, Pattern Barrage) carry over.

## Not yet built

Everything above. `Level3BossPlaceholder.prefab` (a `MarauderBoss`-typed
placeholder) is what actually runs in `Level3.unity` today — see
[marauder-boss.md](marauder-boss.md) and [scene-flow.md](../scene-flow.md).
