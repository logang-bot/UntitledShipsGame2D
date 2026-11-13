# Fable prompt: Attacker cut-in character (static poses)

How to use it: open a new chat on claude.ai, pick **Claude Fable 5.2**, and
paste everything under "Prompt" below. Edit the **Character** section first
if you have your own idea for the pilot. When it looks right, download the
PNGs and put them in `Assets/Art/CutIns/Attacker/` with exactly these names.
Put the source HTML in this folder (`ArtSource/CutIns/Attacker/`).

To iterate, ask for changes in the same chat ("bigger hand in the point
pose", "sharper eyes"). Keep the file names and canvas size unchanged: the
game looks for them.

---

## Prompt

Build an HTML artifact that draws an anime character for a game's
**superpower cut-in card**, entirely in inline **SVG** (no external
images), and lets me download each pose as a **static PNG**.

### Context
This is a neon bullet-hell shooter. When the pilot fires their superpower,
a wide card slides over their slot in the HUD and the pilot strikes a pose:
a wind-up, then a snap to pointing straight at the camera as a huge plasma
beam fires. The game swaps between your still images (anime "limited
animation"), and handles the slide, flash, shake, speed lines and title
text itself. So draw **only the character**, on a transparent background.

### Character (edit freely)
The pilot of **NOVA-11**, the Attacker ship: a delta-wing striker with twin
nose cannons. Young adult, confident and fierce, short swept-back hair with
one long fringe, a sleek dark flight suit with **orange** panel lines and a
glowing collar, a slim headset with a mic boom. Anime style, cel-shaded:
clean lines, two-tone shadows, sharp highlights, a neon orange rim light
from the right.

### Canvas and framing (the same for every image)
- Canvas **1216 × 362 px** (a wide strip, 3.36:1), **transparent
  background**.
- Bust shot (chest up). The character sits in the **left ~55%**, facing and
  leaning **to the right** (toward the playfield). Keep the right ~45%
  empty: the game puts the title text there.
- The character must not jump between images. The head, face, hair and
  shoulders stay in the **same place and size** in every pose; only the
  arm, hand, torso lean and expression change.
- It must read at half size (608 × 181): bold silhouette, big readable
  face, no tiny details.

### Palette
Dark navy and charcoal for the suit and shadows, Attacker orange `#FF8A3D`
for accents and the rim light, warm whites and pale amber for highlights,
natural skin and hair tones. **Never use** hot pink `#FF2D95`, yellow
`#FFE14A` or blue `#5470FF`: those colors belong to the enemies.

### The images to export (exact file names)
1. `attacker_windup.png`: right arm pulled back at shoulder height, fist
   clenched with orange energy crackling around it; eyes narrowed, mouth
   closed.
2. `attacker_smear.png`: the in-between frame as the arm snaps forward,
   drawn as a motion smear (stretched, blurred streak shapes), with the
   shout starting. It's on screen for only about 0.07 s.
3. `attacker_point.png`: the arm thrust **straight at the camera**, index
   finger pointing at the viewer, the hand **big and foreshortened** in the
   foreground; mouth open in a shout; a bright flash on the fingertip.
4. `attacker_point_blink.png`: exactly `attacker_point.png` with the eyes
   closed (a blink). Nothing else changes.

### Preview (in the artifact)
Loop this timeline, with a "Replay" button, on a dark `#04050A`
background inside a 608 × 181 frame with a thin orange border (plus the same
frame at 2×):
- 0–0.25 s: the card slides in from the left (ease-out), wind-up image.
- 0.80 s: a white flash, the smear image for ~0.07 s, then the point image,
  with a 1.0 → 1.06 push-in and a 0.15 s shake.
- About 1.6 s: the blink image for ~0.1 s.
- 2.30–2.55 s: the card slides back out to the left (ease-in).

### Export
Add a **"Download PNGs"** button that renders each of the four images to a
transparent **1216 × 362 PNG** with the file names above.
