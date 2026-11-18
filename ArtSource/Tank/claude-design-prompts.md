# Claude Design prompts: Tank Super Shield + Taunt magnet

Send Prompt A into the existing **Combat FX Designs** project and Prompt B
into the existing **Neon Fleet HUD** project. Put the exported PNGs in
`Assets/Art/CombatFX/Superpowers/Tank/` (Prompt A) and
`Assets/Art/UI/HUD/icons/` (Prompt B), keeping the file names.

Design: `docs/superpowers/specs/2026-11-18-tank-super-shield-taunt-design.md`
(written with the implementation).

---

## Prompt A: Combat FX Designs

Add two new superpower effects for the **Tank (AEGIS-04, color `#00E5FF`)** to this Combat FX sheet, in the same style as the existing Tank shield arc (`tank_shield_idle` / `tank_shield_block`) and taunt frames. Show them over the three boss backgrounds like the others, and include them in the "Download all (ZIP)" export as transparent PNGs.

**1. TAUNT MAGNET** (a sustained aura that replaces the one-shot taunt ring). While Taunt is on, the Tank pulls every attack toward itself for 3 seconds. The player must see at a glance that it's active. Idea: a magnetic field around the ship. A horseshoe-magnet glyph floats just above the ship's nose, its poles pointing up toward the boss. Curved field lines arc from the poles around the ship, and small chevrons or dashes travel inward along them toward the ship.

- Canvas **512 × 512**, ship centered (the ship is about **115 px wide** at this scale, 192 px per game unit).
- Frames: `tank_taunt_magnet_1_activate` (the field snaps on), `tank_taunt_magnet_2_sustain_a`, `tank_taunt_magnet_3_sustain_b`, `tank_taunt_magnet_4_sustain_c` (a seamless loop with the inward motion), `tank_taunt_magnet_5_end` (the field collapses).
- It's on screen for 3 s out of every 4, so most of the fight. Keep it calm: thin lines at roughly 40–60% opacity, bright only at the magnet glyph. Keep the ship body and a ~60 px circle around its center clear (that's the hitbox area).

**2. SUPER SHIELD** (a 4-second superpower). The Tank's existing shield arc grown to **2.5× its width and 2.5× its height**, centered on the ship: a big curved energy wall in front of the Tank.

- Canvas **1024 × 512**. The arc spans exactly **900 px between its two end caps**, horizontally centered, with both caps on the same row (note the row's y in the export). Apex about **190 px above the caps**.
- Heavier than the normal arc: a double stroke, a faint hexagon lattice fill at 10–20% opacity, and small emitter nodes at both caps.
- Frames: `tank_supershield_1_expand` (mid-growth, half size, bright), `tank_supershield_2_idle` and `tank_supershield_3_idle_pulse` (a 2-frame shimmer loop), `tank_supershield_4_block` (the hit flash, like `tank_shield_block`), `tank_supershield_5_ending` (thin and flickering, for the last second), `tank_supershield_6_collapse`.

**Readability rule for both:** enemy bullets are drawn on top of these effects and must stay readable through them. Keep fills very faint, and don't use round white-cored shapes (those read as bullets). **Never use the boss colors** `#FF2D95`, `#FFE14A` or `#5470FF`.

---

## Prompt B: Neon Fleet HUD

Add three small Tank-specific pieces to the party frame (PC layout), in the existing style:

1. **`ico_supershield`**: an ability icon for the Tank's new E superpower, **SUPER SHIELD** (a big arc wall). Same size, stroke and style as `ico_taunt` / `ico_shieldarc`, Tank cyan `#00E5FF`.
2. **`ico_magnet`**: a horseshoe-magnet icon for Taunt, same style.
3. **The Tank's TAUNT box**: in the same spot and at the same size as the Attacker's combo box (the 1-2-3 keycaps), one keycap **`1`** with the magnet icon and a label, in three states:
   - **READY**: keycap with a white edge.
   - **PULLING · 2.1s**: Taunt is active (3 s). Filled cyan, the magnet glowing.
   - **COOLDOWN · 0.8s**: slate with a sweep (the cooldown is 4 s from the press).

Show the Tank's frame in all three states, human and CPU, and export the new icons as PNGs like the existing ones.

---

## Cut-in frames (drawn by the developer, same format as the Attacker's)

- Four transparent PNGs, **1216 × 362**, in `Assets/Art/CutIns/Tank/`:
  - `tank_talk_1.png`: mouth closed.
  - `tank_talk_2.png`: talking.
  - `tank_talk_3.png`: the shout, as the wall goes up (arm thrust forward or braced, cyan energy burst).
  - `tank_talk_4.png`: afterglow, holding the wall.
- Character in the **left ~55%**, facing right. Head, face and shoulders in the **same place** in all four frames. Must read at half size (608 × 181).
- Tank cyan `#00E5FF` rim light and accents. No boss colors.
- The game sets the card's text: kicker **AEGIS-04 · TANK**, title **SUPER SHIELD**.
