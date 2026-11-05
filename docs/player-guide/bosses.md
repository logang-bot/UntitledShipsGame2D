# Bosses

Three bosses, one per level. Each enters after the waves, has two phases
(phase 2 starts at half health and is harder), and is beaten by bringing its
HP to 0. Touching any boss costs a ship 2× a bullet's damage. The right rail
warns about everything below before it lands; see
[the-fight.md](the-fight.md)'s "Reading the HUD".

How they're built: [Marauder](../systems/bosses/marauder-boss.md),
[Halcyon](../systems/bosses/halcyon-boss.md),
[Warden](../systems/bosses/warden-boss.md).

## Level 1 — Marauder (magenta) · RAID

**90 HP.** The classic raid boss: it picks a target, and the party's job is to
keep that target the Tank.

- **Aimed fire** at whoever holds its aggro, every 1.2 s. Phase 2 fires twice
  as often, as a 3-bullet spread.
- **Aggro:** the boss targets whoever has dealt it the most damage. The right
  rail shows who. The Tank's **Taunt** steals the target (`TAUNTED`).
- **Shockwave:** the dim ring around the boss. It brightens as it winds up,
  then pulses. Anyone inside takes 3× damage and is thrown far away. Don't
  linger close.
- **Guided missile:** every few seconds it locks onto the **Medic or the
  Attacker** (the warning bar names who), then fires a missile that curves
  after them. The Tank can block it, but only by actively cutting across its
  path.
- **Pattern Barrage:** about every 7 s, telegraphed and named on the rail: a
  wide aimed **Fan**, a full 360° **Ring**, or a rotating **Spiral**. Never the
  same shape twice in a row.
- **Phase 2:** the enemy waves come back and keep coming until the boss dies.
- **Right now** Marauder stands still at its home position, and its flanking
  minions are switched off (both are tuning choices, not missing features).

**How to win:** Tank taunts and holds the boss's fire with the shield arc,
the Attacker stays lined up underneath, and everyone keeps out of the
shockwave ring.

## Level 2 — Halcyon (yellow) · POSITIONING

**110 HP.** No bullets at all. It roams the whole arena at a speed that keeps
changing, and its damage comes from a pulse that punishes standing together.

- **Roaming:** waypoint to waypoint across the whole playfield. Faster, with
  almost no pauses, in phase 2.
- **Opening:** every 8 s it stops for **2 s**. The model shows chevrons, then
  splits open with a weak-point reticle, and the rail flashes `OPENING!`, a
  second before. It's the easiest time to land hits, since the boss is still.
- **Static Field:** a pulse every 6 s (every 4 s in phase 2). The ring around
  the boss shows its reach and brightens just before it fires. Every ship
  inside takes damage: **1 for a ship on its own, a lot more for ships close
  to each other.**
- **Taunt does nothing** here: Halcyon has no target.

**How to win:** spread out, keep away from the ring, and pile damage into each
Opening. Support's Speed Boost helps everyone reach the boss in time.

## Level 3 — Warden (blue) · COVERAGE

**130 HP**, but it **can't be hurt while any of its arms is up**
(`SHIELDED`). The slowest boss, with the densest fire.

- **Turret arms:** two arms (a third joins in phase 2). Each one picks a random
  living ship, locks on (half a second's warning, shown on its arm card), and
  fires a stream at it for 3 s, then rests and picks again. Several ships are
  under fire at once.
- **Breaking the shield:** an arm goes **DOWN** after 20 damage: it turns grey,
  stops firing and can't be hit for 15 s (its card counts down). Once **every**
  arm is down, the boss takes damage until the first arm comes back.
- **Taunt** makes the arms' next few lock-ons lean toward the Tank (likelier,
  not forced).
- **Lockdown volley:** every 9 s (6 s in phase 2), after a 1 s warning, a wall
  of bullets sweeps in from the top, left or right edge with a couple of gaps.
  Easier to hide behind the Tank's shield arc than to dodge.
- **Big Shot:** every ~8 s (~6 s in phase 2) it charges for 0.6 s (white core,
  a lane on the model), then fires a wide 2-damage shot straight down.
- **Shockwave:** a ring like Marauder's: 3× damage and knockback if you're
  close.

**How to win:** focus one arm at a time until all are down, then everyone
turns on the body. Shelter behind the Tank during Lockdown. CPU teammates
already focus the weakest arm while the boss is shielded.
