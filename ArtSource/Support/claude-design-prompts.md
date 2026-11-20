# Claude Design prompts: Support Echo Gate

Send Prompt A and Prompt C into the existing **Combat FX Designs** project
and Prompt B into the existing **Neon Fleet HUD** project. Put the exported
files in these folders, keeping the file names:

- Prompt A: `Assets/Art/CombatFX/Superpowers/Support/`
- Prompt B: `Assets/Art/UI/HUD/icons/`
- Prompt C: `Assets/Audio/SFX/Superpowers/Support/`

Design: `docs/superpowers/specs/2026-11-19-medic-sanctuary-support-echo-gate-design.md`.
The cut-in pilot has its own prompt: `ArtSource/CutIns/Support/fable-prompt.md`.

---

## Prompt A: Combat FX Designs

Add a new superpower effect for the **Support (ECHO-07, color `#A855F7`)** to this Combat FX sheet, in the same style as the existing Support speed ring. Show it over the three boss backgrounds like the others, and include it in the "Download all (ZIP)" export as transparent PNGs.

**ECHO GATE** (an 8-second superpower). The Support deploys a horizontal energy gate a short distance above itself, and the gate stays in place. Every normal shot the players fire up through it **splits into three** (a narrow fan). Enemy bullets pass through it untouched. While the gate is up, the Support is visibly **holding** it, through a tether beam and a ring around the ship. If the Support dies, the gate shatters.

Scale: 192 px per game unit. The ship is about **115 px wide**.

1. **The gate** (canvas **1024 × 256**). A thin horizontal prism bar, **461 px between its two end nodes**, horizontally and vertically centered. The bar itself is about **24 px** thick, with emitter nodes at both ends (diamond-shaped, echoing the Support's diamond hull) and faint vertical "refraction" ticks along it. Frames:
   - `support_echo_gate_1_deploy`: the bar mid-deploy, bright, the nodes flaring.
   - `support_echo_gate_2_idle` and `support_echo_gate_3_idle_pulse`: a 2-frame shimmer loop, swapped every 0.25 s.
   - `support_echo_gate_4_ending`: thin and flickering, for the last second.
   - `support_echo_gate_5_collapse`: the bar retracting into the two nodes.
   - `support_echo_gate_6_shatter`: the bar breaking into shards (the Support died). Shown for about 0.3 s.
2. **`support_echo_split_spark`** (canvas **128 × 128**): the small flash where one shot crosses the gate and splits into three. A short prismatic burst with three tiny rays fanning upward. It plays many times a second, so keep it small and quick to read.
3. **`support_echo_tether`** (canvas **64 × 256**): a vertical energy beam that **tiles seamlessly top to bottom**. The game stretches it from the Support's ship up to the gate's center. Keep it thin (about 12 px of bright core) with a soft glow.
4. **`support_echo_channel_1`** and **`support_echo_channel_2`** (canvas **512 × 512**, ship centered): a ring around the Support while it holds the gate, built from the ship's gyro-ring motif. Two frames, so the ring turns or pulses. Keep the ship body and a ~60 px circle around its center clear (that's the hitbox area).

**Readability rule:** enemy bullets are drawn on top of these effects and must stay readable through them. Keep fills faint, and don't use round white-cored shapes (those read as bullets). The split shots are the players' own shots, so the spark must not look like an enemy bullet either. **Never use the boss colors** `#FF2D95`, `#FFE14A` or `#5470FF`.

---

## Prompt B: Neon Fleet HUD

Add one Support-specific icon to the party frame (PC layout), in the existing style:

- **`ico_echogate`**: an ability icon for the Support's new superpower, **ECHO GATE** (a horizontal bar with one arrow entering below and three fanning out above). Same size, stroke and style as `ico_supershield`, Support purple `#A855F7`.

Show the Support's frame with it (ready, active, cooldown), human and CPU, and export the icon as a PNG like the existing ones.

---

## Prompt C: Sound effects (Combat FX Designs)

Add sound effects for the Support's **ECHO GATE** to the SFX set made for the other Combat FX, in the same style: clean, synthetic, neon arcade. Export **44.1 kHz mono 16-bit WAV** files with these names. Leave out any demo or preview mixes.

| File | What it is | Length | Notes |
|---|---|---|---|
| `support_echo_deploy` | The gate snapping into place: a fast synth sweep up into a crisp "lock" | 0.5–0.8 s | The superpower's main cue |
| `support_echo_loop` | The gate and tether holding: a resonant, slightly phasing hum | ~2 s | **Must loop seamlessly**. Plays for 8 s under combat, so keep it quiet |
| `support_echo_split` | One shot splitting into three as it crosses | < 0.08 s | A tiny prismatic "tik". Plays **many times a second** (every ally's shots), so it must be very short, soft and not tiring |
| `support_echo_ending` | The gate flickering in its last second | ~1 s | A stutter that warns "about to end" |
| `support_echo_collapse` | The gate retracting into its nodes | 0.3–0.5 s | A falling sweep |
| `support_echo_shatter` | The gate breaking because the Support died | 0.4–0.7 s | A glassy break, clearly different from the collapse |

The music and voice lines play on top of these. Dense bullet sounds play too, so leave headroom and avoid harsh highs.
