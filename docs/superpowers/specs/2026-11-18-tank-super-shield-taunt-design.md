# Tank superpower: Super Shield + Taunt magnet

Date: 2026-11-18. Status: approved and built (Session 60). The second role
after the Attacker's Nova Beam
([spec](2026-11-12-attacker-nova-beam-cutin-design.md)); Medic and Support
follow.

## Why

The Attacker's E became a real superpower in Session 59. The Tank's was
still **Taunt**: instant, a 5 s cooldown, and a one-shot ring as its only
sign. The developer wanted Taunt kept but visible and nearly always
available, and a new big ability that fits the Tank's identity (the
biggest shield in the party).

## Goals, non-goals, anti-goals

- **Goals:** E feels like a superpower and plays the Tank's cut-in card;
  the team can see at a glance when the Tank is taunting; a CPU Tank uses
  both abilities on its own; the art drops in without code changes.
- **Non-goals:** Medic and Support; the phone layout; final pilot art.
- **Anti-goals:** the big wall or the magnet must never hide enemy
  bullets; the magnet must not show while the boss shoots someone else.

## Decisions

| Topic | Decision |
|---|---|
| Keys | **E = Super Shield**, **1 / gamepad Y = Taunt** (the existing `Combo1` action, which only the Attacker used). E stays "the superpower" on every role, so the cut-in always follows E |
| Super Shield | Shield refilled to max on the press; the arc grows to **2.5× its normal width and height**, centered on the ship, sized from the Tank (not the screen, which may change). 0.4 s growth, 4 s in all with a 1 s flicker and a 0.25 s collapse at the end. **Every arc hit is free** for the cast. 25 s cooldown |
| Caster during Super Shield | Free to move, not invulnerable: moving the wall to cover teammates is the point; a bullet that reaches the body still hits |
| Taunt | 3 s window, 4 s cooldown, on every boss. Marauder **holds** the taunter as its target for the window (its aggro overwrite stays); Warden's arm-bias window takes the Tank's 3 s; Halcyon ignores it |
| Taunt visual | A magnet field around the ship for the whole window (activate → 3-frame inward loop → end) |
| HUD | The Tank's cut-in card (`AEGIS-04 · TANK`, `SUPER SHIELD`, Tank cyan), its shout landing as the wall reaches full size; a Taunt box (READY / PULLING / COOLDOWN) in the combo box's spot |
| CPU Tank | Taunts on each boss's `ShouldTaunt`; casts the Super Shield when its shield is at half or below |
| Art | Combat FX and HUD from Claude Design (`ArtSource/Tank/claude-design-prompts.md`); the pilot's four 1216×362 frames drawn by the developer |

## Approaches considered

- **Keys:** E = Super Shield with Taunt on 1 (chosen), or Taunt kept on E
  with a new action for the Super Shield. The first keeps E meaning "the
  superpower" for every role and needs no new input action.
- **Arc hits during the Super Shield:** free block (chosen), draining the
  shield as usual, or half cost. A wall that size could drain all 20
  points in one barrage, so the refill would have been the wall's fuel
  rather than a reward.
- **Taunt length:** a fixed 3 s window with a 4 s cooldown (chosen), a
  3 s cooldown that can be held on permanently, or showing the magnet
  while the boss happens to target the Tank (different per boss, nothing
  on Halcyon).
- **Super Shield size:** 3/4 of the screen at first; the developer chose
  2.5× the normal arc, sized from the Tank.

## How it's built

- `TankAbilitySettings` (one Inspector block on `PlayerAbility`, the arc's
  own numbers included).
- `TankShieldArc`: the always-on arc as a component (collider, sprite,
  block flash), with `SetScale`, the Super Shield look and `FreeBlock`,
  which `Bullet.HitPlayer` checks through `TankShieldArc.Absorbs`.
  `ShieldArcShape` (the parabola, scaled around the centre line) and
  `ShieldArcSpriteFit` (end caps onto the collider) are pure.
- `TankSuperShield`: the cast coroutine, read off the pure
  `SuperShieldTimeline` (frame and scale at a moment). The expand and
  collapse frames are drawn at the full size while the collider grows and
  shrinks under them.
- `TankTaunt` + `TauntMagnetVisual` (pure `TauntMagnetTimeline`).
- `BossEvents.Taunt(boss, taunter, seconds)`.
- `CutInCardView.Bind(role, snap)` with a `CutInRoleArt` per role, driven
  by `PlayerAbility.SuperpowerElapsed` / `SuperpowerSnapSeconds`.
- `TauntKeyView` + pure `TauntDisplay`.
- `AIControllerTank`.

## Open items

- A CPU Tank's Taunt can't be clicked from its frame.
- Every number needs a playtest: cooldowns, 3.5×, 0.4 s growth, the AI's
  50% rule.

## First tuning (later in Session 60)

The developer's call, before a playtest: Taunt's cooldown goes from 4 s
to **8 s** (up 3 s out of 8, so the Tank picks its moments); the Super
Shield grows to **3.5×** instead of 2.5× (about two thirds of the
playfield's width) and lasts **9 s** instead of 4 s. The Super Shield's
cooldown stays 25 s. The decisions table above keeps the first numbers;
`TankAbilitySettings` and `Ship.prefab` hold the current ones.
