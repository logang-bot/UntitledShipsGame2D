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
| Ships and bosses | Modeled in **Blender** and **pre-rendered** to sprites. Each frame is a color image, a normal map and a glow mask. | Detailed ships. A roll animation is just extra rendered frames. Normal maps give real-time lighting from flat images. |
| How they're lit | URP **2D Renderer**, `Sprite-Lit-Default`, `Light2D` | Already in place: `Assets/Settings/Renderer2D.asset`, ships use `Sprite-Lit-Default`, each level has a Global Light 2D. |
| Effects and powers | Neon style inspired by the Tuts+ "Neon Vector Shooter" (Shape Blaster): bloom, additive particles stretched by speed, a warping background grid, black-hole-style powers | The effects are what sell the neon look. They're independent of how the ships are made. |
| Anime ability cut-ins | Genshin/ZZZ-style characters: **VRoid Studio → Blender** (VRM add-on, toon shading), **pre-rendered** to an animation shown in a HUD card | Beginner-friendly character creation. Pre-rendering looks identical on every device and is cheap on phones. |
| Mobile | **One project** with per-platform quality settings | No duplicated code, and it keeps crossplay possible. Pre-rendered sprites cost a phone the same as any 2D image. |
| Priority | Ships, bosses, effects and powers first; HUD and menus later | |

**Pre-rendered, not imported 3D models.** The Blender model is the real
asset either way. Pre-rendering keeps Unity in its current 2D setup and keeps
phones fast. Switching to imported 3D models later wouldn't need any
remodeling.

## Art direction (style guide seed)

- A dark background, with neon emissive accents on detailed hulls. Bloom
  does most of the glowing.
- **Role colors** carry over from `PlayerRoleStats` (`Assets/Scripts/PlayerRole.cs`):
  Attacker red, Tank blue, Medic green, Support yellow. Each ship's glow uses
  its role color.
- Ships must read clearly at their real in-game size, about 0.6 world units.
  Test detail at that size, not zoomed in.
- References:
  - **Shape Blaster** (Tuts+ series): effects, particles, the grid.
  - **Nex Machina**: neon plus detailed rendered art.
  - **Genshin Impact / Zenless Zone Zero**: the cut-in characters.

### The readability rule (bullet hell)

Players must always be able to read enemy bullets through the effects:

- **Enemy bullets** have one fixed look: a bright solid core with a colored
  rim, a consistent color per boss, and the top sorting layer.
- **Player bullets** are dimmer and semi-transparent.
- **Effects and particles** draw *behind* bullets and never use the enemy
  bullets' colors or shapes.
- **Effects get an intensity setting**, which lowers particle density and
  bloom.

## Phases

Each phase lists what to **learn**, what to **make**, when it's **done**, and
a **mobile check**.

### Phase 0: Foundations

- **Learn:**
  - Blender: interface and navigation, basic modeling, materials, camera
    and rendering. Search for "Blender beginner tutorial" and pick a recent
    series.
  - Unity: the URP 2D Renderer, `Sprite-Lit-Default`, `Light2D`, a Global
    Volume with **Bloom**, and sprite import settings.
- **Make:** a cube in Blender, rendered as a color image plus a normal map,
  and imported into a test scene.
- **Done when:** the cube is lit by a moving `Light2D` (the lit side follows
  the light) and bloom makes an emissive part glow.
- **Mobile check:** run the test scene on a phone.

### Phase 1: Pipeline test with one ship (Attacker)

- **Learn:**
  - Hard-surface modeling for a small spaceship.
  - Blender render passes: normal, emission.
  - A little Blender Python, for batch rendering.
- **Make:**
  - **The Blender render setup**, to reuse for every ship:
    - a top-down orthographic camera;
    - a fixed pixels-per-unit and scale;
    - a transparent background;
    - outputs for color, normal map and glow mask.
  - **A batch-render script** that renders all passes, and later all roll
    angles, in one click.
  - **Unity import settings:**
    - PPU and pivot;
    - the normal map as a Secondary Texture `_NormalMap`, plus `_MaskTex`;
    - the glow through an emission/glow approach that works with bloom.
  - The Attacker sprite swapped onto the ship, with its collider checked
    against the new silhouette.
- **Code touchpoint:** `PlayerRoleComponent.Start()` tints the *whole*
  sprite with the role color, and `PlayerDamageFlash` reverts to that tint.
  Detailed art needs either one sprite per role or a tint applied only to
  the glow. That's a small code change, decided here.
- **Done when:** the Attacker is lit and glowing in a real level.
- **Mobile check:** an Android build of that level.

### Phase 2: The four role ships plus roll

- **Make:**
  - Ships for Attacker, Tank, Medic and Support, through the Phase 1
    pipeline.
  - **Roll frames:** 16–24 angles per ship, rendered by the batch script and
    set up as a sprite animation. They're ready for the dodge mechanic when
    it's built. Whether the roll also gives invulnerability frames is a
    gameplay decision for the phase 3 developer.
  - Visuals for the Tank's **Shield Arc** and the Medic's **aura ring**.
    Both are currently `LineRenderer`s on the ships.
- **Check:** the white hit flash (`PlayerDamageFlash`) still reads clearly
  on lit sprites.
- **Done when:** all four ships are in-game, and each roll plays smoothly.
- **Mobile check:** four ships on screen with lighting on.

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
  - **Marauder**, **Halcyon** and **Warden**, through the same pipeline at a
    larger size.
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

### Phase 6: Anime cut-in pipeline

- **Learn:**
  - VRoid Studio.
  - Blender's VRM import add-on.
  - Toon shading and outlines in Blender, using Genshin-style shading
    tutorials.
  - Simple posing and animation.
- **Make:**
  - One character for one role: created in VRoid, refined and toon-shaded
    in Blender, with a short pose animation rendered to an image sequence or
    video.
  - **A HUD card** that slides in from the side when that role's ability
    fires. This needs a small code hook on `PlayerAbility`, plus UI work.
- **Done when:** using the ability shows the card with the animated
  character.
- **Then:** repeat for the other three roles.
- **Mobile check:** video and image-sequence size, and memory.

### Phase 7: Later

The HUD and party frames, menus, fonts, icons, and card art for the deck
layer, once its design settles.

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
  `Assets/Art/VFX`, `Assets/Art/Backgrounds` and `Assets/Art/CutIns`.
- Blender sources (`.blend`) and raw renders live in a top-level `ArtSource/`
  folder **outside** `Assets/`, so Unity doesn't import them.

### Git LFS (set up before the first art commit)

Git LFS is installed, but no file types are tracked yet. `.gitattributes`
only marks `*.png` as binary. Before committing art:
- add LFS tracking for `*.blend`, `*.png`, `*.psd`, `*.mp4` and `*.webm`;
- check that the remote (GitHub) LFS quota is enough.

## Open questions

- **Cut-in quality ceiling.** VRoid plus Blender gets a convincing anime-3D
  look. Commissioning an artist later could push it further.
- **Final per-boss bullet and glow palettes.**
- **The roll mechanic:** purely visual, or does it give i-frames? That's the
  phase 3 developer's call.
- **Audio direction** (FMOD is planned) is out of scope for this roadmap.
