# Halcyon

**Design spec, not yet built.** Level 2's boss. This doc records the design
pitch discussed before implementation starts, following the same "At a
Glance" shape as [marauder-boss.md](marauder-boss.md) — but where that doc
describes code that exists (exact field names, tuning values, verification
notes), this one describes *intent*. Treat every mechanic below as a
starting point for Halcyon's own design/implementation session, not a
finished spec ready to build blind. See the "Open design questions" section
for what that session still needs to decide.

Currently, `Level2.unity`'s boss is `Level2BossPlaceholder.prefab` — a
plain duplicate of [Marauder](marauder-boss.md), mechanically identical to
it. Nothing below exists in code yet.

## At a Glance

- **Identity:** a mobility check — the first fight where standing still is
  the losing strategy. Unlike Marauder (stationary-but-erratic, confined
  near its home position), Halcyon never really stops moving across the
  *whole* arena, so a party that doesn't reposition falls out of its own
  straight-up firing lane.
- **Movement:** continuous roaming across the full playable area (not
  Marauder's ~40%-of-height-near-home limit) — the specific path/pattern is
  undecided (see Open design questions).
- **Mechanics (pitch, not final):**
  - **Full-arena roam** — the core mobility check. Makes Support's
    party-wide Speed Boost (see [player-roles.md](../player-roles.md)'s
    "PlayerAbility.cs") meaningfully valuable for the first time — no
    existing boss demands sustained repositioning the way this would.
  - **Surge window** — periodically, Halcyon briefly stops and becomes
    vulnerable (a telegraphed, stationary window), rewarding a coordinated
    burst: Attacker's 3-attack combo finisher, ideally stacked with
    Support's fire-rate boost.
  - **Static Field pulse** — deals bonus damage to any two ships caught
    standing too close together. Creates real tension against Medic's
    proximity aura (see [player-roles.md](../player-roles.md)'s "PlayerAbility.cs"),
    which wants allies close, and against Support's own positioning near
    whoever it's boosting — clustering to heal or coordinate becomes a
    liability during this fight specifically.
- **Level summary (once built):** reached via `LevelSelect` → `Level2.unity`,
  which will run the same shared `LevelSequencer` timeline every level uses
  (see [level-sequencing.md](../level-sequencing.md)) — no sequencing changes
  anticipated, only the boss instance and its tuning differ from Marauder.

## Design rationale

Marauder already exercises phase-based fire, body contact damage, a
proximity shockwave, a role-targeted homing shot, geometric bullet
patterns, kamikaze sub-enemies, and single-target aggro/taunt — i.e.
everything except a real mobility demand. Support's Speed Boost currently
has no fight that makes it clearly worth using over just re-triggering it
on cooldown; Halcyon is meant to be that fight. The Static Field pulse
exists specifically to create a genuine trade-off against Medic's healing
range, rather than "spread out" being a strictly dominant strategy with no
cost.

## Open design questions

- Exact roam path/pattern — continuous wander, waypoint-to-waypoint like
  Marauder's M-pattern but arena-wide, or something else entirely?
- Surge frequency, duration, and telegraph — how predictable should the
  vulnerable window be?
- Static Field's trigger radius, damage, and cooldown.
- Does Halcyon keep any of Marauder's other mechanics (body contact damage,
  minions, Pattern Barrage, guided missile) or is its attack repertoire
  entirely new? Currently undecided — the pitch above only covers what's
  *new*, not what carries over.
- Does it fire bullets at all during normal movement, or does the challenge
  live entirely in position/timing (dodging Static Field, catching Surge
  windows)?
- HP and other tuning numbers.
- Aggro/targeting model — same single-target threat table as Marauder, or
  something that interacts with the roam pattern?

## Not yet built

Everything above. `Level2BossPlaceholder.prefab` (a `MarauderBoss`-typed
placeholder) is what actually runs in `Level2.unity` today — see
[marauder-boss.md](marauder-boss.md) and [scene-flow.md](../scene-flow.md).
