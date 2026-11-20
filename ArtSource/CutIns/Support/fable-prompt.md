# Fable prompt: Support cut-in character (four frames)

How to use it: open a new chat on claude.ai, pick **Claude Fable 5.2**, and
paste everything under "Prompt" below. Edit the **Character** section first
if you have your own idea for the pilot. When it looks right, download the
PNGs and put them in `Assets/Art/CutIns/Support/` with exactly these names.
Put the source HTML in this folder (`ArtSource/CutIns/Support/`).

To iterate, ask for changes in the same chat ("sharper grin on the shout",
"more glow on the headset"). Keep the file names and canvas size
unchanged: the game looks for them.

The game sets the card's text itself: kicker **ECHO-07 · SUPPORT**, title
**ECHO GATE**. The voice line is recorded separately
(`Assets/Audio/Voice/Support/`).

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

The superpower is **Echo Gate**: the pilot deploys a purple energy gate
that splits every friendly shot passing through it into three.

### Character (edit freely)
The pilot of **ECHO-07**, the Support ship: a diamond hull with spinning
gyro rings and side masts. Young adult, quick-witted and playful, the
team's signal operator who sees the whole fight at once. Short asymmetric
hair with one side shaved, a dark flight suit with **purple** panel lines,
a chunky headset with a glowing purple antenna and a small holographic
display over one eye. Anime style, cel-shaded: clean lines, two-tone
shadows, sharp highlights, a neon purple rim light from the right.

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
Dark navy and charcoal for the suit and shadows, Support purple `#A855F7`
for accents and the rim light, cool whites and pale lavender for
highlights, natural skin and hair tones. **Never use** hot pink `#FF2D95`,
yellow `#FFE14A` or blue `#5470FF`: those colors belong to the enemies.

### The images to export (exact file names)
1. `support_talk_1.png`: mouth closed, a sly half-smile, two fingers
   touching the headset as if listening in, the eye display glowing.
2. `support_talk_2.png`: exactly `support_talk_1.png` with the mouth open
   (talking). Nothing else changes; the game flips between these two.
3. `support_talk_3.png`: the shout as the gate locks in: the other arm
   swept out to the side, palm flat, a bright purple horizontal bar of
   light snapping across behind the hand, mouth open in a grin-shout.
4. `support_talk_4.png`: the afterglow: arm relaxed, a confident grin,
   three small purple light streaks fanning upward from the fingertips.

### Preview (in the artifact)
Loop this timeline, with a "Replay" button, on a dark `#04050A`
background inside a 608 × 181 frame with a thin purple border (plus the
same frame at 2×):
- 0–0.25 s: the card slides in from the left (ease-out), flipping between
  `support_talk_1` and `support_talk_2` every 0.12 s.
- 0.30 s: a white flash, then `support_talk_3` with a 1.0 → 1.06 push-in
  and a 0.15 s shake, held for 0.6 s.
- 0.90 s: `support_talk_4` until the card leaves.
- 1.80–2.05 s: the card slides back out to the left (ease-in).

### Export
Add a **"Download PNGs"** button that renders each of the four images to a
transparent **1216 × 362 PNG** with the file names above.
