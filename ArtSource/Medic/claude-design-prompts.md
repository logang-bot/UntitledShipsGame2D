# Claude Design prompts: Medic Sanctuary

Send Prompt A and Prompt C into the existing **Combat FX Designs** project
and Prompt B into the existing **Neon Fleet HUD** project. Put the exported
files in these folders, keeping the file names:

- Prompt A: `Assets/Art/CombatFX/Superpowers/Medic/`
- Prompt B: `Assets/Art/UI/HUD/icons/`
- Prompt C: `Assets/Audio/SFX/Superpowers/Medic/`

Design: `docs/superpowers/specs/2026-11-19-medic-sanctuary-support-echo-gate-design.md`.
The cut-in pilot has its own prompt: `ArtSource/CutIns/Medic/fable-prompt.md`.

---

## Prompt A: Combat FX Designs

Add a new superpower effect for the **Medic (LUMEN-02, color `#B6FF3C`)** to this Combat FX sheet, in the same style as the existing Medic aura (`medic_aura_idle` / `medic_aura_boost`). Show it over the three boss backgrounds like the others, and include it in the "Download all (ZIP)" export as transparent PNGs.

**SANCTUARY** (a 6-second superpower). The Medic plants a healing dome where it stands. The dome stays in place when the Medic flies away. Allies inside heal quickly, and enemy bullets inside slow down. The team should read it as "the safe place to regroup".

- Canvas **1024 × 1024**. A flat-topped **hexagon**, centered, with a circumradius of **384 px** (corner to center), which is 2 game units at 192 px per unit. The ship is about **115 px wide** at this scale, for reference.
- A bright double-stroke edge, small emitter nodes on the six corners, a faint hexagon-lattice fill at **10–20% opacity**, and a medical cross glyph at the center, also faint, so a ship can sit on it.
- Frames:
  - `medic_sanctuary_1_open`: the dome mid-growth, at about half size, bright, with a burst at the edge.
  - `medic_sanctuary_2_idle` and `medic_sanctuary_3_idle_pulse`: a 2-frame shimmer loop, swapped every 0.25 s. The pulse frame brightens the edge and the corner nodes.
  - `medic_sanctuary_4_ending`: thin and flickering, for the last second.
  - `medic_sanctuary_5_collapse`: the edge breaking up into dashes as it fades.
- The game scales the expand and collapse frames itself, so draw every frame at the full size.

**Readability rule:** enemy bullets are drawn on top of this effect and must stay readable through it. They also slow down inside it, so they'll linger there. Keep the fill very faint, and don't use round white-cored shapes (those read as bullets). **Never use the boss colors** `#FF2D95`, `#FFE14A` or `#5470FF`.

---

## Prompt B: Neon Fleet HUD

Add one Medic-specific icon to the party frame (PC layout), in the existing style:

- **`ico_sanctuary`**: an ability icon for the Medic's new superpower, **SANCTUARY** (a hexagon dome with a cross inside). Same size, stroke and style as `ico_supershield`, Medic lime `#B6FF3C`.

Show the Medic's frame with it (ready, active, cooldown), human and CPU, and export the icon as a PNG like the existing ones.

---

## Prompt C: Sound effects (Combat FX Designs)

Add sound effects for the Medic's **SANCTUARY** to the SFX set made for the other Combat FX, in the same style: clean, synthetic, neon arcade. Export **44.1 kHz mono 16-bit WAV** files with these names. Leave out any demo or preview mixes.

| File | What it is | Length | Notes |
|---|---|---|---|
| `medic_sanctuary_open` | The dome snapping open: a rising chime swell with a soft "whump" | 0.6–0.9 s | The superpower's main cue, but warm, not aggressive |
| `medic_sanctuary_loop` | The dome holding: a soft, airy hum with slow shimmer | ~2 s | **Must loop seamlessly**. Plays for 6 s under combat, so keep it quiet and low-busy |
| `medic_sanctuary_heal_tick` | One heal pulse on an ally inside | < 0.15 s | Very short and soft (a glassy sparkle). Plays up to 4 times a second per ally, so it must not get tiring |
| `medic_sanctuary_ending` | The dome flickering in its last second | ~1 s | A warble or stutter that warns "about to end" |
| `medic_sanctuary_collapse` | The dome breaking up and fading | 0.3–0.5 s | A falling shimmer |
| `medic_aura_tick` | The Medic's small passive heal aura touching an ally | < 0.15 s | Quieter cousin of `medic_sanctuary_heal_tick`; plays once a second |

The music and voice lines play on top of these. Dense bullet sounds play too, so leave headroom and avoid harsh highs.
