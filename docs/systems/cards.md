# Cards & Deckbuilding

The strategic layer merged in from the `tohou_deck` concept. **Nothing here
is implemented yet** — this is the design spec to build against. For the
role system it layers on top of, see [player-roles.md](player-roles.md); for
the bullet-hell changes that pay for it, see "Grazing and energy" below and
[combat.md](combat.md).

## The core idea: the deck is your hotbar

The three source concepts each wanted something different from the player's
attention:

- Touhou wants **all** of it on dodging — there is no time to read a card.
- A deckbuilder wants deliberate, unhurried **choices**.
- An MMO raid wants a **rotation** executed under pressure.

These reconcile if the deliberate choices happen *before* the fight and the
in-fight interaction is a hotbar. So: you build a 10-card deck in a loadout
screen, and during the fight your deck feeds a 3-slot hand you fire with
three keys. You never read a card mid-curtain — you learned what was in your
own deck when you built it.

This is the single design decision everything below follows from.

## Grazing and energy — the mechanic that fuses the two games

Cards cost **energy**. Energy is earned almost entirely by **grazing**:
passing close to an enemy bullet without being hit.

This is the merge's load-bearing mechanic. It means bullet-hell execution
skill directly funds the deckbuilding layer, rather than the two systems
running in parallel and merely coexisting. A player who dodges tightly plays
more cards; a player who hangs back safely at the bottom of the screen has a
deck they can't afford. It also gives the Touhou half a reason to exist
mechanically, instead of being a difficulty setting.

Supporting changes it requires:

- **Small hitbox.** Ships currently collide via a `BoxCollider2D` matching
  the sprite. Touhou-grade curtains need a tiny central hitbox instead —
  roughly 15-20% of the sprite width — with a separate, larger **graze
  radius** around it.
- **Focus mode.** Hold a modifier (Shift / gamepad left trigger) to halve
  move speed for precise threading, render the normally-hidden hitbox, and
  widen the graze window. It applies through the existing non-destructive
  multiplier pattern (see [../architecture.md](../architecture.md)'s
  "Non-Destructive Buff Pattern") — a `focusSpeedMultiplier` field read at
  the point of use, never a mutation of the base stat.

### Starting numbers (placeholders, pre-playtest)

| Value | Start at | Note |
| --- | --- | --- |
| Energy cap | 5 | Small enough that hoarding isn't a strategy |
| Energy per graze | 0.25 | Four tight dodges is about 1 energy |
| Graze credit per bullet | once only | A single bullet can't be farmed |
| Passive energy regen | 0.1/s | A floor, so a pinned player isn't locked out |
| Card cost range | 1-3 | |
| Focus move-speed multiplier | 0.5 | |

Consistent with the rest of the project, these live in a static in-code
table, not a tuning asset — see "Data model" below.

## Deck rules

- **Deck size: exactly 10 cards.** Fixed, not a minimum — a fixed size keeps
  draw odds legible and removes "just add more good cards" as a strategy.
- **Role-gated.** Every card belongs to exactly one role's pool or to a
  **Neutral** pool available to all. You build from your own role's pool
  plus Neutral. This is what keeps the deck *customizing* the role rather
  than replacing it (see [player-roles.md](player-roles.md)).
- **At most 2 copies** of any one card.
- **At most 3 Passive cards** per deck (see the two card kinds below).

## Two card kinds

### Passive (Modifier)

Applied once at fight start and active for the whole encounter. Costs no
energy, occupies no hand slot, and has **zero in-fight attention cost** —
which is exactly why the cap is 3. These are the quiet deckbuilding choices.

They must apply through the same non-destructive multiplier pattern —
runtime-only fields read at the point of use, never multiplied into
`RoleStats` and later divided back out. `RoleStats` stays the single source
of truth for base numbers.

Examples: `+30% graze radius`, `+1 max energy`, `Shield regenerates 0.2/s
outside Medic's aura`, `Signature ability cooldown -25%`.

### Active (Tactic)

Shuffled into a draw pile, drawn into a 3-slot hand, played with a dedicated
key per slot.

- Hand of **3**, drawn at fight start.
- Playing a card: pay its energy cost, the effect fires, the card goes to
  the discard pile, and a **replacement is drawn into that slot
  immediately**.
- A **global cooldown of 0.5s** after any card, so all three can't be dumped
  in a single frame.
- Draw pile empty reshuffles the discard pile back in. Decks cycle; you
  never run dry.

Examples: `Bullet Clear` (destroy every enemy bullet within 2 units),
`Overcharge` (+100% fire damage for 3s), `Phase` (2s of invulnerability that
also disables your own fire), `Rally` (refund 1 energy to every ally).

## In-fight controls

The signature role ability stays exactly where it is — **`E` / gamepad
West**, unchanged, free, no card involved. Cards are additive, not a
replacement.

| Input | Keyboard | Gamepad |
| --- | --- | --- |
| Card slot 1 / 2 / 3 | `1` `2` `3` | North / East / Right Shoulder |
| Focus | `Left Shift` | Left Trigger |
| Role ability | `E` | West |

That's three new `PlayerControls` actions (`Card1`/`Card2`/`Card3`) plus
`Focus`. Because every input-driven component in this project also exposes a
non-input entry point (see [../architecture.md](../architecture.md)'s "Dual
Entry-Point Pattern"), the same rule applies here: `PlayerCards` must expose
`TryPlayCard(int slot)` publicly so `AIController` drives an AI teammate's
deck through the identical path. **AI teammates get decks too** — otherwise
a one-human party plays a fundamentally different fight than a four-human
one.

## Deck loadout scene

A new `DeckBuild.unity` sits between `RoleSelect` and whichever level scene
is chosen, in the flow, and is appended at whatever the next available
Build Settings index is when it's built (currently **8**, since
`LevelSelect`/`Level2`/`Level3` now occupy 4/6/7 — see
[scene-flow.md](scene-flow.md)) — appending rather than inserting avoids
renumbering every scene before it. Flow order and build order don't have to
match; only index 0 is meaningful. Its exact position relative to
`LevelSelect` (before or after level-picking) is a decision for whenever
this scene is actually built, not settled here.

It mirrors `RoleSelect`'s existing single/multi split: one picker for a solo
human, a row per player for 2+ local co-op, each driven by that player's own
paired device. Back returns to `RoleSelect`. AI-filled roles get a hardcoded
default deck per role — no picker row.

## Data model

Following the project's existing convention (see
[../architecture.md](../architecture.md)'s "Deliberately Absent:
ScriptableObjects" and `PlayerRole.cs`'s static `PlayerRoleStats` table),
the first pass uses a **static in-code card library**, not a
`ScriptableObject` asset workflow:

- `Card.cs` — `CardId` enum, `CardData` struct (`id`, `name`, `role`,
  `kind`, `cost`, `description`), static `CardLibrary` lookup.
- `CardEffect.cs` — resolves a `CardId` into its effect. A `switch` on
  `CardId`, matching how `PlayerAbility` branches on
  `PlayerRoleComponent.role` in one script rather than four.
- `PlayerCards.cs` — per-ship `MonoBehaviour`: draw pile, hand, discard,
  energy, `TryPlayCard(int slot)`.
- `PlayerGraze.cs` — per-ship: graze detection against `Bullet.Active` (the
  existing static registry — do **not** add a `FindObjectsByType` scan) and
  energy award.
- `PartyDeckAssignment.cs` — plain `public static class` carrying each
  player's chosen deck from `DeckBuild` into whichever level scene is
  chosen, exactly as `PartyRoleAssignment` carries the role choice today.

One class per file, no exceptions — see
[../architecture.md](../architecture.md)'s "Script Organization" for the bug
that rule exists to prevent.

**This is the point where the no-ScriptableObjects convention is most likely
to become wrong.** Role data is four rows of six numbers; a card library is
*content*, and content wants an asset workflow and a designer-editable
inspector. The convention holds fine for a starter set of ~20 cards. Revisit
it before the pool grows past that — it's a documented trade, not a
prohibition.

## HUD

Party frames already carry role / HP / shield / speed / fire-rate / ability
lines (see [hud-layout.md](hud-layout.md)). Cards add, for the local human
only:

- An **energy bar** on the human's own party frame.
- A **3-slot card tray** at the bottom of the portrait playfield — name and
  cost per slot, greyed when unaffordable, with the global-cooldown sweep.
  Kept inside the playfield rather than in the sidebar because it has to be
  readable in peripheral vision while dodging, and the sidebar doesn't exist
  on phones anyway.
- Passive cards get a small always-visible icon row — you should be able to
  see what you brought without pausing.

AI teammates' decks are deliberately **not** surfaced. Their frames already
have a clickable ability line; three more rows each would swamp the sidebar.

## Boss-side merge: spell cards

The Touhou half needs the boss to change too, not just the player. Level 1's
boss already has HP-threshold phases plus telegraphed Fan / Ring / Spiral
barrages (see [marauder-boss.md](bosses/marauder-boss.md)) — which is most of a Touhou
spell card already.

The merge formalizes it: a **spell card** is a named, timed boss attack
phase with its own HP bar and its own bullet pattern, announced on
`BossPanel`, ending when either its HP is depleted or its timer expires.
Those two outcomes should be distinguishable, so a later scoring pass can
reward an actual capture.

This maps onto the existing two-phase structure without redesigning it:
phases stay the coarse HP-threshold skeleton, and spell cards become the
authored set-pieces inside them.

## Networking note

Cards make the eventual server-authoritative rework (see
[../overview.md](../overview.md)) meaningfully larger: energy, draw order,
hand state, and card resolution all become authoritative state, and draw
order needs a seeded, server-owned RNG rather than bare `UnityEngine.Random`.
Worth writing the deck/draw logic against an explicit seed from day one —
cheap now, expensive to retrofit. This doesn't change the build order;
networking is still last.

## Open questions

- The actual card list beyond a starter set of ~20 (five per role, plus
  Neutral).
- Rarity model — whether one exists at all, given a fixed 10-card deck.
- Where cards come from: unlocks, run rewards, or everything available. The
  loadout screen assumes a pool exists and deliberately doesn't answer this.
- Whether Passive cards should be a separate 3-slot loadout instead of
  competing for the same 10 deck slots.
- Whether grazing should also feed something else (score, a party-wide
  meter) or stay purely a card economy.
