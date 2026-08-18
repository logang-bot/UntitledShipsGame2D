# Scene Setup

Practical reference for how the scene is configured in the Unity editor — what
components live on which GameObjects, what inspector values to set, and how
references are wired. Use this when building or reconstructing the scene.

---

## Main Camera

| Component            | Setting                                      |
| -------------------- | -------------------------------------------- |
| Camera               | Projection: Orthographic, Size: 5            |
| Tag                  | `MainCamera` (required — Camera.main depends on this) |
| **AspectRatioFitter.cs** | targetAspectWidth: 9, targetAspectHeight: 16 |

---

## Player

**Tags:** `Player`
**Rigidbody2D:** Gravity Scale: 0, Freeze Rotation: Z ✓

| Component             | Key inspector values                                                        |
| --------------------- | --------------------------------------------------------------------------- |
| Sprite Renderer       | Any placeholder sprite (square). Color can tint by role later.             |
| Collider2D (Box/Poly) | Is Trigger: **OFF**                                                         |
| **PlayerController.cs** | moveSpeed: 8, screenPadding: (0.5, 0.5), bulletPrefab: PlayerBullet prefab, firePoint: FirePoint child, fireRate: 0.2, bulletSpeed: 12 |
| **Player Input**      | Actions: PlayerControls asset, Default Map: Player, Behavior: Send Messages |
| **PlayerHealth.cs**   | maxHealth: 5                                                                |

### Adding PlayerHealth (what to do in Unity right now)

1. Select the **Player** GameObject in the Hierarchy.
2. **Add Component → PlayerHealth**.
3. Set **Max Health** to `5` (or whatever starting value you want).
4. Done — `Bullet.cs` already calls `TakeDamage(1)` on hit; no further wiring needed.

### Child: FirePoint

Empty GameObject. Position at the ship's nose (top edge of the square). This is
the bullet spawn origin passed to `PlayerController`'s `firePoint` field.

---

## Spawner

Empty GameObject positioned **above** the visible camera area (y ≈ 8).

| Component           | Key inspector values                                                     |
| ------------------- | ------------------------------------------------------------------------ |
| **EnemySpawner.cs** | enemyPrefab: Enemy prefab, enemiesPerWave: 5, spawnInterval: 0.5, waveInterval: 4, spawnWidth: 6 |

---

## Prefabs

### PlayerBullet

| Component   | Setting           |
| ----------- | ----------------- |
| Sprite Renderer | Any small sprite |
| Collider2D  | Is Trigger: **ON** |
| **Bullet.cs** | lifeTime: 3       |

Bullet.cs is initialized at spawn via `Init(direction, speed, ownerTag)` — no
inspector values for direction/speed/owner, those come from the spawning script.

### EnemyBullet

Identical setup to PlayerBullet. Reuses `Bullet.cs`.

### Enemy

**Tag:** `Enemy`
**Rigidbody2D:** Gravity Scale: 0

| Component             | Key inspector values                                                       |
| --------------------- | -------------------------------------------------------------------------- |
| Sprite Renderer       | Any placeholder sprite                                                     |
| Collider2D            | Is Trigger: **OFF**                                                        |
| **Enemy.cs**          | moveSpeed: 2, sineAmplitude: 1.5, sineFrequency: 1, health: 3, bulletPrefab: EnemyBullet prefab, fireInterval: 1.5, bulletSpeed: 6 |

---

## HUDCanvas

Render Mode: **Screen Space - Overlay**. Spans the full window regardless of
the pillarbox. Used for sidebar content visible outside the gameplay area.

| Component              | Key inspector values                                                   |
| ---------------------- | ---------------------------------------------------------------------- |
| Canvas                 | Render Mode: Screen Space - Overlay                                    |
| Canvas Scaler          | UI Scale Mode: Scale With Screen Size (reference resolution to taste)  |
| **HUDSidebarFitter.cs** | aspectFitter: drag Main Camera here, leftSidebar: LeftSidebar rect transform, rightSidebar: RightSidebar rect transform |

### Children

- **LeftSidebar** — Vertical Layout Group. Contains `PartyFrame_1` (and future
  `PartyFrame_2..4`). See `05-unity-notes.md` for Layout Group configuration details.
- **RightSidebar** — Placeholder for BossPanel (boss HP, cast bar, wave counter).

---

## GameplayCanvas

Render Mode: **Screen Space - Camera**, Camera: Main Camera.
Confined to the pillarboxed viewport. Reserved for in-game overlay UI that should
stay within the gameplay area (health bars above ships, floating damage numbers, etc.).
Currently empty — no content attached yet.
