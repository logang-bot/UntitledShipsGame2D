# Attacker superpower: Nova Beam + anime cut-in card (demo)

Date: 2026-11-12. Status: approved, demo being built. Covers the Attacker
only. The other three roles follow once this one plays well.

## Why

The superpowers are where the role fantasy should peak, and today the
Attacker's E is **Big Shot**: one wide bullet on a 3 s cooldown that vanishes
on the first hit. It doesn't feel like a superpower. The fix is two things
together, the way Touhou-style spell cards work:

1. **A power that looks and plays big**: a 3 s plasma beam that burns
   everything in its column.
2. **An anime character cut-in** that announces it: the Attacker's party
   frame on the left rail turns into a card where the pilot strikes a pose.

This is art-roadmap **Phase 6** ("a HUD card that slides in from the side
when that role's ability fires"). The demo also settles that phase's open
route question: **2D illustrated** (layered character art animated in
Unity), not VRoid → Unity.

## Goals, non-goals, anti-goals

- **Goals:** pressing E feels like a superpower; the cut-in and the beam read
  as one event; it works for a human or a CPU Attacker; the character art
  can be swapped in later without code changes.
- **Non-goals (this round):** the Tank, Medic and Support powers; the phone
  layout (phones have no side rails, so the card needs another home there);
  final release character art.
- **Anti-goals:** the beam must never hide enemy bullets or look like a boss
  attack; the cut-in must not pull the player's eyes off the playfield for
  long, or freeze the game for the other players.

## The flow

```
t (s)   0      0.25        0.80                    2.30  2.55        3.80
        |-------|-----------|------------------------|-----|-----------|
E press ▶ card slides in     beam fires (flash/smear) card slides out    beam ends
        ▶ invulnerable + locked in place ................................▶ unlocked
card    [ talk 1/2 flip ][ shout + push-in ][ afterglow hold   ][ out ]
beam                     [ ramp 0.12 s | 3 s plasma column | fade 0.25 s ]
```

## Decisions

| Topic | Decision |
|---|---|
| Name | **NOVA BEAM** (after the NOVA-11 ship), replacing Big Shot |
| Caster during cast | **Invulnerable** from the E press until the beam ends (0.8 + 3 s). The game keeps running |
| Movement | **Locked in place** for the whole cast. Normal auto-fire and the 1/2/3 combo are paused too |
| Beam | From the ship's `FirePoint` to the top of the screen, **0.9 × ship width** (0.54 world units). Pierces everything in its column; does not erase enemy bullets |
| Damage | A tick every 0.1 s of `fireDamage × tickDamageMultiplier` (start 0.5, so 1 per tick for the Attacker: 30 per cast, a fifth of Marauder), never below 1 after rounding. Credited to the ship (DPS meter, aggro). A shielded Warden still takes nothing |
| Cooldown | 20 s from the press (placeholder, tune in playtest) |
| Beam look | **White core, warm Attacker colors** shifting through orange → orange-amber → red-gold, plus lightning arcs and sparks. Never a boss color (`#FF2D95`, `#FFE14A`, `#5470FF`) |
| Beam rendering | Hand-written HLSL shader (like `ShipMesh.shader`) + LineRenderer lightning + Shuriken particles |
| Cut-in placement | Over the caster's own party frame on the left rail, sliding in from the rail's left edge and back out the same way |
| Cut-in animation | **Key frames (limited animation)**, the standard anime technique. First planned as wind-up → smear → point at the camera; built with the developer's own **four static frames** of one pointing pose: two talking frames flipping every 0.12 s until the beam fires, the shout (fingertip flash, glowing suit) with a white flash, push-in and shake held 0.6 s, then an afterglow frame until the card leaves |
| CPU Attacker | Same card in its own frame. It only casts when the boss is inside its column |
| Character art | Planned as layered vector art from **Claude Fable 5.2**. The developer instead made **static whole-character PNGs** (`attacker_talk_1..4.png`, 1216×362, transparent); Unity does the slide, flash, push-in and shake |

### Why key poses, and why not ZZZ-style 3D

| Approach | Verdict |
|---|---|
| Still illustration + motion graphics (Touhou) | The genre standard, but the user wants the character to act |
| **Key poses** (anime limited animation) | **Chosen.** The pose and the "point at the camera" moment for 2–3 drawings, played with plain uGUI |
| Rigged 2D (Spine, Live2D, Unity 2D Animation) | The upgrade path: rig the same layers later for smooth in-betweens. A foreshortened point still needs its own drawing |
| 3D toon (ZZZ, Genshin) | Rejected for now: the most work by far, and the detail is lost in a 608×181 strip at the edge of the screen |

## The beam

- **Cast sequencing** (`AttackerBeamCast`): on the press, grant
  invulnerability, lock the ship and play the wind-up frames at the muzzle
  (the imported `attacker_big_shot_1..4`). After 0.8 s, spawn the beam.
  After 3 s more, unlock.
- **Damage** (`BeamColumnDamage`): every tick, one `Physics2D.OverlapBoxAll`
  over the column, filtered on the `Enemy` tag, and each target hit once
  through the shared `EnemyDamage` dispatch (moved out of `Bullet`).
- **Look:** an additive HDR shader on a stretched quad (white core band,
  noise streaks flowing up, warm edge bands shifting hue, flickering width),
  so the bloom carries the glow; jagged lightning arcs regenerated every
  ~0.05 s; spark particles at the muzzle, the top and every hit point; a
  small camera shake on fire.
- **Readability rule check:** sorting order −2 (the ability-effect layer),
  so player bullets (5) and enemy bullets (10) draw on top. Warm colors stay
  clear of all three boss colors. The core is a band, not a round
  white-cored shape, so it can't be mistaken for a bullet.

## The cut-in card

- A `CutInCard` child of `PartyFrame.prefab` (608×181 at 1080p), clipped to
  the frame. Inside: a dark Attacker panel with an orange frame, speed
  lines, one full-card portrait image, a white flash overlay, and the
  `NOVA-11 · ATTACKER` kicker over the `NOVA BEAM` title.
- Driven by the ship's own cast clock (`PlayerAbility.AttackerCastElapsed`),
  read every frame like the rest of the HUD. Pausing freezes it with the
  game.
- `CutInTimeline` handles the slide, and `CutInPoseTimeline` the frame,
  flash, push-in and shake. Both are pure and tested.
- Static whole frames rule out the hair sway and blink the layered plan had.

## Character art with Claude

- **Model:** Claude Fable 5.2, Anthropic's most capable model, for drawing
  the character. A clean anime figure in SVG is where that shows. Opus 5.5
  is plenty for the Unity work.
- **Where:** a claude.ai artifact, which previews the animation live.
  Standalone Claude Design closes on December 14, 2026.
- **What Claude can and can't make:** Claude draws by writing code (SVG),
  so expect crisp flat cel-shaded vector art, not painted art. For the
  release, a commissioned illustrator's layered PSD is the usual route.
  Steam requires disclosing AI-generated content if Claude-made art ships.
- **Prompt:** `ArtSource/CutIns/Attacker/fable-prompt.md`. Sources (SVG,
  HTML) go in `ArtSource/CutIns/Attacker/`, and the PNG layers in
  `Assets/Art/CutIns/Attacker/` (UI sprites, 200 px/unit like the HUD art).
  Check that Git LFS tracks `*.png` before committing them.

## Open items

- The pilot's character concept (look, personality) is the user's call. The
  prompt carries an editable proposal.
- Damage, cooldown and timing numbers need a playtest.
- The other three roles' powers and cut-ins.
- Where the card goes on phones, which have no side rails.
