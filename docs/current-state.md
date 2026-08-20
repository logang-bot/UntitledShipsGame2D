# Current Playable State

What the game actually does right now, and how to test it. For what's next,
see [roadmap.md](roadmap.md); for the full history of how we got here, see
[progress-log.md](progress-log.md). This file should be kept in sync with
`roadmap.md`'s "Implemented" section — update both when a feature moves from
"In Progress"/"Planned" to done.

## What's playable

- **Ship movement** — a single player ship strafes left/right/up/down within
  a fixed portrait viewport. The ship has a fixed orientation (no rotation,
  Galaga-style) and always fires straight up. See
  [systems/movement.md](systems/movement.md).
- **Shooting & enemies** — hold Fire to auto-fire. Enemies spawn in waves
  from the top of the screen, drift down in a sine-wave pattern, and
  periodically fire straight down. Player and enemy bullets both deal
  collision damage in their respective directions. See
  [systems/combat.md](systems/combat.md).
- **Player health, damage feedback & game over** — the ship has HP; enemy
  bullets deal 1 damage each hit. A non-fatal hit flashes the ship's sprite
  and briefly shakes the camera. At 0 HP the ship disables and a full-screen
  "Game Over" overlay appears with a Restart button that reloads the scene
  from scratch. See [systems/combat.md](systems/combat.md).
- **Player roles & abilities** — the ship can be assigned a role
  (`Attacker`, `Tank`, `Medic`, `Support`) via the `PlayerRoleComponent` in
  the Inspector, which changes health, **shield**, fire rate, move speed,
  and sprite tint color. Shield absorbs damage before health and only ever
  refills via Medic's proximity aura (see below) — see
  [systems/player-roles.md](systems/player-roles.md)'s "Shield stat". The
  party frame now shows a shield bar alongside the health bar. There's no
  in-game role-selection UI yet — it's Inspector-only for now. Press **E**
  to use the role's ability: Tank taunts (redirects the boss's aggro to
  you — see the Boss encounter bullet below and
  [systems/player-roles.md](systems/player-roles.md)'s "Aggro/targeting"
  section), Medic temporarily expands its passive heal/shield aura into a
  much larger, faster one for a few seconds, Support temporarily boosts
  move speed and fire rate, Attacker fires a 3x-width, harder-hitting
  bullet with visible recoil. Medic's aura is always on — a thin ring
  around it shows the current radius, tiny by default until boosted — and
  heals/shields any ally (including the human player) who gets close
  enough, flashing them green when it actually helps. The party frame
  shows which ability is boosted/by how much and its cooldown, on a
  legible dark panel. See
  [systems/player-roles.md](systems/player-roles.md).
- **Portrait/crossplay screen layout** — the gameplay area stays locked to a
  9:16 portrait aspect ratio and adapts automatically: full-width on
  narrow/phone-like aspects, pillarboxed with HUD sidebar space on wider
  desktop aspects. See [systems/hud-layout.md](systems/hud-layout.md).
- **Boss encounter** — a boss with 60 HP (recently doubled, see "Tuning" in
  [systems/boss.md](systems/boss.md)) across 2 HP-based phases (Phase 2 at
  ≤50% HP fires faster and in a 3-bullet spread) and a real aggro system:
  whoever deals the most damage is the boss's target, and Tank taunt
  redirects it. 3 CPU-controlled AI teammates (`Teammate_Tank`/
  `Teammate_Medic`/`Teammate_Support`) fight alongside the human `Player`,
  covering whichever roles aren't human-played — move, auto-fire, and use
  their role's ability autonomously. Whichever teammate is currently
  playing **Tank** steers to a guard point between the boss and the other
  AI teammates and physically blocks bullets aimed at them, in addition to
  taunting for aggro; whichever teammate is playing **Medic** hangs back
  toward the rear of the party by default, away from the boss, but breaks
  off to approach whichever ally has dropped to 55% health or shield or
  below (whoever's worst off, re-evaluated every frame — reacts to the
  human player being hurt too, not just the other AI teammates);
  Attacker/Support still just weave side-to-side (see
  [systems/boss.md](systems/boss.md)'s "AI teammate behavior" for what's
  still planned there). Medic's AI currently presses its ability the
  instant it's off cooldown regardless of need — a known, flagged-temporary
  placeholder, not a finished heuristic. All player-dealt fire damage was
  cut 40% in the
  same tuning pass as the boss HP increase. `BossPanel` shows the boss's
  live HP bar, phase, and current target. See
  [systems/boss.md](systems/boss.md).

## What's NOT there yet

- No networking/multiplayer — the 3 teammates are CPU-controlled, not real
  human players; local co-op isn't wired up.
- Only one scene exists (`Assets/Scenes/SampleScene.unity`) — no Main Menu,
  Role Select, or Lobby scenes yet (Game Over is a same-scene UI overlay, not
  a separate scene — see [systems/combat.md](systems/combat.md)). See
  `roadmap.md`'s "Networking (last)" section for where scene scaffolding
  fits into the build order.
- No local co-op / dynamic player count — the party is 4 fixed, hand-placed
  scene objects (`Player` + 3 `Teammate_*`), not a runtime spawner that
  reacts to however many humans are actually playing.
- Attacker/Support AI teammates don't have role-specific positioning yet
  (Tank and Medic both do) — see [systems/boss.md](systems/boss.md)'s "AI
  teammate behavior". No bullet-dodging or teammate separation either.
  Manually triggering a teammate's ability from the party frame is also
  designed but not built.
- No minions around the boss yet — the ship-shrink tuning (see
  [systems/boss.md](systems/boss.md)) was done to leave room for them, but
  no minion script/prefab exists.
- No real art — the avatar slot and every ship are placeholder colored
  squares, no audio.

See [roadmap.md](roadmap.md)'s "Development priority order" for the
authoritative build sequence: full basic mechanics first, then
player-vs-boss dynamics validated with CPU-controlled AI teammates, then
real networking last, then art/audio.

## How to test it

1. Open the project in Unity (`SampleScene` under `Assets/Scenes/`).
2. *(Optional)* Select the `Player` GameObject and change the
   `PlayerRoleComponent`'s **Role** field before pressing Play, to try a
   different role's stats/tint. If you do, also change the `Teammate_*`
   that currently has that role to `Attacker` (or swap two roles) so all 4
   roles stay covered exactly once — see [systems/boss.md](systems/boss.md)
   for why. Do this in Edit mode, not while playing.
3. Press **Play**. Note: `EnemySpawner`'s `Spawner` is auto-disabled by
   `Boss.Awake()` at Play start, so the old top-down enemy waves don't spawn
   during a boss-fight test — the boss is the only enemy on screen.
4. Move with **WASD** (arrow keys are not currently bound). Hold **Space**
   or **left mouse button** to fire — it auto-fires while held. Press **E**
   to use the current role's ability (see step 7 for what each role does).
   The 3 `Teammate_*` ships fight alongside you fully autonomously — no
   input needed for them.
5. Watch the boss fire at whichever ship (yours or a teammate's) currently
   holds its aggro, shown live as `BossPanel`'s "Target:" text on the right.
   At ≤50% HP its fire rate doubles and it switches to a 3-bullet spread
   (`BossPanel`'s "Phase" text flips to "Phase 2"). Taking hits reduces
   shield first, then HP (a hit fully absorbed by shield still flashes the
   ship and shakes the camera); at 0 HP a ship disables. Whichever teammate
   is playing Tank will visibly hold position between the boss and the
   other 2 AI teammates rather than weaving, and whichever is playing Medic
   will hang back near the rear of the party instead — you'll also see a
   thin ring around it showing its aura's reach. The left-sidebar shows 4 party
   frames (you + 3 teammates), each with a live avatar, role/HP/shield-bar/
   move-speed/fire-rate/ability text, tinted to match the role, on a dark
   panel.
6. At 0 HP, **only the human `Player`** triggers the "Game Over" overlay and
   ends the test — a teammate dying just grays out its own party frame and
   it keeps fighting inactive. Click **Restart** to reload the scene from
   scratch (HP, boss, teammates, and party frames all reset).
7. To see the different roles side by side, stop Play, change the Role
   field(s) per step 2, and Play again — compare HP taken to disable, fire
   rate, move speed, and tint color against the table in
   [systems/player-roles.md](systems/player-roles.md). As Medic, notice the
   dim ring around your ship even without pressing anything — that's the
   passive aura, tiny by default, healing/shielding any ally that gets
   close enough (they flash green when it actually helps); press **E** to
   drastically expand the ring and speed up the healing for a few seconds.
   Press **E** as Support to briefly boost move speed/fire rate, or as Tank
   to fire the taunt event — this now has a real effect: it forces the boss
   to switch its target to you (watch `BossPanel`'s "Target:" text change)
   — watch the party frame's ability line show the boost amount and
   cooldown countdown. As Attacker, **E** fires a wider, harder-hitting
   shot with a visible backward recoil kick.
8. *(Optional)* Resize the Game view window (or check the Scene view via
   Main Camera) to see the portrait pillarboxing adapt live — the
   `AspectRatioFitter`/`HUDSidebarFitter` combo is marked `[ExecuteAlways]`
   so this previews without even pressing Play.

There's no build/executable yet — testing happens in the Unity Editor.
