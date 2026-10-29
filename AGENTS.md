# tohou_deck

Codename for the developer's first Steam game. Formerly two separate
concepts — a solo Touhou-style bullet-hell deckbuilder (`tohou_deck`) and a
Galaga-style co-op raid-boss shooter (`UntitledShipsGame2D`) — now merged
into one project. This repo is the merged project: it keeps the ships
game's full codebase and git history, with the deckbuilding layer added on
top.

## Concept

A **co-op bullet-hell raid-boss deckbuilder**. 1-4 players each pilot a ship
in a fixed-orientation, portrait playfield, fight through dense Touhou-style
bullet curtains, and bring down a scripted, multi-phase raid boss.

Three layers stack:

1. **Dodging (execution)** — Touhou-grade bullet curtains, a tiny central
   hitbox, focus mode, and grazing. This is the moment-to-moment skill.
2. **Roles (coordination)** — each player locks one of Attacker / Tank /
   Medic / Support, with fixed role stats and a signature ability. Beating
   the boss requires the team to cover its mechanics, MMO-raid style. This
   is the social skill.
3. **Deck (strategy)** — before the fight, each player builds a deck from
   their role's card pool. In the fight, that deck *is* their hotbar. This
   is the preparation skill.

The merge hinges on one mechanic: **grazing pays for cards.** Energy —
the resource that plays cards — is earned by dodging bullets closely.
Bullet-hell skill funds the deckbuilding layer, so the two halves aren't
parallel systems, they're one loop. See
[docs/systems/cards.md](docs/systems/cards.md).

## Target platforms

PC via Steam, and Google Play (crossplay) — this is why the playfield is a
fixed 9:16 portrait aspect on every platform, pillarboxed with HUD sidebars
on desktop.

## Tech stack

- **Engine:** Unity 6 LTS. Unity's 2023 Runtime Fee proposal was fully
  canceled in September 2024 and no longer exists; the model is seat-based
  (Personal free under $200K trailing-12-month revenue/funding). Re-verify
  at unity.com/products/pricing-updates if it's been a while.
- **Template:** 2D (URP) — 2D lights/bloom carry the neon look, and Built-in
  RP is in maintenance mode.
- **Language:** C#.
- **Input:** Unity's New Input System, custom `PlayerControls` actions asset.
- **Networking (planned, last):** Nakama self-hosted on Fly.io,
  server-authoritative.
- **Audio (planned):** FMOD, adaptive to boss phase.
- **Art (in progress, parallel to phase 3):** the player ships are the
  Claude Design "Ship Designs" GLB models, rendered live (glTFast + a custom
  `ShipMesh.shader`) under the URP 2D Renderer with bloom; everything else
  stays URP 2D `Sprite-Lit-Default` + `Light2D`; Shape Blaster-inspired neon effects;
  anime ability cut-ins as 2D illustrations or VRoid → Unity (route decided
  later). Blender is optional. See [docs/art-roadmap.md](docs/art-roadmap.md).

For the full reasoning behind each of these, see
[docs/overview.md](docs/overview.md)'s "Tech Stack Decisions".

## Working agreement

- First-time game project for the developer — favor a solid, well-understood
  foundation over speed.
- Build step by step: confirm each foundational decision before building on
  top of it, rather than assuming and re-deriving later.
- **Prove gameplay is fun before investing in infrastructure.** Build order
  is local prototype → full basic mechanics → boss mechanics validated with
  CPU-controlled AI teammates → deck layer → real networking → final art.
  See [docs/roadmap.md](docs/roadmap.md). Phase 2 is done. Phase 3 (the
  deck layer) belongs to another developer, and the art now starts early,
  in parallel, as replacements for placeholders that already work. See
  [docs/art-roadmap.md](docs/art-roadmap.md).
- Keep `docs/current-state.md` and `docs/roadmap.md`'s "Implemented" section
  in sync whenever a feature ships.
- Code conventions (flat `Assets/Scripts/`, one class per file, plain
  `MonoBehaviour`s, Inspector wiring, no singletons/DI/ScriptableObjects)
  are documented and deliberate — read
  [docs/architecture.md](docs/architecture.md) before adding a new pattern.

## Decisions log

| Decision | Choice | Notes |
|---|---|---|
| Engine | Unity | Picked over MonoGame and Unreal; MonoGame was the close second (pure C#, no licensing strings, but no editor/built-in tooling) |
| Unity version | Unity 6 LTS | LTS for stability over the project's lifetime |
| Render pipeline | URP, 2D template | Standard modern choice for 2D; needed for the neon/glow look |
| Ship orientation | Fixed, no rotation, fires straight up | Galaga/Touhou DNA, not a twin-stick. Deliberate, not a placeholder |
| Screen layout | Fixed 9:16 portrait, pillarboxed on desktop | Crossplay with Google Play; sidebars become HUD space |
| Merged concept | Co-op raid-boss bullet-hell deckbuilder | Merges `tohou_deck` and `UntitledShipsGame2D`; ships repo is the base |
| Player count | 1-4 co-op retained | Solo already works — 3 AI teammates fill the unpicked roles |
| Deck vs. roles | Deck **customizes** a chosen role | Role sets the baseline stats/ability; cards modify and extend. Roles are not replaced |
| Deck ownership | Per-player, not party-shared | Preserves role identity and keeps balancing tractable |
| Run structure | Pre-fight loadout screen | Simplest path to validating whether cards are fun; roguelike runs / meta-progression deferred |
| Card economy | Energy earned by grazing | The mechanic that fuses bullet-hell skill with the deck layer |
| Work split | Phase 3 (hitbox/focus/grazing/cards) by another developer; art in parallel | Art replaces working placeholders, so it never blocks gameplay |
| Art pipeline | Player ships are the design GLB models (`Assets/Art/Ships/`) rendered **live** as meshes; baking to sprites is only a possible phone optimization; Blender optional | Session 51 chose live models over a sprite-first route: they already existed, and give banking and animated parts for free. Replaces the Session 50 plan (2D first, then a bake) |
| Role colors | Design palette: Attacker `#FF8A3D`, Tank `#00E5FF`, Medic `#B6FF3C`, Support `#A855F7` | Match the ship models; set in `PlayerRoleStats` (Session 51) |
| Colors | **One palette**: every color is a `PaletteColor` token in `NeonPalette`; Inspector fields use `PaletteSwatch`, scene objects `PaletteTint` | No color literals anywhere else (Session 54). Only exception: the ship GLB materials. See `docs/systems/ui-kit.md` |
| Menu scenes | Five screen scenes loaded **additively** over a persistent `MenuBackground.unity`; PC menus fill the window | Keeps the design's background and slide transitions without a singleton or one merged scene (Session 54). Mobile layouts deferred |
| Level scenes | **One shared `Level.unity`**; each boss is its own prefab, and `LevelSetup` spawns only the picked one | Replaced three drifting `Ctrl+D` copies (Session 51). Two bosses can't coexist, and boss edits live in per-boss prefabs |
| Boss art and backgrounds | Bosses and one parallax background per boss designed in Claude Design (2D + 3D GLB, signature color per boss); backgrounds dark with stars and city rooftops far below, in scrolling layers | Same pipeline as the ships; each fight gets its own look without hurting bullet readability (Session 52, first draft). Backgrounds ship as static PNG layers with a random start offset and mirror, not a port of the design's JS generator (Session 53) |
| Effects style | Neon, inspired by Tuts+ "Neon Vector Shooter" (Shape Blaster) | Bloom, additive stretched particles, warping grid, black-hole powers; enemy bullets must stay readable over effects |
| Ability cut-ins | 2D illustrated, or VRoid → Unity (UniVRM + MToon); picked at the start of art Phase 6 | Neither route needs Blender. 2D matches the ships and Touhou's spell-card cut-ins; VRoid → Unity keeps the anime-3D look |
| Mobile | One project, per-platform quality tiers | No duplicated code; keeps crossplay possible. A sibling project was considered and rejected |
| Version control | git, ships-game history retained | `origin` still points at `logang-bot/UntitledShipsGame2D` — retarget before pushing |

## Open questions (not yet decided)

- The actual card list, rarity model, and per-role pool sizes — only the
  framework and a starter set are designed. See
  [docs/systems/cards.md](docs/systems/cards.md)'s "Open questions".
- How cards are acquired/unlocked over time (the loadout screen assumes a
  pool exists; where it comes from is deferred).
- Number and structure of bosses/stages beyond Level 1.
- The cut-in route, whether live ship models hold up on low-end phones
  (phone work deferred),
  final art palettes, and audio direction. The art pipeline
  itself is decided; see
  [docs/art-roadmap.md](docs/art-roadmap.md)'s "Open questions".
- Steam integration approach (Steamworks.NET vs. other), achievements,
  leaderboards.
- Whether the card layer forces ScriptableObjects — see
  [docs/architecture.md](docs/architecture.md)'s "Deliberately Absent:
  ScriptableObjects".
