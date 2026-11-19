# Audio sources

Provisional audio (Session 61). Everything here is a placeholder until the
FMOD pass (`docs/roadmap.md`, "Art & audio").

**Before any build leaves this machine** (a Steam playtest, a store page
video, a shared APK), replace every track marked "not cleared" below with
audio the project has the rights to ship.

| File | Used by | Source | License |
|---|---|---|---|
| `Music/Menu_Neverending_DowntownBinary.mp3` | `MenuBackground.unity` → `Music` (all five menu screens) | "Neverending" by Downtown Binary | **Not cleared.** Unknown; check before shipping |
| `Music/Level_LoseMyMind.mp3` | `Level.unity` → `Music` (every boss) | "Lose My Mind" (feat. Doja Cat), from *F1 The Movie* | **Not cleared.** Commercial release; can't ship |
| `Voice/Tank/tank_voice_burst.wav` | `PartyFrame.prefab` → `CutInCard` (Tank) | Provided by the developer | Fill in |
| `Voice/Attacker/attacker_voice_final_flash.wav` | `PartyFrame.prefab` → `CutInCard` (Attacker) | Provided by the developer | Fill in |

## Sound effects

Provisional SFX (Session 62), generated with Claude Design from the
Combat FX set and exported as `combat_fx_sfx.zip`. The zip's three
`*_demo.wav` previews (whole ability sequences) were left out. All clips
are 44.1 kHz mono 16-bit WAV. Who plays what is in
`docs/systems/audio.md`.

| Folder | Used by | Source | License |
|---|---|---|---|
| `SFX/Bullets/Player/` | `Ship.prefab` → `ShipSfx` (role shots, combo shots, hit-confirm) | Claude Design (AI-generated) | Fill in |
| `SFX/Bullets/Enemy/` | `ShipSfx.bulletHit` and each bullet's `BulletSfx.impact` | Claude Design (AI-generated) | Fill in |
| `SFX/Bullets/Marauder/`, `Wave/`, `Warden/` | `BulletSfx` on the enemy bullet prefabs (`Assets/Prefabs/Bullets/`, `EnemyBullet.prefab`) | Claude Design (AI-generated) | Fill in |
| `SFX/Minions/Marauder/`, `Wave/` | The death burst prefabs in `Assets/Prefabs/FX/` | Claude Design (AI-generated) | Fill in |
| `SFX/Superpowers/Attacker/` | `Ship.prefab` → `AttackerBeamSfx` (Nova Beam) | Claude Design (AI-generated) | Fill in |
| `SFX/Superpowers/Tank/` | `Ship.prefab` → `SuperShieldSfx`, `TauntSfx`, `ShipSfx.superShieldBlock` | Claude Design (AI-generated) | Fill in |
| `SFX/Particles/particle_glow_dot.wav` | `AttackerBeam.prefab` (the beam's impact sparks) | Claude Design (AI-generated) | Fill in |

**Imported but not used yet:**

- everything Halcyon (that boss fires no bullets and has no minions);
- the Warden minion's deaths and fragments;
- `warden_bullet_s` / `_m`, `marauder_bullet_l`, `wave_bullet_m` / `_l`;
- `attacker_big_shot`, `attacker_crit_shot` / `_muzzle` / `_impact` (no crit mechanic exists);
- `particle_streak` / `_ring` / `_shard` (no particle systems use them).

If any clip is AI-generated, Steam's AI content disclosure applies when it
ships. Every SFX above is.
