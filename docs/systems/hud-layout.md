# HUD & Screen Layout

The in-level HUD follows the Claude Design "Neon Fleet HUD v2" design
(`Neon Fleet HUD v2.dc.html`, with its `HUD Party Frame`, `HUD Boss Panel`,
`Menu Button` and `Hint Bar` components), PC layout only (Session 57). The
phone layout and everything the design marks **PLANNED** (card tray,
energy cells, spell-card rows, focus hitbox and graze ring) are not built.
The PLANNED parts belong to the deck layer (phase 3).

Squad on the left rail, boss on the right, and nothing on the playfield
except short callouts. Bullets are the only white-cored round glowing shapes
on screen: the HUD uses square pips, square sweeps and flat fills.

## Fixed portrait playfield, pillarboxed on PC

The playfield is a fixed 9:16 portrait area on every platform. On PC it is
centered with pillarbox bars on both sides, and those bars are the HUD
rails.

### AspectRatioFitter.cs

**Attached to:** `Main Camera`. Keeps gameplay locked to 9:16 (pillarbox on
wide screens, letterbox on tall ones), `[ExecuteAlways]`, recalculates only
on screen resize. The computed `Camera.rect` is snapped to whole pixels so
the HUD rails and the viewport always agree on the same boundary (a
sub-pixel seam showed up at 1920×1080 before, Session 34).
`GetViewportPixelRect()` returns the viewport in screen pixels.

### HUDSidebarFitter.cs

**Attached to:** `HUDCanvas`. Sizes `LeftSidebar` and `RightSidebar` to the
pillarbox bars whenever the screen size or canvas scale changes. The bars
are measured in screen pixels and divided by the canvas scale factor, since
`HUDCanvas` scales with screen height. At 16:9 each rail is 656 units wide.

## Canvases

| Canvas | Render mode | Scaler | Holds |
| --- | --- | --- | --- |
| `HUDCanvas` | Screen Space - Overlay | Scale With Screen Size, 1920×1080, match height | Both rails and the three overlays |
| `GameplayCanvas` | Screen Space - Camera (Main Camera) | Scale With Screen Size, 608×1080, match height | The playfield callout banner |

Every size below is in 1080p reference units, as the design specifies, so a
1600×900 window draws the same layout at ×0.833.

## Parts

All in `Assets/Prefabs/UI/HUD/`, built from the menu UI kit (`NeonFrame`,
`TagChip`, `MenuButton`, `HintBar`, the Chakra Petch / JetBrains Mono TMP
fonts, `PaletteTint`). Icons and sprites are in `Assets/Art/UI/HUD/`:
white PNGs at 2× (200 px per unit), tinted in engine, baked once from the
design's own SVG symbols (the same set the design's "Download HUD assets"
ZIP holds). `frame_stroke`, `frame_stroke_2px`, `keycap` and `pip_empty` are
9-sliced; `tile_hazard` repeats.

| Prefab | Script(s) | What it is |
| --- | --- | --- |
| `PartyFrame` | `PartyFrameUI` + child views | One squad frame |
| `DamageMeter` | `DamageMeterView`, `DamageMeterRowView` ×4 | The live meter under the frames |
| `BossPanel` | `BossPanelView` + row views + the three feeds | The one shared boss panel |
| `CalloutBanner` | `CalloutBannerView` | Playfield callouts |
| `VictorySummary` | `VictorySummaryView`, `DamageMeterRowView` ×4 (large) | Victory's per-ship table |

The HUD only reads game state. Nothing here owns or changes gameplay
values; the one exception is the CPU ability button, which calls the same
`PlayerAbility.TryUseAbility()` the AI already calls.

## Left rail: squad

`LeftSidebar` (vertical layout, padding 18/24/20, gap 10) holds the
`SQUAD · n PILOTS · n CPU` header, `PartyFrame_1..4`, a flexible spacer, and
the `DamageMeter`.

### PartyFrameManager.cs

**Attached to:** `LeftSidebar`. `players[]` is overwritten with the live
roster by `PartySetupBootstrap` before any `Awake()`. In `Awake()` it:

- gives each ship a **seat** (`FrameSeat`): humans `P1`, `P2`... and CPUs
  `CPU 1`, `CPU 2`... in `players[]` order. The first human is the local
  player. Human vs CPU is `IsHuman(ship)` (its `PlayerInput` is enabled).
- binds the frames in **role order** (Attacker, Tank, Medic, Support), so a
  slot never moves; unused frames hide.
- adds each ship's `PlayerHealth.OnDeath` → `PartyFrameUI.OnPlayerDied` at
  runtime (spawned ships can't carry a persistent listener to the scene).
- fills the squad summary text.

`SeatOf(ship)` is read by Game Over's CPU list and the Victory table.

### PartyFrameUI.cs

The coordinator for one frame. `Initialize(ship, seat)` caches the ship's
components and paints the seat look; every `Update()` hands live state to
its child views. Frame looks (`PartyFrameLooks`): local player = solid
role stroke, glow and a 3 px inset edge; other human = dim role stroke; CPU
= dashed slate; DOWN = slate.

| View | Shows |
| --- | --- |
| `FrameIdentityView` | Who-tag (`◤ P1` filled / `P2` outlined / `◇ CPU 1` dashed), ship name (`RoleCardText.ShipName`), role, `LOW HP` tag (≤ ⅓ HP), Speed Boost buff chip |
| `FrameVitalsView` + 2× `PipRowView` | HP as 14 px square pips (amber when low), shield as 9×14 segments (fixed width per point), the n/max numbers, `BROKEN` at 0 shield. A shield over 12 points (the Tank's 20) moves to its own line |
| `FrameStatsView` | SPD, FIRE (live, with buff multipliers) and real DPS from the boss's `BossDamageLedger`. Values boosted by Speed Boost turn light purple with a `+` (the design's ▲ isn't in the fonts) |
| `AbilityCellView` | The square sweep around the icon (`AbilityCellState`): solid = ready, filling = active time left, grey track filling back up = cooldown; name, status, seconds, 3 px active bar |
| `ComboKeysView` | Attacker only: the 1-2-3 keycaps and `NEXT ▸ n` / `COMBO ✓` / `BROKEN` (`ComboDisplay`) |

`PipRowView` resizes to the ship's max each time it changes, because
`PlayerRoleComponent` applies the role's max HP and shield in `Start()`,
after the frames are bound.

**CPU ability trigger.** On a CPU's frame the ability cell is a button
(the listener is added in code, since each frame is generic until it knows
its ship): `CLICK` keycap when ready, white border and `TRIGGER` on hover,
`WAIT` + lock while cooling. The human's own frame shows no key.

**DOWN.** There is no revive, so a dead ship's frame stays in its slot,
fades to 40% under a hatch and shows a `DOWN` badge; its ability reads
`OFFLINE`.

`AbilityTimes.Read` takes the cooldown remaining and total from
`PlayerAbility` (`CooldownRemaining`, `CooldownFor(role)`) and the active
time from Speed Boost / Aura Boost. The Attacker combo reads
`AttackerComboExpectedSlot`, `AttackerComboBrokenAt` and
`AttackerComboFinishedAt`.

### DamageMeterView.cs

A Recount-style meter: one row per ship (role icon, ship name, a role-color
bar relative to the top dealer, TOTAL, DPS, %), sorted by damage
(`DamageMeterRows`, ties keep role order), refreshed every 0.2 s. **Boss
damage only**: minions and wave enemies are not counted. When the boss
dies its ledger's clock stops, so the meter freezes as
`DAMAGE · BOSS DOWN` with a lock icon. Bound by `LevelSetup`'s `onSpawned`.

## Damage tracking: BossDamageLedger

Each boss owns a `BossDamageLedger` (`IBoss.DamageLedger`), a plain class
holding raw damage per ship plus the fight clock: `Record(source, amount)`,
`GetDamageDealt(ship)`, `Dps(ship)`, `CombatElapsed`, `BeginCombat()`
(called in each boss's `OnEnable`, the moment `LevelSequencer` starts the
fight) and `EndCombat()` (in `Die()`). It outlives the boss GameObject, so
the HUD keeps reading the final numbers after the kill. `Bullet` passes its
owner ship into every boss's `TakeDamage`. Warden also records hits on its
arms, since the arms are the fight's only target while the core is
shielded. Damage stays separate from Marauder's aggro table, because a
taunt overwrites aggro.

## Right rail: boss

`RightSidebar` (padding 18/24/20, gap 12) holds the `BOSS` label, the
`BossPanel`, a spacer and the `ESC  START  PAUSE` key hint.

### BossPanelView.cs

The one shared panel. It only draws; each boss has a **feed** that reads the
boss every frame and pushes plain states into it. Hidden until the
boss-entry callout leaves (`Reveal()`, also on `BossCombat`).

- **Header** (`BossHeaderView`): boss mark, fight kind (`RAID · 01`,
  `POSITIONING · 02`, `COVERAGE · 03`), name, `PHASE n`, `SHIELDED` tag, HP
  number / % / 28 px bar. While shielded the bar is hatched with
  `CORE SHIELDED · HP CAN'T DROP`. `DEFEATED` banner on the kill.
- **Warnings** (`WarningBarView`, up to 2): solid boss color, ink text,
  hazard edge, draining base line, optional target role chip. The only
  solid boss-color fill on the rail, so it can't be mistaken for a
  cooldown. Flips to an outlined bar once the attack has fired.
- **OPENING!** (white, the only good-news row), **aggro** (`AggroRowView`:
  role chip, ship, `TAUNTED`), **arms** (`ArmCardView`, Warden: NOT ACTIVE /
  IDLE / LOCK + role / DOWN n s), **cooldown tiles** (`CooldownTileView`,
  3-up: radial ring filling in the boss color, `READY`, solid `NOW` disc
  while the attack runs).

Colors come from `BossPalette` (signature / dim / bg per boss).

| Feed | Warnings | Other rows | Tiles |
| --- | --- | --- | --- |
| `MarauderPanelFeed` | `MISSILE LOCK-ON` ▸ target, then `MISSILE FIRED` while it lingers; `BARRAGE · FAN/RING/SPIRAL` during the wind-up | Aggro on `CurrentTarget` (TAUNTED when it's the Tank) | Shockwave, Missile, Barrage |
| `HalcyonPanelFeed` | `SURGE` while it telegraphs | OPENING! while the Surge is active | Surge, Static Field |
| `WardenPanelFeed` | `INCOMING: LEFT/RIGHT/TOP`, `BIG SHOT!` | SHIELDED header, taunt window as aggro, arms A/B/C | Shockwave, Big Shot, Lockdown |

Feeds time warnings themselves (`WarningClock`: how long a telegraph flag
has been on, against the mechanic's public `telegraphTime`) and learn each
cooldown's total from the largest remaining time seen (`CooldownClock`,
via `CooldownTileFeed`). So the boss mechanics needed no new timers.

## Playfield callouts

`LevelCallouts` (on `GameplayCanvas`) polls `LevelSequencer.CurrentState`
and plays `CalloutBannerView`: a band in the upper third with kicker /
title / sub, no backing panel. Timings from the design (`CalloutSpecs`):

| Callout | When | Look | In / hold / out |
| --- | --- | --- | --- |
| `STAGE 01 · WAVE` | `MinionPhase1` | 1 px cyan lines | 160 / 1200 / 240 ms |
| `WARNING · <BOSS> · <kind>` | `BossEntrance` | Hazard bands in the boss color, scrolling at 40 px/s | 200 / 1800 / 300 ms |
| `<BOSS> · PHASE 2` | the boss's `OnPhase2` | Lines in the boss color (Warden: `SHIELDED`) | 120 / 900 / 200 ms |

## Overlays

Full screen on `HUDCanvas`, covering both rails. Each uses the kit's
`MenuButton` (primary LG 64 + secondary MD 52) and `HintBar`, and
`OverlayFocus` puts focus on the first button.

- **PausePanel** (`PauseUI` on `PauseController`): 86% backdrop,
  `<BOSS> · <kind>` kicker, PAUSED, Resume / Restart / Change Roles / Quit to
  Main Menu.
- **GameOverPanel** (`GameOverUI`): 80% backdrop so the CPUs can be seen
  still fighting, `GAME OVER` / `ALL PILOTS DOWN`, the
  `CPU STILL FIGHTING` list (`CpuStillFightingView`, hidden when no CPU is
  alive), Restart / Change Roles.
- **VictoryPanel** (`VictoryUI`): 90% backdrop, `VICTORY` in the boss color,
  `<BOSS> DEFEATED`, the per-ship summary (`VictorySummaryView`: ship,
  pilot, total, DPS, %), Restart / Change Roles.

`BossNameLabel` fills the boss name into the kickers and titles, bound by
`onSpawned`. The overlays' mutual-exclusion guards are unchanged.

## Scene wiring (`Level.unity`)

`LevelSetup.bosses[]`, per level:

| Event | Listeners |
| --- | --- |
| `onSpawned` | that boss's feed `Bind`, `DamageMeterView.Bind`, `LevelCallouts.Bind`, the three `BossNameLabel.Bind` |
| `onPhase2` | unchanged (Marauder: `EnemySpawner.StartSpawning`) |
| `onDefeated` | that boss's feed `ShowDefeated`, `VictoryUI.Show` |

`GameOverUI.partyFrameManager` → `LeftSidebar`. It was unset before Session
57, so Game Over never triggered.

## Known Editor-only quirk: Scene view canvas visualization

Screen Space - Overlay canvases (like `HUDCanvas`) render as an oversized
flat plane near world origin in **Scene view only**. Toggle `HUDCanvas`'s
eye icon in the Hierarchy while doing world-space work.

## Not yet built

- The phone layout (top strip, squad chips) and the design's PLANNED deck
  parts.
- Live 3D ship portraits on the frames (they show the role icon).
- The "in Medic aura" buff chip: nothing tracks which ships an aura is
  reaching.
- The PC rails sliding in with the level (`ui-kit.md`'s transitions).
