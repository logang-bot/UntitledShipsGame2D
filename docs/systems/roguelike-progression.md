# Roguelike Progression (Attacker test)

An in-fight upgrade loop, built for the human Attacker only. All numbers are
test values. Design record:
[2026-11-03-roguelike-attacker-design.md](../superpowers/specs/2026-11-03-roguelike-attacker-design.md).

## How it plays

Kill mobs → earn XP → level up → the game freezes → pick 1 of 3 cards → the
Attacker gets stronger.

- **XP:** every mob killed by any ship gives the Attacker 1 XP. A mob drifting
  off screen gives nothing, and neither do bosses. The 5 level-ups cost 1, 2, 3,
  4 and 5 XP (15 total), then the bar shows MAX.
- **Reset:** level and upgrades last for one fight. Restart begins again at 0.
- **Picking:** click a card, press 1/2/3, or navigate with keyboard or gamepad and
  submit. Input is ignored for the first 0.3s so a press already on its way can't
  pick an unread card. Pause is blocked while the screen is open.

## The cards

All three are always offered, and every pick stacks.

| Card | First pick | Each further pick |
|---|---|---|
| **Attack Damage** | +1 damage | +1 damage |
| **Crit Strike** | Red shot every 3s for attack damage +100% | +100% crit bonus |
| **Multishot** | Default attack fires 3 shots, 15° apart | +2 shots, same spacing, so the cone widens |

Attack Damage is a flat whole number because mobs round damage to whole numbers.
Crit Strike scales with Attack Damage. It is a single straight shot that Multishot
doesn't affect.

## Automatic attacking

Every ship, human or AI, fires whenever it isn't frozen. There is no fire input.

On this branch the Attacker has no manual attacks:

- The 1-2-3 combo is removed. It remains on `main` and on
  `feature/boss-tuning-attacker-combo`.
- Big Shot is switched off by `PlayerAbility.attackerBigShotEnabled`, for human and
  AI Attackers alike. Tick the field in the Inspector to compare.

## HUD

`CooldownBarUI` sits at the bottom centre of the playfield: an icon per attack,
with a wipe and timer for its cooldown and a flash when it fires. The XP bar and
level sit above the icons. The bar is hidden when no human is playing Attacker.

## Code

Everything lives in `Assets/Scripts/AttackerProgression/`.

| Script | Job |
|---|---|
| `AttackerProgression` | Finds the human Attacker, counts XP, applies cards, casts Crit Strike |
| `AttackerXpTrack` | Level curve (plain class, tested) |
| `AttackerUpgrades` | Turns pick counts into stats (plain class, tested) |
| `LevelUpUI` | Freeze-and-pick screen, on `HUDCanvas` |
| `CooldownBarUI` | Icons and XP bar, on `GameplayCanvas` |
| `MobKills` | Static event that `Enemy` and `Minion` report kills to |

`LevelUpUI` and `CooldownBarUI` build themselves in code like `DpsMeterUI`, because
they're placeholders with nothing to art-direct yet. `MobKills` is static because
mobs spawn at runtime, so there's no Inspector slot to wire.

`PlayerController` holds the card effects as non-destructive fields
(`fireDamageBonus`, `shotsPerAttack`), so the role's base stats never change.

## Not built yet

XP for other roles, progression for an AI Attacker, card art, rarity, persistence
between fights, and boss rebalancing for the extra Attacker power.
