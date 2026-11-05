# Current Playable State

What is built right now, and what isn't. This page is the **status**; it
doesn't explain how to play.

- **How to play, screen by screen** (controls, HUD, roles, bosses, overlays):
  [player-guide/](player-guide/README.md).
- **How each part is built:** [systems/](systems/).
- **What's next:** [roadmap.md](roadmap.md). **How we got here:**
  [progress-log.md](progress-log.md).

Keep this file in sync with `roadmap.md`'s "Implemented" section, and the
player guide in sync with anything a player can see.

> **Branch:** `feature/roguelike-attacker-progression`. Items marked
> **(this branch)** aren't on `main`.

## Built

### Screens and flow

- **Seven scenes:** `MainMenu` (0) → `Lobby` (1) → `JoinLobby` (2) →
  `RoleSelect` (3) → `LevelSelect` (4) → one shared `Level` (5) for all three
  bosses, with `MenuBackground` (6) loaded under the five menu screens. Pause,
  Game Over, Victory and the level-up screen are overlays inside `Level`.
  See [systems/scene-flow.md](systems/scene-flow.md).
- **Menus in the neon style (Session 54, PC):** every screen before a fight
  follows the Claude Design "Pre-Fight Menus" design, on one UI kit and one
  palette. Keyboard, mouse and gamepad all navigate. See
  [systems/ui-kit.md](systems/ui-kit.md).
- **Local co-op, 1–4 players on one machine:** device join screen, a per-player
  role picker for 2+ players, CPU teammates fill the rest. Online is a locked
  "coming later" card. See [systems/player-roles.md](systems/player-roles.md).

### The fight

- **Level timeline:** intro glide, free movement, 15 s of enemy waves, boss
  entrance, boss fight. See [systems/level-sequencing.md](systems/level-sequencing.md).
- **Movement and shooting:** fixed-orientation ships in a 9:16 portrait
  playfield, solid-body collision between ships, boss and enemies. **(this
  branch)** Every ship fires automatically; there is no fire input. See
  [systems/movement.md](systems/movement.md) and
  [systems/combat.md](systems/combat.md).
- **Damage rules:** shield before HP, only the Medic refills shields, DOWN with
  no revive, Game Over when every human is down, Victory when the boss dies.
  See [systems/combat.md](systems/combat.md).
- **Enemies:** wave enemies in four formations with three movement styles,
  kamikaze contact damage, ~30% Explosive. See [systems/combat.md](systems/combat.md).
- **CPU teammates:** per-role positioning, bullet dodging, ability use, and
  click-to-trigger from the human's HUD. See
  [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md).
- **Roles:** Attacker, Tank, Medic and Support with fixed stats; Taunt + Shield
  Arc, Aura + Aura Boost, party-wide Speed Boost. See
  [systems/player-roles.md](systems/player-roles.md).
- **Roguelike Attacker progression (this branch):** XP from party kills, five
  level-ups, a freeze-and-pick screen with three stacking cards (Attack Damage,
  Crit Strike, Multishot). The Attacker's combo is removed and Big Shot is off.
  See [systems/roguelike-progression.md](systems/roguelike-progression.md).

### Bosses

- **Marauder (Level 1):** aggro and taunt, aimed fire, shockwave, guided
  missile, Pattern Barrage, waves again in phase 2. Currently tuned down: it
  doesn't move and its minions are off. See
  [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md).
- **Halcyon (Level 2):** no bullets, full-arena roam, Opening window, Static
  Field pulse. See [systems/bosses/halcyon-boss.md](systems/bosses/halcyon-boss.md).
- **Warden (Level 3):** turret arms that shield the boss, Lockdown volley, Big
  Shot, shockwave. See [systems/bosses/warden-boss.md](systems/bosses/warden-boss.md).

### HUD and art

- **In-level HUD (Session 57, PC):** party frames, damage meter, one shared
  boss panel, playfield callouts, Pause / Game Over / Victory on the menu kit.
  **(this branch)** The Attacker frame's level-up rows and the level-up screen
  (Session 59). See [systems/hud-layout.md](systems/hud-layout.md).
- **Ships and bosses** are the Claude Design 3D models rendered live (Sessions
  51 and 58), each boss fight has its own parallax background (Session 53), and
  the Combat FX sprites are in (Session 55). See [art-roadmap.md](art-roadmap.md).

## What's NOT there yet

- **Online play.** No networked multiplayer until Nakama lands; Lobby's ONLINE
  card is locked.
- **Settings.** There is no Settings / Options screen: controls can't be
  rebound, and there are no audio or display options.
- **Phone layouts.** Menus and HUD are PC only.
- **Audio.** None yet.
- **The deck layer** (grazing, energy, cards) is a design spec only. See
  [systems/cards.md](systems/cards.md).
- **Art still missing:** the Main Menu wordmark is a placeholder, and the
  avatar slot is a placeholder shape. Some imported Combat FX art isn't used
  yet; see [art-roadmap.md](art-roadmap.md)'s "Combat FX imported but not used
  yet". The Combat FX sprites are deliberately not in Sprite Atlases (Session 56).
- **No build.** Everything is tested in the Unity Editor.

## Known issues

- Sometimes a white shape appears in the centre of the screen for the moment a
  hit ship flashes. Intermittent, not yet diagnosed; see
  [roadmap.md](roadmap.md)'s "Known issues".
- The Pause hint bar shows `B · RESUME`, but gamepad B doesn't resume. Use
  Start, Esc or the RESUME button.

## Running it in the Editor

1. Open `Assets/Scenes/MainMenu.unity` (Build Settings index 0) and press Play.
   Then follow the screens; [player-guide/menus.md](player-guide/menus.md)
   explains each one.
2. For quick iteration, open any scene directly and press Play. Menu scenes
   load `MenuBackground` themselves. `Level.unity` opened directly starts
   Level 1 (Marauder) with the roles set on `Player` / `Teammate_*` in the
   Inspector (currently a human Attacker and three CPUs).
3. To reach the boss sooner, lower `LevelSequencer.minionPhase1Duration` in
   `Level.unity`.
4. Menu navigation has to be checked by hand with the Game view focused:
   simulated keyboard input doesn't reach UI actions (see
   [systems/input.md](systems/input.md)'s testing note).
