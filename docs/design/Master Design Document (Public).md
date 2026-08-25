---
type: design-document
created: 2026-08-20
status: living-document
sanitized: true
note: "Public/shareable version of this project's design documentation."
---

# Master Design Document (Public)

Structured design document for an original game project. This document gets refined/edited over time as design decisions firm up.

## Design pillars (the "why" behind everything else)
1. **Emergent, not authored.** No hardcoded mastery/progression/storyline systems. Systems should generate their own complexity from simple rules interacting, not from scripted content.
2. **Discovery over instruction.** The game should not blatantly signpost everything — a sandbox-first, exploration-driven sense of finding things out for yourself.
3. **Believable autonomy.** NPC societies must behave *intelligently*, not just procedurally-but-dumbly — a settlement of twenty residents sustained by a single tiny farm plot is a failure state to avoid. Society-level resource logic needs to actually make sense at the society's own scale.
4. **Procedural wherever possible, but performance-first.** Whenever something can be done or calculated procedurally, that's preferred — but always balanced against the hard architectural constraint below. Procedural generation that isn't performance-conscious isn't acceptable.
5. **No entity-based simulation at scale.** This is the single most load-bearing technical decision in the whole design — see Technical Architecture below.

## The three scales
A player moves between three distinct zoom levels, each a genuinely different mode of play, not just a visual zoom. See [[03 Active Projects/Xander's Game/Design/Public/Scale Terminology (Public).md]] for the full reference table (fixed internal terms vs. situational UI labels). Summary:

### 1. Individual Scale (the game's starting point)
*Situational labels: Ground (planet/moon surface) · Deck (inside a ship/station in transit) · Drift (floating freely in open space, no vessel)*
- Individual character sprite, top-down 2D, in a clean, classic top-down visual style (**not 3D**).
- **Axis convention:** ground-plane movement is **X/Z**, with **Y constrained** as a deliberately limited vertical axis (floors/elevation) — not the same geometry as a side-view 2D game where the world is seen from the side. Camera sits at roughly a 30–45° angle from perpendicular-to-ground, not a strict overhead view.
- Handheld tools, face-to-face NPC conversation, walking through foliage/terrain (forest, lava-rock, etc. — terrain-appropriate materials/armour gate access).
- Basic crafting, moving small cargo/objects by hand.
- Inside a vehicle: player sprite visible inside it, can activate controls; inside an automated-pilot rocket, can use a note-taking interface or craft while travel resolves.
- **Design intent: must feel "full of life and options at any given time"** — this is the scale the game is fundamentally about, not a tutorial layer to rush past.

### 2. Operational Scale (midgame — most active play happens here)
*Situational labels: Site (settlement/building) · Ship (spacecraft/rocket) · Boat (liquid-based transport) · Vehicle (car/rover/land transport) · Plane (tentative, atmosphere-only, deferred)*
- Land/sea vehicles, terrain-custom to the planet.
- Atmospheric flight, gated by the planet's chemical composition (can't fly anything through an atmosphere it's not built for).
- Rockets/space vehicles: launch from a landed position (planet or station) into orbit, travel to new system areas.
- Flying/driving over villages, cities, other societies' colonies, space stations, planetary wilderness.
- This is where most resource extraction/farming/midgame work happens.

### 3. Universal Scale (the broadest view — "endgame" marker)
*No situational labels — single context.*
- Locked behind a crafted item or radar — not available from the start.
- Live view of observable-universe celestial bodies.
- Shows trajectories of ships/cargo — the *visible result* of both the player's own actions and NPC-society activity, not a separate simulation layer.
- Represents planetary-scale resource extraction/trade/automation running in the background, driven by what the player has actually built.
- Also where threats become visible at scale — e.g. an enemy armada approaching.

## Procedural universe generation
- Universe = empty space + celestial bodies, generated (not hand-placed).
- **Planet generation formula, chemistry-driven — real detail now in its own document:** [[03 Active Projects/Xander's Game/Design/Public/Planetary Generation System (Public).md]] — element/compound registry, temperature/gravity/atmosphere formulas, size bounds (in the game's own **Stellar Measure, sm**, unit), region-based spatial organization, and a first-pass society habitat-requirements table, all derived from a reference sample of planets.
- **Genetics-style trait combination** for individual lifeform appearance/traits — a real mechanism (not just flavor text) for generating organic-feeling variation in creatures/NPCs across the game.

## The Unified Compositional Model (a core architecture pillar)
**The idea:** the same set of base compound/element descriptors used to generate a planet's chemistry also generates *everything smaller* — regional terrain and biomes, flora/fauna, and every item the player can hold, store, and craft (a physical inventory-item system with real material properties). One compositional table, referenced fractally at every scale, rather than separate hand-authored rulesets for "planet chemistry" and "item properties" that could drift out of sync with each other.

**The generation direction is top-down, universal → planetary:** the compositional/generation engine gets designed first at the **universal scale** (what compounds exist, in what ratios, on which bodies), then expands downward — planetary/regional generation derives from a body's composition, and the player-scale walkable world (terrain, resources, items, creatures) is the fullest expansion of that same engine, not a separate system bolted on underneath it. Player movement/manipulation at the smallest scale is "just an extension to the base generation engine" — the player doesn't interact with a different system than the one that generated the universe, they interact with the finest-grained resolution of the *same* system.

**Practical consequence for the compound/element registry we're about to build:** every descriptor we settle on for planetary chemistry (e.g. "carbon-based," "metallic," "ice/volatile-rich") needs to double as a descriptor for physical item/material properties (density, flammability, hardness, toxicity, conductivity, visual appearance) — the registry is being designed once, for both jobs simultaneously, not twice.

**Status:** registry/formula not yet built — this is the current work in progress (see Open — next steps in the main project note).

## The Six Societies
A faction-based structure: six intelligent-life factions, each with independent standing (friend/enemy/neutral) toward each of the other five — visualized as a 6-node relationship wheel.

| Society | Nature | Environment | Alignment | Notes |
|---|---|---|---|---|
| **Humanoids** | Natural human-like settlements | Earth-like/organic planets | Yang | The "default" relatable society |
| **The Industry** | Robots/AI lifeforms | Desolate, resource-dense, harsh planets | Yin-leaning, "not powerful" | Ugly machinery aesthetic, heavy space presence |
| **The Cult** | Rare, small settlements, magical/advanced | — | Yin worshippers | Split off from Humanoids, became their mortal enemy — an ideological schism turned bitter rivalry |
| **The Bugs** | Large colonies, farming-heavy | Can build hives from raw materials even in space | **Yang-leaning** | Organic themselves, but without Humanoid-level complex thought. Build by Yang principles (won't destroy life/things unnecessarily) but pragmatically "march over anything in their path" when needed — same practical indifference as Industry, despite the philosophical alignment difference. Less calm/more assertive than Mushrooms. |
| **The Mushrooms** | Tiny, hive-minded mass, barely noticeable until it "presents itself" | Requires almost nothing to live | **Both Yin and Yang** | At the center of both life and death — decomposers, resetting the life cycle as bottom feeders. Population scale makes total elimination effectively impossible. |
| **The Network** | Single hive-minded mechanical presence, acts as one unit | — | Opposes **both** Yin and Yang | **A totalitarian singularity that mimics Yin's own nature** — ironic given its ideological opposition to Yin. **Story hook, not yet committed:** if narrative elements get written, Yin may be the one to eventually crush the Network. |

**Relationship wheel.** Relationships are **directional/asymmetric**. 3 states: **Friendly** (denotes a general positive characteristic to the relationship, not necessarily a battle-pact; some Friendly pairs, like Cult↔Industry, are transactional trade relationships rather than anything resembling an alliance), **Neutral** (covers both situational neutrality and genuine indifference), **Enemy**.

**Layout mirrors the Yin/Yang symbol, by design:** hexagon positions put true opposites across from each other — **Mushrooms↔Network**, **Humanoids↔Cult**, **Bugs↔Industry** — with a tilted dotted divider (between Humanoids/Network on one side, Cult/Mushrooms on the other) so **Mushrooms lands fully on the Yang side** and **Network fully on the Yin side**, rather than a forced even split.

**Yin and Yang have real nodes on the chart:** both sit on the pure horizontal line through the hexagon's true center, symmetric left/right, in close toward the middle. Styled distinctly (gold-bordered) from the six society nodes to read as conceptually different — forces, not societies. Visual: [[03 Active Projects/Xander's Game/Design/assets/relationship-wheel.png]] (internal asset — not part of this sanitized doc set).

**Network neutral to all, Mushrooms neutral to all — the general rule wins.** Real philosophical grounding for *why* both are universally neutral, not just an arbitrary simplification — see "Life/death philosophical framework" below.

**Relationship design finalized — all lines rendered fully opaque.** The Reasoning column below remains genuinely incomplete in places by design — "finalized" refers to the relationship *values*, not that every cell has a filled-in narrative justification yet.

| Source → Target | Stance | Reasoning |
|---|---|---|
| Humanoids → Cult | Enemy | Split off from the Humanoids, became their mortal enemy (original founding lore) |
| Cult → Humanoids | Enemy | (same) |
| Humanoids → Bugs | **Friendly** | Humanoids value the Bugs' large-scale farming/labor capacity — mutual, uncomplicated collaboration between the two Yang-camp organic societies |
| Bugs → Humanoids | **Friendly** | (same) |
| Humanoids → Mushrooms | Friendly | Mushrooms support the whole human ecosystem, and Humanoids know this, so they act in the Mushrooms' benefit |
| Mushrooms → Humanoids | Neutral | |
| Humanoids → Industry | Enemy | The Industry marches over human habitat |
| Industry → Humanoids | Neutral | The Industry is simply indifferent to Humanoids — doesn't register them as significant either way |
| Humanoids → Network | Enemy | |
| Network → Humanoids | Neutral | Confirmed — see note above |
| Industry → Cult | **Friendly** | The Industry handles materials in abundance but not rarity/magic — Cult's spell technology lets Industry hunt down additional resources and perform conversions it couldn't manage alone |
| Cult → Industry | **Friendly** | The Cult deals in magic, spells, and exceptionally rare materials but not infrastructure or scale — Cult uses Industry's infrastructure to house members in the harshest climates. Complementary need on both sides, not a one-sided favor. |
| Industry → Bugs | Neutral | |
| Bugs → Industry | Enemy | The Industry are stronger, more organized lifeforms of a similar underlying variety to the Bugs, and more resilient to the Bugs than the reverse — the Bugs are aware of this flaw in their own design |
| Industry → Mushrooms | **Friendly** | The Industry could use the Mushrooms in farming and transforming materials |
| Mushrooms → Industry | Neutral | |
| Industry → Network | Enemy | (original founding lore — established rivals) |
| Network → Industry | Neutral | Confirmed — see note above |
| Cult → Bugs | Neutral | The Cult uses Bugs as a resource for spell materials — extractive, not reciprocal; Bugs receive no major benefit in return |
| Bugs → Cult | **Friendly** | The Bugs aren't intelligent enough to distinguish the Cult from Humanoids philosophically — they read Cult as the same lifeform and wrongly extend it the same trust/treatment they give Humanoids. A mistaken alliance, not a reciprocated one. |
| Cult → Mushrooms | Friendly | |
| Mushrooms → Cult | Neutral | |
| Cult → Network | Enemy | |
| Network → Cult | Neutral | |
| Bugs → Mushrooms | Friendly | |
| Mushrooms → Bugs | Neutral | |
| Bugs → Network | Enemy | |
| Network → Bugs | Neutral | |
| Mushrooms → Network | Neutral | Mushrooms are so expansive and vast in wisdom that they don't concern themselves with the Network, and assume some fundamental limit in its own existence will protect them regardless |
| Network → Mushrooms | Neutral | The Network does observe the Mushrooms, but accepts it is far smaller and younger than them in its current state — it spends its time trying to expand and catch up in size, cutting the Mushrooms down as it goes. A worldly equivalent of the Yin/Yang story arcs. |

**The Mushrooms/Network dynamic:** both exist "on their own terms as the most direct equivalent to Yang and Yin respectively" — Mushrooms genuinely unconcerned with the Network (vast enough not to need to be), while the Network *is* aware of and actively encroaching on the Mushrooms specifically, despite the relationship being mechanically "Neutral" both ways. The flat 3-state label doesn't fully capture this asymmetry in *attention* even where the stance itself is symmetric.

### Life/death philosophical framework
Real thematic architecture behind *why* the Mushrooms and Network are universally Neutral, not an arbitrary simplification:

- **The Mushrooms feed off dead and inanimate material, in service of life** — decomposers that move the dead back into the living, "a supporter of life" through death. Because their function is to *convert* death into life, they need to be inherently neutral to both sides of that exchange, while genuinely **favouring life when needed** — "as it is a force that moves the dead into the living." Their Yang lean (see the tilted relationship-wheel divider) isn't arbitrary — it's the direct consequence of their ecological function.
- **The Network, by contrast, consumes the living, and is inherently dead — though not inanimate.** A mirror-opposite function to the Mushrooms: where Mushrooms convert dead→living, the Network converts living→dead — animate, active, functioning, but philosophically on the death side of the ledger. This is the real grounding for the Network's Yin lean.
- **This frames the living organic societies (Humanoids, Bugs, Cult) as a kind of middle ground within the setting's own philosophy** — with "dead" itself acting as a conduit between two extremes: **inanimate unexistence** on one end and **complete wholistic infinity** on the other. Still actively being thought through, not presented as finished cosmology.
- **A striking consequence:** with Industry→Mushrooms now Friendly, **4 of the 5 other societies (Humanoids, Cult, Bugs, Industry) all independently regard the Mushrooms positively**, while the Mushrooms themselves stay neutral in return to every one of them — and the *one* relationship that isn't warm toward the Mushrooms is the Network's. Every living-oriented and even resource-driven society values what the Mushrooms provide; only the force that consumes life rather than supporting it doesn't. This pattern wasn't designed top-down — it fell out naturally from individual relationship decisions made independently.

## The Two Fundamental Forces
Explicitly **not** good/evil or a Western religious dualism — "beings which possess typically different features, though with some overlap." Working names "Yin"/"Yang" are placeholders (see naming flags).

### Yin
- Enormous, edgeless creature — a face floating in the emptiness of space, possibly spawning from a black hole.
- Controls a floating white hand puppeting a dummy made of shiny celestial material; sparkles rainbow, wears jewelry.
- Visual direction: sharp, triangular natural-rock-formation features it can produce on demand, and an ornate, otherworldly boss-creature design language drawing on classic platformer final-boss silhouettes.
- Likely the game's final boss.
- Reward for a successful interaction: **a weapon.**
- **Spawn placement:** Yin occupies the universal origin coordinate (see the origin-void mechanic in [[03 Active Projects/Xander's Game/Design/Public/Planetary Generation System (Public).md]]). **The player's own spawn point is deliberately placed far from this origin**, so that finding Yin becomes a genuine emergent late-game discovery rather than something encountered early — consistent with the discovery-over-instruction design pillar, not signposted to the player in any way.
- **Idea, parked:** an unlabeled physical rendition of the full relationship wheel (hexagon, arrows, Yin/Yang placement) somewhere discoverable in-world — a rune, a constellation, possibly sited near the origin itself.

### Yang
- Lives at the *lower* scale (Individual Scale) — a bright shining humanoid, wise, mildly magical, never abuses its power.
- Only found in natural wilderness the player must reach on foot, having left ship and armour behind (literally "reduce themselves to being naked").
- Reward for a successful interaction: **a tool.**

## Autonomous NPC-society economy
Every society's settlements need internally-consistent resource logic scaled to their actual population/needs — not window-dressing. This is closely tied to the deterministic-simulation architecture below, since "20 residents, one farm" is exactly the kind of thing a real input/output model would catch and a naive entity-placement approach wouldn't.

## Technical architecture — the Containment Collapse Model
This is the design's central engineering problem. Its actual shape ties directly to the Individual/Operational/Universal scale system.

**Target hardware, stated explicitly: a mid-range 2019-era laptop.** Not high-end gaming hardware — this is a hard, real constraint the whole architecture is designed around, not an aspirational target.

- **No per-item/per-entity simulation at scale.** Physically simulating every entity/mob/NPC in every loaded area is the wrong model for a universe this size — too slow, too labor-intensive computationally.
- **Universal Scale simulation always runs, in the background, for the whole universe at once.** This is non-negotiable: a player who automates resource extraction on one planet, leaves, and returns later needs to find the results waiting — the underlying simulation can't pause just because nobody's watching. Same for an automated rocket launch reaching a planet while the player is elsewhere. **The Star Chart** — a craftable/unlockable item — lets the player *view* this always-running simulation directly at Universal Scale, and is also where planets (procedurally coded, not hand-named — see [[03 Active Projects/Xander's Game/Design/Public/Planetary Generation System (Public).md]]) can be given a player-chosen display name.
- **The universe is organized into regions** (4500×4500 sm each, a real 9×9 grid of 500×500 sm coordinates — deliberately not called "chunks," to avoid overloaded terminology) — this is the concrete spatial unit the collapse/expand mechanism above actually operates on, and also how stars get placed relative to planets for the temperature formula. Full detail: [[03 Active Projects/Xander's Game/Design/Public/Planetary Generation System (Public).md]].
- **Only one place ever needs full live rendering at a time — because the player can only physically be in one place.** Individual/Operational Scale rendering (sprites, NPCs, machines, individual entities) is narrowly scoped to wherever the player actually is; everywhere else stays collapsed.
- **The collapse is recursive, following physical containment.** A machine's inputs/outputs render individually only when the player is inside the vehicle/site that houses it. Step back one level (player leaves the ship) and that machine's behavior collapses into a single term in *the ship's* aggregate input/output function. Step back again (the ship is parked at a station or planet) and the ship — with everything it houses already collapsed inside it — becomes one term in *that station or planet's* aggregate function. Every level of containment (item → machine → vehicle → site/station/planet → system → universe) can independently be either fully expanded (player present) or collapsed to pure math (player absent) — this directly mirrors the Individual → Operational → Universal scale hierarchy and the situational-label containers (Ship, Site, Boat, Vehicle) named in [[03 Active Projects/Xander's Game/Design/Public/Scale Terminology (Public).md]].
- **Re-expansion on entry, not continuous background simulation.** Entering a collapsed environment (landing on a planet, boarding a ship) computes "time has elapsed, apply the baked function's result" *once*, places/updates the actual state (items generated, storage modified, entities placed) accordingly, and only *then* switches that environment into live, per-entity rendering. Nothing is touched on disk or recomputed continuously while collapsed.
- **Hide loading, don't block on it.** A progressive-reveal loading technique (e.g. a planet's atmosphere loads first as a flat color/texture, detail fades in after) as the general technique for masking the expand step, used as needed rather than a blocking load screen.
- **Open sub-problem:** recomputation cadence for slower, universe-shaping changes (society expansion, composition evolution over time) is a separate, harder question from the collapse/expand mechanism above — deciding *how often* those larger, non-immediate numbers get recalculated is still open and likely needs its own answer once there's a real simulation to tune against.
- **Status:** the *mechanism* is now designed (this section). What's not yet done: the actual data structures/formulas for "aggregate input/output function," and where exactly the containment tree's boundaries sit — this is the next real implementation question once the design side is far enough along.

## Procedural music engine (future system, explicitly deferred)
- Auto-composes according to preset instruments/rhythms/genre rules, keyed to in-game context (Industry setting vs. wild nature vs. empty space, etc.).
- **Yin's theme, specifically:** aims for a mood of awe, making the player feel small. (An original procedurally-generated score, not a licensed track.)
- Not a near-term build target — noted for the record, not scoped yet.

## Naming flags (placeholders, not final)
- **"Xander's Game"** — already logged in the main project note as a placeholder; needs a real, distinct title.
- **"Yin" / "Yang"** — real, culturally-specific philosophical terms being borrowed for original fictional entities that are explicitly *not* that concept. Worth original names eventually, not urgent now but flagged so it doesn't quietly become permanent by inertia.
  - **Naming exploration — parked, not locked.** Extensive real-word brainstorming (deliberately plain-English rather than invented-fantasy vocabulary — see setting note below). **Current leading candidates: "Warden" for Yang, "Entropy" for Yin** — not required to be a strict antonym pair. If a true antonym for Yang is ever wanted after all, real candidates include **Negentropy** (an established physics term) or **Order** (plain-English equivalent, phonetically close to Warden); **Syntropy**/**Extropy** are real but more fringe/philosophical.
  - **Setting clarification (real worldbuilding decision, not just a naming preference):** the game takes place in the **far future**. This is *why* plain, real English words (Entropy, Warden, Order, Singularity) read as more fitting than invented fantasy vocabulary — a modern/post-modern, scientifically-literate far-future tone, not a medieval-fantasy one.

## Open decisions
See the accompanying project note for the live list of open items.
