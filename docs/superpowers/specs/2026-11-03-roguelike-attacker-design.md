# Roguelike Attacker Progression — Design Spec

First test of an in-fight roguelike layer, on the Attacker role only. Written
on 2026-11-03 from the developer's brief, before any code. All numbers are
test values, chosen so the loop can be felt quickly.

**Status: approved and built.** Items marked **(proposed)** were not in the brief
and were accepted by the developer. See
[roguelike-progression.md](../../systems/roguelike-progression.md) for the current
build.

## The loop

Kill mobs → earn XP → level up → the game stops → pick 1 of 3 cards → the
Attacker gets stronger → back into the fight.

## XP and levels

- Every mob killed gives **1 XP**. Mobs are wave enemies and Marauder's
  minions. Bosses and boss arms give none.
- There are **5 level-ups**, needing 1, 2, 3, 4 and 5 XP in turn. That is
  **15 XP in total**.
- After the last level-up, XP stops and the bar shows MAX.
- **(proposed)** A kill by any ship counts, including AI teammates and a ship
  ramming a mob. Only the Attacker receives the XP.
- **(proposed)** A mob that drifts off the bottom of the screen gives nothing.
- **(proposed)** Level and upgrades reset on every new fight or Restart.

### Is 15 XP reachable?

The pre-boss phase spawns 4 waves of 5, so 20 mobs. 15 XP needs 75% of them
killed. With party-wide credit the Attacker should finish most or all of its
levels before the boss. If only the Attacker's own kills counted, AI teammates
would take many of those kills and level-ups would come far later.

Halcyon and Warden have no minions, so waves are their only XP source. Marauder
also has minions during the fight.

## Level-up screen

- Three cards appear and **the game freezes** until one is picked, as it does
  in the pause menu. Reading a card mid-curtain would get the player killed.
- **(proposed)** Pick with the mouse, keys 1/2/3 or gamepad navigation.
- **(proposed)** If two level-ups arrive together, the screens show one after
  the other.
- **(proposed)** All three cards are always offered and every pick stacks. This
  keeps "3 options" true across all 5 level-ups with only 3 cards in the pool.

## The cards

Base Attacker: 2 damage per shot, 2.5 shots per second (one shot every 0.4s).

| Card | First pick | Each further pick (proposed) |
|---|---|---|
| **Attack Damage** | +1 damage | +1 damage |
| **Crit Strike** | Unlocks the Crit Strike spell | +100% crit bonus |
| **Multishot** | Default attack fires 3 shots in a cone | +2 shots |

### Attack Damage

Raises the base damage that the default attack and Crit Strike both use.

**(proposed)** The bonus is a flat whole number because wave enemies and minions
round damage to whole numbers. A +25% card would turn 2 into 2.5, which rounds
back to 2 and does nothing against them.

### Crit Strike

- Fires by itself **every 3 seconds** once unlocked.
- Hits for attack damage **plus** a crit bonus, so it scales with Attack Damage.
- **(proposed)** The crit always lands, and the bonus starts at +100% of attack
  damage. With no other upgrades that is 2 + 2 = 4 damage.
- It is a single straight **red** shot, wider than a normal one so a crit is easy
  to see. Multishot does not affect it.

### Multishot

- Replaces the default attack with 3 shots in a cone: straight up and
  **(proposed)** 15° to each side.
- **(proposed)** Every shot deals full attack damage.
- The cone rewards position. Up close all 3 can hit a large boss, while at range
  the side shots can miss. That is a real choice against plain damage, but it may
  also be the strongest card, so watch it in playtest.

## Attacking becomes automatic

- The Fire input (Space, left mouse, gamepad South) is **removed**. Ships fire
  whenever they aren't frozen in an intro or boss entrance.
- **(proposed)** This applies to **all roles**. The input is shared, so a human
  Tank, Medic or Support would otherwise have no way to shoot.
- The player's whole job in a fight becomes positioning and dodging, plus any
  manual abilities that remain (see Open questions).

## Cooldown bar

- A small row of icons sits **at the bottom centre of the playfield**, about 40px
  each and semi-transparent, so bullets and the ship stay readable behind it.
- Each icon has a sweep showing the time left, and it **flashes when that attack
  fires**, which shows which attack was just used.
- Icons: **Attack** from the start, and **Crit Strike** once unlocked.
- Attack cycles every 0.4s, so it reads more as a pulse than a timer. That is
  acceptable, but check in playtest that it isn't distracting.
- A thin XP bar and level number sit directly above the icons.
- Icons are placeholder coloured squares with a letter until final art.

## Scope

**In:** human Attacker progression, the three cards, auto-fire, the cooldown bar,
the XP bar.

**Out for this branch:**
- Progression for Tank, Medic and Support, which show no XP bar.
- **(proposed)** Progression for an AI Attacker. It stays at base power, so
  fights without a human Attacker play as they do today, apart from auto-fire.
- Rarity, unlocks, card art, and persistence between fights.
- Balance: bosses are not retuned for the extra Attacker power yet.

## Effect on existing design decisions

This branch tests a direction that differs from what the project has logged:

- `AGENTS.md` says **"Run structure: pre-fight loadout screen"**, with roguelike
  runs deferred. This feature picks upgrades during the fight.
- `docs/systems/cards.md` is built on "you never read a card mid-curtain" and on
  energy earned from grazing. The freeze keeps the first rule. Level-up cards are
  paid for by kills, not grazing, so they don't test the grazing economy.
- Removing Fire means the Attacker has little left to press. `docs/systems/cards.md`
  plans a 3-key hotbar, so this needs revisiting if the hotbar is ever built.

These docs stay unchanged until playtesting shows whether this direction wins.

## Resolved questions

1. **Combo and Big Shot:** the combo is removed from this branch and stays on `main`
   and `feature/boss-tuning-attacker-combo`. Big Shot is switched off, not removed.
2. **XP credit:** party-wide kills, all going to the Attacker. Other roles may earn
   XP later.
3. **Repeat picks:** every card stacks.

## How we'll know it works

Pick Attacker, play a fight on Marauder, and check:

- Space does nothing and the ship fires by itself.
- Mobs fill the XP bar at the 1/2/3/4/5 thresholds.
- Each level-up freezes the game and shows 3 cards, and picking one resumes it.
- Each card visibly changes combat, and Crit Strike fires every 3 seconds.
- The cooldown bar shows attacks firing and doesn't block play.

The feel questions are for the developer: are level-ups exciting, are the choices
real, and does the game stay fun without a fire button?
