# Medic Sanctuary + Support Echo Gate

Date: 2026-11-19. Status: approved (Session 64). The last two roles after
the Attacker's Nova Beam
([spec](2026-11-12-attacker-nova-beam-cutin-design.md)) and the Tank's
Super Shield ([spec](2026-11-18-tank-super-shield-taunt-design.md)).

## Why

The Medic and Support still had their first Q abilities (Aura Boost, Speed
Boost): no cut-in, no voice, no superpower art. Each role's superpower
should fill its own slot: the Attacker kills, the Tank walls, **the Medic
gives the team a place to stand**, **the Support multiplies the team's
damage**.

## Goals, non-goals, anti-goals

- **Goals:** Q feels like a superpower on both roles and plays their
  cut-in card; a CPU Medic and Support cast on their own; the art and sound
  are delegated to Claude through written prompts and drop in without code
  changes.
- **Non-goals:** the warping background grid (it was tied to a black-hole
  idea, now dropped); a second ability on E for these roles; changing how
  CPU allies position around the gate.
- **Anti-goals:** neither effect may hide enemy bullets; the Support's
  power must not become a second Tank wall.

## Decisions

| Topic | Decision |
|---|---|
| Old abilities | **Aura Boost and Speed Boost are dropped**, not moved to E. The Medic's small passive heal aura stays (the developer dropped the boost, not the aura) |
| Medic superpower | **Sanctuary**: a lime hexagon dome, radius 2 units, forms **where the Medic is and stays put** for 6 s (0.3 s open, 1 s ending flicker, 0.25 s collapse). Allies inside, the Medic included, get +1 health and +1 shield every 0.25 s; enemy bullets inside move at 40% speed, at full brightness. The Medic is free to move and isn't invulnerable. Ends early if the Medic dies. 25 s cooldown |
| Support superpower | **Echo Gate**: a horizontal purple gate, 2.4 units wide, deploys **1.5 units above the Support** and stays put for 8 s (0.3 s deploy, 1 s ending, 0.25 s collapse). Every ally's **regular** shot crossing it splits into **3 shots in a ±12° fan**, each at normal damage; split shots never split again. Combo shots and the Nova Beam don't split. Enemy bullets pass through. 25 s cooldown |
| Holding feedback | A purple tether from the Support to the gate's centre and a channel ring around the Support, for the whole cast. The Support moves freely; the tether follows |
| Support dies | The gate **shatters** at once (shatter frame and sound) |
| Credit | Split shots keep the shooter, so the damage meter and Marauder's aggro count them for the shooter. Firing through the gate can pull aggro off the Tank |
| HUD | Cut-in cards `LUMEN-02 · MEDIC` / `SANCTUARY` (lime) and `ECHO-07 · SUPPORT` / `ECHO GATE` (purple); the shout lands as the dome or gate reaches full size |
| CPU Medic | Casts when 2 or more allies within the dome's radius are at or below the existing `medicApproachThreshold` (health or shield) |
| CPU Support | Casts when the boss's X is inside the gate's span and at least one other ally is horizontally inside it too |
| Art and sound | Delegated: `ArtSource/Medic/` and `ArtSource/Support/claude-design-prompts.md` (Combat FX, HUD icon, SFX), `ArtSource/CutIns/<Role>/fable-prompt.md` (the pilots). Voice lines recorded by the developer |

All numbers are placeholders for a playtest. Three times the regular
damage through the gate is strong on purpose: it only pays off when the
team and the boss are lined up with it.

## Approaches considered

- **Medic:** Sanctuary (chosen), a channeled revive (iconic, but a human's
  own death ends the run, so it would only save CPU teammates), and
  Lifeline tethers that heal at any range.
- **Support:** a black hole (Singularity) was chosen first and then
  rejected. On the Support it worked like a moving bullet shield, the
  Tank's Super Shield in purple, and its pull looked like the Taunt magnet.
  Time Dilation and Echo (ghost copies) were the other options. The
  developer's own idea, a gate that multiplies shots, won: it's offense,
  and it asks for positioning.
- **Gate placement:** carried above the Support (looked like the Tank's
  arc), dropped in place (chosen), or a full-width line (almost no
  positioning).

## How it's built

- `Bullet` gains `Kind` (`PlayerShotKind`, set by
  `PlayerController.SpawnBullet`), `IsEcho` and a speed factor.
- Medic: `MedicSanctuarySettings`, pure `SanctuaryTimeline`
  (`SanctuaryFrame`), `SanctuaryZone` (trigger that slows enemy bullets,
  heal tick through `MedicHeal`), `MedicSanctuary` (the cast),
  `SanctuarySfx`.
- Support: `SupportEchoGateSettings`, pure `EchoGateTimeline`
  (`EchoGateFrame`) and `EchoSplit`, `EchoGateZone` (trigger that splits
  shots), `EchoGateTether`, `SupportEchoGate` (the cast, shattering on the
  Support's `OnDeath`), `EchoGateSfx`.
- `PlayerAbility` reports the cut-in clock for all four roles.
- `AIControllerMedic.ShouldCastSanctuary`, `AIControllerSupport.ShouldCastEchoGate`.

## Open items

- The Claude Design art and SFX, the pilots' frames and the voice lines;
  placeholder sprites are used until they land.
- CPU allies don't move under the gate on purpose yet.
- Every number needs a playtest.
