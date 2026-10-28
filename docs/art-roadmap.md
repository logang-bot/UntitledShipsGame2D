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
| Ships and bosses | **2D neon vector sprites first**, from the "Ship Designs" project in Claude Design. Each ship is an SVG, imported into Unity as a sprite. | At real in-game size (about 65 px tall on a 1080p desktop, 115 px on a 1080×1920 phone) what reads is the silhouette and the glowing trim, which the 2D set already has. It's the cheapest route, and it fits the current URP 2D setup. |
| The 3D look | **Planned (Phase 2b).** The same project has 3D versions of all four ships, saved in the repo as GLBs (`ArtSource/Ships/`). They're **pre-rendered inside Unity** by an editor script, not in Blender, and replace the 2D sprites. | The models already exist, so nothing needs modeling. Unity can render the frames itself, including the roll frames. |
| Blender | **Optional, not a foundation.** Only for refining a 3D model or rendering a cleaner normal map. | It was the biggest learning cost in the old plan, and none of the current designs need it. |
| How they're lit | URP **2D Renderer**, `Sprite-Lit-Default`, `Light2D`, with bloom doing most of the glow | Already in place: `Assets/Settings/Renderer2D.asset`, ships use `Sprite-Lit-Default`, each level has a Global Light 2D. Normal maps are optional for flat line art. |
| Effects and powers | Neon style inspired by the Tuts+ "Neon Vector Shooter" (Shape Blaster): bloom, additive particles stretched by speed, a warping background grid, black-hole-style powers | The effects are what sell the neon look. They're independent of how the ships are made. |
| Anime ability cut-ins | **Decided at the start of Phase 6**, between two routes, neither needing Blender: **2D illustrated** characters animated in Unity, or **VRoid → Unity** through UniVRM and its toon shader (MToon). | 2D matches 2D ships, and it's how Touhou's own spell-card cut-ins work. VRoid → Unity keeps the anime-3D look while skipping Blender entirely. |
| Mobile | **One project** with per-platform quality settings | No duplicated code, and it keeps crossplay possible. Sprites cost a phone the same however they were made. |
| Priority | Ships, bosses, effects and powers first; then the pre-level screens (main menu through level select); then the in-level HUD | The screens before the level show the ships and bosses, so they come after those exist. |
| Screens and HUD style | One shared **UI kit** built from the `Neon Fleet HUD` design: dark panels, neon role-colored frames, **Chakra Petch** and **JetBrains Mono** fonts. Every menu and the HUD use it. | One look across all screens, built once. Both fonts are free Google Fonts (SIL Open Font License), usable in Unity through TextMesh Pro. |

**Why 2D first, then 3D.** The 2D sprites replace the placeholders in days
and prove the import, glow and tint setup. The 3D bake then reuses that
setup, so it only swaps the images. At their real size the 3D ships' fine
detail (surface details, turret barrels, sensor dishes) shrinks to 1–2
pixels, so the bake should be judged at in-game size. What 3D really adds
is banking/roll and lighting that reacts to explosions.

## Source designs

The ship designs live in the Claude Design project "Ship Designs"
(`claude.ai/design`, project `074be517-d927-47e1-b562-9e823449cd3c`):

| File | What it holds |
| --- | --- |
| `Ship Designs.dc.html` | The four ships as 2D SVG line art (set 1a), with 96/48/32 px scale previews, and the 3D set (1b) embedded |
| `Ship Models 3D.html` + `ships3d.js` | The 3D models, built from simple shapes in three.js, with GLB/OBJ download. **Saved:** `ArtSource/Ships/aegis-04.glb`, `nova-11.glb`, `lumen-02.glb`, `echo-07.glb` |
| `Neon Fleet HUD.dc.html` | The in-level HUD: screen layout for PC (1600×900) and mobile (390×844), the four-slot squad rail (human vs CPU frames, special-attack states) and the boss panel. The source of the UI kit (Phase 7) and the HUD (Phase 8). There are **no designs yet** for the screens before a level. |

| Role | Ship | Shape | Design color |
| --- | --- | --- | --- |
| Tank | AEGIS-04 | Shield-shaped hull, side armor pods, dashed shield arc ahead | Cyan `#00E5FF` |
| Attacker | NOVA-11 | Delta wing, twin nose cannons | Orange `#FF8A3D` |
| Medic | LUMEN-02 | Hexagon hull, cross, dashed halo ring | Lime `#B6FF3C` |
| Support | ECHO-07 | Diamond hull, gyro rings, side masts | Purple `#A855F7` |

**Standalone Claude Design closes by December 14, 2026.** The four GLBs are
already in the repo. The four SVGs still need exporting, in Phase 1, before
that date. Each GLB keeps its parts and materials named (for example
`shield_emitter`, `halo_ring`, `gyro_a`, `aegis_cyan`), so the animated parts
and the emissive trim can be picked out at bake time.

## Art direction (style guide seed)

- A dark background, with neon emissive accents on dark hulls. Bloom does
  most of the glowing.
- **Role colors:** the designs and the code disagree, and Phase 1 settles it.
  The code (`PlayerRoleStats` in `Assets/Scripts/PlayerRole.cs`) uses
  Attacker red, Tank blue, Medic green, Support yellow. The designs use
  Attacker orange, Tank cyan, Medic lime, Support purple. Whichever wins
  also drives the HUD, since `PartyFrameUI` and `DpsMeterUI` tint with the
  same `tintColor`.
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

### Phase 0: Foundations

- **Learn:** the URP 2D Renderer, `Sprite-Lit-Default`, `Light2D`, a Global
  Volume with **Bloom**, sprite import settings, and Unity's built-in Vector
  Graphics importer for SVGs (`com.unity.modules.vectorgraphics`, already in
  `Packages/manifest.json`).
- **Make:** one design SVG imported as a sprite into a test scene, with a
  Global Volume and bloom.
- **Done when:** the ship sprite shows up at in-game size and its neon trim
  glows under bloom.
- **Mobile check:** run the test scene on a phone.

### Phase 1: Pipeline test with one ship (Attacker)

- **Make:**
  - **Export** the four SVGs from Claude Design into the repo (the GLBs are
    already in `ArtSource/Ships/`).
  - **Split each SVG into two layers:** the dark hull and the neon strokes.
    The strokes become the glow (a separate sprite or a mask), so the hull
    stays dark while the glow takes the role color and drives bloom.
  - **Unity import settings:**
    - SVGs imported as **textured sprites** (not vector meshes), rendered at
      2–4× in-game size for crisp downscaling, and atlas-friendly;
    - PPU and pivot;
    - the glow through an approach that works with bloom: an HDR-colored
      child sprite, or a small Shader Graph sprite shader with emission.
      Decided here.
  - The Attacker sprite swapped onto the ship, with its collider checked
    against the new outline.
- **Decide:** the role colors (see "Art direction").
- **Code touchpoint:** `PlayerRoleComponent.Start()` tints the *whole*
  sprite with the role color, and `PlayerDamageFlash` reverts to that tint.
  Multi-color art needs either one sprite per role or a tint applied only to
  the glow layer. With these designs, each role has its own ship, so the
  hull is drawn per role and only the glow needs the tint. That's a small
  code change, decided here.
- **Done when:** the Attacker is glowing in a real level.
- **Mobile check:** an Android build of that level.

### Phase 2: The four role ships plus motion

- **Make:**
  - Ships for Attacker, Tank, Medic and Support, through the Phase 1
    pipeline.
  - **Motion, done in Unity instead of rendered frames:**
    - banking: a slight horizontal squash toward the movement direction,
      plus a few hand-tweaked frames if that isn't enough;
    - engine flames that flicker and stretch with speed;
    - the animated parts from the designs as separate child sprites: the
      Tank's shield pulse, the Medic's halo spin, the Support's gyro.
  - Whether a roll also gives invulnerability frames is a gameplay decision
    for the phase 3 developer.
  - Visuals for the Tank's **Shield Arc** and the Medic's **aura ring**.
    Both are currently `LineRenderer`s on the ships. The designs already
    show them as a dashed arc and a dashed ring.
- **Check:** the white hit flash (`PlayerDamageFlash`) still reads clearly
  on the new sprites.
- **Done when:** all four ships are in-game and look alive in motion.
- **Mobile check:** four ships on screen with bloom on.

### Phase 2b: 3D bake inside Unity

The planned replacement for the 2D ships, using the saved GLBs. The 2D
sprites stay as the fallback, and as the reference for how the ships must
read at in-game size.

- **Make:**
  - Import the GLBs into a separate bake scene, not a gameplay scene. Unity
    needs a glTF importer package for this (Unity's glTFast,
    `com.unity.cloud.gltfast`). Check that it brings in the neon edge lines,
    which are stored as line primitives.
  - An editor script with an orthographic top-down camera that renders each
    ship into a `RenderTexture` and saves PNGs:
    - a color image and a glow mask (emissive parts only);
    - **roll frames:** 16–24 angles, as a sprite animation;
    - a normal map, through a small normal-output shader, if real-time
      lighting is wanted.
  - The baked frames swapped in, in place of the 2D sprites. Gameplay code
    doesn't change.
- **Blender** comes in only if a model needs refining, or Unity's normal
  pass isn't good enough.
- **Done when:** the baked ships replace the 2D ones and roll smoothly.

### Phase 3: Effects foundation

- **Learn:** Unity's Particle System (Shuriken), additive materials, and
  basic Shader Graph.
- **Make:**
  - **Bloom tuning** for the final look.
  - **Bullet looks** that follow the readability rule, for player bullets,
    each boss's bullets, and the wide Big Shot.
  - **Hit sparks and explosions:** particles in **Stretched Billboard** mode,
    so fast particles become streaks, with additive blending.
  - **The warping background grid**: a spring-mass grid that explosions push
    and black holes pull, ported from the Tuts+ "Warping Grid" part. It's
    the biggest custom piece in this roadmap.
- **Done when:** a full boss fight shows the new bullets, impact effects and
  a reacting grid, with enemy bullets still easy to read.
- **Mobile check:** a lower-resolution grid and a capped particle count on
  the low tier.

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
  - **Marauder**, **Halcyon** and **Warden**, designed in the same 2D neon
    style and brought in through the Phase 1 pipeline at a larger size.
    Once 3D models exist for them, they follow the Phase 2b bake like the
    ships.
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
[systems/scene-flow.md](systems/scene-flow.md)). Today they're plain Unity
UI buttons and text that work, so this is a restyle, not new behavior.

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
- **Depends on:** the Phase 2 ship sprites (`MainMenu`, `RoleSelect`) and
  the Phase 5 boss art (`LevelSelect`). Until those exist, the cards use
  the current placeholders, and the art drops in later without changing
  the layout.
- **Code touchpoint:** mostly none, since the scripts only reference
  buttons and text. Two small changes: `JoinLobbyUI` writes the slot text
  as a string and would need to show a device icon instead, and the
  `RoleSelect` cards need each role's ship sprite.
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

- Unity assets live in `Assets/Art/Ships`, `Assets/Art/Bosses`,
  `Assets/Art/VFX`, `Assets/Art/Backgrounds` and `Assets/Art/CutIns`. The
  ship SVGs go straight into `Assets/Art/Ships`, since Unity imports them.
- Sources Unity shouldn't import live in a top-level `ArtSource/` folder
  **outside** `Assets/`: the design GLBs (`ArtSource/Ships/`), any `.blend`
  files, raw renders and cut-in source art. The Phase 2b bake scene loads
  the GLBs from there, or a copy goes into `Assets/` only for the bake.

### Git LFS

LFS is enabled in this repo and tracks `*.glb`. Before committing other
art:
- add LFS tracking for `*.png`, `*.psd`, `*.blend`, `*.mp4` and `*.webm`.
  SVGs are text and stay in plain git;
- check that the remote (GitHub) LFS quota is enough.

## Open questions

- **Role colors:** the code's red/blue/green/yellow or the designs'
  orange/cyan/lime/purple. Settled in Phase 1.
- **How much of the 3D bake survives at in-game size,** and whether it
  needs a normal map for real-time lighting.
- **Cut-in route:** 2D illustrated or VRoid → Unity, decided at the start of
  Phase 6. Commissioning an artist later could raise the quality of either.
- **Final per-boss bullet and glow palettes.**
- **Screens on desktop:** do the menus fill the whole window, or stay
  inside the 9:16 playfield frame with the sidebars like the levels?
  Decided with the Phase 7 mockups.
- **The game's title and logo,** needed for `MainMenu`.
- **The roll mechanic:** purely visual, or does it give i-frames? That's the
  phase 3 developer's call.
- **Audio direction** (FMOD is planned) is out of scope for this roadmap.
