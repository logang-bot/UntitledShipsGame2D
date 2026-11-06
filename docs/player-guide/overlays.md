# Overlays

Screens that appear over a running fight. All of them darken the whole
screen; the level-up screen keeps your Attacker frame lit. None of them can
open on top of another.

More detail: [../systems/hud-layout.md](../systems/hud-layout.md)'s
"Overlays" (layout and looks), [../systems/scene-flow.md](../systems/scene-flow.md)
(Pause, Restart and the scenes each button loads) and
[../systems/combat.md](../systems/combat.md) (when Game Over and Victory
trigger).

## Pause

**Opens with** Esc or gamepad Start, from any joined player (pause is shared).
The fight freezes completely: ships, bullets, the boss and every timer.

**On screen:** the boss and kind as a kicker, `PAUSED`, and four buttons.

| Button | What it does |
| --- | --- |
| **RESUME** | Back to the fight exactly where it stopped. Esc / Start again does the same |
| **RESTART** | Replays the same boss with the same party and roles, from the intro |
| **CHANGE ROLES** | Back to Role Select with the same players |
| **QUIT TO MAIN MENU** | Ends the run |

Move with ↑ / ↓ (or D-pad), select with Enter / A, or click.

Pause doesn't open during a level-up, or over Game Over or Victory.

> **Known gap:** the hint bar says `B · RESUME`, but gamepad B isn't wired.
> Resume with Start, Esc or the RESUME button.

## Level-up (this branch)

**Opens when** the human Attacker completes a level (see
[the-fight.md](the-fight.md)'s "Level-ups"). The fight freezes and the whole
screen darkens behind the cards, except your Attacker frame, so you can read
your current upgrades while choosing. The pause hint reads `PAUSE · LOCKED`.

**On screen:** `LEVEL UP` and the pilot (`NOVA-11 · P1`), the level change
(`LV 2 → 3`), and either `FIGHT FROZEN · CHOOSE ONE` or, when more picks are
waiting, `PICK 1 / 2` and `+1 MORE PICK AFTER THIS`. Then three cards, each
with its key, icon, name, what it does, and the number it changes
(`DAMAGE 3 → 4`). A `NEW` chip marks a card's first pick; five pips show how
many times it was taken.

| Action | Keyboard & mouse | Gamepad |
| --- | --- | --- |
| Pick directly | 1 / 2 / 3, or click a card | — |
| Move focus | ↑ / ↓, or hover | D-pad / stick |
| Pick the focused card | Enter | A |

For the first 0.3 s the cards are greyed out under `INPUT LOCKED`: presses
from the fight are ignored so you can't pick a card you haven't read. Then
card 1 takes focus. The picked card turns orange for a quarter of a second,
and either the next waiting level-up opens or the fight resumes. Esc does
nothing here.

Rules and numbers: [../systems/roguelike-progression.md](../systems/roguelike-progression.md).

## Game Over

**Opens when** every human ship is down. CPU teammates may still be alive and
fighting behind it: the backdrop is lighter so you can watch them, and a
`CPU STILL FIGHTING` box lists who.

**On screen:** `GAME OVER`, `ALL PILOTS DOWN`.

| Button | What it does |
| --- | --- |
| **RESTART** | Same boss, same party and roles |
| **CHANGE ROLES** | Back to Role Select |

Move with ← / → (or D-pad), select with Enter / A, or click. If the CPUs then
beat the boss, Victory does not appear on top: the first end screen wins.

## Victory

**Opens when** the boss dies.

**On screen:** `VICTORY` in the boss's colour, `<BOSS> DEFEATED`, and a summary
of every ship: name, pilot, total damage, DPS and share of the party's damage.

| Button | What it does |
| --- | --- |
| **RESTART** | Fight the same boss again with the same party |
| **CHANGE ROLES** | Back to Role Select |

Move with ← / → (or D-pad), select with Enter / A, or click.
