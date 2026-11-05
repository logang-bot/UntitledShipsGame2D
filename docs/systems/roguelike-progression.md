# Roguelike Progression (Attacker test)

**Status: experiment, on `feature/roguelike-attacker-progression` only.** It
tests one question: *is the game more fun if the Attacker grows stronger during
a fight by picking upgrades, instead of playing a manual combo?* Nothing here is
final. Every number is a test value, picked so the loop can be felt within one
fight. Design record (the original brief and the decisions behind it):
[2026-11-03-roguelike-attacker-design.md](../superpowers/specs/2026-11-03-roguelike-attacker-design.md).

## In one minute

- Every ship **fires by itself**. There is no fire button any more.
- Each mob the party kills gives the **human Attacker 1 XP**.
- XP fills **5 level-ups** (1, 2, 3, 4, then 5 XP). Each one **freezes the
  fight** and offers **3 upgrade cards**. The Attacker picks one and the fight
  resumes.
- The cards are **Attack Damage**, **Crit Strike** and **Multishot**. All three
  are offered every time, and every pick stacks.
- The Attacker's **1-2-3 combo is removed** and **Big Shot is switched off**, so
  the Attacker's whole job is moving, dodging and choosing upgrades.
- Progress lasts **one fight**. Restart starts again from level 0.

## What changes compared to `main`

| | `main` | This branch |
| --- | --- | --- |
| Firing | Hold Fire (Space / left mouse / gamepad South) | Every ship, human or CPU, fires automatically whenever it isn't frozen. The Fire input and its bindings are gone |
| Attacker manual attacks | 1-2-3 combo, Big Shot on E / gamepad West | Combo removed. Big Shot switched off (`PlayerAbility.attackerBigShotEnabled = false`), so E does nothing for the Attacker |
| Attacker power | Fixed role stats (2 damage, 2.5 shots/s) | Same base, plus whatever upgrades were picked this fight |
| Attacker party frame | Ability cell + combo box | Attack and Crit cooldown cells, an XP row with level and upgrade chips, a `LEVEL UP` tag |
| Fight flow | Never stops except Pause | Stops for every level-up until a card is picked |
| Tank, Medic, Support | Unchanged | Unchanged, apart from auto-fire |
| CPU Attacker | Plays the combo | Auto-fires at base power, gets no upgrades. Its frame says `AUTO-FIRE · NO UPGRADES (CPU)` |
| Role Select (Attacker) | `BIG SHOT` — "A wide, heavy shot. Plus a 3-attack combo." | `LEVEL UP` — "Fires on its own. Kills earn XP; each level-up adds an upgrade." |

The combo still exists on `main` and on `feature/boss-tuning-attacker-combo`, so
the two versions can be played side by side.

## The loop, step by step

### 1. Earning XP

- **Every mob killed by any ship** gives the human Attacker 1 XP. CPU
  teammates' kills count, and so does a mob that dies ramming a ship.
- **No XP** for bosses, Warden's arms, or a mob that drifts off the bottom of the
  screen.
- Only a **human** Attacker progresses. With no human Attacker in the party,
  nobody earns XP and the fight plays like `main` with auto-fire.

Where the mobs come from (current scene values):

| Fight | Before the boss | During the boss |
| --- | --- | --- |
| Marauder (Level 1) | 4 waves of 5 = **20 mobs** (one wave every 4 s for 15 s) | Waves restart in **phase 2** and keep coming every 4 s, so XP keeps flowing until MAX |
| Halcyon (Level 2) | 20 mobs, same waves | None |
| Warden (Level 3) | 20 mobs, same waves | None |

Wave mobs have 3 HP. Marauder's own minions would also give XP, but they are
switched off right now (`MarauderBoss.enableMinions = false`).

Reaching **MAX (15 XP)** before the boss means killing 15 of the 20 pre-boss
mobs (75%). On Halcyon and Warden, the level reached before the boss is the
level the Attacker keeps for the whole boss fight.

### 2. Levelling up

| Level-up | XP needed | Total XP |
| --- | --- | --- |
| 0 → 1 | 1 | 1 |
| 1 → 2 | 2 | 3 |
| 2 → 3 | 3 | 6 |
| 3 → 4 | 4 | 10 |
| 4 → 5 | 5 | 15 → **MAX** |

After level 5 the XP row reads `MAX` and kills stop giving XP.

### 3. The level-up screen

1. The kill that completes a level **freezes the fight** on the next frame:
   bullets, timers and the boss all stop, exactly like Pause.
2. The screen covers the **playfield only**. The other party frames, the damage
   meter and the boss rail dim to 35% so the freeze is obvious. The Attacker's
   own frame stays lit, so current upgrades can be read while choosing.
3. For the first **0.3 s, input is locked** (an `INPUT LOCKED` bar drains). A
   key or click already on its way from the fight is dropped, so it can't pick
   a card the player never read.
4. Card 1 takes focus. **Pick** with:
   - the keys **1 / 2 / 3** (top row or numpad), or
   - a **mouse click**, or
   - **↑ / ↓ or the D-pad** to move focus (hovering with the mouse moves it
     too), then **Enter** or **gamepad A**.
5. The picked card turns solid orange for **250 ms**, the upgrade applies
   immediately, and the Attacker's frame updates.
6. If more level-ups are **banked**, the next screen opens straight away with
   `PICK 2 / 2`, its own `LV a → b`, and a fresh 0.3 s lock. Otherwise the fight
   resumes. Picks bank when several mobs die on the same frame (a Multishot cone
   or a crit hitting a cluster can do that), or while the Attacker is dead
   (see "Known limits").

While the screen is open, **Pause (Esc / Start) is blocked** and the rail reads
`PAUSE · LOCKED`. The screen never opens on top of Pause, Game Over or Victory.
It waits until Pause is closed, and while the Attacker is dead it doesn't open
at all.

### 4. The cards

| Card | First pick | Each further pick |
| --- | --- | --- |
| **Attack Damage** | +1 damage per shot | +1 more |
| **Crit Strike** | Unlocks a crit shot every 3 s for attack damage +100% | +100% crit bonus |
| **Multishot** | The attack fires 3 shots in a 15°-spaced cone | +2 shots, same spacing, so the cone widens |

Each card shows its live effect as **before → after** (for example
`DAMAGE 3 → 4`, `SHOTS 5 → 7`). First picks of Crit Strike and Multishot carry
a `NEW` chip. Five pips and `×n` show how often a card was already taken.

**Attack Damage**
- A flat whole number, because mobs round incoming damage to whole numbers. A
  +25% card would turn 2 into 2.5, which rounds back to 2 and does nothing.
- Raises both the normal attack and Crit Strike.
- The first pick matters a lot against mobs: at 2 damage a 3 HP mob takes two
  hits, at 3 damage it takes one.

**Crit Strike**
- Fires **by itself every 3 s** once unlocked. The first one comes 3 s after
  the pick. It never misses its timing, but it can still miss its target.
- Damage = attack damage × (1 + crit bonus). With 2 damage and one pick that is
  2 × 2 = 4. Every further pick adds another +100%.
- One straight shot from the nose: a long **vermilion lance** (`#FF5A1F`, kept
  off pure red so it never reads as enemy fire), with a muzzle flash and an
  impact burst. Its hitbox is 1.5× a normal shot's width. Multishot doesn't
  affect it.

**Multishot**
- Replaces the single shot with a cone: 3 shots, then 5, 7, 9, 11, spaced 15°
  apart (11 shots span ±75°).
- **Every shot deals full damage.**
- The cone rewards position. Close under a big boss, most shots hit. At range,
  the outer shots fly wide. It is probably the strongest card (see below), so
  watch it in the playtest.

## What the upgrades do to the numbers

Damage per second against one target **if every shot hits**. Base team for
comparison: Attacker 5, Support 2, Medic ≈1, Tank 1, so about 9 for the whole
party. Boss health: Marauder 90, Halcyon 110, Warden 130.

| Attacker after 5 picks | Damage per shot | Shots per attack | Crit | DPS (all hits) |
| --- | --- | --- | --- | --- |
| No upgrades (level 0) | 2 | 1 | — | 5 |
| 5 × Attack Damage | 7 | 1 | — | 17.5 |
| 5 × Crit Strike | 2 | 1 | 12 every 3 s | 9 |
| 5 × Multishot | 2 | 11 | — | 55 |
| 2 Damage, 2 Multishot, 1 Crit | 4 | 5 | 8 every 3 s | ≈ 53 |

The Multishot rows assume the whole cone lands, which only happens point-blank on
a large target. Real numbers will be lower, but the table shows the scale: a
levelled Attacker can out-damage the rest of the party several times over, and
**bosses have not been retuned** for that.

## How to play-test it

### Quickest way in

1. Check out `feature/roguelike-attacker-progression` and open the project.
2. Open `Assets/Scenes/Level.unity` and press Play. Played directly, the scene
   puts **you on the Attacker** with three CPU teammates, against **Marauder**.
   Marauder is the best fight for this test, because its phase 2 keeps sending
   waves.

### The real flow

Main Menu → Play → Lobby → **LOCAL** → Join Lobby → **Role Select: pick
Attacker** → Level Select → any boss. Try co-op too: with two humans, one of them must be the Attacker for
progression to run.

### Controls

See the player guide: [the fight's controls](../player-guide/the-fight.md#controls)
and [the level-up screen](../player-guide/overlays.md#level-up-this-branch).
In short: arrow keys / left stick to move, firing is automatic, E does nothing
for the Attacker here, and cards are picked with 1 / 2 / 3, a click, or
focus + Enter / A.

### What you should see

- [ ] The Attacker fires on its own from the start. Space does nothing.
- [ ] The Attacker's frame shows `ATTACK · DMG 2` with a sweep that refills
      every 0.4 s, and an XP row at `LV 0 · 0/1 XP`.
- [ ] The first wave kill fills the XP cell (it flashes white with `+1`), the
      `LEVEL UP` tag appears, and the fight freezes into the level-up screen.
- [ ] Cards are greyed with `INPUT LOCKED` for a moment, then card 1 takes
      focus.
- [ ] Picking Multishot makes the Attacker fire a 3-shot cone, and the frame
      shows `ATTACK ×3` plus a `MULTI ×3` chip.
- [ ] Picking Crit Strike adds a `CRIT` cell with a 3 s timer, and a vermilion
      lance fires every 3 s (muzzle flash at the nose, burst on hit).
- [ ] Two level-ups in a row show `PICK 1 / 2` then `PICK 2 / 2`.
- [ ] After level 5 the XP row is one solid bar reading `MAX`.
- [ ] Esc does nothing during a level-up. After Restart the Attacker is back to
      level 0 with no upgrades.

### Iterating faster

There is no XP cheat. To reach the interesting levels sooner:
- lower the costs in `AttackerXpTrack.XpPerLevelUp` (for example
  `{ 1, 1, 1, 1, 1 }`), or
- lengthen the pre-boss waves with `LevelSequencer.minionPhase1Duration` on the
  `LevelSequencer` object in `Level.unity` (currently 15 s), or
- shorten `EnemySpawner.waveInterval` (currently 4 s) for more mobs.

## Tuning knobs

| What | Where | Now |
| --- | --- | --- |
| XP per level-up | `AttackerXpTrack.XpPerLevelUp` (code) | 1, 2, 3, 4, 5 |
| Damage per Attack Damage pick | `AttackerUpgrades.DamagePerPick` (code) | +1 |
| Crit bonus per pick | `AttackerUpgrades.CritBonusPerPick` (code) | +100% |
| Multishot shots | `AttackerUpgrades.FirstMultishotShots` / `ShotsPerExtraPick` (code) | 3, then +2 |
| Crit interval and hitbox width | `AttackerProgression` object in `Level.unity`: `critInterval`, `critWidthMultiplier` | 3 s, 1.5× |
| Cone spacing | `Ship.prefab` → `PlayerController.multishotAngleStep` | 15° |
| Big Shot on/off (for comparison) | `Ship.prefab` → `PlayerAbility.attackerBigShotEnabled` | off |
| Input lock, confirm and slide-in times | `LevelUpFlow` constants (code) | 0.3 s, 0.25 s, 0.15 s |
| Pre-boss waves | `EnemySpawner` (`enemiesPerWave`, `waveInterval`) and `LevelSequencer.minionPhase1Duration` in `Level.unity` | 5 per wave, every 4 s, for 15 s |
| Mob health | `Enemy.prefab` → `health` | 3 |

With Big Shot ticked on, E fires it again for human and CPU Attackers, but the
Attacker's frame doesn't show its cooldown in this mode.

## Questions the playtest should answer

**Feel**
- Are level-ups exciting, or an interruption? Five freezes per fight: too many,
  too few, or right?
- Are the choices real, or is one card always right? Watch Multishot.
- Is the Attacker still fun with no fire button and no combo, only movement and
  picks?
- Does the Attacker still feel like a different role from the others, or just
  "the one with the upgrades"?

**Pacing and balance**
- How many levels do you reach before the boss on each fight? Halcyon and
  Warden have no XP after the waves, so that level is final.
- How short do boss fights get with a levelled Attacker? (Bosses aren't retuned.)
- Does the 3 s crit read clearly in a busy fight, and does the lance stay
  separate from enemy fire?

**Fit with the rest of the game**
- `AGENTS.md` logs a **pre-fight loadout** as the run structure, with roguelike
  runs deferred. This picks upgrades *during* the fight.
- The deck layer (`docs/systems/cards.md`) pays for cards with **energy from
  grazing**. Level-up cards are paid for by kills, so they don't use the grazing
  economy. Would both layers coexist, or does one replace the other?
- The planned card tray also uses keys 1 / 2 / 3. If both ever ship together,
  one needs new keys.
- Removing Fire leaves the Attacker little to press. The deck layer plans a
  3-key hotbar, which would give it buttons back.

These project docs (`AGENTS.md`, `cards.md`) are deliberately unchanged until
the playtest decides whether this direction wins.

## Known limits and rough edges

- **Only the human Attacker** progresses. Tank, Medic, Support and a CPU
  Attacker get nothing.
- **No balance pass.** Bosses keep their health and patterns, so a levelled
  Attacker can end fights very quickly.
- **Co-op input is shared.** The 1/2/3 keys and UI navigation aren't tied to
  the Attacker's own device, so in local co-op another player can pick for them.
  Not tested yet.
- **XP keeps counting while the Attacker is dead**, but the level-up screen
  never opens for a dead Attacker. With no revive, those picks are never used.
  This only matters if revives are added later.
- **Banked picks** show back to back, which is intended, but it means a longer
  freeze.
- **PC layout only.** No phone layout for the level-up screen or the Attacker
  frame yet.
- **Not checked live yet:** a CPU Attacker's frame, mouse hover and gamepad
  navigation on the cards, the crit muzzle and impact frames on screen, and the
  MAX state.

## HUD

Designed in Claude Design ("Neon Fleet HUD v2", section 10 "Roguelike Test",
plus the `HUD Upgrade Card` component and the Crit Strike shot in "Combat FX
Designs"). PC only.

- **Attacker party frame.** The ability cell and combo box give way to an
  `ATTACK` cell (square sweep refilling every shot, `×n` shot-count tag, `DMG n`,
  80 ms fire flash) and, once Crit Strike is taken, a `CRIT` cell (3 s sweep,
  `+n% DMG`, 120 ms flash), then an XP row: `LV n`, one cell per XP the next
  level costs (the newest flashes white with `+1` for 200 ms), `n/next XP` or
  `MAX`, and a chip per upgrade taken (`DMG +n`, `CRIT ×n`, `MULTI ×shots`). A
  `LEVEL UP` tag sits next to the role name while a pick is banked. A CPU
  Attacker's frame shows one dashed `AUTO-FIRE · NO UPGRADES (CPU)` line.
- **Level-up screen.** Covers the playfield only (a 608-wide column on
  `HUDCanvas`), backdrop 86%. Header: `LEVEL UP`, the pilot (`NOVA-11 · P1`),
  `PICK i / n` when picks are banked, `LV a → b`, and either
  `FIGHT FROZEN · CHOOSE ONE` or the amber `+n MORE PICK AFTER THIS`. Then the
  `INPUT LOCKED` drain bar for the first 0.3 s, the three cards stacked
  vertically, and the hint bar (`1 2 3 PICK`, `↑ ↓ NAVIGATE`, `A ENTER CONFIRM`).
  Cards slide up 12 px and fade in over 150 ms.
- **Card states** (`UpgradeCardState`): rest, focus (orange frame, glow, 3 px
  inset edge, `ENTER A PICK` tag; hover and keyboard / gamepad share one focus),
  picked (solid orange, `PICKED` tag, held 250 ms), locked (55%, during the
  input lock) and dim (30%, the other cards while a pick confirms).
- **While open**, every other party frame, the damage meter and the boss rail
  drop to 35%, and the rail's pause hint reads `PAUSE · LOCKED` with a lock icon.
- **Crit Strike shot**: a 16 × 92 px vermilion (`Crit`, `#FF5A1F`) lance with a
  muzzle star at the nose and a diamond impact burst (120 ms) where it hits.

## Code

Everything lives in `Assets/Scripts/AttackerProgression/`.

| Script | Job |
|---|---|
| `AttackerProgression` | Finds the human Attacker, counts XP, applies cards, casts Crit Strike, hands itself to the Attacker's frame |
| `AttackerXpTrack` | Level curve (plain class, tested) |
| `AttackerUpgrades` | Turns pick counts into stats (plain class, tested) |
| `MobKills` | Static event that `Enemy` and `Minion` report kills to |
| `LevelUpUI` | The level-up screen's controller: open / freeze / pick / reopen if banked / close |
| `LevelUpFlow`, `PickStreak` | The screen's timing and the banked-pick count (plain classes, tested) |
| `LevelUpHeaderView`, `UpgradeCardView` (+ `UpgradeCardLooks`, `UpgradeCardCopy`, `UpgradeCardState`) | The header and the cards (`UpgradeCard.prefab`) |
| `LevelUpKeys` | The 1 / 2 / 3 pick keys, live only while the screen is open |
| `HudFreezeDimmer` | Dims the rails and locks the pause hint while the screen is open |
| `RogueFrameView` (+ `CooldownCellView`, `XpRowView`, `UpgradeChipView`) | The Attacker frame's roguelike rows, inside `PartyFrame.prefab` |
| `CooldownCellState`, `XpRowState`, `UpgradeChips` | What those rows draw (plain structs, tested) |

`MobKills` is static because mobs spawn at runtime, so there's no Inspector
slot to wire. `AttackerProgression` binds the Attacker's frame in `Start` via
`PartyFrameManager.FrameFor(PlayerRole.Attacker)`, since the frames are bound in
`PartyFrameManager.Awake`. Crit Strike fires `PlayerShotKind.Crit`: `PlayerShotLook`
dresses it with the lance sprite and gives the bullet its `impactFx`
(`CritImpactFx.prefab`); `ShipCombatFx.PlayCritMuzzle` spawns `CritMuzzleFx.prefab`
at the ship's fire point.

`PlayerController` holds the card effects as non-destructive fields
(`fireDamageBonus`, `shotsPerAttack`), so the role's base stats never change.

## Not built yet

XP for other roles, progression for an AI Attacker, rarity, persistence between
fights, and boss rebalancing for the extra Attacker power. The phone layout of
the level-up screen and the Attacker frame isn't built (PC only, like the rest
of the HUD).
