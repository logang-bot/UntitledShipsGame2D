# Player Roles

## PlayerRole.cs

**Defines:** `PlayerRole` enum (`Attacker`, `Tank`, `Medic`, `Support`),
`RoleStats` struct (health/fire-rate/move-speed multipliers + sprite tint
`Color`), and the static `PlayerRoleStats` lookup table (one `RoleStats` per
role, placeholder balancing values).

No `ScriptableObject` asset workflow — role data is a static in-code table,
matching the project's existing plain-`MonoBehaviour`, low-infra style. Easy
to migrate to `ScriptableObject`s later if hand-tuning in the Inspector
becomes worth the friction.

## PlayerRoleComponent.cs

Deliberately its own file, separate from `PlayerRole.cs` — Unity requires a
`MonoBehaviour`/`ScriptableObject` class to be the filename-matching class
in its file for reliable script serialization. `PlayerRoleComponent` was
originally bundled into `PlayerRole.cs` (whose matching class is the enum);
that produced a broken, non-asset-backed script reference on the component
(silently, no compile error) — Unity logged "referenced script is missing"
only once something tried to actually use the component at runtime. See
[../unity-notes.md](../unity-notes.md) for the general gotcha. Fixed by
moving it to its own filename-matching file, which is also consistent with
every other script in this project (one class per file).

**Attached to:** `Player` GameObject (alongside `PlayerController` and
`PlayerHealth` — see [combat.md](combat.md) and [movement.md](movement.md)).
**Requires:** nothing external; tints its own `SpriteRenderer` if one is
present.

Holds the `role` field for this player instance and exposes `Stats`
(computed on access via `PlayerRoleStats.Get(role)` — not cached in `Awake`,
so it's safe regardless of Unity's unordered `Awake`/`Start` execution
across sibling components).

- `PlayerController.Start()` multiplies `moveSpeed`/`fireRate` by
  `Stats.moveSpeedMultiplier`/`Stats.fireRateMultiplier`.
- `PlayerHealth.Awake()` multiplies `maxHealth` by
  `Stats.healthMultiplier`.
- Both do a null-check on `GetComponent<PlayerRoleComponent>()` so behavior
  is unchanged if the component is missing.

Key public fields: `role` (default `Attacker`).

## Current balancing values (placeholders, tunable)

| Role     | Health ×   | Fire rate ×          | Move speed × | Tint            |
| -------- | ---------- | --------------------- | ------------- | ---------------- |
| Attacker | 0.8 (lower) | 0.75 (faster)          | 1.0            | red/orange        |
| Tank     | 1.6 (higher)| 1.2 (slower)           | 0.8 (slower)   | blue               |
| Medic    | 1.0         | 1.0                    | 1.0            | green              |
| Support  | 1.0         | 1.0                    | 1.15 (faster)  | yellow/gold        |

## Scene wiring — Player

| Component               | Key inspector values                            |
| ------------------------ | -------------------------------------------------- |
| **PlayerRoleComponent**  | role: Attacker (change in Inspector to test other roles) |

Confirmed attached and working: verified live via the Unity MCP bridge —
entering Play mode with the default `Attacker` role showed `maxHealth` 5→4,
`fireRate` 0.2→0.15, and the sprite tinted red, matching the table above.

## Not yet built

- Only one `Player` instance exists in the scene; local co-op (multiple
  players/roles at once) isn't wired up yet — `PartyFrame_2..4` have no data
  source until it is.
- Role-specific abilities (Tank taunt, Medic heal, Support buffs) are not
  implemented — only passive stat multipliers + tint exist so far.

Role display on the HUD (name/role text + tinted health bar on
`PartyFrame_1`) is now live — see [hud-layout.md](hud-layout.md)'s
`PartyFrameUI.cs` entry.
