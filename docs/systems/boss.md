# Boss Encounter

The boss encounter prototype: a single boss with two HP-based phases, a
threat-table aggro system that Tank taunt redirects, and 3 CPU-controlled AI
teammates covering the roles the human isn't playing. This is the project's
core design bet (`../overview.md`) — proving MMO-raid-style role coordination
is fun — reached before any networking exists, per `../roadmap.md`'s
priority order.

## Boss.cs

**Attached to:** `Boss` GameObject (`Assets/Prefabs/Boss.prefab`, one
instance placed directly in `SampleScene` — not spawned; a boss is a
one-off, not a wave, so it doesn't go through `EnemySpawner.cs`).
**Requires:** tag `Enemy` (so `Bullet.cs`'s existing player-bullet-vs-`Enemy`
branch collides with it), a `bulletPrefab` (reuses `EnemyBullet.prefab`), a
`targets[]` array wired to all 4 player-controlled ships.

### Movement and firing

Sine-drifts near the top of the screen — same pattern as `Enemy.cs`, no
pathfinding. Every `Update()`, it picks the current target (see Aggro below)
and fires at it on an interval:

- **Phase 1** (HP > 50%): one aimed shot every `phase1FireInterval` (1.2s).
- **Phase 2** (HP ≤ 50%): fire interval halves (`phase2FireInterval`, 0.6s)
  and it fires a 3-bullet spread (`spreadAngle`, ±15°) instead of a single
  shot.

**This is the whole "2-phase" design** — Phase 1 and Phase 2 are two
difficulty tiers of the *same* HP bar (100%→50% and 50%→0%), not two
separate encounters or health bars. Reaching 0 HP in either phase calls
`Die()` and ends the fight; there's no third phase after Phase 2 by design,
since defeating the boss is the intended end state, not a transition to
something else. If a future encounter design wants an enrage tier or a
scripted final phase beyond simple death, that's a bigger, separate change
from what's built here.

### Aggro / targeting

A plain threat table (`Dictionary<GameObject, float> aggro`, private,
populated in `Awake()` from `targets[]`): every point of damage a target
deals adds to that target's aggro; the boss's `CurrentTarget` is whichever
active target currently holds the highest aggro. No decay — kept
prototype-simple, matching the project's "prove fun before infra" style
(see `player-roles.md`'s prior "Aggro/targeting" note, now implemented
here instead of deferred).

`PickTarget()` uses `Dictionary.TryGetValue` rather than a raw indexer —
found live during testing that a raw `aggro[t]` indexer crashed with a
`KeyNotFoundException` every `Update()` (and silently stopped the boss from
firing, since the exception aborted the rest of `Update()` before reaching
`Fire()`) under conditions not fully root-caused; `TryGetValue` makes the
lookup safe regardless of whether `aggro` and `targets[]` ever drift out of
sync, at negligible cost.

`TauntedBy(GameObject taunter)` — the real listener for Tank taunt (see
below) — sets the caster's aggro to `(current highest aggro) + tauntBonus`
(100), guaranteeing an immediate target switch to the taunter.

### Public API

- `CurrentHealth`, `IsPhase2`, `CurrentTarget` — read-only, drive
  `BossPanelUI.cs` (below) and are the values to inspect when testing via
  the Unity MCP bridge.
- `TakeDamage(int amount, GameObject source)` — called by `Bullet.cs` on a
  player-bullet hit; `source` is the shooter, used for aggro attribution.
  Crossing the 50%-HP threshold flips `IsPhase2` and fires `OnPhase2`;
  reaching 0 HP fires `OnDefeated` then `Destroy(gameObject)`.
- `TauntedBy(GameObject taunter)` — see above.
- `OnPhase2`, `OnDefeated` — `UnityEvent`s for other systems to react to
  (currently only `OnDefeated` has a listener: `BossPanelUI.ShowDefeated()`).

Key public fields: `maxHealth` (30), `sineAmplitude`/`sineFrequency` (2 /
0.5), `bulletPrefab`, `phase1FireInterval`/`phase2FireInterval` (1.2 / 0.6),
`bulletSpeed` (6), `spreadAngle` (15°), `targets[]`, `tauntBonus` (100),
`enemySpawner` (drag `Spawner` — auto-disabled in `Awake()` so wave enemies
from `EnemySpawner.cs` don't confound a boss-fight test).

## Bullet.cs — boss damage dispatch

`Init(Vector2 dir, float spd, string ownerTag, GameObject ownerObject =
null)` gained the optional `ownerObject` param (default keeps `Enemy.cs`'s
existing `Init(Vector2.down, bulletSpeed, "Enemy")` call compiling
unchanged) so player bullets can attribute damage back to their shooter for
aggro. `OnTriggerEnter2D`'s player-bullet-vs-`Enemy`-tag branch now also
checks for a `Boss` component (in addition to the existing `Enemy` check)
and calls `boss.TakeDamage(damage, ownerObject)` — needed because bullet
damage previously only ever dispatched to `Enemy.TakeDamage`. See
[combat.md](combat.md) for `Bullet.cs`'s full reference.

## AIController.cs

**Attached to:** the 3 `Teammate_*` GameObjects (`Assets/Prefabs/Teammate.prefab`
instances — see below), replacing `PlayerInput`.
**Requires:** `PlayerController`, `PlayerAbility`, `PlayerHealth`,
`PlayerRoleComponent` on the same GameObject (same component set as the
human `Player`) — reused as-is, no AI-specific duplicate logic.

Drives a CPU-controlled teammate every `Update()`:

- **Movement**: a sine-weave strafe (`weaveFrequency`/`weaveSpeed`) via
  `PlayerController.SetMoveDirection(Vector2)` — a non-input entry point
  added to `PlayerController.cs` alongside the existing input-driven
  `OnMove(InputValue)`, so movement can be driven directly without
  constructing a fake `InputValue` (which isn't valid outside a real input
  callback).
- **Firing**: continuous auto-fire via `PlayerController.SetFiring(bool)`,
  the equivalent non-input entry point for `OnFire(InputValue)`.
- **Abilities**: `PlayerAbility.TryUseAbility()` — a public method extracted
  from the private dispatch inside `OnAbility(InputValue)`, so it's callable
  directly and still goes through the same shared cooldown gate the human
  player uses. Per-role heuristic: **Tank** taunts whenever it doesn't
  currently hold the boss's aggro (`boss.CurrentTarget != gameObject`);
  **Medic** heals itself below `medicHealThreshold` (60% of `maxHealth`);
  **Support**/**Attacker** just retry every frame — safe and cheap since
  `TryUseAbility()`'s own cooldown gate no-ops until ready.

Key public fields: `boss` (drag the `Boss` instance), `weaveFrequency` (0.8),
`weaveSpeed` (1), `medicHealThreshold` (0.6).

## BossPanelUI.cs

**Attached to:** `BossPanel` (child of `HUDCanvas`, replacing its old
"Boss stats coming soon" placeholder — see [hud-layout.md](hud-layout.md)).
**Requires:** a direct `boss` reference (this panel is scene-bound, not a
reusable prefab like `PartyFrame.prefab`).

Every `Update()`, reads `Boss.CurrentHealth/maxHealth` into a health-bar
`Image.fillAmount` + `"HP: x/y"` text, `Boss.IsPhase2` into a `"Phase
1"`/`"Phase 2"` text, and `Boss.CurrentTarget`'s `PlayerRoleComponent.role`
into a `"Target: {role}"` text — same "HUD only reads, never owns game
state" pattern as `PartyFrameUI.cs`. `ShowDefeated()` (wired to
`Boss.OnDefeated`) overwrites the phase text with `"DEFEATED"`.

Key public fields: `boss`, `healthBarFill`, `healthText`, `phaseText`,
`targetText`. Key public method: `ShowDefeated()`.

## PlayerAbility.cs / PlayerController.cs — non-input entry points

Both scripts gained public methods so `AIController` can drive them without
going through `PlayerInput`'s input-callback path:

- `PlayerController.SetMoveDirection(Vector2)` / `SetFiring(bool)` —
  extracted from `OnMove(InputValue)` / `OnFire(InputValue)`, which now just
  unwrap the `InputValue` and call these. No behavior change for the human
  `Player`.
- `PlayerAbility.TryUseAbility()` — extracted from the private dispatch
  previously inline in `OnAbility(InputValue)`. The four `Trigger*` methods
  (`TriggerTaunt`, `TriggerHeal`, `TriggerBuff`, `TriggerBigShot`) stay
  private/unchanged.

Also: `PlayerController.SpawnBullet()` now passes `gameObject` into
`Bullet.Init(..., ownerObject)` so aggro attribution works for player fire
too (see Bullet.cs above).

## Scene wiring

### Boss

**Tag:** `Enemy`. **Prefab:** `Assets/Prefabs/Boss.prefab` (SpriteRenderer,
Rigidbody2D at Gravity Scale 0, non-trigger BoxCollider2D — same physical
setup as `Enemy.prefab`). One instance in `SampleScene`, positioned at
`(0, 4.2, 0)` — **must stay within the camera's visible range**: the Main
Camera is orthographic with size 5, so world Y outside roughly `[-5, 5]` is
off-screen (an earlier placement at `y=6` was invisible in Play mode; caught
via a screenshot during testing, not by inspecting numbers alone).

| Component      | Key inspector values                                                    |
| --------------- | ----------------------------------------------------------------------- |
| Transform       | position (0, 4.2, 0), scale (1.6, 1.6, 1) — **not** shrunk with the ships below |
| **Boss.cs**     | `targets`: `Player` + all 3 `Teammate_*`; `bulletPrefab`: EnemyBullet prefab; `enemySpawner`: `Spawner`; `OnDefeated`: `BossPanel/BossPanelUI.ShowDefeated()` |

### Teammate_Tank / Teammate_Medic / Teammate_Support

Each is a duplicate of `Player`'s component set with `PlayerInput` removed
and `AIController` added, tagged `Player` (so `Bullet.cs`'s existing
player/enemy tag logic treats them exactly like the human player), with a
distinct `PlayerRoleComponent.role` (Tank / Medic / Support — `Player`
itself stays the default `Attacker`) so all 4 roles are covered exactly
once. `Teammate_Tank` is the one actually linked to
`Assets/Prefabs/Teammate.prefab`; `Teammate_Medic`/`Teammate_Support` were
duplicated from it *before* the prefab link was created, so they're
plain independent GameObjects with the same component values, not prefab
instances — a future edit meant to apply to all three teammates has to be
applied to each individually (or to `Teammate.prefab` **plus** the two
non-linked copies), not just to the prefab asset.

| Component            | Key inspector values                                                        |
| --------------------- | ----------------------------------------------------------------------------- |
| Transform              | scale (0.6, 0.6, 1) — see "Ship scale" tuning below                          |
| **AIController.cs**   | `boss`: the `Boss` instance                                                   |
| **PlayerRoleComponent** | role: Tank / Medic / Support respectively                                    |

Role assignment (who plays which of the 4 roles) is **Inspector-only** —
matching the existing single-player role-selection pattern
(`player-roles.md`) — there's no in-game role-select screen; swap
`PlayerRoleComponent.role` on `Player` and the corresponding `Teammate_*`
by hand to test a different human role.

### Tank taunt → boss aggro

On all 4 `PlayerAbility` components (`Player` + 3 `Teammate_*`), `OnTaunt`
has a persistent listener to `Boss.TauntedBy(GameObject)` with the fixed
argument dragged to that same GameObject (each player's taunt targets
itself). This is **additive** to the Session 9 placeholder feedback
(`PlayerDamageFlash.Flash()` + `CameraShake.Shake()`) — both listeners fire
on every taunt, not a replacement.

### Death handling

Only the human `Player`'s `PlayerHealth.OnDeath` shows `GameOverPanel`
(`GameOverPanel/GameOverUI.Show()`). Each `Teammate_*`'s `OnDeath` is wired
only to its own party frame (`PartyFrame_N/PartyFrameUI.OnPlayerDied()`) —
a teammate dying just grays its frame, it doesn't end the whole test.

## Tuning: fire cadence and ship scale

Two follow-up balance passes on the base prototype:

- **Fire cadence** — `PlayerController.fireRate`'s base value went from
  `0.2` to `0.35` (script default, `Teammate.prefab`, and all 4 scene
  ships), making the fight take more sustained effort. Role multipliers
  still apply on top unchanged, so relative balance between roles is
  preserved: Attacker `0.35 × 0.75 = 0.2625s`, Support `0.35 × 1.0 =
  0.35s`, Medic `0.35 × 1.0 = 0.35s`, Tank `0.35 × 1.2 = 0.42s`.
- **Ship scale** — `Player`/`Teammate_*` `Transform.localScale` went from
  `1.0` to `0.6` (40% smaller). The `Boss` was deliberately **not** shrunk
  (stays `1.6`) so it still reads as the big, central target — this leaves
  visual room for minions planned around the boss (see `../roadmap.md`)
  without the boss itself shrinking to match. Child objects (`FirePoint`)
  and `BoxCollider2D` needed no separate edit: Unity scales a child's
  effective position and a collider's size by the parent transform's scale
  automatically.

## Known environment quirk hit during testing

Same one documented in `../progress-log.md` Sessions 6–8: this Unity Editor
instance does not reliably tick Play-mode `Update()` while its window is
unfocused/idle, so real elapsed time between MCP tool calls can be
near-zero for a while and then jump substantially once the window regains
focus. During boss-fight testing this showed up as the boss appearing to
take almost no damage across several tool calls and then being defeated
between the next two — not a bug in `Boss.cs`, just this same simulation-
pacing quirk. `manage_camera` screenshot calls (each forces one manual
frame step) remain the reliable way to pump deterministic frames for
testing, as in prior sessions.

## Verified (Unity MCP bridge, Play mode)

- Phase transition flips `IsPhase2` exactly at the 50%-HP boundary and
  fires `OnPhase2` exactly once.
- Aggro correctly tracks the highest damage-dealer as `CurrentTarget`;
  `TauntedBy()` redirects it to the taunter; a second immediate taunt is
  blocked by `PlayerAbility.CooldownRemaining`.
- AI teammates autonomously move, auto-fire, and trigger their role's
  ability over time (observed Tank's taunt firing for real the moment it
  didn't hold aggro, and Support's buff auto-activating).
- Boss defeat (`TakeDamage` to 0) fires `OnDefeated`, `BossPanelUI` shows
  `"DEFEATED"`, and the `Boss` GameObject is destroyed with no console
  errors.
- All 4 `PartyFrameUI` instances (see [hud-layout.md](hud-layout.md)) and
  `BossPanelUI` read live, correct values with no drift from `Boss`'s /
  each `PlayerHealth`'s actual state.

## Not yet built

- **Minions around the boss** — motivated the ship-shrink above; no
  minion script or prefab exists yet.
- **Local co-op / dynamic player count** — the party is 4 fixed, hand-
  placed scene objects, not a runtime spawner (see `../roadmap.md`'s "In
  Progress").
- **A third/"enrage" phase, or any behavior after Phase 2 beyond death** —
  not planned; see "Movement and firing" above for why 2 phases is the
  complete, intended design, not a partial one.
