# UI Kit and Palette

The shared look of the pre-fight menus (art Phase 7), built from the Claude
Design "Pre-Fight Menus" design (`Pre-Fight Menus.dc.html`, with its
`Menu Button` and `Hint Bar` components). PC layouts only (1600×900
reference); the mobile layouts wait for the deferred phone work. The
in-level HUD (Phase 8) will reuse the same kit. For how the screens load
and transition, see [scene-flow.md](scene-flow.md).

## One palette: `NeonPalette`

Every color in the project comes from one file. No other script, prefab or
scene holds a color value of its own.

| Piece | What it is |
| --- | --- |
| `PaletteColor` | Enum of every token, with **explicit numbers**. Fields serialize the number, so append new tokens with a fresh number; never renumber or reuse one. |
| `NeonPalette` | `Get(token)`, `Get(token, alpha)`, `RichTextHex(token)` (for TMP `<color>` tags). The only place a color literal may live. |
| `RolePalette` | Each role's family from the design: main, dim stroke, dark card fill, and glow/fill variants (`For(role)`, `TokenFor`, `DimTokenFor`, `BgTokenFor`). `PlayerRoleStats.tintColor` reads it. |
| `PaletteSwatch` | `[Serializable]` `{ key, alpha }`, converts implicitly to `Color`. Replaces Inspector `Color` fields (shockwave/static-field rings, Medic aura, Tank shield arc, damage flash, DPS meter, explosive tint). |
| `PaletteTint` | Component that colors its sibling `Graphic` (Image, RawImage, TMP text), `SpriteRenderer` or `Camera` background from a swatch, in the editor too. Every scene camera uses it with `Bg`. |

Token groups: UI neutrals (`Bg`, `Panel`, `Line`/`Line2`, `Slate`, `Muted`,
`Text`…`Text5`, `TextSoft`, `Ink`), notice amber, boss magenta and its
shades, the four roles with dim/bg shades, the three boss signature colors,
gameplay colors (shockwave, static field, explosive tint), the old HUD
greys, and kit shades (grid line, vignette, hatch, slot, bar track).

**Known exception:** the ship model materials (`Assets/Art/Ships/Materials/`,
e.g. `edge_FF8A3D`) carry their own emission colors from the GLB import.

**Linear color space:** the project renders in linear space, so a
translucent layer blends brighter than the design's CSS (which blends in
sRGB). Where it mattered (the locked role tile's tint) the alpha is lowered
to read the same.

## Fonts

TextMesh Pro SDF assets in `Assets/Art/UI/Fonts/`, from the TTFs in
`Assets/Art/UI/Import/` (Google Fonts, SIL Open Font License):

- **Chakra Petch** Regular / SemiBold / Bold: titles, buttons, names, body.
- **JetBrains Mono** Regular / Bold: tags, kickers, step crumbs, numbers,
  hint keys.

Atlases are static and hold ASCII + Latin-1 (all of English and Spanish)
plus the design's symbols. Chakra Petch falls back to JetBrains Mono Bold
for ▸ and ◇. Neither font has ✓ or ◤, so those are kit sprites
(`kit_check`, `kit_corner`) used through `TagChipGlyph`. Chakra Petch also
contains Thai glyphs; they're never baked.

Big titles get the design's neon text-shadow through `TextGlow` (a soft
TMP underlay in a palette color). Screen-space UI isn't touched by the
camera's bloom.

## Textures and icons

- `Assets/Art/UI/Kit/`: generated once (no generator kept): `kit_frame`
  (1px, 9-slice), `kit_frame_dashed` (tiled), `kit_glow` (soft 9-slice),
  `kit_hatch` (135° stripes, tiled), `kit_grid` (44 px cell), `kit_vignette`
  (radial), `kit_gradient` (vertical), `kit_check`, `kit_corner`. All white,
  tinted by token.
- `Assets/Art/UI/Import/`: PNGs exported from the design: `ico_kbm`,
  `ico_pad`, `ico_lock` (white, tinted), role icons `ico_atk/tank/med/sup`
  and boss placeholders `boss_1..3` (own colors).
- `Assets/Art/UI/Portraits/<Role>Portrait.renderTexture`: the live 3D ships
  on the role cards, rendered by `MenuBackground`'s `ShipPortraitStudio`
  (see [scene-flow.md](scene-flow.md)).

## Parts (`Assets/Prefabs/UI/`)

| Prefab / script | Use |
| --- | --- |
| `MenuButton` | The design's Menu Button. States normal / hover / focus / pressed / disabled from `MenuButtonStyles` (primary and secondary). Focus = hover colors + white outline 5 px outside + ▸. Disabled = lock glyph and `disabledReason` under the button (`SetDisabledReason`). The uGUI `Button` keeps click, navigation and `interactable`; its own transition is None. Sizes: LG 64 (prefab), MD 52 (set per instance). |
| `NeonFrame` | Panel/card frame: glow, fill, hatch, gradient, stroke (solid or dashed) layers, colored by a `NeonFrameLook`. Presets: `NeonFrameLooks` (panel, CPU, locked, joined, open slot, boss, mode) and `RoleFrameLooks` (role cards, tiles, squad chips). |
| `TagChip` | Small badge (▸ FOCUS, ◤ YOU · P1, ◇ CPU, ✓ LOCKED, TAKEN · P2, role tags, P1). Looks in `TagChipLooks`. |
| `MenuHeader` | NEON FLEET + the 01–05 step crumbs; `stepIndex` lights the current one. |
| `MenuFooter`, `HintBar`, `HintItem`, `HintKey` | Bottom bar: optional version text and the key hints, set per screen. |
| `MenuToast` | One-line notice that fades in, holds 2 s and fades out. |
| `CardFocusVisual`, `PointerClickEvent` | Focus outline/tag for a selectable card; a click handler for a non-selectable element (Lobby's locked ONLINE card). |
| `MenuHotkey` | Screen shortcut on any device (B/Esc = Back, gamepad Start = Start), built like `PauseUI`'s standalone `InputAction`. |

Role Select and Level Select views: `RoleCardView` (+ `StatBarView`,
`AbilityLineView`), `RoleTileView`, `SquadChipView`/`SquadStripView`,
`LevelCardView`. Data: `RoleCardText`, `RoleStatScale` (stat bars on one
shared scale per stat, values from `PlayerRoleStats`), `SquadLineup` (who
flies each role), `LevelCardText` (threat levels are the design's
placeholders).

## Screens

| Screen | Design | Notes |
| --- | --- | --- |
| `MainMenu` | 1a (full window) | Wordmark is a placeholder block until a logo exists. QUIT has no confirm modal yet (the design left it out of scope). |
| `Lobby` | 2a | ONLINE is a locked card: focus skips it, a click shows a toast. |
| `JoinLobby` | 3a–c | Manual joining through `JoinDeviceInput` + `JoinPressRouter` (see scene-flow.md). |
| `RoleSelect` | 4a / 4b | Live 3D ships on cards and tiles. |
| `LevelSelect` | 5a | Boss art is the placeholder glyph until the boss renders exist (art Phase 5). |

## Transitions

From the design's "Transitions" section, in `MenuScreen` / `MenuTween`
(unscaled time, so a paused game can't freeze them):

- **Forward:** current screen fades out and slides 24 px left (160 ms,
  ease-in); the next fades in from 24 px right (220 ms, ease-out); header,
  content and footer stagger by 40 ms.
- **Back:** same timings, mirrored.
- **Persistent background:** grid, vignette and the drifting fleet live in
  `MenuBackground` and never restart. The fleet glides off-screen away from
  the Main Menu.
- **Join:** a joined slot fills with a 150 ms top-down wipe; empty slots
  pulse their prompt.
- **Role pick:** the picked card pulses 1.00 → 1.03 → 1.00 (the others
  switch to the CPU look at once; the design's 200 ms fade isn't built).
- **Launch:** the chosen boss card grows to fill the screen (300 ms), then
  cuts through black (120 ms) into `Level`.

Not built from the design: the 80 ms focus-glow fade-in, and the PC rails
sliding in with the level HUD.
