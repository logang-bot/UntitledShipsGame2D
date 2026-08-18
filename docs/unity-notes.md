# Unity Editor Notes

Recurring Unity editor gotchas and technical boundaries encountered while building
this project. Worth reading before building any new UI panels, scripts, or scene
configurations.

---

## UI Layout Groups

### Layout Groups only control direct children

A `Vertical Layout Group` or `Horizontal Layout Group` on an object only
manages the sizing/position of its **direct** children — it has zero effect on
grandchildren. If a panel's own contents look unmanaged/overlapping, check
whether *that specific panel* has its own Layout Group, not just its parent.

This is why the HUD structure ends up nested two levels:
- `LeftSidebar` — Vertical Layout Group, manages `PartyFrame_1..4` (the rows)
- Each `PartyFrame_N` — Vertical Layout Group, manages its own name/role/health
  bar children

### Image vs Raw Image

Easy to pick the wrong one from the `UI >` context menu — they sit near each
other.
- **Image** — supports Image Type (Simple/Filled/Sliced/Tiled), Fill Amount,
  works with UI Sprites. Use this for health bars, icons, backgrounds.
- **Raw Image** — just displays a raw Texture. No Image Type, no Fill options.

If "Image Type" isn't showing in the Inspector, you almost certainly have a
Raw Image instead of an Image. Check the component header text to confirm.

### Panel ≠ Canvas

A `Panel` (via `UI > Panel`) is just a GameObject with a regular `Image`
component using Unity's default background sprite. It is **not** a Canvas. The
actual Canvas is the top-level container (e.g. `HUDCanvas`). Panels are just
convenient rectangle containers for grouping and backgrounding a section of UI.

### Elements with no inherent size need an explicit `Layout Element`

Text components can calculate their own preferred width/height from font and
content. **Images (and empty Panels) cannot** — they have no content to measure,
so without help they'll size to zero or collapse into overlapping siblings.

Fix: add a `Layout Element` component and manually set **Preferred Width** and/or
**Preferred Height** for whichever axis the parent Layout Group controls. Missing
just one axis (e.g. setting Width but forgetting Height) is a common way to end up
with an invisible or misshapen element.

### Layout Group settings that matter most

On any `Vertical Layout Group` / `Horizontal Layout Group`:

- **Control Child Size** (Width / Height) — whether the group overrides children's
  size at all. Usually want both checked.
- **Child Force Expand** (Width / Height) — whether children stretch to fill all
  leftover space, ignoring their preferred size. Most common source of confusing bugs:
  - **Force Expand Height ON** on a sidebar Vertical Layout Group stretches each
    row across the entire remaining height (all 4 party frames split the full sidebar
    height). Turn **off** for a list of fixed-height rows.
  - **Force Expand Width ON** inside a row forces children to divide width evenly,
    ignoring Preferred Width. Turn **off** if you need specific different widths.
  - Force Expand Width **on** for a single child with no fixed width (like a health
    bar meant to fill remaining space) is the easy way to get "stretch to fill."
- **Child Alignment** — only matters once sizing is correct; it positions children
  within leftover space. Don't reach for this to fix a sizing bug — check
  Control Child Size and Force Expand first.

---

## Scene View Quirks

### Screen Space - Overlay canvas draws a giant rectangle in Scene view

Screen Space - Overlay canvases have no real world-space position, so Scene view
draws an oversized flat preview plane near world origin (sized to the Canvas Scaler's
reference resolution). This has zero effect on Game view or the actual build — a
known Unity editor quirk when an Overlay canvas coexists with world-space objects.

Workflow tip: toggle the eye icon next to the Canvas in the Hierarchy to hide it
from Scene view while doing gameplay work; toggle back for UI work. Isolation View
(crosshair icon in Scene view toolbar) works for quick one-off focus.

---

## Script serialization: filename must match a MonoBehaviour/ScriptableObject class

Unity allows multiple classes in one `.cs` file, but reliable script
serialization depends on the file's *matching-name* class being the
`MonoBehaviour`/`ScriptableObject`. If the matching-name class is something
else (an enum, a plain struct, a static class) and a differently-named
`MonoBehaviour` lives in the same file, adding that `MonoBehaviour` as a
component can silently produce a broken script reference — no compile
error, the Inspector even shows the component's fields correctly — but the
serialized `m_Script` entry in the scene/prefab YAML ends up missing its
`guid`/`type` (just a bare `fileID`), which is not resolvable on
deserialization. Symptoms: Console logs "The referenced script (Unknown) on
this Behaviour is missing!" and any other object holding a reference to
that component gets `null` at runtime (`NullReferenceException` in
whatever tried to use it), even though everything looks fine in the Editor
immediately after adding it.

Hit this with `PlayerRoleComponent`, originally bundled into `PlayerRole.cs`
(matching class: the `PlayerRole` enum). Fix: move it to its own
`PlayerRoleComponent.cs` — one class per file, filename matching class name,
same convention every other script in this project already follows.
Confirmed the fix via `UnityEditor.MonoScript.FromMonoBehaviour(component)` +
`AssetDatabase.TryGetGUIDAndLocalFileIdentifier` returning a real asset path
and GUID afterward (both were empty/failing before).

A stuck broken instance (once created) can resist `DestroyImmediate` via
`GetComponent<T>()` lookups — if cleanup doesn't seem to take, loop over
`gameObject.GetComponents<T>()` and destroy all of them, or use
`GameObjectUtility.RemoveMonoBehavioursWithMissingScript()`.

---

## ExecuteAlways and Editor Preview

Regular `MonoBehaviour` scripts only run in Play mode. Adding `[ExecuteAlways]`
above the class declaration makes the script also run in Edit mode, so effects are
visible in the **Game view tab** without pressing Play.

Important boundary: `camera.rect` (used by `AspectRatioFitter`) only affects the
camera it's set on. Scene view always uses its own independent editor camera — the
pillarbox effect will **never** preview in Scene view, only in Game view. This is a
hard technical boundary, not a bug.
