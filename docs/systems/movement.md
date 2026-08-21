# Movement

## Design decision: fixed ship orientation

The ship has a fixed orientation (no rotation), matching Galaga rather than a
twin-stick shooter. Ships strafe within the viewport and always fire straight
up — this is a deliberate decision, not a placeholder pending rotation support
(see `PlayerController.cs` below, and `Bullet.cs` in
[combat.md](combat.md) for the firing side). Omnidirectional enemy spawning is
not planned, since it was only ever motivated by twin-stick rotation; enemies
continue to spawn from the top (see `EnemySpawner.cs` in
[combat.md](combat.md)).

## PlayerController.cs — movement

**Attached to:** `Player` GameObject.
**Requires:** `Rigidbody2D`, a `Player Input` component (Actions =
`PlayerControls`, Behavior = Send Messages — see [input.md](input.md)).

Handles ship movement, clamped to stay within the camera viewport. Move input
arrives via `OnMove(InputValue)`, called automatically by the `Player Input`
component — not polled manually.

Key public fields: `moveSpeed` (a fixed per-role value, see
[player-roles.md](player-roles.md)'s "Fixed per-role stats" — not a
hand-set default like `screenPadding`), `screenPadding` (default 0.5, 0.5),
`speedBuffMultiplier` (default `1`, non-destructive runtime multiplier set
by Support's party-wide Speed Boost — see [player-roles.md](player-roles.md);
`HandleMovement()` reads `moveSpeed * speedBuffMultiplier` rather than ever
mutating `moveSpeed` itself).

This same script also handles shooting — see [combat.md](combat.md) for the
`bulletPrefab`/`firePoint`/`shotsPerSecond`/`bulletSpeed` fields and `Fire()`.

## Scene wiring — Player

**Tag:** `Player`
**Rigidbody2D:** Gravity Scale: 0, Freeze Rotation: Z ✓

| Component                | Key inspector values                                   |
| ------------------------ | -------------------------------------------------------- |
| Collider2D (Box/Poly)    | Is Trigger: **OFF**                                       |
| **PlayerController.cs**  | moveSpeed: overwritten by role at `Start()` (see [player-roles.md](player-roles.md)), screenPadding: (0.5, 0.5) |
| **Player Input**         | Actions: PlayerControls asset, Default Map: Player, Behavior: Send Messages |
| Transform                | localScale: (0.6, 0.6, 1) |

(`Player` also carries `PlayerHealth.cs` and `PlayerRoleComponent` — see
[combat.md](combat.md) and [player-roles.md](player-roles.md). The 3
CPU-controlled `Teammate_*` ships share this same movement code via
`AIController.cs` — see [boss.md](boss.md).)
