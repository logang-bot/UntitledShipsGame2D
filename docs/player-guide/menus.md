# Menus

The five screens before a fight, in order. All of them share one background:
a dark grid with the four ships drifting across it, which keeps running while
screens slide in and out. Each screen has a step counter in its header
(`02 / 05`, …) and a hint bar at the bottom listing its keys.

Common to every screen:

| Action | Keyboard & mouse | Gamepad |
| --- | --- | --- |
| Move focus | Arrow keys, or hover | D-pad / left stick |
| Select | Enter, or click | A |
| Back | Esc | B |

Role Select and Level Select open with **nothing highlighted**, so a stray
press from the previous screen can't pick something. The first arrow or D-pad
press highlights the first option. The other screens start with their first
button focused.

How these screens are built: [../systems/scene-flow.md](../systems/scene-flow.md),
[../systems/ui-kit.md](../systems/ui-kit.md).

## Main Menu

**On screen:** the game's wordmark (a placeholder block for now), and two
buttons.

| Button | What it does |
| --- | --- |
| **PLAY** | Goes to the Lobby |
| **QUIT** | Closes the game (in the Editor, stops Play mode) |

## Lobby — choose a mode

**On screen:** two mode cards and a Back button.

| Choice | What it does |
| --- | --- |
| **LOCAL** | Couch co-op on this machine, 1–4 players. Goes to Join Lobby |
| **ONLINE** | Locked, marked **COMING LATER** (there is no online play yet). Keyboard and gamepad focus skip it; clicking it only shows a notice |
| **BACK** (Esc / B) | Main Menu |

## Join Lobby — who is playing

**On screen:** `JOIN THE SQUAD`, four slots (`PLAYER 1`–`PLAYER 4`), each
`OPEN SLOT` until someone claims it, and an `n / 4` counter.

| Action | How |
| --- | --- |
| **Join** | Keyboard player: press any key or click (keyboard and mouse together are one player). Gamepad player: press any button on that gamepad |
| **Leave** | Esc on the keyboard, or B on that gamepad. Slots to the right shift left |
| **Continue** | Enter or Start from a joined device, or click **CONTINUE**. Needs at least one player |
| **Back to Lobby** | Esc / B while **nobody** has joined, or the Back button. With players joined, Esc / B only makes that player leave, so a stray press can't pull the squad back |

Each slot shows its player number and device (`KEYBOARD + MOUSE`, or a
gamepad number). Every later screen and the fight listen to each player only
on the device they joined with.

**Solo play:** join with one device and Continue. The other three roles are
flown by CPU teammates.

## Role Select — choose your role

What this screen looks like depends on how many players joined.

### One player

**On screen:** `CHOOSE YOUR ROLE`, four role cards (Attacker, Tank, Medic,
Support), each with its ship spinning live, its name (NOVA-11, AEGIS-04,
LUMEN-02, ECHO-07), its stats on one shared scale (HP, shield, speed, damage,
fire rate) and its ability lines. A squad strip shows the party that will fly.

| Action | How |
| --- | --- |
| Highlight a role | Arrow keys / D-pad, or hover |
| Choose it | Enter / A, or click the card |
| **START** | Enabled once a role is chosen (it shows why when it isn't). Enter or click, or gamepad **Start** from anywhere on the screen |
| **BACK** | Esc / B |

The three roles you don't choose are flown by CPU teammates.

The role cards are summarised in [roles.md](roles.md). **(this branch)** The
Attacker's card reads **LEVEL UP** — "Fires on its own. Kills earn XP; each
level-up adds an upgrade."

### Two or more players

**On screen:** `CHOOSE ROLES`, one row per player. Each player moves their own
highlight with their own device, so everyone picks at the same time.

| Action | Keyboard player | Gamepad player |
| --- | --- | --- |
| Move highlight | ← / → or A / D | D-pad or left stick ← / → |
| **Lock** the role | Enter | A |
| **Unlock** it | Esc | X |
| **START** (all rows locked) | Click START | Start |
| **BACK** | Backspace | B |

Two players can't lock the same role. Roles nobody locks are flown by CPU
teammates.

## Level Select — choose the boss

**On screen:** `SELECT TARGET`, three boss cards with the boss's portrait, its
level, a one-word style, threat pips and your squad's chips.

| Card | Boss | Style |
| --- | --- | --- |
| **LEVEL 01** | Marauder | **RAID** — the classic: aggro, telegraphed attacks, Tank holds the boss |
| **LEVEL 02** | Halcyon | **POSITIONING** — no bullets, a roaming boss and a punishing pulse |
| **LEVEL 03** | Warden | **COVERAGE** — turret arms lock onto several ships at once |

| Action | How |
| --- | --- |
| Highlight a boss | Arrow keys / D-pad, or hover |
| **Launch** | Enter / A, or click the card. The card grows, the screen cuts to black, and the fight loads |
| **BACK** | Esc / B, to Role Select |

What each fight asks of you: [bosses.md](bosses.md).
