# Fable prompt: Medic cut-in character (four frames)

How to use it: open a new chat on claude.ai, pick **Claude Fable 5.2**, and
paste everything under "Prompt" below. Edit the **Character** section first
if you have your own idea for the pilot. When it looks right, download the
PNGs and put them in `Assets/Art/CutIns/Medic/` with exactly these names.
Put the source HTML in this folder (`ArtSource/CutIns/Medic/`).

To iterate, ask for changes in the same chat ("softer smile in the talking
frame", "bigger glow on the shout"). Keep the file names and canvas size
unchanged: the game looks for them.

The game sets the card's text itself: kicker **LUMEN-02 · MEDIC**, title
**SANCTUARY**. The voice line is recorded separately
(`Assets/Audio/Voice/Medic/`).

---

## Prompt

Build an HTML artifact that draws an anime character for a game's
**superpower cut-in card**, entirely in inline **SVG** (no external
images), and lets me download each frame as a **static PNG**.

### Context
This is a neon bullet-hell shooter. When the pilot fires their superpower,
a wide card slides over their slot in the HUD. The pilot talks for a
moment, then shouts as the power lands, then holds an afterglow pose until
the card leaves. The game swaps between your still images (anime "limited
animation"), and handles the slide, flash, shake, speed lines and title
text itself. So draw **only the character**, on a transparent background.

The superpower is **Sanctuary**: the pilot plants a glowing lime hexagon
dome on the battlefield that heals the team and slows enemy bullets.

### Character (edit freely)
The pilot of **LUMEN-02**, the Medic ship: a hexagon hull with a medical
cross and a dashed halo ring. Young adult, calm and warm but steady under
fire, the one who keeps everyone alive. Shoulder-length hair tucked behind
one ear, a dark flight suit with **lime** panel lines and a small glowing
cross on the collar, a slim visor pushed up on the forehead. Anime style,
cel-shaded: clean lines, two-tone shadows, sharp highlights, a neon lime
rim light from the right.

### Canvas and framing (the same for every image)
- Canvas **1216 × 362 px** (a wide strip, 3.36:1), **transparent
  background**.
- Bust shot (chest up). The character sits in the **left ~55%**, facing and
  leaning **to the right** (toward the playfield). Keep the right ~45%
  empty: the game puts the title text there.
- The character must not jump between images. The head, face, hair and
  shoulders stay in the **same place and size** in every frame; only the
  mouth, eyes, hand and glow change.
- It must read at half size (608 × 181): bold silhouette, big readable
  face, no tiny details.

### Palette
Dark navy and charcoal for the suit and shadows, Medic lime `#B6FF3C` for
accents and the rim light, soft whites and pale mint for highlights,
natural skin and hair tones. **Never use** hot pink `#FF2D95`, yellow
`#FFE14A` or blue `#5470FF`: those colors belong to the enemies.

### The images to export (exact file names)
1. `medic_talk_1.png`: mouth closed, focused eyes, one hand raised at chest
   height with a faint lime glow gathering in the palm.
2. `medic_talk_2.png`: exactly `medic_talk_1.png` with the mouth open
   (talking). Nothing else changes; the game flips between these two.
3. `medic_talk_3.png`: the shout as the dome goes up: the open hand thrust
   forward and slightly down, as if pressing the dome into place, a bright
   lime hexagon glyph flaring in the palm, mouth open in a call, eyes wide.
4. `medic_talk_4.png`: the afterglow: hand lowered, a small confident
   smile, the hexagon glow fading around the fingers.

### Preview (in the artifact)
Loop this timeline, with a "Replay" button, on a dark `#04050A`
background inside a 608 × 181 frame with a thin lime border (plus the same
frame at 2×):
- 0–0.25 s: the card slides in from the left (ease-out), flipping between
  `medic_talk_1` and `medic_talk_2` every 0.12 s.
- 0.30 s: a white flash, then `medic_talk_3` with a 1.0 → 1.06 push-in and
  a 0.15 s shake, held for 0.6 s.
- 0.90 s: `medic_talk_4` until the card leaves.
- 1.80–2.05 s: the card slides back out to the left (ease-in).

### Export
Add a **"Download PNGs"** button that renders each of the four images to a
transparent **1216 × 362 PNG** with the file names above.
