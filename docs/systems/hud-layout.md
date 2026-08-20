# HUD & Screen Layout

## Design decision: fixed portrait aspect ratio on all platforms

Fixed portrait aspect ratio (9:16) gameplay area on all platforms — not just
mobile. On phones this fills the screen naturally. On PC/desktop, the
gameplay area stays centered with pillarbox bars on either side, and those
bars are used as HUD space (party frames, boss info) rather than left empty.
This is a deliberate, common technique for crossplay portrait games, not an
unusual layout — the bars are put to work instead of wasted. A single
dynamic camera-viewport approach handles both cases automatically based on
actual screen aspect at runtime; no separate build/project needed.

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

Key public fields: `targetAspectWidth`, `targetAspectHeight`.
Key public method: `GetViewportPixelRect()` — returns the current gameplay
viewport in screen pixels, used by `HUDSidebarFitter` to size the side HUD
to match exactly.

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
**Requires:** `public void Initialize(GameObject player)` to be called
before it does anything useful — see `PartyFrameManager.cs` below. Also
holds Inspector-dragged references to the frame's own children: avatar
`Image`, health bar `Image`, and five `TextMeshProUGUI` children (role,
health, move speed, fire rate, ability) — these bake into the prefab fine
since they're internal to it.

`playerHealth`/`playerRole`/`playerController`/`playerAbility` are
**private**, set only by `Initialize()` — not Inspector fields. A prefab
asset can't hold a serialized reference to a specific scene's `Player`
object, so wiring moved from drag-and-drop to runtime:
`Initialize(GameObject player)` does the four `GetComponent<>()` lookups,
then runs one-time setup (role text set to `"Role: " + PlayerRole`, tints
avatar/health-bar/role-text to `Stats.tintColor`, matching the ship sprite's
tint). `Update()` early-returns until `Initialize()` has run, then keeps
health bar `fillAmount`, `"HP: current/max"` text, move-speed/fire-rate text
(read live from `PlayerController` every frame — already
role-multiplier-adjusted by then), and `abilityText` (`"{AbilityName}:
{StatusText}"`, reading `PlayerAbility`'s public status getters — see
[player-roles.md](player-roles.md)) up to date. `PartyFrameUI` never
computes cooldown/buff math itself, only formats what `PlayerAbility`
already exposes — same "HUD only reads, never owns game state" pattern as
the health/movement stats. Only real, data-backed stats are shown — no
"Attack"/"Defense" labels, since those aren't mechanics that exist yet.
`PlayerName` text is static placeholder text ("Player 1") — there's no name
data anywhere in the codebase to bind it to.

Key public fields: `healthBarFill`, `avatarImage`, `roleText`, `healthText`,
`moveSpeedText`, `fireRateText`, `abilityText`. Key public method:
`Initialize(GameObject)`.

**Planned, not yet implemented** (see [boss.md](boss.md)'s "Manual teammate
ability triggering"): `abilityText` becomes a clickable/tappable UI element
that calls the bound player's `PlayerAbility.TryUseAbility()` directly —
the same public, cooldown-gated method `AIController.cs` and the human
`Player`'s own `OnAbility(InputValue)` already use (see
[player-roles.md](player-roles.md)). Click and tap both fire Unity UI's
standard pointer-click event, so this needs no separate PC/mobile control
scheme. Also planned: a second shield-bar `Image` alongside `healthBarFill`
once the shield stat (`player-roles.md`) exists.

## PartyFrameManager.cs

**Attached to:** `LeftSidebar`.
**Requires:** `players[]` and `partyFrames[]` — parallel arrays,
Inspector-dragged (index 0 = `Player`, 1-3 = `Teammate_Tank`/`Teammate_Medic`/
`Teammate_Support`; matching `PartyFrame_1..4`), matching this project's
explicit-wiring style (no `FindObjectOfType`).

In `Awake()`, loops `for (int i = 0; i < partyFrames.Length && i <
players.Length; i++) partyFrames[i].Initialize(players[i]);`. Using
`Awake()` (not `Start()`) matters: Unity guarantees every object's `Awake()`
finishes before any `Start()` begins, so this runs before anything could
observe a half-initialized frame — same ordering hazard already documented
for `PlayerRoleComponent.Stats`, avoided the same way.

Went from single `player`/`partyFrame1` fields to these parallel arrays
once the boss prototype added 3 CPU-controlled teammates (see
[boss.md](boss.md)) needing party frames too — `PartyFrameUI.cs` itself
needed **no changes**, since `Initialize(GameObject)` already only does
generic `GetComponent<>()` lookups that work on any player-shaped
GameObject, human or AI-controlled. This is still **not** a real runtime
spawner, though — the 4 slots are fixed, hand-wired Inspector references,
not a loop that reacts to however many players/teammates actually exist.
That "loop over connected players and `Instantiate()`s `PartyFrame.prefab`
per player" version is still the natural extension once local co-op (a
variable player count) exists.

Key public fields: `players[]`, `partyFrames[]`.

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
| Canvas Scaler            | UI Scale Mode: Scale With Screen Size (reference resolution to taste)        |
| **HUDSidebarFitter.cs**  | aspectFitter: drag Main Camera here, leftSidebar/rightSidebar: sidebar rect transforms |
| **PartyFrameManager.cs** | `players[]`: `Player`, `Teammate_Tank`, `Teammate_Medic`, `Teammate_Support`; `partyFrames[]`: `PartyFrame_1..4`'s `PartyFrameUI` (matching index order) |

**Children** (direct children of `HUDCanvas`, siblings of each other — there
is no `RightSidebar` wrapper, confirmed by reading the live scene):
- **LeftSidebar** — Vertical Layout Group, and now also carries
  `PartyFrameManager.cs` (table above). Contains **`PartyFrame_1..4`** —
  4 instances of `PartyFrame.prefab` (see Prefabs below), added once the
  boss prototype's 3 AI teammates needed frames too (see
  [boss.md](boss.md)); instantiated from the prefab, not hand-duplicated.
  See [../unity-notes.md](../unity-notes.md) for Layout Group configuration
  details.
- **BossPanel** — now shows the boss's real HP bar, phase, and current
  target, driven by `BossPanelUI.cs`; the old "Boss stats coming soon"
  placeholder text is gone. Full reference: [boss.md](boss.md).
- **GameOverPanel** — full-rect dark overlay + "Game Over" text + Restart
  button, `GameOverUI.cs` attached. Hidden by default (shown on
  `PlayerHealth.OnDeath`). Lives here rather than `GameplayCanvas` because it
  needs to cover the pillarbox bars too. See
  [combat.md](combat.md#gameoverpanel) for the death/restart flow.

### Prefabs

**`Assets/Prefabs/PartyFrame.prefab`** — the party frame, avatar + role +
live stats, `PartyFrameUI.cs` attached. Reusable: `PartyFrame_1..4` are all
instances of it (see `PartyFrameManager.cs` above) — instantiated from the
prefab, unlike the old hand-duplicated `PartyFrame_2..4` stub objects they
replaced, which went stale the moment `PartyFrame_1` was reworked. Still
not a runtime spawner, though — see "Not yet built" below.

**Background contrast fix**: the root `Image`'s color was originally white
at 39% alpha (`RGBA(1,1,1,0.392)`) — confirmed live via the Unity MCP bridge
that this is *not* actually grey, it's a near-transparent white blended
over `HUDCanvas`'s dark backdrop, which reads as a washed-out light panel.
All the `TextMeshProUGUI` children are opaque white, so the real problem
was white-on-near-white, not white-on-mid-grey. Fixed by changing the root
`Image` to a solid dark panel (`RGBA(0.05, 0.05, 0.08, 0.85)`), consistent
with the project's stated cyberpunk/dark-neon aesthetic (`../overview.md`)
— text stays white and now has genuine contrast regardless of what's behind
the canvas. Edited on the prefab (not just the scene instance) so it's the
default for every future party frame; confirmed via MCP that `PartyFrame_1`
picked up the change with no stale per-instance override (and `PartyFrame_2..4`
inherited it correctly too, being instantiated after the fix).

### GameplayCanvas

Render Mode: **Screen Space - Camera**, Camera: Main Camera. Confined to the
pillarboxed viewport. Reserved for in-game overlay UI that should stay
within the gameplay area (health bars above ships, floating damage numbers,
etc.). Currently empty — no content attached yet.

## Known Editor-only quirk: Scene view canvas visualization

Screen Space - Overlay canvases (like `HUDCanvas`) render as an oversized
flat plane near world origin in **Scene view only** — a known Unity editor
quirk from visualizing screen-space UI and world-space gameplay objects in
one 3D preview. Confirmed live via Scene-view vs. Game-view screenshots
through the Unity MCP bridge: Scene view shows the large panel, Game view
(and the actual build) does not. Workflow fix: toggle the eye icon next to
`HUDCanvas` in the Hierarchy to hide it from Scene view while doing
world-space/gameplay work; toggle back on for UI work. Isolation View
(crosshair icon in Scene view toolbar) works too for one-off focus.

## Not yet built

- No runtime spawner for party frames — `PartyFrameManager.players[]`/
  `partyFrames[]` are 4 fixed, hand-wired slots (`Player` + 3 `Teammate_*`),
  not a "loop over however many players/teammates actually exist and
  `Instantiate()`" spawner. Deferred until the player/teammate count needs
  to vary at runtime (local co-op, or a different minion/teammate count).
- `PlayerName` text is still static placeholder ("Player 1", same on every
  party frame) — no name data model exists.
- Avatar is an untinted-sprite placeholder box (tinted to role color) — no
  ship art exists yet. Ship *size* did change, though — see
  [boss.md](boss.md)'s "Tuning" section (`Player`/`Teammate_*` shrunk to
  0.6x scale; the avatar slot itself is unaffected since it's UI, not the
  world-space ship sprite).
- Click/tap-to-trigger-ability on `abilityText` and a shield bar alongside
  `healthBarFill` are designed but not implemented — see the
  `PartyFrameUI.cs` section above and [boss.md](boss.md)'s "Manual teammate
  ability triggering".
