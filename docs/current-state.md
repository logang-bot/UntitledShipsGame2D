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
- **Role Select + Victory screens** — the game boots into `RoleSelect.unity`:
  pick one of the 4 roles for the human `Player` and press Start; the 3 AI
  teammates automatically take the remaining 3 roles. Defeating the boss
  shows a `VictoryPanel` (mirrors Game Over) offering "Play Again" (same
  party, same roles) or "Change Roles" (back to Role Select); Game Over has
  a matching "Change Roles" button alongside its Restart. See
  [systems/player-roles.md](systems/player-roles.md)'s "Role Select scene".
- **Boss encounter** — a boss with 90 HP across 2 HP-based phases (Phase 2
  at ≤50% HP fires faster and in a 3-bullet spread) and a real aggro
  system: whoever deals the most damage is the boss's target, and Tank
  taunt redirects it. The boss erratically dashes or holds still (random
  probability every ~1.5s) and can push down toward the ships within
  roughly the top 2/5 of the playable height before retreating back up. A
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
  reliably can against every other bullet. AI teammates keep a minimum
  distance from the boss by default so they don't wander into the
  contact/shockwave range. See [systems/boss.md](systems/boss.md)'s
  "Movement and firing", "Body contact damage", "Shockwave", "Guided
  missile", and "BossPanelUI.cs" sections. 3 CPU-controlled AI teammates
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
  (see [systems/boss.md](systems/boss.md)'s "Attacker patrol positioning").
  Medic's AI currently presses its ability the instant it's off cooldown
  regardless of need — a known, flagged-temporary placeholder, not a
  finished heuristic. Every role has fixed, role-specific health/shield/
  fire-damage/fire-rate/move-speed values (see
  [systems/player-roles.md](systems/player-roles.md)'s "Fixed per-role
  stats") — Attacker hits hardest on regular fire (2.0, vs. Tank/Support's
  1.0 and Medic's 0.7), on top of its Big Shot ability. `BossPanel` shows
  the boss's live HP bar, phase, current target, guided-missile warning,
  and shockwave/guided-missile cooldowns. See
  [systems/boss.md](systems/boss.md).
- **Solid-body collision** — no two ships (human or AI) can occupy the same
  space, and no ship can occupy the boss's body either; each ship resolves
  its own position against every other ship and the boss every frame
  (`ShipCollisionUtil.cs`, an exact box-vs-box push-out, not a physics
  simulation). Touching the boss still deals contact damage — detection now
  comes from this same overlap check instead of a physics trigger callback.
  See [systems/boss.md](systems/boss.md)'s "Solid-body collision".

## What's NOT there yet

- No networking/multiplayer — the 3 teammates are CPU-controlled, not real
  human players; local co-op isn't wired up.
- Two scenes exist: `RoleSelect.unity` (entry point, Build Settings index 0)
  and `Gameplay.unity` (gameplay, index 1) — no Main Menu or Lobby scene
  yet (Game Over/Victory are same-scene UI overlays within `Gameplay`,
  not separate scenes — see [systems/combat.md](systems/combat.md)). See
  `roadmap.md`'s "Networking (last)" section for where the rest of scene
  scaffolding fits into the build order.
- No local co-op / dynamic player count — the party is 4 fixed, hand-placed
  scene objects (`Player` + 3 `Teammate_*`), not a runtime spawner that
  reacts to however many humans are actually playing.
- No bullet-dodging for AI teammates. Manually triggering a teammate's
  ability from the party frame is also designed but not built. See
  [systems/boss.md](systems/boss.md)'s "Not yet built".
- No minions around the boss yet — no minion script/prefab exists.
- No geometric/varied bullet spread patterns beyond the guided missile and
  the boss's two phase-based shot patterns.
- No real art — the avatar slot and every ship are placeholder colored
  squares, no audio.

See [roadmap.md](roadmap.md)'s "Development priority order" for the
authoritative build sequence: full basic mechanics first, then
player-vs-boss dynamics validated with CPU-controlled AI teammates, then
real networking last, then art/audio.

## How to test it

1. Open the project in Unity, with `RoleSelect` (under `Assets/Scenes/`) as
   the open scene — it's Build Settings index 0, so this is also what a real
   build boots into.
2. Press **Play**, click one of the 4 role buttons (Attacker/Tank/Medic/
   Support — this is the role the human `Player` will use), then click
   **Start** once it's enabled. This loads `Gameplay` and auto-assigns
   the 3 remaining roles to the 3 `Teammate_*` AI ships (any of the 4 you
   didn't pick, exactly once each). *(Opening `Gameplay` directly instead
   and pressing Play still works too, and falls back to whatever roles are
   currently hand-set on `Player`/`Teammate_*` in the Inspector — useful for
   quick iteration without going through Role Select each time.)*
3. Note: `EnemySpawner`'s `Spawner` is auto-disabled by
   `Boss.Awake()` at Play start, so the old top-down enemy waves don't spawn
   during a boss-fight test — the boss is the only enemy on screen.
4. Move with **WASD** (arrow keys are not currently bound). Hold **Space**
   or **left mouse button** to fire — it auto-fires while held. Press **E**
   to use the current role's ability (see step 7 for what each role does).
   The 3 `Teammate_*` ships fight alongside you fully autonomously — no
   input needed for them.
5. Watch the boss dash erratically or hold still (no predictable drift) and
   occasionally push down toward the party before retreating — it fires at
   whichever ship (yours or a teammate's) currently holds its aggro, shown
   live as `BossPanel`'s "Target:" text on the right. At ≤50% HP its fire
   rate doubles and it switches to a 3-bullet spread (`BossPanel`'s "Phase"
   text flips to "Phase 2"). Every few seconds `BossPanel` also flashes a
   "Guided missile: {role}" warning naming Medic or Attacker — a curving
   shot is inbound for that ship, so get Tank in its path if you can — and
   shows "Shockwave"/"Guided Missile" cooldown countdowns ("Ready" when
   available). A dim ring around the boss marks its shockwave range,
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
   4 party frames (you + 3 teammates), each with a live avatar, role/HP/
   shield-bar/move-speed/fire-rate/ability text, tinted to match the role.
6. At 0 HP, **only the human `Player`** triggers the "Game Over" overlay and
   ends the test — a teammate dying just grays out its own party frame and
   it keeps fighting inactive. Click **Restart** to retry with the exact
   same party/roles, or **Change Roles** to go back to the Role Select
   screen and pick again. Defeating the boss instead shows a **Victory**
   overlay with the same two options ("Play Again" / "Change Roles").
7. To see the different roles side by side, use **Change Roles** (from
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
8. *(Optional)* Resize the Game view window (or check the Scene view via
   Main Camera) to see the portrait pillarboxing adapt live — the
   `AspectRatioFitter`/`HUDSidebarFitter` combo is marked `[ExecuteAlways]`
   so this previews without even pressing Play.

There's no build/executable yet — testing happens in the Unity Editor.
