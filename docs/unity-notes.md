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
- `LeftSidebar` — Vertical Layout Group, manages the party frame rows
  (`PartyFrame_1..4`, all instances of `PartyFrame.prefab`, not hand-
  duplicated rows — see `systems/hud-layout.md`)
- Each party frame — Horizontal Layout Group at the root (avatar + info
  column side by side), with a nested Vertical Layout Group inside the info
  column managing its own name/role/health/stat/ability text children

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

## Duplicating a GameObject before it's a prefab instance

`Duplicate` (Ctrl+D, or the equivalent MCP `manage_gameobject` duplicate
action) makes an independent copy with matching values — it does **not**
retroactively link the copy to a prefab created *later* from the original.
Concretely: `Teammate_Tank` was duplicated twice (`Teammate_Medic`,
`Teammate_Support`) before `Teammate_Tank` itself was converted into
`Assets/Prefabs/Teammate.prefab`. The two duplicates stayed plain
GameObjects with matching-at-the-time values, not prefab instances — a
later edit to the prefab's defaults (e.g. tuning `fireRate`/scale, see
`systems/bosses/marauder-boss.md`) only propagated to `Teammate_Tank`; the other two needed
the same edit applied directly. If several near-identical objects need to
stay in sync going forward, prefab-ize (or instantiate from an existing
prefab) *before* duplicating, not after.

## Orthographic camera visible range = ± Size

An orthographic `Camera`'s visible world-space Y range is roughly
`[-Size, +Size]` (Size 5 → visible Y ≈ `-5..5`); X range depends on aspect
on top of that. Placing an object outside this range produces **no error
and no warning** — every script/event/component can be wired perfectly and
the object is simply invisible. Hit this positioning the `Boss` at `y=6`
against a Size-5 camera (`systems/bosses/marauder-boss.md`) — every field checked out fine
until an actual screenshot was taken. When something should be visible but
isn't and nothing is throwing, check world position against the camera's
actual visible bounds before suspecting the logic.

## ExecuteAlways and Editor Preview

Regular `MonoBehaviour` scripts only run in Play mode. Adding `[ExecuteAlways]`
above the class declaration makes the script also run in Edit mode, so effects are
visible in the **Game view tab** without pressing Play.

Important boundary: `camera.rect` (used by `AspectRatioFitter`) only affects the
camera it's set on. Scene view always uses its own independent editor camera — the
pillarbox effect will **never** preview in Scene view, only in Game view. This is a
hard technical boundary, not a bug.

## `GameObject.Find` skips inactive objects

`GameObject.Find`/`GameObject.Find("Name")` only searches **active** GameObjects —
once something calls `SetActive(false)` (e.g. `PlayerHealth.Die()` on a ship), it
silently stops being findable this way, and code that assumed it would still be
there throws a `NullReferenceException` one line later with no indication *why* the
reference was null. Hit repeatedly during MCP-driven Play-mode combat testing
(`docs/progress-log-archive.md` Session 20) when ambient boss fire killed a test ship
between tool calls.

Fix: search all objects regardless of active state via
`Resources.FindObjectsOfTypeAll<Transform>()` (filter by `.name` and
`.gameObject.scene.IsValid()` to exclude prefab-asset transforms, which this method
also returns), then `SetActive(true)` to revive it for further testing. Only needed
for *finding* an inactive object — once you already hold a reference, calling
methods on it works normally regardless of active state.

## Editor doesn't tick Play-mode Update()/coroutines while unfocused (and the inverse)

An unfocused, idle Unity Editor window can stop ticking Play-mode
`Update()`/`FixedUpdate()`/coroutines entirely — `Time.time` stays frozen
across MCP tool calls and even a real multi-second wall-clock sleep. Each
`manage_camera` screenshot call with `include_image: true` forces exactly
one manual frame step (~0.02s), which is the reliable way to pump enough
deterministic frames for timer-based logic (flash/shake durations, ability
cooldowns, coroutine sequences) to complete during testing. Calling
`EditorApplication.QueuePlayerLoopUpdate()` manually from `execute_code` to
force a tick produces benign "PlayerLoop called recursively" console
warnings — harmless, but don't combine it with the screenshot-step
technique.

This is not a universal, permanent state: at least one session had an
Editor instance instead tick Play mode continuously in real time while
unfocused, with no forced steps needed — and a focused Editor window also
runs Play mode in real time in the background, which has interrupted
scripted verification more than once (enemy fire killing a test ship
mid-check). Don't assume either behavior going in — check whether
`Time.time` is advancing across calls before relying on the frame-step
workaround or on background real-time progress.

One sharp edge that follows from this: a single forced frame step in an
idle/unfocused Editor can carry an oversized, real-world-clock-sized
`Time.deltaTime` — large enough to blow straight past a short
`lifeTime`-based `Destroy(gameObject, 3f)` safety cleanup in one tick.

Hit and re-confirmed in `docs/progress-log.md`/`docs/progress-log-archive.md`
Sessions 6, 7, 8, 9, 10, 12, 16, 18, 19, 20, 22, 23, 24.

## Prefab-instance overrides need RecordPrefabInstancePropertyModifications(), not just SetDirty()

Editing a field, or a `UnityEvent`'s persistent listener list, on a
**prefab instance** (not the prefab asset itself) requires calling
`PrefabUtility.RecordPrefabInstancePropertyModifications(component)` in
addition to `SetDirty()`, or the change silently fails to serialize. There's
no error and no warning — in-memory reads (including
`GetPersistentEventCount()` on a `UnityEvent`) correctly report the new
value for the rest of the session, but a full scene reload from disk
reverts it. Applies identically to a plain object-reference field (a
`teammates[]` array) and to a `UnityEvent` persistent listener entry (an
`OnDefeated` hookup) — same fix either way.

This does **not** apply to brand-new script fields that have never been
serialized on that instance before; those simply take the compiled C#
default with no override step needed, since there's no existing serialized
entry to update.

In this project, `Teammate_Tank` is the only one of the 4 ships that's an
actual `Teammate.prefab` instance (`Teammate_Medic`/`Teammate_Support` are
plain duplicated GameObjects, not prefab instances — see "Duplicating a
GameObject before it's a prefab instance" above), so it's the one that
repeatedly needs this treatment. Standing verification habit established
because of this gotcha: after any prefab-instance edit, force a full scene
reload from disk and re-read the value — never trust the in-memory value
alone.

**Sharper variant, hit while building `Ship.prefab` for local co-op**: an
instance value is only recorded as an override if it *differs* from the
prefab's default **at the moment `RecordPrefabInstancePropertyModifications`
is called**. `Player`'s `PlayerInput.enabled = true` was set while
`Ship.prefab`'s own default for that field was still `true` too (since the
prefab was created *from* `Player`) — no diff, so nothing was recorded, and
`Player` silently fell back to inheriting the prefab's default from then on.
Later, changing `Ship.prefab`'s default `PlayerInput.enabled` to `false` (to
fix a separate AI-auto-pairing bug) silently flipped `Player`'s live value
too, with no error, no warning, and a correct-looking in-memory read right
up until a real disk reload exposed it. Any instance value that happens to
equal the prefab's current default is at risk of this — re-apply (and
re-record) every such value *after* changing a prefab's own defaults, not
just once when the instance was first configured, and verify with the same
"force a full disk reload" habit above.

Hit and re-confirmed in `docs/progress-log.md`/`docs/progress-log-archive.md`
Sessions 12, 13, 16, 18, 19, 20.

## Changing a script's default value doesn't retroactively update an already-serialized field

Unity serializes a `MonoBehaviour` field's value once, into the scene/prefab
YAML, the first time a component holding that field is placed with a real
value. After that, editing the field's default in the `.cs` source has zero
effect on values already serialized elsewhere — deserialization finds an
explicit override on disk and uses that instead of the new compiled
default. Symptom: the field reads the *old* value everywhere it was
previously placed (every live scene instance and every prefab asset
default), even though the source change looks like it should have updated
the number project-wide.

Only affects fields that were **already serialized** somewhere. A brand-new
field added later has no prior serialized value to conflict with, and
correctly picks up the new default automatically on every instance.

No shortcut fix exists — the value has to be re-set explicitly on every
place it was previously serialized: each live scene instance *and* each
prefab asset default, individually. Same "force a full disk reload, don't
trust the in-memory value" verification habit as the prefab-instance gotcha
above applies here too.

Hit and re-confirmed in `docs/progress-log.md`/`docs/progress-log-archive.md`
Sessions 11, 12, 16, 19, 24.
