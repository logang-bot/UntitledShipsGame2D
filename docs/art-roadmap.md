# Art Roadmap

How the game goes from colored placeholder squares to its final look. The
art runs **in parallel** with the gameplay work: phase 3 of
[roadmap.md](roadmap.md) (small hitbox, focus mode, grazing, energy, cards)
belongs to another developer, while the art starts now. That's a deliberate
early start compared with the "art last" build order in
[overview.md](overview.md).

Two rules keep the parallel work safe:
- **Art never blocks gameplay.** Every asset replaces a placeholder that
  already works.
- **Gameplay code stays art-agnostic.** Scripts know about sprites,
  colliders and events, never about how an image was made.

## Decisions

| Topic | Decision | Why |
| --- | --- | --- |
| Player ships | **Live 3D models** (Session 51): the four "Ship Designs" GLBs (`Assets/Art/Ships/Models/`), imported with glTFast and rendered as meshes under the URP 2D Renderer. | The models already existed, so nothing needed modeling, and live meshes give banking and animated parts (halo, gyros, reactor core) for free. Chosen over baking them to sprites first. |
| Bosses | **Designed in Claude Design** (Session 52 prompt): each boss as a 2D sprite and a 3D GLB model built like the ships, with named parts (separate nodes for anything that moves, e.g. Warden's arms), a phase 2 look, and a **signature neon color** distinct from the four role colors. In game they follow the ships' live-model route. | Same pipeline as the ships, so nothing new to learn. The signature color is also the boss's bullet color, which keeps bullets readable. |
| Backgrounds | **One per boss** (Session 52, first draft): dark, with stars and small city buildings seen from above, far below, scrolling vertically in **parallax layers** (far stars → near buildings). Each boss's background has its own identity. | Gives each fight a sense of place and motion without competing with bullets. Layers are separate so the game scrolls them at different speeds. |
| Phone work | **Deferred** (Session 52): no phone builds or tuning until the art is further along. | Keeps the focus on the look first. The quality-tier plan below still stands. |
| Baking to sprites | **Not planned; a possible phone optimization.** If the live meshes cost too much on low-end phones, an editor script can render them to sprite frames later. | Gameplay code never sees how a ship is drawn, so swapping is local to the ship visual. |
| Blender | **Optional, not a foundation.** Only for refining a 3D model. | It was the biggest learning cost in the old plan, and none of the current designs need it. |
| How they're lit | Ships: a custom unlit `ShipMesh.shader` (fixed-direction Lambert from mesh normals, plus HDR emission for the neon). Everything else: URP **2D Renderer**, `Sprite-Lit-Default`, `Light2D`. Bloom does most of the glow. | URP's `Mesh2D-Lit` lights meshes only through normal-map textures and ignores mesh normals, so the models would look flat. |
| Effects and powers | Neon style inspired by the Tuts+ "Neon Vector Shooter" (Shape Blaster): bloom, additive particles stretched by speed, a warping background grid, black-hole-style powers | The effects are what sell the neon look. They're independent of how the ships are made. |
| Anime ability cut-ins | **Decided at the start of Phase 6**, between two routes, neither needing Blender: **2D illustrated** characters animated in Unity, or **VRoid → Unity** through UniVRM and its toon shader (MToon). | 2D matches 2D ships, and it's how Touhou's own spell-card cut-ins work. VRoid → Unity keeps the anime-3D look while skipping Blender entirely. |
| Mobile | **One project** with per-platform quality settings | No duplicated code, and it keeps crossplay possible. Sprites cost a phone the same however they were made. |
| Priority | Ships, bosses, effects and powers first; then the pre-level screens (main menu through level select); then the in-level HUD | The screens before the level show the ships and bosses, so they come after those exist. |
| Screens and HUD style | One shared **UI kit**, first from the `Neon Fleet HUD` design and built from the `Pre-Fight Menus` design (Session 54, see `systems/ui-kit.md`): dark panels, neon role-colored frames, **Chakra Petch** and **JetBrains Mono** fonts. Every menu and the HUD use it. | One look across all screens, built once. Both fonts are free Google Fonts (SIL Open Font License), usable in Unity through TextMesh Pro. |

**Readability at real size.** At in-game size (about 65 px tall on a 1080p
desktop, 115 px on a 1080×1920 phone) the 3D ships' fine detail (surface
details, turret barrels, sensor dishes) shrinks to 1–2 pixels. What reads
is the silhouette and the glowing trim, so judge any ship change at
in-game size, not zoomed in.

## Source designs

The ship designs live in the Claude Design project "Ship Designs"
(`claude.ai/design`, project `074be517-d927-47e1-b562-9e823449cd3c`):

| File | What it holds |
| --- | --- |
| `Ship Designs.dc.html` | The four ships as 2D SVG line art (set 1a), with 96/48/32 px scale previews, and the 3D set (1b) embedded |
| `Ship Models 3D.html` + `ships3d.js` | The 3D models, built from simple shapes in three.js, with GLB/OBJ download. **Saved and in use:** `Assets/Art/Ships/Models/aegis-04.glb`, `nova-11.glb`, `lumen-02.glb`, `echo-07.glb` |
| `Neon Fleet HUD.dc.html` | The in-level HUD: screen layout for PC (1600×900) and mobile (390×844), the four-slot squad rail (human vs CPU frames, special-attack states) and the boss panel. The source of the HUD (Phase 8) and the starting point of the UI kit. |
| `Pre-Fight Menus.dc.html` + `Menu Button.dc.html` + `Hint Bar.dc.html` | The five screens before a level (Main Menu, Lobby, Join Lobby, Role Select single and multi, Level Select) at PC 1600×900 and mobile 390×844, the UI kit (frames, button states, slot frames, icons, colour tokens, type scale) and the screen transitions. **PC built** (Session 54, Phase 7); mobile waits for the phone work. Icons exported as PNGs to `Assets/Art/UI/Import/`. |
| `Boss Designs.dc.html` + `bossBackgrounds.js` | The three bosses (2D + 3D, phase 2 looks, signature colors: Marauder `#FF2D95`, Halcyon `#FFE14A`, Warden `#5470FF`), Marauder's minions, and the four-layer parallax background per boss, from the prompt summarized in Phase 2c. **Backgrounds in use** (Session 53, PNGs in `Assets/Art/Backgrounds/`); the bosses aren't in the game yet (Phase 5). |
| `Combat FX Designs.dc.html` | Bullets, auras and rings, the four role abilities as wind-up / activate / sustain / end frames, every boss's minions plus the wave enemies with death bursts, Marauder and Warden shockwave rings, Halcyon's Static Field, and four white particle textures, each shown over the three backgrounds. **In use** (Session 55): its "Download all (ZIP)" export, 108 PNGs, in `Assets/Art/CombatFX/`. See Phase 3. |

| Role | Ship | Shape | Design color |
| --- | --- | --- | --- |
| Tank | AEGIS-04 | Shield-shaped hull, side armor pods, dashed shield arc ahead | Cyan `#00E5FF` |
| Attacker | NOVA-11 | Delta wing, twin nose cannons | Orange `#FF8A3D` |
| Medic | LUMEN-02 | Hexagon hull, cross, dashed halo ring | Lime `#B6FF3C` |
| Support | ECHO-07 | Diamond hull, gyro rings, side masts | Purple `#A855F7` |

**Standalone Claude Design closes by December 14, 2026.** The four GLBs are
in the repo. The four SVGs aren't needed for the ships any more, but are
worth exporting before that date as icon or menu source. Each GLB keeps its
parts and materials named (for example `shield_emitter`, `halo_ring`,
`gyro_a`, `aegis_cyan`), which is how the materials and animated parts were
assigned.

## Art direction (style guide seed)

- A dark background, with neon emissive accents on dark hulls. Bloom does
  most of the glowing.
- **Role colors** follow the designs (Session 51): Attacker orange, Tank
  cyan, Medic lime, Support purple, in `PlayerRoleStats.tintColor`
  (`Assets/Scripts/PlayerRole.cs`). That also drives the HUD (`PartyFrameUI`,
  `DpsMeterUI`); the ability rings on `Ship.prefab` and the RoleSelect
  buttons carry the same values.
- Ships must read clearly at their real in-game size, about 0.6 world units.
  Test detail at that size, not zoomed in.
- References:
  - **Shape Blaster** (Tuts+ series): effects, particles, the grid.
  - **Nex Machina**: neon plus detailed rendered art.
  - **Touhou** spell-card cut-ins, and **Genshin Impact / Zenless Zone
    Zero**: the cut-in characters.

### The readability rule (bullet hell)

Players must always be able to read enemy bullets through the effects:

- **Enemy bullets** have one fixed look: a bright solid core with a colored
  rim, a consistent color per boss, and the top sorting layer.
- **Player bullets** are dimmer and semi-transparent.
- **Effects and particles** draw *behind* bullets and never use the enemy
  bullets' colors or shapes.
- **Effects get an intensity setting**, which lowers particle density and
  bloom.
- Boss bullet colors are picked to stay clear of the four role colors.

## Phases

Each phase lists what to **learn**, what to **make**, when it's **done**, and
a **mobile check**.

### Phases 0–2: The four role ships (done, Session 51)

Built straight from the 3D models instead of the planned 2D sprites first.

- **In place:**
  - glTFast (`com.unity.cloud.gltfast` 6.20.0) imports the GLBs from
    `Assets/Art/Ships/Models/`.
  - `Assets/Art/Ships/ShipMesh.shader` and 24 materials in
    `Assets/Art/Ships/Materials/`: shared hull/plate/dark, per-ship neon and
    accent (HDR emission), glass, additive engine plumes, and one material
    per edge-line color.
  - One prefab per ship in `Assets/Art/Ships/Prefabs/` (`Nova11`,
    `Aegis04`, `Lumen02`, `Echo07`), with `ShipPartSpinner` on Lumen's
    `halo_ring` and Echo's gyros, and `ShipPartPulse` on the Aegis and Echo
    cores.
  - `Ship.prefab`: no `SpriteRenderer` any more. A `Visual` child holds the
    model's rotation (face the camera, nose up), scale (0.07) and z offset
    (0.5, so the whole model sits behind z=0 and enemy bullets draw over it).
    `ShipVisual` spawns the role's prefab in `Start` and drives the hit
    flash through `_FlashColor`; `ShipBank` rolls the model up to 35° while
    the ship moves sideways. The `BoxCollider2D` is unchanged, so gameplay is
    too.
  - A Global Volume with Bloom (threshold 1) in `Level.unity`.
- **Still open from these phases:**
  - ~~**A dark background.**~~ Done in Phase 2c (Session 53).
  - Tune the ship scale, light direction, hull brightness and bloom against
    that background, at in-game size.
  - Engine flames that react to speed, and the Tank shield-emitter pulse
    (the design pulses its glow, which needs per-renderer emission).
  - ~~Visuals for the Tank's **Shield Arc** and the Medic's **aura ring**.~~
    Done with the Combat FX sprites (Session 55, see Phase 3).
  - Whether a roll also gives invulnerability frames is a gameplay decision
    for the phase 3 developer.
- **Mobile check (deferred, Session 52):** four ships on screen with bloom
  on, on a real phone. Each ship is 30–55 meshes. If it's too heavy, bake the
  models to sprites (see "Baking to sprites" above) or merge each ship's
  static meshes.

### Phase 2c: Per-boss backgrounds (first draft)

- **Design first:** the per-boss backgrounds and the boss designs (Phase 5)
  come from one Claude Design prompt (Session 52). It asks for:
  - a dark background with stars, and small buildings seen from above (city
    rooftops and blocks far below), scrolling top to bottom as the ships fly
    "up";
  - separate layers that each tile seamlessly vertically (e.g. far stars,
    near stars, far buildings, near buildings), with a suggested relative
    scroll speed per layer;
  - a distinct identity per boss, matching the fight (suggested: Marauder a
    raided industrial district, Halcyon a storm-lit city, Warden a fortified
    military zone);
  - a mockup per boss with the boss, the four ships and a few enemy bullets
    at real size, to judge readability.
- **Readability rules for backgrounds:** dark, low-contrast and
  low-saturation, with only small dim lights. Nothing that looks like a
  bullet, nothing in the boss's signature color, and far less glow than the
  ships and the boss (below the bloom threshold).
- **Designed (Session 53):** `Boss Designs.dc.html` in the Claude Design
  project. Each boss gets four 1080×1920 layers that tile vertically:
  far stars (opaque base, ×0.08), near stars (×0.2), far buildings
  (×0.45) and near buildings (×1). Speeds are relative to the near layer.
  Themes: Marauder's burning foundry district, Halcyon's storm-drowned
  towers, Warden's walled garrison.
- **In place (Session 53):**
  - The 12 PNGs, exported from the design's "Download all backgrounds
    (ZIP)" button, in `Assets/Art/Backgrounds/<Boss>/`. Imported as
    sprites at **192 pixels per unit**, so one layer is exactly the
    playfield (5.625 × 10 units). Mesh type Full Rect (needed for tiled
    drawing), no mipmaps, high-quality compression.
  - `ParallaxLayer` draws its sprite tiled two tiles tall, scrolls it down
    and wraps it after one tile. Each run starts at a random point in the
    loop. Speeds: 0.063 / 0.158 / 0.354 / 0.79 units/s. The near layer
    moves at the design mockup's 0.14 playfield widths per second.
  - `ParallaxBackground` on each prefab root mirrors the whole background
    left-to-right on a coin flip. All four layers flip together, so the
    rooftop shadows stay consistent.
  - `<Boss>Background.prefab` per boss: z = 9, sorting orders −40 to −10,
    and **`Sprite-Unlit-Default`**, so the scene's 2D lights don't tint
    the designed colors.
  - `BossEntry.backgroundPrefab`, spawned by `LevelSetup` for the picked
    boss. The playfield camera background is `#04050A`.
  - The near layer repeats every ~13 s, the far buildings every ~28 s, and
    the stars every 1–3 minutes. If it still feels repetitive in play,
    ask the design for 2–3 seeded variants per boss and pick one per run.
- **Then:** tune ship scale, hull brightness and bloom against the dark
  backgrounds (see the open items in Phases 0–2).
- **Later:** whether the backgrounds need more (e.g. per-phase changes, the
  warping grid from Phase 3 on top) is decided after this first draft.
- **Done when:** each of the three fights scrolls its own background, and
  enemy bullets stay easy to read over it.

### Phase 3: Effects foundation

- **Learn:** Unity's Particle System (Shuriken), additive materials, and
  basic Shader Graph.
- **Make:**
  - **Bloom tuning** for the final look.
  - **Bullet looks** that follow the readability rule, for player bullets,
    each boss's bullets, and the wide Big Shot. Designs requested in
    Session 53 (`Combat FX Designs.dc.html`, with the auras, ability
    effects and minions). **Done, Session 55** (below). Explosive minions
    and their fragments now carry the boss colour in the sprites, so
    `explosiveTintColor` is gone.
  - **Hit sparks and explosions:** particles in **Stretched Billboard** mode,
    so fast particles become streaks, with additive blending.
  - **The warping background grid**: a spring-mass grid that explosions push
    and black holes pull, ported from the Tuts+ "Warping Grid" part. It's
    the biggest custom piece in this roadmap.
- **Done when:** a full boss fight shows the new bullets, impact effects and
  a reacting grid, with enemy bullets still easy to read.
- **Mobile check:** a lower-resolution grid and a capped particle count on
  the low tier.
- **In place (Session 55): the Combat FX sprites.**
  - **Import:** 108 PNGs from the design's ZIP in `Assets/Art/CombatFX/`
    (`Bullets/`, `Auras/`, `Superpowers/`, `Minions/`, `Particles/`), as
    sprites at **192 px/unit** (the design's 1080 px playfield), no mipmaps,
    uncompressed, pivot centre (`support_speed_ring` pivots on its ring
    centre, 0.5 / 0.609). Every effect uses `Sprite-Unlit-Default`, like
    the backgrounds.
  - **Sorting:** auras and danger rings −5, ability effects −2, player
    bullets 5, death bursts 8, enemy bullets **10**.
  - **Bullets:** one prefab per look in `Assets/Prefabs/Bullets/` (table in
    `systems/combat.md`). Enemy hitboxes now match the visible coloured
    body (was a 0.05 × 0.5 box for everything); player hitboxes are
    unchanged. `Bullet.alignToDirection` turns missiles, capsules, rods and
    shards along their flight.
  - **Minions and waves:** `HostileLook` picks the hull per movement
    pattern and standard/explosive type, and spawns the 3-frame death burst
    (`Assets/Prefabs/FX/<Set>Death[Explosive]`, a `SpriteSequence`).
    Prefab scales went to 1 with colliders resized to keep the old world
    hitboxes.
  - **Role visuals:** `ShipCombatFx` on `Ship.prefab` holds the sprites:
    the Medic aura (idle, boosted, pulsing) is scaled to the real heal
    radius; the Tank shield arc fits the collider, whose depth went 0.4 →
    0.2 to match the sprite, and flashes on a block; Support's speed ring
    is always purple; healed allies show a heal flash. Each ability plays
    its frame sequence (`TauntFx`, `AuraBoostFx`, `SpeedBoostFx`,
    `BigShotFx`).
  - **Boss rings:** `DangerZoneVisual` children for the Marauder and
    Warden shockwaves (the Warden frames were added to the design for this)
    and Halcyon's Static Field, scaled so the drawn ring sits on the damage
    radius.
  - **Imported, not used yet:** see the list just below.
  - **Still open in this phase:** bloom tuning, hit sparks and explosions,
    the warping grid. `WardenBigShot` isn't attached to `WardenBoss.prefab`,
    so its bullet has no prefab yet.

#### Combat FX imported but not used yet

Everything below is imported (192 px/unit, same settings as the rest)
but nothing in the game spawns it yet. Paths are under
`Assets/Art/CombatFX/`. Check this list first when a matching mechanic
lands, and update it when an asset gets wired.

| Asset(s) | Why it's unused | Where it would go |
| --- | --- | --- |
| `Minions/Halcyon/halcyon_minion`, `halcyon_minion_explosive` | Halcyon's fight has no minions (`MinionSpawner` only works with `MarauderBoss`) | A `HostileLook` on a Halcyon minion prefab, if that fight gets minions |
| `Minions/Warden/warden_minion`, `warden_minion_explosive` | Same: Warden's fight has no minions | Same, for Warden |
| `Minions/Halcyon/halcyon_death_1–3`, `halcyon_death_explosive_1–3` | Already built into `Prefabs/FX/HalcyonDeath` and `HalcyonDeathExplosive`, but nothing spawns those prefabs | `HostileLook.deathFxPrefab` / `explosiveDeathFxPrefab` on a Halcyon minion |
| `Minions/Warden/warden_death_1–3`, `warden_death_explosive_1–3` | Already built into `Prefabs/FX/WardenDeath` and `WardenDeathExplosive`; unused | Same, for Warden |
| `Bullets/Halcyon/halcyon_bullet_s`, `_m`, `_l`, `halcyon_fragment_shard`, `halcyon_fragment_ring` | Halcyon fires no bullets (the design calls this set "a reserve") | Halcyon minion fire and fragments, or any future Halcyon bullet attack |
| `Bullets/Warden/warden_bullet_s`, `_m` | Warden's arms fire the turret capsules and Lockdown fires rods, so no orbs | Any future Warden aimed or ring attack |
| `Bullets/Warden/warden_bullet_l` | Meant for `WardenBigShot`, which isn't attached to `WardenBoss.prefab` | A `WardenBigShotBullet` prefab, circle collider r 0.083 (set `widthMultiplier` to 1, since the orb is already big) |
| `Bullets/Warden/warden_fragment_shard`, `warden_fragment_ring` | No Warden explosive minions | Warden explosive-minion fragments |
| `Bullets/Warden/warden_lockdown_wall_segment` | 8 rods in one sprite; Lockdown spawns rods one by one, since each rod is its own `Bullet` | Only if Lockdown is ever rebuilt as one moving wall object |
| `Bullets/Marauder/marauder_bullet_l` | All Marauder fire uses the medium orb, the minions the small one | A heavier Marauder attack, e.g. a slower big round in Pattern Barrage |
| `Bullets/Wave/wave_bullet_m`, `wave_bullet_l` | Wave enemies fire the small orb | Tougher wave enemy types |
| `Bullets/*/*_fragment_ring` (Marauder, Wave; plus Halcyon and Warden above) | A preview of 8 shards in a ring, from the design; in game each fragment is its own `Bullet` with the shard sprite | Nothing planned; reference only |
| `Superpowers/Tank/tank_aggro_mark` | The mark already appears inside `tank_taunt_2_activate` / `_3_sustain` | An aggro indicator shown separately, e.g. above whichever ship the boss is targeting |
| `Superpowers/Medic/medic_aura_boost_3_sustain`, `_4_end` | `AuraBoostFx` plays only wind-up and activate; the boosted aura ring (`medic_aura_boost`) already covers the sustain, and the ring returning to idle is the end | A visible "boost ending" beat if the aura's return to idle feels too abrupt |
| `Superpowers/Support/support_speed_boost_3_sustain` | The party ring (`Auras/Player/support_speed_ring`) is the same picture and is what every ship shows | Nothing planned; duplicate of the ring |
| `Particles/particle_glow_dot`, `particle_streak`, `particle_ring`, `particle_shard` | No particle systems yet. `FX-Additive.mat` (URP Particles/Unlit, additive, glow dot as its texture) is ready | Hit sparks and explosions (Stretched Billboard), tinted with Start Color, per the Phase 3 "Make" list above |

### Phase 4: Powers

- **Make:**
  - **The black hole:** a core sprite, an orbiting particle spray,
    `ParticleSystemForceField` (gravity plus vortex) pulling nearby
    particles into a swirl, and an implosion pull on the grid.
  - It's then generalized into a reusable "power VFX kit" (core, spray,
    force field, grid pull) for the next powers.
- **Coordinate:** the abilities and cards that use these powers belong to
  the phase 3 developer. Build visual kits ahead of the mechanics, and hook
  them up once a mechanic exists.
- **Done when:** a black hole can be placed in a level and looks like the
  reference.
- **Mobile check:** the same effect on the low tier.

### Phase 5: Bosses

- **Make:**
  - **Marauder**, **Halcyon** and **Warden**, from the Claude Design boss
    designs (see Phase 2c's "Design first"). The prompt describes each
    fight as built: Marauder the brawler (M-pattern hops, aimed shots,
    shockwave, guided missile, Pattern Barrage, flanking minions, aggro);
    Halcyon the storm (no bullets, full-arena roam, Surge stillness window,
    Static Field pulse); Warden the fortress (two turret arms, a third in
    phase 2, Lockdown wall, shockwave). A boss is about 3× a ship's size.
  - The 3D models follow the ships' live-model route (glTFast +
    `ShipMesh.shader` + a visual child on each boss prefab); a drawn boss
    goes in as a sprite.
  - Each boss's **signature color** becomes its bullet color (see the
    readability rule).
  - **Warden's arms** as separate sprites, which matches their existing child
    GameObjects (`ArmA`/`ArmB`/`ArmC`). The arms' "down" look replaces the
    gray tint `WardenArmHealth` applies today.
  - **Telegraph visuals:**
    - Marauder/Warden: the shockwave ring.
    - Warden: the Lockdown volley warning and the Big Shot flash.
    - Halcyon: the Opening and the Static Field pulse.
  - **A Phase 2 visual change** for each boss, such as damage or a color
    shift.
- **Done when:** all three fights play with final boss art, and every
  telegraph is clear.
- **Mobile check:** a boss fight at peak effect density.

### Phase 6: Anime cut-ins

- **Decide first:** make a quick mockup of one character's cut-in each way,
  then pick one route for all four roles.
  - **2D illustrated:** a drawn character (drawn yourself or commissioned),
    split into a few layers, animated in Unity: slide in, slight parallax,
    speed lines, a flash.
    - Cheapest to run, and matches the 2D ships.
    - Each character must be drawn, and new poses mean new drawings.
  - **VRoid → Unity:** a character made in VRoid Studio, imported with
    **UniVRM**, using its **MToon** toon shader. Pose and animate it in
    Unity, then play it live or record it with **Unity Recorder**.
    - The anime-3D look, new poses for free, no Blender.
    - More setup, and a VRoid look unless refined further.
- **Make:** **a HUD card** that slides in from the side when that role's
  ability fires. This needs a small code hook on `PlayerAbility`, plus UI
  work.
- **Done when:** using the ability shows the card with the animated
  character.
- **Then:** repeat for the other three roles.
- **Mobile check:** image or video size and memory, or the cost of a live
  model if that route is picked.

### Phase 7: Pre-level screens

The screens a player passes through before a fight, in build order (see
[systems/scene-flow.md](systems/scene-flow.md)). They were plain Unity
UI buttons and text that worked, so this was mostly a restyle.

**Status (Session 54): PC done.** All five screens follow the
`Pre-Fight Menus` design on PC, full window (layout 1a for the Main
Menu), with the UI kit, the two fonts, one project-wide palette
(`NeonPalette`), live 3D ships on the role cards, a persistent
`MenuBackground` scene with the drifting fleet, and the design's
transitions. Details in [systems/ui-kit.md](systems/ui-kit.md). Still
open: the mobile layouts, a real logo for the wordmark slot, boss art on
the Level Select cards (Phase 5), QUIT's confirm modal, and a check with
real gamepads. The table below is the original brief.

| Screen | What it does today | What the art adds |
| --- | --- | --- |
| `MainMenu` | Play, Quit | The game's title/logo, and a live background: the Phase 3 warping grid with the four ships idling |
| `Lobby` | Local, Online (disabled until Nakama), Back | Two large mode cards. Online shows a clear "coming later" state, not just a grayed button |
| `JoinLobby` | Four slot rows (`Slot N: Empty` / the device's control scheme), Continue once someone joins | Four slot cards with a "press any button to join" prompt and a device icon (keyboard or gamepad) per joined player |
| `RoleSelect` | Single-player (`RoleSelectUI`) and multi-player (`RoleSelectMultiUI`) pickers | One card per role: the role's ship, its color, its stats and its signature ability. Unpicked roles show the HUD design's CPU frame (dashed, dimmer) |
| `LevelSelect` | One button per level, Back | One card per boss (Marauder, Halcyon, Warden) with its art, name and level number |

- **Design first:** mockups of all five screens in the `Neon Fleet HUD`
  style, before building anything. Standalone Claude Design closes by
  December 14, 2026, so new mockups are made in Claude's Design artifacts
  instead.
- **Learn:** Unity UI (the uGUI `Canvas` the screens already use), 9-slice
  sprites for panels and frames, TextMesh Pro font assets, and simple UI
  animation (fades, slides, button hover/press states).
- **Make:**
  - **The UI kit**, reused by every screen and later the HUD:
    - panel, card and button sprites (9-sliced) with normal, hover, pressed
      and disabled states;
    - the human and CPU slot frames;
    - the two fonts as TextMesh Pro assets;
    - device icons (keyboard, gamepad) and role icons.
  - The five screens restyled with the kit, one at a time, in build order.
  - **Transitions** between screens: a short fade or slide, so the flow
    feels like one piece.
- **Depends on:** the ship models (done; shown in menus either live under
  a UI camera or as rendered portraits) and the Phase 5 boss art
  (`LevelSelect`). Until those exist, the cards use
  the current placeholders, and the art drops in later without changing
  the layout.
- **Code touchpoint:** mostly none, since the scripts only reference
  buttons and text. Two small changes: `JoinLobbyUI` writes the slot text
  as a string and would need to show a device icon instead, and the
  `RoleSelect` cards need each role's ship (its model or a rendered
  portrait of it).
- **Coordinate:** a **loadout (deck) screen** will sit between `RoleSelect`
  and `LevelSelect` once the deck layer exists. Its layout and card visuals
  belong to the phase 3 developer, but it should use the same UI kit.
- **Done when:** the whole flow from `MainMenu` to the first level plays in
  the new style, works with mouse, keyboard and gamepad, and has no
  placeholder UI left.
- **Mobile check:** the screens fit 9:16 portrait, buttons are large enough
  to tap, and nothing sits under the phone's notch or rounded corners.

### Phase 8: In-level HUD and later

- **The in-level HUD:** the squad rail and party frames, the boss panel, the
  DPS meter, and the Pause, Game Over and Victory overlays, from the
  `Neon Fleet HUD` design and with the Phase 7 UI kit.
- **Later:** card art for the deck layer, once its design settles.

## Cross-cutting

### Mobile quality tiers (from Phase 0 on)

- Unity **Quality levels** with one **URP asset per tier**:
  - bloom quality on or off;
  - how many `Light2D`s are used, or lit materials off on the lowest tier;
  - particle caps;
  - grid resolution.
- **Android texture overrides:** a smaller max size, plus compression.
- **Sprite Atlases**, to cut draw calls.
- Check on a real phone in **every phase**, not at the end.

### Asset organization

- Unity assets live in `Assets/Art/Ships` (`Models/`, `Materials/`,
  `Prefabs/`, `ShipMesh.shader`), `Assets/Art/Bosses`, `Assets/Art/VFX`,
  `Assets/Art/Backgrounds` and `Assets/Art/CutIns`. The ship GLBs live in
  `Assets/` because the game renders them directly.
- Sources Unity shouldn't import live in a top-level `ArtSource/` folder
  **outside** `Assets/`: any `.blend` files, raw renders and cut-in source
  art.

### Git LFS

LFS is enabled in this repo and tracks `*.glb`. Before committing other
art:
- add LFS tracking for `*.png`, `*.psd`, `*.blend`, `*.mp4` and `*.webm`.
  SVGs are text and stay in plain git;
- check that the remote (GitHub) LFS quota is enough.

## Open questions

- **Live models on phones** (deferred, Session 52): whether four live
  ships (30–55 meshes each), a 3D boss and bloom hold up on low-end phones,
  or need baking or mesh merging.
- **Explosion lighting on ships:** `ShipMesh.shader` uses a fixed light
  direction; reacting to `Light2D`s would need extra shader work.
- **Cut-in route:** 2D illustrated or VRoid → Unity, decided at the start of
  Phase 6. Commissioning an artist later could raise the quality of either.
- **Each boss's signature color** (its bullets and glow): proposed by the
  Claude Design boss designs, then confirmed.
- **Backgrounds beyond the first draft:** per-phase changes, the warping
  grid on top, or more layers. Decided after seeing the first draft in game.
- ~~**Screens on desktop:**~~ **Decided (Session 54):** the menus fill the
  whole window (the design's layout 1a), not the 9:16 playfield frame.
- **The game's title and logo,** needed for `MainMenu`.
- **The roll mechanic:** purely visual, or does it give i-frames? That's the
  phase 3 developer's call.
- **Audio direction** (FMOD is planned) is out of scope for this roadmap.
