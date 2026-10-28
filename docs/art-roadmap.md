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
| A 3D look, if wanted later | **Optional upgrade.** The same project has 3D versions of all four ships, built from simple shapes in code and downloadable as GLB. They'd be **pre-rendered inside Unity** by an editor script, not in Blender. | The models already exist, so nothing needs modeling. Unity can render the frames itself. Only taken if the 2D ships look flat in motion. |
| Blender | **Optional, not a foundation.** Only for refining a 3D model or rendering a cleaner normal map, if the 3D upgrade happens. | It was the biggest learning cost in the old plan, and none of the current designs need it. |
| How they're lit | URP **2D Renderer**, `Sprite-Lit-Default`, `Light2D`, with bloom doing most of the glow | Already in place: `Assets/Settings/Renderer2D.asset`, ships use `Sprite-Lit-Default`, each level has a Global Light 2D. Normal maps are optional for flat line art. |
| Effects and powers | Neon style inspired by the Tuts+ "Neon Vector Shooter" (Shape Blaster): bloom, additive particles stretched by speed, a warping background grid, black-hole-style powers | The effects are what sell the neon look. They're independent of how the ships are made. |
| Anime ability cut-ins | **Decided at the start of Phase 6**, between two routes, neither needing Blender: **2D illustrated** characters animated in Unity, or **VRoid → Unity** through UniVRM and its toon shader (MToon). | 2D matches 2D ships, and it's how Touhou's own spell-card cut-ins work. VRoid → Unity keeps the anime-3D look while skipping Blender entirely. |
| Mobile | **One project** with per-platform quality settings | No duplicated code, and it keeps crossplay possible. Sprites cost a phone the same however they were made. |
| Priority | Ships, bosses, effects and powers first; HUD and menus later | |

**Why 2D first and not the 3D models.** At their real size the 3D ships'
fine detail (surface details, turret barrels, sensor dishes) shrinks to 1–2
pixels and reads as noise. What 3D really adds is banking/roll and lighting
that reacts to explosions. Both are nice, but neither is what makes a ship
readable. Shipping 2D first replaces the placeholders in days. The 3D path
stays open without redoing anything.

## Source designs

The ship designs live in the Claude Design project "Ship Designs"
(`claude.ai/design`, project `074be517-d927-47e1-b562-9e823449cd3c`):

| File | What it holds |
| --- | --- |
| `Ship Designs.dc.html` | The four ships as 2D SVG line art (set 1a), with 96/48/32 px scale previews, and the 3D set (1b) embedded |
| `Ship Models 3D.html` + `ships3d.js` | The 3D models, built from simple shapes in three.js, with GLB/OBJ download |
| `Neon Fleet HUD.dc.html` | A HUD design, kept for Phase 7 |

| Role | Ship | Shape | Design color |
| --- | --- | --- | --- |
| Tank | AEGIS-04 | Shield-shaped hull, side armor pods, dashed shield arc ahead | Cyan `#00E5FF` |
| Attacker | NOVA-11 | Delta wing, twin nose cannons | Orange `#FF8A3D` |
| Medic | LUMEN-02 | Hexagon hull, cross, dashed halo ring | Lime `#B6FF3C` |
| Support | ECHO-07 | Diamond hull, gyro rings, side masts | Purple `#A855F7` |

**Standalone Claude Design closes by December 14, 2026.** Export the four
SVGs and the four GLBs into the repo (see "Asset organization") in Phase 1,
so the source of truth stays in git.

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
  - **Export** the four SVGs and four GLBs from Claude Design into the repo.
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
- **Decision gate, the 3D upgrade:** after a playtest, do the ships look flat
  in motion? Only if yes, add **Phase 2b**.
- **Mobile check:** four ships on screen with bloom on.

### Phase 2b (optional): 3D bake inside Unity

Only if the Phase 2 gate says the 2D ships look flat.

- **Make:**
  - Import the GLBs into a separate bake scene, not a gameplay scene.
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
    style and brought in through the Phase 1 pipeline at a larger size. If
    Phase 2b happened, they follow the 3D route instead.
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

### Phase 7: Later

The HUD and party frames (starting from the `Neon Fleet HUD` design), menus,
fonts, icons, and card art for the deck layer, once its design settles.

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
  **outside** `Assets/`: the design GLBs, any `.blend` files, raw renders
  and cut-in source art.

### Git LFS (set up before the first art commit)

Git LFS is installed, but no file types are tracked yet. `.gitattributes`
only marks `*.png` as binary. Before committing art:
- add LFS tracking for `*.png`, `*.psd`, `*.glb`, `*.blend`, `*.mp4` and
  `*.webm`. SVGs are text and stay in plain git;
- check that the remote (GitHub) LFS quota is enough.

## Open questions

- **Role colors:** the code's red/blue/green/yellow or the designs'
  orange/cyan/lime/purple. Settled in Phase 1.
- **The 3D upgrade:** taken only if the Phase 2 gate says so.
- **Cut-in route:** 2D illustrated or VRoid → Unity, decided at the start of
  Phase 6. Commissioning an artist later could raise the quality of either.
- **Final per-boss bullet and glow palettes.**
- **The roll mechanic:** purely visual, or does it give i-frames? That's the
  phase 3 developer's call.
- **Audio direction** (FMOD is planned) is out of scope for this roadmap.
