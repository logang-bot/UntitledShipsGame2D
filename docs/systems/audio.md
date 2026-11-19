# Audio

Provisional audio: plain Unity `AudioSource`s through one `AudioMixer`,
replaced by the FMOD pass later (`roadmap.md`, "Art & audio"). Files and
their license status are in `Assets/Audio/SOURCES.md`.

## Mixer

`Assets/Audio/GameMixer.mixer` (Session 62): Master → `Music`, `Voice`,
`SFX` → `Superpowers`. `SFX` has a Compressor so a dense volley can't
clip. No Settings screen reads it yet; balance by moving group faders in
the mixer window.

| Group | Sources |
|---|---|
| Music | `MusicTrack` on `MenuBackground.unity` → `Music` (volume 0.8) and `Level.unity` → `Music` (0.5) |
| Voice | `PartyFrame.prefab` → `CutInCard`'s voice source |
| SFX | Every source below, except the superpowers |
| Superpowers (child of SFX, **+6 dB**) | `Ship.prefab`'s superpower one-shot source (shared by `AttackerBeamSfx`, `SuperShieldSfx`, `TauntSfx`) and their three loop sources |

The superpower clips are mastered about as loud as a normal shot (−17 to
−24 dB RMS), so at full cue volume they disappeared under the fight. A cue's
volume can't go above 1, so the boost comes from the group. The Super
Shield's block sound stays on `ShipSfx` (plain SFX), since it repeats on
every blocked bullet.

Music ignores `AudioListener.pause` and keeps playing under the pause
menu; voices and SFX freeze with it.

## Sound effects: who plays what

No audio manager or singleton: each emitter owns its `AudioSource` and calls
`SfxPlayback`.

- **`SfxCue`**: a sound as set in the Inspector, its variants (one picked
  at random) and a volume.
- **`SfxPlayback`**: `Play(source, cue)` is a `PlayOneShot` gated by
  `SfxVoiceLimit`; `StartLoop` / `StopLoop` drive a dedicated looping source.
- **`SfxVoiceLimit`**: refuses a clip that started less than 0.03 s ago (so
  a 12-bullet ring or a 10-rod wall sounds once) or already has 3 copies
  playing. Static, cleared on play-mode entry. Loops and play-on-awake
  sources don't go through it.

| Moment | Played by | Where it's cued |
|---|---|---|
| Normal fire, per role | `Ship.prefab` → `ShipSfx.roleShots` | `PlayerController.SpawnBullet` |
| Attacker combo 1 / 2 / 3 finisher / broken | `ShipSfx` | `PlayerController.SpawnBullet` (by `PlayerShotKind`) |
| A player shot lands | the **shooter's** `ShipSfx.shotHit` | `Bullet.HitEnemy` |
| An enemy bullet hits a ship | the hit ship's `ShipSfx`, playing the bullet's `BulletSfx.impact` (else `bulletHit`) | `Bullet.DamagePlayer` (not while invulnerable) |
| The Super Shield blocks a bullet | the Tank's `ShipSfx.superShieldBlock` | `Bullet.HitPlayer` |
| An enemy bullet is fired | `BulletSfx.spawn` on the bullet prefab | `BulletSfx.Start` |
| Marauder's missile in flight | a second, looping source on `MarauderMissile.prefab` | play on awake |
| Nova Beam wind-up / start + loop / end | `Ship.prefab` → `AttackerBeamSfx` | `AttackerBeamCast.CastRoutine` |
| Nova Beam impact sparks | `AttackerBeam.prefab` → `AttackerBeam.sparkSound` | `AttackerBeam.EmitSparks` |
| Super Shield expand + loop / ending / collapse | `Ship.prefab` → `SuperShieldSfx` | `TankSuperShield`, when the drawn frame changes |
| Taunt activate + loop / end | `Ship.prefab` → `TauntSfx` | `TauntMagnetVisual`, when the drawn frame changes |
| Minion and wave-enemy deaths | an `AudioSource` (play on awake) on each death burst prefab in `Assets/Prefabs/FX/` | `SpriteSequence` waits for the sound before destroying the burst |

The ability sounds follow the visual timelines (`SuperShieldTimeline`,
`TauntMagnetTimeline`) instead of their own timers, so retuning a duration
keeps sound and picture together. The Super Shield's ending frame flickers
with Idle, so `SuperShieldSfx` plays its ending once per cast.

A wave enemy that drifts off the bottom of the screen is removed by
`Enemy.Despawn`, with no burst and no death sound.

All SFX sources are 2D (spatial blend 0) and output to `SFX`.

## Adding a sound

1. Put the WAV in `Assets/Audio/SFX/<Category>/` and add its row to
   `SOURCES.md`.
2. If an existing component covers the moment (a new bullet prefab, say),
   add the component, give it an `AudioSource` set to 2D / SFX group /
   play-on-awake off, and fill its cue.
3. Otherwise add an `SfxCue` field to the component that owns the moment
   and call `SfxPlayback.Play(source, cue)` there. Prefer the object that
   outlives the moment: a destroyed object's `PlayOneShot` is cut off.

## Not covered yet

There are no clips yet for the Medic aura, the Support speed boost, a ship
going down, the boss phase change and defeat, the Warden core's shielded
"plink", and the UI. See `roadmap.md`, "Provisional sound effects".
