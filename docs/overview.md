# Project Overview

## Concept

A co-op bullet-hell raid-boss deckbuilder: a top-down shooter with online
co-op boss encounters, dense Touhou-style bullet curtains, and a per-player
deck that acts as your hotbar during the fight.

*(This project began as `UntitledShipsGame2D`, a Galaga-inspired co-op raid
shooter, and was merged with `tohou_deck`, a solo Touhou-style bullet-hell
deckbuilder. Everything below is the merged direction; see `../AGENTS.md`'s
"Decisions log" for what the merge decided and `systems/cards.md` for the
deck layer's design.)*

- **Core loop**: bullet-pattern arcade shooting (Galaga DNA) and dense
  Touhou-style bullet curtains, combined with a scripted, multi-phase boss
  encounter in the style of an MMO raid boss (WoW-inspired).
- **Three stacked skill layers**: dodging (execution — small hitbox, focus
  mode, grazing), roles (coordination), and deck (preparation). The layer
  that binds them is **grazing**: dodging closely earns the energy that pays
  for cards, so bullet-hell skill funds the deckbuilding layer instead of
  running beside it. See `systems/cards.md`.
- **Deck**: each player builds a fixed 10-card deck from their **own role's**
  card pool before the fight. Cards customize a role, they don't replace it —
  role stats and the signature `E` ability stay exactly as they are, and cards
  layer on top. See `systems/cards.md`.
- **Players**: 1-4, each piloting a ship with a defined RPG-style role:
  - Attacker (DPS)
  - Tank
  - Medic
  - Support
- **Coordination requirement**: like an MMO raid, the team must coordinate role
  responsibilities to beat the boss — this is the central design hook, and what
  differentiates the project from existing neon arcade shooters (see Prior Art below).
- **Ship orientation**: fixed (no rotation), matching Galaga rather than a
  twin-stick shooter. Ships strafe within the viewport and always fire straight
  up. This is a deliberate design decision, not a placeholder.
- **Visual style**: cyberpunk / neon, high-contrast glow aesthetic.
- **Platforms**: targeting Steam and Google Play (crossplay) — this drives some
  ongoing decisions (portrait orientation, input scheme flexibility).
- **Screen layout**: fixed portrait aspect ratio (9:16) gameplay area on all
  platforms. On phones this fills the screen naturally. On PC/desktop, the
  gameplay area is centered with pillarbox bars on either side, and those bars
  are used as HUD space (party frames, boss info) rather than left empty — see
  `systems/hud-layout.md` and `unity-notes.md` for the implementation approach.

## Prior Art / Positioning

Researched to confirm this isn't a re-tread of an existing game:

- **Nex Machina / Resogun** (Housemarque) — closest visual reference (neon,
  high-contrast, rendered/voxel art), but pure score-attack arcade, no RPG roles,
  no scripted boss-raid design.
- **Alienation** (Housemarque) — closest mechanical reference (actual RPG classes,
  co-op up to 4, twin-stick), but wave-survival/looter-shooter structured, not built
  around a single scripted boss encounter.
- **Touhou series** (Team Shanghai Alice) — the reference for the bullet-curtain
  half: tiny hitbox, focus mode, grazing, named spell-card boss phases. Solo,
  no roles, no deck.
- **Slay the Spire / roguelike deckbuilders** — the reference for the deck half,
  but turn-based, so all the tension is in the choice, none in the execution. The
  merge's whole bet is that a deck can be a real-time hotbar without eating the
  attention that dodging needs.
- **Conclusion**: the specific combination of Galaga-style bullet patterns + a
  scripted raid-style boss + hard role-locked co-op (tank/healer/dps/support) is not
  a crowded space, and adding a per-role deck funded by grazing has no close
  comparison at all. Risk is design complexity (boss encounters are hard to get
  right, and the deck layer adds a second system that must not fight the first
  for the player's attention), not idea collision.

## Tech Stack Decisions

| Layer                  | Choice                                                                                    | Why                                                                                                                                                                                                                                                                  |
| ---------------------- | ----------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Engine                 | Unity 6.4 LTS                                                                             | Best 2D shooter multiplayer ecosystem; Unity's runtime fee was cancelled Sept 2024, back to per-seat subscription (Personal tier free under $200k revenue)                                                                                                           |
| Project template       | Universal 2D (URP)                                                                        | Faster iteration than 3D for testing gameplay feel; 2D Lights/Bloom still deliver a strong neon look                                                                                                                                                                 |
| Networking backend     | Nakama (self-hosted, Go-based) on Fly.io                                                  | Open-source, authoritative server (required for boss-fight integrity), matchmaking + leaderboards built in, reuses existing Go/Fly.io experience                                                                                                                     |
| Input                  | Unity's New Input System (not legacy Input Manager)                                       | Actively developed; `PlayerInput`'s device-pairing is what local co-op (multiple players, multiple devices, one machine) is built on — implemented, see `roadmap.md`'s "Local co-op / dynamic player count". Uses a custom `PlayerControls` Input Actions asset (not Unity's auto-generated default) for full control over action names. |
| Art pipeline (planned) | Blender-rendered sprites with normal maps → Unity URP 2D `Sprite-Lit-Default` + `Light2D` | Gets dynamic lighting/shadows reacting to ship geometry without hand-painting normal maps; Blender already produces this as a free byproduct of the 3D model                                                                                                         |
| Audio (planned)        | FMOD                                                                                      | Adaptive music for boss phase transitions (intensify at HP thresholds)                                                                                                                                                                                               |

## Architecture Principles

1. **Prove gameplay is fun before investing in infrastructure.** Build order is:
   local single-player prototype → full basic mechanics → player-vs-boss mechanics
   (single boss, one role mechanic) validated with CPU-controlled AI teammates
   filling the non-human roles → then the bullet-hell/deck layer (grazing, energy,
   cards) on top of that proven fight → only then real (human) networking, which
   upgrades those AI teammates to real players → only then final art. See
   `roadmap.md` for the current status against this order. The deck layer sits
   deliberately *after* the boss fight is proven fun: if the fight isn't fun
   without cards, cards won't rescue it, and if it is, cards have something worth
   modifying.
2. **Server-authoritative for boss encounters.** Clients send input, server (Nakama)
   resolves combat — required so a 4-player boss fight's state can't desync or be
   cheated.
3. **Offline mode = the same authoritative simulation, run locally.** The
   combat/boss-AI logic layer should not care whether it's driven by network
   messages or local input, so offline isn't a separate mode to bolt on later.
4. For how these principles show up as concrete code conventions in the
   current codebase (script organization, communication patterns, etc.),
   see `architecture.md`.
