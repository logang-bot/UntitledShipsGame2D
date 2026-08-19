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
`Image`, health bar `Image`, and four `TextMeshProUGUI` children (role,
health, move speed, fire rate) — these bake into the prefab fine since
they're internal to it.

`playerHealth`/`playerRole`/`playerController` are **private**, set only by
`Initialize()` — not Inspector fields. A prefab asset can't hold a
serialized reference to a specific scene's `Player` object, so wiring moved
from drag-and-drop to runtime: `Initialize(GameObject player)` does the
three `GetComponent<>()` lookups, then runs one-time setup (role text set
to `"Role: " + PlayerRole`, tints avatar/health-bar/role-text to
`Stats.tintColor`, matching the ship sprite's tint). `Update()` early-returns
until `Initialize()` has run, then keeps health bar `fillAmount`, `"HP:
current/max"` text, and move-speed/fire-rate text (read live from
`PlayerController` every frame — already role-multiplier-adjusted by then)
up to date. Only real, data-backed stats are shown — no "Attack"/"Defense"
labels, since those aren't mechanics that exist yet (bullet damage is still
hardcoded in `Bullet.cs`). `PlayerName` text is static placeholder text
("Player 1") — there's no name data anywhere in the codebase to bind it to.

Key public fields: `healthBarFill`, `avatarImage`, `roleText`, `healthText`,
`moveSpeedText`, `fireRateText`. Key public method: `Initialize(GameObject)`.

## PartyFrameManager.cs

**Attached to:** `LeftSidebar`.
**Requires:** `player` (drag the `Player` GameObject) and `partyFrame1`
(drag `PartyFrame_1`'s `PartyFrameUI`) — both Inspector-dragged, matching
this project's explicit-wiring style (no `FindObjectOfType`).

In `Awake()`, calls `partyFrame1.Initialize(player)`. Using `Awake()` (not
`Start()`) matters: Unity guarantees every object's `Awake()` finishes
before any `Start()` begins, so this runs before anything could observe a
half-initialized frame — same ordering hazard already documented for
`PlayerRoleComponent.Stats`, avoided the same way.

This is deliberately **not** a real multi-player spawner — it's the minimal
seam for the current single-player scene. The natural extension once local
co-op exists: a version of this manager that loops over connected players
and `Instantiate()`s `PartyFrame.prefab` per player instead of holding one
fixed reference.

Key public fields: `player`, `partyFrame1`.

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
| **PartyFrameManager.cs** | player: drag `Player`, partyFrame1: drag `PartyFrame_1`'s `PartyFrameUI` |

**Children** (direct children of `HUDCanvas`, siblings of each other — there
is no `RightSidebar` wrapper, confirmed by reading the live scene):
- **LeftSidebar** — Vertical Layout Group, and now also carries
  `PartyFrameManager.cs` (table above). Contains a single **`PartyFrame_1`**
  — an instance of `PartyFrame.prefab` (see Prefabs below). The old
  `PartyFrame_2..4` stub GameObjects (flat layout, never wired, went stale
  the moment `PartyFrame_1` was reworked) were deleted; more frames are
  added later by instantiating the prefab, not by hand-duplicating scene
  objects. See [../unity-notes.md](../unity-notes.md) for Layout Group
  configuration details.
- **BossPanel** — a background `Image` with one centered `TextMeshProUGUI`
  child reading "Boss stats coming soon". No HP bar/cast bar/wave counter
  sub-elements exist yet; that content is deferred until a boss actually
  exists (see [../roadmap.md](../roadmap.md)'s priority order).
- **GameOverPanel** — full-rect dark overlay + "Game Over" text + Restart
  button, `GameOverUI.cs` attached. Hidden by default (shown on
  `PlayerHealth.OnDeath`). Lives here rather than `GameplayCanvas` because it
  needs to cover the pillarbox bars too. See
  [combat.md](combat.md#gameoverpanel) for the death/restart flow.

### Prefabs

**`Assets/Prefabs/PartyFrame.prefab`** — the party frame, avatar + role +
live stats, `PartyFrameUI.cs` attached. Reusable: instantiate one per
player once local co-op exists (see `PartyFrameManager.cs` above) instead
of hand-duplicating scene objects, which is what the old `PartyFrame_2..4`
were and why they went stale.

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

- No spawner for players 2–4 yet — `PartyFrame.prefab` exists and
  `PartyFrameManager.cs` is the seam, but the actual "loop over connected
  players and `Instantiate()`" logic is deferred until local co-op exists.
  Tracked under "Finish the HUD" in [../roadmap.md](../roadmap.md).
- `BossPanel`'s real content (HP bar, cast bar, wave counter) is still just
  the "coming soon" placeholder text — deferred until a boss exists.
- `PlayerName` text is still static placeholder ("Player 1") — no name data
  model exists.
- Avatar is an untinted-sprite placeholder box (tinted to role color) — no
  ship art exists yet.
