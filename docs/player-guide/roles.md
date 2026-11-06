# Roles

Every ship plays one of four roles. A role sets the ship's stats, a passive
and one ability (E / gamepad X). Roles nobody picks are flown by CPU
teammates, so every fight has all four. How roles are built:
[../systems/player-roles.md](../systems/player-roles.md).

## At a glance

| Role | Ship | HP | Shield | Damage per shot | Fire rate | Speed | Ability |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **Attacker** | NOVA-11 | 6 | 5 | 2.0 | 2.5/s | 2.4 | None **(this branch)**: level-ups instead |
| **Tank** | AEGIS-04 | 8 | 20 | 1.0 | 1/s | 1.2 | Taunt |
| **Medic** | LUMEN-02 | 4 | 3 | 0.7 | 1.5/s | 2.4 | Aura Boost |
| **Support** | ECHO-07 | 5 | 3 | 1.0 | 2/s | 3.6 | Speed Boost |

Speed is in world units per second. On paper the Attacker deals more than half
the party's damage on its own (5 of about 9 damage per second, if every shot
hits).

## Attacker — NOVA-11 (orange)

**Job:** damage. The hardest-hitting regular fire in the party, on a fragile
ship.

- **(this branch)** No manual attack: no 1-2-3 combo and no Big Shot (E does
  nothing). A human Attacker levels up from party kills and picks upgrades:
  Attack Damage, Crit Strike, Multishot. See
  [the-fight.md](the-fight.md)'s "Level-ups".
- **On `main`:** a 1-2-3 combo on keys 1/2/3 and Big Shot on E (a wide,
  double-damage shot with recoil).
- **How to play:** stay under the boss. Shots only go straight up, so lining
  up horizontally is your damage. Marauder's guided missile can lock onto you.

## Tank — AEGIS-04 (cyan)

**Job:** take the hits meant for others. Slowest ship, biggest shield.

- **Passive — Shield Arc:** a wide curved arc always above the ship. It blocks
  any bullet crossing its width, not just ones aimed at the Tank, but each
  blocked bullet drains the Tank's own shield, then HP.
- **Ability — Taunt** (5 s cooldown):
  - **Marauder:** the boss switches its target to you (the aggro line on the
    right rail shows `TAUNTED`).
  - **Warden:** the arms' next few lock-ons lean toward you (not forced).
  - **Halcyon:** no effect (Halcyon has no target).
- **How to play:** park the arc between the boss and your squad. Against
  Warden's Lockdown wall, let the squad shelter behind you.

## Medic — LUMEN-02 (lime)

**Job:** keep everyone alive. The only source of shield refills in the game.

- **Passive — Aura:** a thin ring around the ship. Any other ship inside it
  (human or CPU) regains 1 HP and 1 shield every second, and flashes green when
  it helps. The Medic doesn't heal itself. The ring is small by default, so
  allies have to come close.
- **Ability — Aura Boost** (4 s, 10 s cooldown): the ring grows to three times
  the size and heals four times as fast.
- **How to play:** stay behind the party and duck in to whoever is hurt. Watch
  out for Marauder's guided missile, which can target you, and Halcyon's
  Static Field, which punishes the bunching that healing invites.

## Support — ECHO-07 (purple)

**Job:** make the whole party faster. The fastest ship.

- **Ability — Speed Boost** (4 s, 15 s cooldown): every ship in the party, CPUs
  included, moves **and** fires 1.5× faster. A purple ring shows on every
  boosted ship, its engine flames burn longer and brighter, and the boosted
  stats on the left rail turn light purple with a `+`.
- **How to play:** time the boost for the boss's damage windows (Halcyon's
  Opening, Warden's arms all down) or to get everyone out of a barrage.
