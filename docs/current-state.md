# Current Playable State

What the game actually does right now, and how to test it. For what's next,
see [roadmap.md](roadmap.md); for the full history of how we got here, see
[progress-log.md](progress-log.md). This file should be kept in sync with
`roadmap.md`'s "Implemented" section — update both when a feature moves from
"In Progress"/"Planned" to done.

## What's playable

- **Main Menu / Lobby / Join / Pause** — the game now boots into
  `MainMenu.unity` (Build Settings index 0): Play → `Lobby`, Quit. Lobby
  offers **Local** (proceeds to `JoinLobby`, a co-op device-join screen —
  see "Local co-op" below) or **Online** — Online is a disabled placeholder
  button, since there's no Nakama backend yet. Role Select (now index 3)
  gained a Back button (to `JoinLobby` or `Lobby`). Mid-run, pressing
  **Escape** (or a gamepad's **Start**) during a level scene pauses (freezes the
  game via `Time.timeScale`, shows Resume/Restart/Change Roles/Quit to Main
  Menu) — disabled while Game Over/Victory is already showing, and will
  auto-disable once Online mode is real. See
  [systems/scene-flow.md](systems/scene-flow.md).
- **Local co-op (up to 4 players, one machine)** — `Lobby`'s Local button
  leads to `JoinLobby.unity`: press any key/click (keyboard+mouse) or any
  button on a gamepad to claim one of 4 slots, shown live; **Continue** once
  at least one player has joined. `RoleSelect` then shows either the
  original single-picker (0-1 joined) or a per-player row picker (2+
  joined) — each row moves its own highlight and locks a role using that
  player's own device (dpad/stick or WASD, confirm with South/Enter), and
  no two players can lock the same role. Any of the 4 roles nobody picked
  is filled by AI, same as always. See
  [systems/player-roles.md](systems/player-roles.md)'s "Local co-op /
  dynamic player count".
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
- **Player roles & abilities** — each ship is assigned a role (`Attacker`,
  `Tank`, `Medic`, `Support`) via `PlayerRoleComponent`, which sets its
  health, shield, fire damage, fire rate, move speed, and sprite tint color
  to fixed, role-specific values (see
  [systems/player-roles.md](systems/player-roles.md)'s "Fixed per-role
  stats" for the full table). Shield absorbs damage before health and only
  refills via Medic's proximity aura — see
  [systems/player-roles.md](systems/player-roles.md)'s "Shield stat". The
  party frame shows a shield bar alongside the health bar. Press **E** to
  use the role's ability: Tank taunts (redirects the boss's aggro to you —
  see the Boss encounter bullet below and
  [systems/player-roles.md](systems/player-roles.md)'s "Aggro/targeting"
  section), Medic temporarily expands its passive heal/shield aura into a
  much larger, faster one for a few seconds, Support triggers a **party-wide**
  Speed Boost (move speed **and** fire rate, all 4 ships, not just itself)
  with a shared gold ring showing on every ship while it's active, Attacker
  fires a wider, harder-hitting bullet (damage scales live off its own
  current fire damage) with visible recoil. Medic's aura is always on — a
  thin ring around it shows the current radius, tiny by default until
  boosted — and heals/shields any ally (including the human player) who
  gets close enough, flashing them green when it actually helps. Tank also
  always shows a wide curved shield arc above it — passive, always-on,
  independent of `E` — and it physically blocks incoming fire (draining
  Tank's own shield/health, not a free block) across a width wider than
  Tank's own body. The party frame shows which ability is boosted/by how
  much and its cooldown. See
  [systems/player-roles.md](systems/player-roles.md).
- **Portrait/crossplay screen layout** — the gameplay area stays locked to a
  9:16 portrait aspect ratio and adapts automatically: full-width on
  narrow/phone-like aspects, pillarboxed with HUD sidebar space on wider
  desktop aspects. See [systems/hud-layout.md](systems/hud-layout.md).
- **Role Select + Level Select + Victory screens** — `RoleSelect.unity` shows
  either the original single 4-button picker (0-1 players joined at
  `JoinLobby` — pick one of the 4 roles and press Start) or, with 2+ local
  co-op players joined, a row-per-player picker (see "Local co-op" above);
  whichever roles nobody picked auto-fill with AI. Start now leads to
  `LevelSelect.unity`, a 3-card picker (**Marauder** / **Halcyon** / **Warden**
  — see [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)) that loads the
  chosen level's own scene. Defeating the boss shows a `VictoryPanel`
  (mirrors Game Over) offering "Play Again" (same level, same party/roles/
  devices) or "Change Roles" (back to Role Select); Game Over has a matching
  "Change Roles" button alongside its Restart. See
  [systems/scene-flow.md](systems/scene-flow.md) and
  [systems/player-roles.md](systems/player-roles.md)'s "Role Select scene".
- **Level 1 sequencing** — the fight is now a scripted opening, not an
  instant drop-in. The 4 ships start below the visible screen and glide up
  into a line near the bottom over ~4s (frozen, no input); control hands
  over fully for a further ~4s of free movement; then minions start
  spawning (`EnemySpawner`'s wave-formation system, now in random order)
  for ~2 minutes; once that timer's up and the screen is clear of minions,
  the boss glides down from off-screen-top to its home position over ~4s
  (ships frozen again — no movement, firing, or abilities) before combat
  begins. See [systems/level-sequencing.md](systems/level-sequencing.md).
- **Boss encounter** — a boss with 90 HP across 2 HP-based phases (Phase 2
  at ≤50% HP fires faster and in a 3-bullet spread) and a real aggro
  system: whoever deals the most damage is the boss's target, and Tank
  taunt redirects it. Movement is a fixed, scripted pattern (not AI): snap
  suddenly to one side, advance down toward the ships within roughly the
  top 2/5 of the playable height, retreat back to that side, return to
  center, wait 1.5s, then repeat mirrored to the other side — looping for
  the whole fight, unchanged across both phases. Reaching phase 2 also
  brings minions back (same wave system as the pre-boss sequencing above),
  fighting alongside the ongoing boss fight. A
  dim ring around the boss always shows its shockwave danger zone (~1.5
  ship-widths out) — it pulses brighter as a telegraphed shockwave winds
  up, then flashes on impact — getting caught inside deals 3x bullet
  damage plus a dramatic knockback (~3.5 units, ~5.8 ship-widths). Touching
  the boss's body directly (no ring warning for this one) costs a ship 2x
  its regular bullet damage. Every few seconds it also fires a **guided
  missile** that homes in on whichever Medic or Attacker it locks onto
  (curving toward them in flight, not a straight shot) — `BossPanel` shows
  a warning naming the targeted role during the lock-on so Tank knows who
  to protect, plus live "Shockwave"/"Guided Missile" cooldown countdowns;
  Tank can still block the missile, but only by actively cutting across its
  path, not just by standing between the boss and the target the way it
  reliably can against every other bullet. On top of that, every ~7 seconds
  the boss also unleashes a telegraphed **Pattern Barrage** — randomly a wide
  aimed Fan, a full 360° Ring, or a rapid rotating Spiral of bullets (never
  the same shape twice in a row), with `BossPanel` naming the incoming shape
  during the wind-up and showing its own cooldown countdown. AI teammates
  keep a minimum distance from the boss by default so they don't wander into
  the contact/shockwave range. See [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)'s
  "Movement and firing", "Body contact damage", "Shockwave", "Guided
  missile", "Pattern Barrage", and "BossPanelUI.cs" sections. 3 CPU-controlled AI teammates
  (`Teammate_Tank`/`Teammate_Medic`/`Teammate_Support`) fight alongside the
  human `Player`, covering whichever roles aren't human-played — move,
  auto-fire, and use their role's ability autonomously. Whichever teammate
  is currently playing **Tank** steers to a guard point between the boss
  and the other AI teammates and physically blocks bullets aimed at them
  (backed by its shield arc too), in addition to taunting for aggro;
  whichever teammate is playing **Medic** hangs back toward the rear of the
  party by default, away from the boss, but breaks off to approach
  whichever ally has dropped to 55% health or shield or below (whoever's
  worst off, re-evaluated every frame — reacts to the human player being
  hurt too); whichever teammate is playing **Support** roams the playable
  area freely (random waypoint wander, no fixed zone); whichever teammate
  is playing **Attacker** patrols side-to-side around the boss's own
  current X position rather than a fixed, boss-independent lane, holding a
  balanced mid-distance (not as close as Tank, not as far back as Medic) —
  since ships never rotate and shots only ever fire straight up, tracking
  the boss's X is what keeps its shots actually landing as the boss moves
  (see [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)'s "Attacker patrol positioning").
  *(Written for the single-human default; with 2+ local co-op players
  joined — see "Local co-op" above — the same per-role behavior applies to
  whichever roles end up AI-controlled, and `Teammate_Tank`/`_Medic`/
  `_Support`'s GameObject names are cosmetic only, not a guarantee of which
  role or control type actually ends up there.)* Medic's AI currently presses its ability the instant it's off cooldown
  regardless of need — a known, flagged-temporary placeholder, not a
  finished heuristic. Every role has fixed, role-specific health/shield/
  fire-damage/fire-rate/move-speed values (see
  [systems/player-roles.md](systems/player-roles.md)'s "Fixed per-role
  stats") — Attacker hits hardest on regular fire (2.0, vs. Tank/Support's
  1.0 and Medic's 0.7), on top of its Big Shot ability. `BossPanel` shows
  the boss's live HP bar, phase, current target, guided-missile warning,
  pattern-barrage warning, and shockwave/guided-missile/pattern-barrage
  cooldowns. See
  [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md).
- **Solid-body collision** — no two ships (human or AI) can occupy the same
  space, and no ship can occupy the boss's body either; each ship resolves
  its own position against every other ship and the boss every frame
  (`ShipCollisionUtil.cs`, an exact box-vs-box push-out, not a physics
  simulation). Touching the boss still deals contact damage — detection now
  comes from this same overlap check instead of a physics trigger callback.
  See [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)'s "Solid-body collision".
- **Bullet-dodging + manual ability triggering** — the 3 AI teammates now
  steer away from incoming enemy fire that's on an imminent collision
  course (a sideways step out of the bullet's path, blended into whatever
  role-positioning they're already doing rather than abandoning it), on
  top of the boss-avoidance floor and role positioning they already had.
  Separately, the human can now click a teammate's ability status line
  (bottom of its party frame) to force that teammate's ability to fire
  right now, subject to its own cooldown — the human's own frame has no
  such button, since **E** already does this for the human. See
  [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)'s "Bullet-dodging" and
  [systems/player-roles.md](systems/player-roles.md)'s "PlayerAbility.cs".
- **Minions around the boss** — up to 2 smaller enemy ships flank the boss
  at once, tracking its position and always firing at whoever currently
  holds the boss's own aggro (so Tank taunt redirects them too). They start
  spawning the instant boss combat begins — not any earlier, and not during
  the boss's own entrance glide (`MinionSpawner` only enables once
  `MarauderBoss` does; see
  [systems/level-sequencing.md](systems/level-sequencing.md)'s "Boss
  visibility/collision") — and keep spawning throughout the whole
  fight. They're solid (ships push off them) and **kamikaze** — touching one
  deals contact damage once and kills the minion immediately, rather than
  letting it tank hits repeatedly. They also still take damage from player
  fire like any other enemy, and are all destroyed the instant the boss is.
  About 30% of spawned minions are a distinct **Explosive** type, tinted
  orange — killing one any way (contact or shot down) bursts it into a ring
  of 8 fragments that fly outward and can hit any nearby ship, including the
  one that killed it, so shooting one from a distance isn't automatically
  safe. See [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)'s "Minion.cs /
  MinionSpawner.cs".

- **Enemy spawn pattern variety** — the minion wave system (`EnemySpawner.cs`) picks a formation
  at random each wave from 4 options: `Random` (original uniform-random X), `Line`
  (evenly spaced), `Cluster` (tight jittered group around one random center), and `VFormation`
  (a symmetric V shape as it descends). Each formation pairs with one of 3 `Enemy.cs` movement
  patterns — `SineWave` (original), `ZigZag` (erratic alternating drift), or `StraightDive`
  (fast, no horizontal movement). Runs automatically as part of the Level 1 sequencing above
  (pre-boss, and again at boss phase 2) — no manual setup needed to see it. These wave enemies are
  also **kamikaze** now, same as the boss-flanking minions below — touching one deals contact
  damage once and destroys it immediately, rather than passing through with no effect. About 30%
  of spawned wave enemies (`EnemySpawner.explosiveEnemyChance`) are a distinct **Explosive** type,
  tinted orange — same mechanic as the boss-flanking Explosive minions below: killing one any way
  (contact or shot down) bursts it into a ring of 8 fragments that fly outward and can hit any
  nearby ship. See [systems/combat.md](systems/combat.md).

- **DPS on the party frame** — each frame now shows a live `DPS:` line below
  Fire Rate, driven by real damage dealt to the boss
  (`MarauderBoss.GetDamageDealt(ship) / CombatElapsed`), not the theoretical
  `fireDamage x shotsPerSecond` ceiling — switched once the Attacker combo
  landed, since combo/Big Shot hits bypass `fireDamage` and never moved the
  old number. See [systems/hud-layout.md](systems/hud-layout.md)'s "DPS line".
- **DPS meter (Recount-style)** — the left sidebar now carries a damage
  meter below the party frames: one role-tinted bar per ship, sorted by
  damage descending, showing total damage, DPS, and percent of party damage,
  with the party total in its title. **Boss damage only** — minions and wave
  enemies are deliberately not counted. The DPS denominator is time since
  boss combat began, so it reads 0 until the boss actually engages. When the
  boss dies the meter freezes on the final numbers and retitles to
  `DAMAGE · BOSS DOWN`. Built procedurally by `DpsMeterUI.cs` — attach it to
  an empty GameObject under `LeftSidebar` and drag the `Boss` in; it builds
  its own panel, rows, and bars on `Start()`. See
  [systems/hud-layout.md](systems/hud-layout.md)'s "DpsMeterUI.cs".
- **Aggro now works on the normal route into the game** — previously
  `MarauderBoss` built its aggro table from the four Inspector-wired
  `targets[]` objects, which the co-op spawner deactivates and replaces with
  freshly-spawned ships. So on any run that went through Join Lobby (i.e.
  the normal flow, including a single player), no ship was in the aggro
  table: damage never became threat, the boss's `CurrentTarget` stayed a
  disabled marker for the entire fight, and Tank's taunt silently did
  nothing. `PartySetupBootstrap` now assigns `boss.targets = spawned`
  alongside the roster assignments it already made. See
  [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md)'s "Aggro roster comes from
  `targets[]`".

- **Halcyon (Level 2's boss)** — a pure positioning fight, no ambient
  bullets at all. It roams the entire arena continuously (not confined
  near a home position like Marauder), periodically stopping for a brief
  ~2s window (telegraphed ~1s ahead) that's the only reliable time to land
  hits on it. Its real damage source is a Static Field pulse every 6s
  (4s once it hits phase 2): any two ships both near the boss and near
  each other when it pulses take a hit — a dim ring around it always
  shows the pulse's range, brightening right before it fires. Touching the
  boss directly still costs a ship 2x bullet damage, same as Marauder. It
  has no aggro/target system at all — Tank's Taunt does nothing against it
  (still flashes/shakes as feedback, just doesn't redirect anything). See
  [systems/bosses/halcyon-boss.md](systems/bosses/halcyon-boss.md).

## What's NOT there yet

- No networked/authoritative multiplayer (multiple humans across *separate*
  machines) — Lobby's **Online** option is a disabled placeholder until
  Nakama networking lands. Local co-op (multiple humans on *one* machine) is
  implemented — see the "Local co-op" bullet above.
- Eight scenes now exist: `MainMenu.unity` (0), `Lobby.unity` (1),
  `JoinLobby.unity` (2), `RoleSelect.unity` (3), `LevelSelect.unity` (4),
  `Level1.unity` (5), `Level2.unity` (6), `Level3.unity` (7) — see
  [systems/scene-flow.md](systems/scene-flow.md). Game Over/Victory/Pause
  are same-scene UI overlays within whichever level scene is active, not
  separate scenes (see [systems/combat.md](systems/combat.md)).
- No real art — the avatar slot and every ship are placeholder colored
  squares, no audio.

See [roadmap.md](roadmap.md)'s "Development priority order" for the
authoritative build sequence: full basic mechanics first, then
player-vs-boss dynamics validated with CPU-controlled AI teammates, then
real networking last, then art/audio.

## How to test it

1. Open the project in Unity, with `MainMenu` (under `Assets/Scenes/`) as
   the open scene — it's Build Settings index 0, so this is also what a real
   build boots into.
2. Press **Play**, click **Play** on the Main Menu to load `Lobby`, then
   click **Local** (Online is a disabled placeholder for now) to load
   `JoinLobby`. Press any key or click to join as Player 1 (Keyboard &
   Mouse), then click **Continue** to load `RoleSelect`. Click one of the 4
   role buttons (Attacker/Tank/Medic/Support — this is the role the human
   `Player` will use), then click **Start** once it's enabled. This loads
   `LevelSelect` — click a card (**Marauder**, **Halcyon**, or **Warden**;
   Marauder and Halcyon both have real boss mechanics, Warden is still a
   placeholder, see
   [systems/bosses/marauder-boss.md](systems/bosses/marauder-boss.md) and
   [systems/bosses/halcyon-boss.md](systems/bosses/halcyon-boss.md)) to load that
   level's scene, which auto-assigns the 3 remaining roles to 3 AI ships (any
   of the 4 you didn't pick, exactly once each). *(Opening `Level1`/
   `Level2`/`Level3` directly instead and pressing Play still works too,
   bypassing every menu scene entirely, and falls back to whatever roles are
   currently hand-set on `Player`/`Teammate_*` in the Inspector — useful for
   quick iteration. A Back button on Lobby/JoinLobby/Role Select/Level Select
   lets you step back a screen at a time instead.)*
3. *(Optional, local co-op)* At `JoinLobby`, connect one or more gamepads and
   press a button on each to join additional players (up to 4 total) — each
   claims its own slot, shown live. Click **Continue** once everyone's
   joined; with 2+ players, `RoleSelect` shows a row per player instead of
   the single picker — move each row's highlight with that player's own
   dpad/stick or WASD and confirm with South/Enter to lock a role (no two
   rows can lock the same one). Click **Start** once every row is locked,
   then pick a level at `LevelSelect` as above. Any role nobody picked is
   filled by AI, same as the single-player flow.
4. Watch the opening sequence play out automatically: all 4 ships glide up
   from below the screen into their starting line (~4s, no input works
   yet), then control hands over fully for ~4s of free movement before
   minion waves start (`EnemySpawner`, random formation each wave, ~2
   minutes), then — once the screen is clear of minions — the boss glides
   down from off-screen into position (~4s, ships frozen again) before
   combat begins. See [systems/level-sequencing.md](systems/level-sequencing.md).
   *(To iterate faster while testing, temporarily lower `LevelSequencer`'s
   `minionPhase1Duration` in the Inspector.)*
5. Move with **WASD** (or a gamepad's left stick, if you joined that way).
   Hold **Space**/**left mouse button** (or a gamepad's South button) to
   fire — it auto-fires while held. Press **E** (or a gamepad's West
   button) to use the current role's ability (see step 9 for what each role
   does). Any AI-controlled ships fight alongside you fully autonomously —
   no input needed for them.
6. Watch the boss run its scripted pattern (snap to a side, advance toward
   the party, retreat, return to center, pause, repeat mirrored) — it fires at
   whichever ship (yours or a teammate's) currently holds its aggro, shown
   live as `BossPanel`'s "Target:" text on the right. At ≤50% HP its fire
   rate doubles and it switches to a 3-bullet spread (`BossPanel`'s "Phase"
   text flips to "Phase 2"). Every few seconds `BossPanel` also flashes a
   "Guided missile: {role}" warning naming Medic or Attacker — a curving
   shot is inbound for that ship, so get Tank in its path if you can — and
   shows "Shockwave"/"Guided Missile" cooldown countdowns ("Ready" when
   available). Roughly every 7 seconds `BossPanel` also flashes "Incoming:
   Fan/Ring/Spiral Barrage" — a wide aimed cone, a full radial burst, or a
   fast rotating stream of bullets, never the same shape twice in a row —
   plus its own "Pattern Barrage" cooldown countdown. A dim ring around the boss marks its shockwave range,
   brightening as it winds up — don't linger inside it or touch the boss
   directly, both cost real damage (2x for contact, 3x for the shockwave
   plus a strong knockback that'll send a ship flying). Taking hits reduces
   shield first, then HP (a hit fully absorbed by shield still flashes the
   ship and shakes the camera); at 0 HP a ship disables. Whichever teammate
   is playing Tank will visibly hold position between the boss and the
   other 2 AI teammates rather than weaving — you'll also see a wide curved
   shield arc above it, always on, which physically blocks bullets crossing
   its width, not just ones hitting the ship itself — whichever is playing
   Medic will hang back near the rear of the party instead — you'll also
   see a thin ring around it showing its aura's reach — whichever is
   playing Support will wander freely around the whole play area rather
   than holding a spot — and whichever is playing Attacker will patrol
   side-to-side but stay roughly under wherever the boss currently is
   horizontally, at a distance between Tank's and Medic's, rather than
   patrolling a fixed lane independent of the boss. The left-sidebar shows
   4 party frames (you + your teammates), each with a live avatar, name
   ("Player 1"/"Player 2"/... for humans in join order, "CPU 1"/"CPU 2"/...
   for the AI-controlled slots),
   role/HP/shield-bar/move-speed/fire-rate/ability text, tinted to match
   the role. Each teammate's ability line is clickable — click it (e.g. on
   the Tank's frame) to force that teammate's ability to fire right now,
   subject to its own cooldown (it visibly greys out while on cooldown);
   your own frame has no such button since **E** already covers you. Watch
   a teammate near the boss during a Pattern Barrage or a regular shot and
   you'll see it occasionally juke sideways out of a bullet's path without
   abandoning its role positioning (Tank still roughly holds its guard
   point, Medic still hangs back, etc.).
7. Press **Escape** (or a gamepad's Start button) at any point during the run to pause — a `Paused`
   overlay appears (Resume/Restart/Change Roles/Quit to Main Menu) and the
   game genuinely freezes (`Time.timeScale = 0`, so ships, bullets, the boss,
   and minions all stop mid-motion). Press **Escape** again or click
   **Resume** to continue exactly where you left off. Pause won't open on
   top of an already-showing Game Over/Victory overlay.
8. At 0 HP, **only a human-controlled ship** triggers the "Game Over" overlay
   and ends the test — a teammate dying just grays out its own party frame and
   it keeps fighting inactive (if the remaining teammates go on to defeat
   the boss after that, the Victory panel is suppressed rather than popping
   on top of Game Over — whichever end screen shows first wins). Click
   **Restart** to retry with the exact
   same party/roles, or **Change Roles** to go back to the Role Select
   screen and pick again. Defeating the boss instead shows a **Victory**
   overlay with the same two options ("Play Again" / "Change Roles").
9. To see the different roles side by side, use **Change Roles** (from
   either end screen, or just stop Play and press Play again from
   `RoleSelect`) and pick a different role each time — compare HP/shield
   taken to disable, fire damage, fire rate, and move speed against the
   table in [systems/player-roles.md](systems/player-roles.md)'s "Fixed
   per-role stats". As Medic, notice the dim ring around your ship even
   without pressing anything — that's the passive aura, tiny by default,
   healing/shielding any ally that gets close enough (they flash green when
   it actually helps); press **E** to drastically expand the ring and speed
   up the healing for a few seconds. Press **E** as Support to trigger a
   **party-wide** Speed Boost — watch every ship (including AI teammates)
   light up with a shared gold ring and visibly speed up/fire faster
   together, not just your own ship. As Tank, notice the wide curved shield
   arc above your ship even without pressing anything — that's always on;
   press **E** to fire the taunt event instead, which has a real effect: it
   forces the boss to switch its target to you (watch `BossPanel`'s
   "Target:" text change) — watch the party frame's ability line show the
   boost amount and cooldown countdown. As Attacker, **E** fires a wider,
   harder-hitting shot (damage scales with your own current fire damage)
   with a visible backward recoil kick.
10. *(Optional)* Resize the Game view window (or check the Scene view via
   Main Camera) to see the portrait pillarboxing adapt live — the
   `AspectRatioFitter`/`HUDSidebarFitter` combo is marked `[ExecuteAlways]`
   so this previews without even pressing Play.

There's no build/executable yet — testing happens in the Unity Editor.
