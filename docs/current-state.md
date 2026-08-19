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
  the Inspector, which changes health, fire rate, move speed, and sprite
  tint color. There's no in-game role-selection UI yet — it's Inspector-only
  for now. Press **E** to use the role's ability: Tank broadcasts a taunt
  event (flashes the ship and shakes the camera as placeholder feedback —
  no boss/aggro system exists yet to actually redirect, see
  [systems/player-roles.md](systems/player-roles.md)'s "Aggro/targeting"
  section), Medic heals self, Support temporarily boosts move speed and
  fire rate, Attacker fires a 3x-width,
  3x-damage bullet with visible recoil. The party frame shows which
  ability is boosted/by how much and its cooldown, on a legible dark panel.
  See [systems/player-roles.md](systems/player-roles.md).
- **Portrait/crossplay screen layout** — the gameplay area stays locked to a
  9:16 portrait aspect ratio and adapts automatically: full-width on
  narrow/phone-like aspects, pillarboxed with HUD sidebar space on wider
  desktop aspects. See [systems/hud-layout.md](systems/hud-layout.md).

## What's NOT there yet

- No boss encounter — Tank taunt has nothing to affect yet, and Medic heal
  only targets self, pending a real boss/AI teammate to target.
- No networking/multiplayer — only one player instance exists in the scene;
  local co-op isn't wired up.
- Only one scene exists (`Assets/Scenes/SampleScene.unity`) — no Main Menu,
  Role Select, or Lobby scenes yet (Game Over is a same-scene UI overlay, not
  a separate scene — see [systems/combat.md](systems/combat.md)). See
  `roadmap.md`'s "Networking (last)" section for where scene scaffolding
  fits into the build order.
- Only one party frame is shown — `PartyFrame_1` is a reusable prefab now
  (`Assets/Prefabs/PartyFrame.prefab`), but nothing spawns more copies of it
  yet (no second player exists to spawn one for). `BossPanel` is still just
  a "coming soon" placeholder.
- No real art — the avatar slot and ship are placeholder colored squares, no
  audio.

See [roadmap.md](roadmap.md)'s "Development priority order" for the
authoritative build sequence: full basic mechanics first, then
player-vs-boss dynamics validated with CPU-controlled AI teammates, then
real networking last, then art/audio.

## How to test it

1. Open the project in Unity (`SampleScene` under `Assets/Scenes/`).
2. *(Optional)* Select the `Player` GameObject and change the
   `PlayerRoleComponent`'s **Role** field before pressing Play, to try a
   different role's stats/tint. Do this in Edit mode, not while playing.
3. Press **Play**.
4. Move with **WASD** (arrow keys are not currently bound). Hold **Space**
   or **left mouse button** to fire — it auto-fires while held. Press **E**
   to use the current role's ability (see step 7 for what each role does).
5. Watch enemy waves spawn from the top and drift down; avoid or out-DPS
   their return fire. Taking hits reduces HP, flashes the ship, and shakes
   the camera; at 0 HP the ship disables. The left-sidebar party frame shows
   an avatar placeholder plus live role/HP/move-speed/fire-rate/ability
   text, all tinted to match the role, on a dark panel.
6. At 0 HP, a "Game Over" overlay appears and the party frame's health bar
   grays out. Click **Restart** to reload the scene from scratch (HP,
   enemies, and party frame all reset).
7. To see the different roles side by side, stop Play, change the Role
   field, and Play again — compare HP taken to disable, fire rate, move
   speed, and tint color against the table in
   [systems/player-roles.md](systems/player-roles.md). Press **E** as Medic
   to heal, as Support to briefly boost move speed/fire rate, or as Tank to
   fire the taunt event (flash + camera shake — placeholder feedback, no
   boss to actually affect yet) — watch the party frame's ability line show
   the boost amount and cooldown countdown. As Attacker, **E** fires a
   wider, harder-hitting shot with a visible backward recoil kick.
8. *(Optional)* Resize the Game view window (or check the Scene view via
   Main Camera) to see the portrait pillarboxing adapt live — the
   `AspectRatioFitter`/`HUDSidebarFitter` combo is marked `[ExecuteAlways]`
   so this previews without even pressing Play.

There's no build/executable yet — testing happens in the Unity Editor.
