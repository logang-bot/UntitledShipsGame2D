# HUD & Screen Layout

## Design decision: fixed portrait aspect ratio on all platforms

Fixed portrait aspect ratio (9:16) gameplay area on all platforms — not just
mobile. On phones this fills the screen naturally. On PC/desktop, the
gameplay area stays centered with pillarbox bars on either side, and those
bars are used as HUD space (party frames, boss info) rather than left empty.
A single dynamic camera-viewport approach handles both cases automatically
based on actual screen aspect at runtime; no separate build/project needed.

## AspectRatioFitter.cs

**Attached to:** `Main Camera`.
**Requires:** nothing external — self-contained, reads
`Screen.width`/`Screen.height`.

Keeps gameplay locked to a fixed portrait aspect ratio (default 9:16),
centered on screen. Handles two cases:
- **Pillarbox** (screen wider than target — the PC case): portrait game area
  centered, bars on left/right used as HUD space.
- **Letterbox** (screen narrower/taller than target): bars on top/bottom,
  game area centered vertically. Unlikely in practice given the 9:16 target
  and phone sizes, but handled automatically.

On phones already close to the target aspect, bars shrink to near-zero
automatically. No platform branching needed; it's purely aspect-driven.
Marked `[ExecuteAlways]` so it also runs in the Editor outside Play mode,
letting Game view preview the pillarbox/letterbox live. Recalculates only on
screen resize (not every frame).

**Whole-pixel snapping**: the computed `Camera.rect` is rounded to an exact
integer pixel boundary (`Mathf.Round(rect.x * Screen.width) / Screen.width`,
same for `y`/`width`/`height`) before being assigned. Without this, the
pillarbox math lands on a fractional pixel at most screen sizes (e.g. at
1920x1080 the boundary is `656.25px`) — `Camera.rect` drives the GPU
viewport, which Unity rounds to an integer pixel internally, but
`HUDSidebarFitter` reads the same rect back as an *unrounded* float
(`GetViewportPixelRect()`) to size the HUD sidebars. Those two independent
roundings could disagree by a sub-pixel sliver, visible as a thin seam at
the sidebar/viewport edge — user-reported (see `progress-log.md`'s Session
34), and only reproduced at screen sizes where the boundary isn't already a
whole pixel by coincidence (2560x1440 happens to land exactly on `875px`,
which is why it didn't show up in the first live check). Snapping here
means both sides always agree on the same integer boundary, with nothing
left to round independently downstream.

Key public fields: `targetAspectWidth`, `targetAspectHeight`.
Key public method: `GetViewportPixelRect()` — returns the current gameplay
viewport in screen pixels (now always whole numbers, see above), used by
`HUDSidebarFitter` to size the side HUD to match exactly.

## HUDSidebarFitter.cs

**Attached to:** `HUDCanvas`.
**Requires:** a reference to the `AspectRatioFitter` on Main Camera, and
`RectTransform` references to the left/right sidebar panels.

Dynamically resizes the sidebar panels every frame (on screen resize) to
exactly match `AspectRatioFitter`'s computed pillarbox bar width, closing
the gap between the gameplay viewport and the HUD. Also `[ExecuteAlways]`
for Editor preview without Play.

Key public fields: `aspectFitter`, `leftSidebar`, `rightSidebar`. Note:
`rightSidebar` points directly at `BossPanel` — there's no separate
`RightSidebar` wrapper GameObject (see Scene wiring below).

## PartyFrameUI.cs

**Attached to:** `PartyFrame.prefab` (see Prefabs below) — reusable, not
scene-specific.
**Requires:** `public void Initialize(GameObject player, string
displayName, bool isHumanPlayer)` to be called before it does anything
useful — see `PartyFrameManager.cs` below. Also holds Inspector-dragged references to
the frame's own children: avatar `Image`, health bar `Image`, shield bar
`Image` (`ShieldBar`), and seven `TextMeshProUGUI` children (name, role,
health, move speed, fire rate, **DPS**, ability) — these bake into the prefab fine
since they're internal to it.

`playerHealth`/`playerRole`/`playerController`/`playerAbility` are
**private**, set only by `Initialize()` — not Inspector fields. A prefab
asset can't hold a serialized reference to a specific scene's `Player`
object, so `Initialize(GameObject player, string displayName, bool
isHumanPlayer)` does the four `GetComponent<>()` lookups, then runs
one-time setup: `nameText`'s text is set to the passed-in `displayName`
(`PartyFrameManager.cs` decides what that string is — see below), role
text set to `"Role: " + PlayerRole`, and tints
avatar/health-bar/role-text to `Stats.tintColor`, matching the ship
sprite's tint — `shieldBarFill` is deliberately **not** tinted, always
shield-blue.
`Update()` early-returns until `Initialize()` has run, then keeps health
bar `fillAmount`, `"HP: current/max"` text, `shieldBarFill.fillAmount`
(`CurrentShield`/`maxShield`, see [player-roles.md](player-roles.md)'s
"Shield stat"), move-speed/fire-rate text (read live from
`PlayerController` every frame — each ship's fixed per-role base times its
current buff multiplier, e.g. `"Fire Rate: {shotsPerSecond *
fireRateBuffMultiplier:0.0}/s"`, so the display updates live during
Support's party-wide Speed Boost — see [player-roles.md](player-roles.md)'s
"Fixed per-role stats"), and `abilityText` (`"{AbilityName}: {StatusText}"`,
reading `PlayerAbility`'s public status getters — see
[player-roles.md](player-roles.md)) up to date. `OnPlayerDied()` grays out
`shieldBarFill` too, alongside `healthBarFill`. `PartyFrameUI` never
computes cooldown/buff/shield math itself, only formats what
`PlayerHealth`/`PlayerAbility` already expose — same "HUD only reads, never
owns game state" pattern throughout. Only real, data-backed stats are
shown — no "Attack"/"Defense" labels, since those aren't mechanics that
exist yet.

Key public fields: `healthBarFill`, `shieldBarFill`, `avatarImage`,
`nameText`, `roleText`, `healthText`, `moveSpeedText`, `fireRateText`,
`abilityText`, `abilityButton`. Key public method: `Initialize(GameObject,
string, bool)`.

**Manual ability triggering**: `abilityText`'s GameObject also carries a
`Button` component (`abilityButton`) — the text doubles as its own click
surface rather than a separate button element, since it already shows
exactly the state a trigger needs (e.g. `"Taunt: Ready"`). `Initialize()`
hides/disables this button on the human's own frame (`isHumanPlayer`,
computed by `PartyFrameManager.cs` from the same `AIController`-presence
check it already used for the CPU display name) and, for teammate frames,
wires its `onClick` to that ship's `PlayerAbility.TryUseAbility()` — the
same public, cooldown-gated method `AIController.cs`'s auto-retry and the
human `Player`'s own `OnAbility(InputValue)` already call. `Update()`
additionally drives `abilityButton.interactable` off `CooldownRemaining`
each frame, so it visibly greys out during cooldown rather than silently
no-oping on click. See [player-roles.md](player-roles.md)'s
"PlayerAbility.cs" for the full mechanics writeup, including why the
`onClick` wiring happens in code rather than as an Inspector persistent
listener.

## PartyFrameManager.cs

**Attached to:** `LeftSidebar`.
**Requires:** `players[]` and `partyFrames[]` — parallel arrays,
Inspector-dragged (index 0 = `Player`, 1-3 = `Teammate_Tank`/`Teammate_Medic`/
`Teammate_Support`; matching `PartyFrame_1..4`), matching this project's
explicit-wiring style (no `FindObjectOfType`).

In `Awake()`, loops over both arrays, computing a display name per slot
before calling `Initialize`: whichever ship has no `AIController` component
(present on all 3 `Teammate_*`, absent from `Player` — the same signal
[marauder-boss.md](bosses/marauder-boss.md) uses for this distinction) is the human and shows
`"Player 1"`; every other slot shows `"CPU " + n`, numbered in array order
(so `"CPU 1"`/`"CPU 2"`/`"CPU 3"` for whichever 3 GameObjects aren't the
human, independent of their current role). Using `AIController` presence
rather than a raw index (`i == 0`) keeps this correct even if `players[]`'s
wiring order ever changed. Using `Awake()` (not `Start()`) matters: Unity
guarantees every object's `Awake()` finishes before any `Start()` begins,
so this runs before anything could observe a half-initialized frame.

Not a real runtime spawner — the 4 slots are fixed, hand-wired Inspector
references, not a loop that reacts to however many players/teammates
actually exist. `PartyFrameUI.cs` itself needs no changes to support a
different player count, since `Initialize(GameObject, string, bool)` only
does generic `GetComponent<>()` lookups (plus setting the passed-in
display name and human/AI flag) that work on any player-shaped
GameObject, human or AI-controlled —
a "loop over connected players and `Instantiate()`s `PartyFrame.prefab`
per player" version is the natural extension once local co-op (a variable
player count) exists; that version would need its own human-vs-AI naming
scheme too, since the current `AIController`-presence check is unaffected
by player count.

Key public fields: `players[]`, `partyFrames[]`.

### DPS line

`dpsText` (`DpsText`, directly below `FireRateText` in `InfoColumn`) shows
this ship's **real** damage-per-second dealt to the boss —
`MarauderBoss.GetDamageDealt(ship) / MarauderBoss.CombatElapsed`, read via
`playerController.bossObject as MarauderBoss` (`bossObject` is already
wired for contact-damage collision, so no extra Inspector reference was
needed — see `../architecture.md`'s "Boss-type-agnostic orchestration:
IBoss"). The cast is `null` for a Halcyon-side ship (no `GetDamageDealt`
equivalent exists there), which the existing null guard already handles
for free. It is **null-guarded** in `Update()`, matching `shieldText`, so a
`PartyFrame` instance predating the line keeps working rather than
throwing, and reads `0.0` before combat starts (`CombatElapsed` is `0`
until then).

Originally showed `PlayerController.CurrentDps` (`fireDamage x
shotsPerSecond` — a static "if every normal shot lands" ceiling) instead.
Switched once the Attacker combo landed: combo/Big Shot hits fire through
`FireBigShot()` with their own computed damage, entirely bypassing
`fireDamage`, so `CurrentDps` never moved even while the boss was visibly
taking bonus damage from a correctly-played rotation — indistinguishable
from the number being broken. `CurrentDps` itself is untouched and still a
valid stat (see `PlayerRole.cs`'s `RoleStats.Dps`), just no longer surfaced
here.

Read it against the `DpsMeter` panel below: this line is one ship's own
number, the meter is the whole party's damage compared side by side — same
underlying data, different scope.

## DpsMeterUI.cs

**Attached to:** an empty `RectTransform` GameObject under
`HUDCanvas/LeftSidebar` (sibling of `PartyFrame_1..4`, below them).
**Requires:** one Inspector reference — `boss`, the scene's `Boss` — exactly
like `BossPanelUI.cs`. Nothing else needs wiring.

A Recount-style damage/DPS meter for the boss fight: one bar per ship,
sorted by damage descending, showing total damage, DPS, and percent of party
damage. Bars are tinted with the ship's own `PlayerRoleStats.tintColor`, so a
row reads as the same ship as its party frame and its sprite.

**Boss damage only.** Minion and wave-enemy damage is deliberately excluded —
the meter exists to compare each role's contribution to the encounter that
matters, and counting trash would drown that signal.

**Built procedurally**, not from a prefab with Inspector-dragged children —
the one UI script here that does. Two reasons it's the exception: the row
count is genuinely variable (1–4, however many ships the party ended up
with), and a meter is telemetry rather than authored layout, so there's
nothing to art-direct. It builds its own panel `Image`,
`VerticalLayoutGroup`, `LayoutElement`, title, and rows in `Build()` on
`Start()`, and declares its own `preferredHeight` so `LeftSidebar`'s vertical
layout can size it.

Bar length is anchor-driven (`RectTransform.anchorMax.x`), **not**
`Image.fillAmount` — `fillAmount` needs a real sprite to behave, and these
are sprite-less colored rects. Repaints are throttled to `refreshInterval`
(0.2s) rather than running every frame, since reformatting four rows of text
per frame is pure string garbage for no readability gain.

**Data source:** `MarauderBoss.GetDamageDealt(GameObject)` and
`MarauderBoss.CombatElapsed` (see
[marauder-boss.md](bosses/marauder-boss.md)'s "Damage tracking"). The DPS denominator is
time since boss combat began, not since the scene loaded.

When the boss dies its GameObject is destroyed, so the meter freezes on the
final numbers and retitles to `DAMAGE · BOSS DOWN` rather than blanking —
the end-of-fight totals are the point of having one.

Key public fields: `boss`, `titleHeight`/`rowHeight`/`rowSpacing`/`padding`,
`titleFontSize`/`rowFontSize`, `refreshInterval`, and the color set
(`panelColor`/`titleColor`/`barTrackColor`/`rowTextColor`/`fallbackBarColor`).

## Scene wiring

### Main Camera

| Component               | Setting                                        |
| ------------------------ | ------------------------------------------------- |
| Camera                   | Projection: Orthographic, Size: 5                  |
| Tag                      | `MainCamera` (required — `Camera.main` depends on it) |
| **AspectRatioFitter.cs** | targetAspectWidth: 9, targetAspectHeight: 16       |

### HUDCanvas

Render Mode: **Screen Space - Overlay**. Spans the full window regardless of
the pillarbox. Used for sidebar content visible outside the gameplay area.

| Component               | Key inspector values                                                     |
| ------------------------ | ---------------------------------------------------------------------------- |
| Canvas                   | Render Mode: Screen Space - Overlay                                          |
| Canvas Scaler            | UI Scale Mode: Constant Pixel Size (scale factor 1) — matters for the pixel-snapping note above: 1 canvas unit = 1 real screen pixel, no separate scale factor to account for |
| **HUDSidebarFitter.cs**  | aspectFitter: drag Main Camera here, leftSidebar/rightSidebar: sidebar rect transforms |
| **PartyFrameManager.cs** | `players[]`: `Player`, `Teammate_Tank`, `Teammate_Medic`, `Teammate_Support`; `partyFrames[]`: `PartyFrame_1..4`'s `PartyFrameUI` (matching index order) |

**Children** (direct children of `HUDCanvas`, siblings of each other — there
is no `RightSidebar` wrapper):
- **LeftSidebar** — Vertical Layout Group, and also carries
  `PartyFrameManager.cs` (table above). Contains **`PartyFrame_1..4`** — 4
  instances of `PartyFrame.prefab` (see Prefabs below) — and, below them,
  **`DpsMeter`**, an otherwise-empty GameObject carrying `DpsMeterUI.cs`
  (see above), which builds its own contents at runtime. See
  [../unity-notes.md](../unity-notes.md) for Layout Group configuration
  details.
- **BossPanel** — the boss's real HP bar, phase, current target,
  guided-missile warning, and shockwave/guided-missile cooldowns, driven by
  `BossPanelUI.cs`. Full reference: [marauder-boss.md](bosses/marauder-boss.md).
- **GameOverPanel** — full-rect dark overlay + "Game Over" text + Restart/
  Change Roles buttons, `GameOverUI.cs` attached. Hidden by default (shown
  on `PlayerHealth.OnDeath`). Lives here rather than `GameplayCanvas`
  because it needs to cover the pillarbox bars too. See
  [combat.md](combat.md#gameoverpanel) for the death/restart flow.
- **VictoryPanel** — mirrors `GameOverPanel`, shown on `MarauderBoss.OnDefeated`
  via `VictoryUI.cs`. See [player-roles.md](player-roles.md)'s "Role Select
  scene".

`GameOverUI.victoryPanelRoot` is dragged to `VictoryPanel` and
`VictoryUI.gameOverPanelRoot` is dragged to `GameOverPanel` — a
mutual-exclusion guard so the 3 CPU teammates defeating the boss after the
human `Player` has already died (they keep fighting; only the human's death
ends the test, see [marauder-boss.md](bosses/marauder-boss.md)'s "Death handling") can't pop
`VictoryPanel` on top of an already-showing `GameOverPanel`, or vice versa.
Each `Show()` early-returns before activating its own panel if the other's
is already active — a true no-op, not a flash-then-hide. Boss combat itself
is unaffected; it still resolves and the `Boss` GameObject still gets
destroyed normally either way, only the end-screen popup is guarded.

### Prefabs

**`Assets/Prefabs/PartyFrame.prefab`** — the party frame, avatar + role +
live stats, `PartyFrameUI.cs` attached. `PartyFrame_1..4` are all instances
of it (see `PartyFrameManager.cs` above). Root `Image` is a solid dark
panel (`RGBA(0.05, 0.05, 0.08, 0.85)`), consistent with the project's
cyberpunk/dark-neon aesthetic (`../overview.md`).

### GameplayCanvas

Render Mode: **Screen Space - Camera**, Camera: Main Camera. Confined to the
pillarboxed viewport. Reserved for in-game overlay UI that should stay
within the gameplay area (health bars above ships, floating damage numbers,
etc.). Currently empty — no content attached yet.

## Known Editor-only quirk: Scene view canvas visualization

Screen Space - Overlay canvases (like `HUDCanvas`) render as an oversized
flat plane near world origin in **Scene view only** — a known Unity editor
quirk from visualizing screen-space UI and world-space gameplay objects in
one 3D preview. Doesn't affect Game view or the actual build. Workflow fix:
toggle the eye icon next to `HUDCanvas` in the Hierarchy to hide it from
Scene view while doing world-space/gameplay work; toggle back on for UI
work. Isolation View (crosshair icon in Scene view toolbar) works too for
one-off focus.

## Not yet built

- No runtime spawner for party frames — `PartyFrameManager.players[]`/
  `partyFrames[]` are 4 fixed, hand-wired slots (`Player` + 3 `Teammate_*`),
  not a "loop over however many players/teammates actually exist and
  `Instantiate()`" spawner. Deferred until the player/teammate count needs
  to vary at runtime (local co-op, or a different minion/teammate count).
- Avatar is an untinted-sprite placeholder box (tinted to role color) — no
  ship art exists yet.
