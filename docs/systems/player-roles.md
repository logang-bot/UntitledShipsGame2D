# Player Roles

## PlayerRole.cs

**Defines:** `PlayerRole` enum (`Attacker`, `Tank`, `Medic`, `Support`),
`RoleStats` struct (health/fire-rate/move-speed multipliers + sprite tint
`Color`), the static `PlayerRoleStats` lookup table (one `RoleStats` per
role, placeholder balancing values), and `PlayerRoleComponent`.

No `ScriptableObject` asset workflow — role data is a static in-code table,
matching the project's existing plain-`MonoBehaviour`, low-infra style. Easy
to migrate to `ScriptableObject`s later if hand-tuning in the Inspector
becomes worth the friction.

### PlayerRoleComponent

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

- HUD does not display role yet (name/role text on `PartyFrame_1` is still
  placeholder) — tracked under "Finish the HUD" in [../roadmap.md](../roadmap.md)
  and [hud-layout.md](hud-layout.md).
- Only one `Player` instance exists in the scene; local co-op (multiple
  players/roles at once) isn't wired up yet.
- Role-specific abilities (Tank taunt, Medic heal, Support buffs) are not
  implemented — only passive stat multipliers + tint exist so far.
