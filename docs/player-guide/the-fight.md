# The Fight

Everything that happens after Level Select launches a boss. The playfield is a
fixed portrait strip in the middle of the screen; on PC the bars either side
are the HUD rails: the **squad on the left, the boss on the right**.

## Controls

| Action | Keyboard & mouse | Gamepad |
| --- | --- | --- |
| Move | Arrow keys | Left stick |
| Fire | Automatic, always on **(this branch)** | Automatic |
| Role ability | E | X (West) |
| Trigger a CPU teammate's ability | Click its ability cell on the left rail | — |
| Pause / resume | Esc | Start |
| Pick a level-up card **(this branch)** | 1 / 2 / 3, or click | D-pad + A |

Ships never turn: they face up and fire straight up. Every ship, yours and the
CPUs', fires on its own whenever it can move **(this branch; on `main` you hold
Space / left mouse / gamepad A)**. Ships are solid: they push off each other,
off the boss and off enemies.

More detail: [../systems/movement.md](../systems/movement.md) (movement,
collision) and [../systems/input.md](../systems/input.md) (how the controls are
wired).

## How a level runs

| Step | How long | What you can do |
| --- | --- | --- |
| **Intro** | ~4 s | Nothing. The four ships glide up from below into a line |
| **Free movement** | ~4 s | Move. No enemies yet |
| **Waves** | 15 s of spawning, then until the screen is clear | Fight 4 waves of 5 enemies. A callout announces the wave |
| **Boss entrance** | ~4 s | Nothing. Ships freeze while the boss glides down. A callout names the boss |
| **Boss fight** | Until the boss or the party falls | Everything. At half health the boss enters **phase 2** (a callout says so) and gets harder |

The fight ends with **Victory** when the boss dies, or **Game Over** when every
human ship is down. See [overlays.md](overlays.md).

How it is sequenced: [../systems/level-sequencing.md](../systems/level-sequencing.md).

## Reading the HUD

Every part of the HUD, with sizes and states:
[../systems/hud-layout.md](../systems/hud-layout.md).

### Left rail: the squad

`SQUAD` and a count (`1 PILOT · 3 CPU`), then one frame per ship, always in the
order Attacker, Tank, Medic, Support:

- **Who-tag:** `◤ P1` filled is you (your frame also has a bright edge),
  `P2` outlined is another human, `◇ CPU 1` dashed is a CPU teammate. Then the
  ship's name and role.
- **HP** as square pips (amber when low, with a `LOW HP` tag at a third or
  less) and **SH** (shield) as thin segments, with `BROKEN` when the shield is
  empty.
- **SPD**, **FIRE** and **DPS**: speed and fire rate (light purple with a `+`
  while Speed Boost is on), and real damage per second against the boss.
- **Ability cell:** a square sweep around the ability icon. Solid means
  `READY`, filling means the effect is `ACTIVE`, a grey track filling back up
  means `COOLDOWN` with seconds left. On a CPU's frame the cell is a button:
  `CLICK` when ready, `TRIGGER` on hover, `WAIT` while cooling.
- A purple chip appears while Support's Speed Boost is on that ship.
- **DOWN:** a dead ship's frame fades under a hatch with a `DOWN` badge. There
  is no revive.

**(this branch)** The Attacker's frame has no ability cell. It shows an
`ATTACK` cell, a `CRIT` cell once Crit Strike is picked, and an XP row; see
"Level-ups" below.

Under the frames, the **damage meter** lists every ship's damage to the boss:
total, DPS and share. It counts boss damage only, and reads 0 until the boss
fight starts.

### Right rail: the boss

One panel for whichever boss you're fighting:

- **Header:** boss name, phase, an HP bar, and `SHIELDED` while it can't be
  hurt (Warden).
- **Warning bars** for whatever is coming: a missile lock-on (naming who is
  targeted), a barrage (naming its shape), Halcyon's Opening (`OPENING!`),
  Warden's Lockdown and Big Shot. Each counts down to the moment it lands.
- **Aggro:** who the boss is targeting, and `TAUNTED` after a Tank taunt
  (Marauder).
- **Arm cards** (Warden): each arm's health, lock-on and `DOWN` timer.
- **Cooldown tiles** for the boss's other attacks.
- At the bottom: `ESC START PAUSE`.

### On the playfield

Only short **callouts** (wave start, boss entry, phase 2). Bullets are the only
round, white-cored glowing shapes on screen: if it glows round and white, dodge
it.

## Damage and survival

- **Shield first, then HP.** Every hit drains shield before health. Shields
  don't regenerate on their own: only the Medic's aura refills them.
- **Enemy bullets** deal 1 damage.
- **Touching a boss** costs 2× a bullet's damage.
- **Shockwaves** (Marauder, Warden: the ring around the boss) cost 3× and
  knock the ship far away.
- **Halcyon's Static Field** costs 1 to a ship on its own and much more to
  ships bunched together; see [bosses.md](bosses.md).
- A hit flashes the ship and shakes the camera, even if the shield took it all.
- At 0 HP a ship goes **DOWN** for the rest of the fight. CPU teammates keep
  fighting after you fall; the fight is lost when **every human** is down.

More detail: [../systems/combat.md](../systems/combat.md) (health, damage,
Game Over) and [../systems/player-roles.md](../systems/player-roles.md)
(shield).

## Enemies

- **Wave enemies** come down from the top in formations (random, a line, a
  cluster, a V), each with a movement style (sine wave, zig-zag or straight
  dive), and fire straight down. They have 3 HP.
- They are **kamikaze**: touching one hurts your ship and destroys it.
- About 30% are **Explosive** (a distinct hull): killing one by any means
  bursts it into 8 fragments that can hit any nearby ship, including yours.
  Shooting one from a distance isn't automatically safe.
- Enemies that drift off the bottom of the screen just leave.
- Marauder's waves return in phase 2. Marauder also has flanking minions, but
  they are switched off right now.

More detail: [../systems/combat.md](../systems/combat.md) (waves, formations,
Explosive enemies).

## CPU teammates

Any role no human picked is flown by a CPU. They move, dodge and use their
abilities on their own:

| Role | What the CPU does |
| --- | --- |
| Tank | Holds a guard point between the boss and the other CPUs, blocking shots, and taunts when it helps |
| Medic | Hangs back, and flies to whichever ally drops to 55% health or shield. Uses Aura Boost as soon as it's ready |
| Support | Wanders the whole arena |
| Attacker | Patrols under the boss at mid range |

They sidestep bullets heading straight for them. Against Warden, while the boss
is shielded, they all go after the arm with the least health left.

You can force a CPU's ability right now by clicking the ability cell on its
frame (if it's ready).

More detail: the AI teammate sections of
[../systems/bosses/marauder-boss.md](../systems/bosses/marauder-boss.md)
(positioning, dodging, ability use) and
[../systems/bosses/warden-boss.md](../systems/bosses/warden-boss.md) (focusing
the arms).

## Level-ups (this branch)

If a human plays the Attacker, every enemy the party kills gives the Attacker
1 XP. Five level-ups cost 1, 2, 3, 4 and 5 XP. Each one **freezes the fight**
and offers three upgrade cards; picking one makes the Attacker stronger for
the rest of the fight:

| Card | Effect |
| --- | --- |
| **Attack Damage** | +1 damage per shot |
| **Crit Strike** | A vermilion lance fires by itself every 3 s for double damage; more picks add +100% |
| **Multishot** | The attack becomes a cone of 3 shots; more picks add 2 |

The Attacker's frame tracks it all: the `ATTACK` cell (fire rhythm, `×n` shots,
`DMG n`), the `CRIT` cell, and the XP row (`LV n`, XP cells, `n/next XP` or
`MAX`, upgrade chips). A `LEVEL UP` tag shows while a pick is waiting. Progress
lasts one fight.

The pick screen is described in [overlays.md](overlays.md). Exact rules,
numbers and how to play-test it:
[../systems/roguelike-progression.md](../systems/roguelike-progression.md).
