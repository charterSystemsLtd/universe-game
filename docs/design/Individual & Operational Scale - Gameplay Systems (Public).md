---
type: design-document
created: 2026-08-23
status: exploratory — parked, not yet decided
sanitized: true
note: "Public/shareable version of this project's design documentation."
---

# Individual & Operational Scale — Gameplay Systems (Public)

Forward-thinking design conversation, explicitly **parked for later iteration** rather than locked — these are decisions to largely park and iterate over later, but worth capturing now so nothing gets lost. Where a real, concrete decision *did* emerge from the conversation, it's marked as such; everything else is inspiration/direction, not a commitment.

Prompted by noticing an early design instinct leaning heavily toward a classic farming/resource-collection loop, and worrying that doesn't leave enough room for player creativity in a flat 2D game — plus early thinking on combat difficulty and vehicle piloting feel.

## Axis convention (real correction, now reflected in the Master Design Document too)
The game's ground-plane movement is **X/Z**, with **Y constrained** as a deliberately limited vertical axis (floors/elevation) — this is a genuinely different geometry from a side-view 2D game (which uses X/Y, viewing the world from the side, with full unconstrained vertical movement and no Z axis at all). Camera sits at roughly a **30–45° angle from perpendicular-to-ground** — not a strict top-down view, which matters for how much height/verticality can actually be *read visually* even before any camera-occlusion tricks come into play.

## Building & creativity in a flat, X/Z world
Real concern: full freeform 3D block-stacking isn't available. Top-down (and near-top-down) games have their own strong creativity tradition that doesn't depend on it:

- **Terraforming as the creative medium** — since extraction already reshapes terrain (digging out Silicon-rich rock yields raw Silicon *and* edits the landscape), the same action can serve as both resource-gathering and creative expression, no separate "creative mode" tool needed.
- **Layout-based creativity instead of stacking** — colony-management and farming-sim games are largely top-down (or close to it) and are widely considered deeply creative despite little/no verticality. Creativity lives in room/zone shape, path design, decorative density, efficient-vs-decorative tradeoffs. Maps cleanly onto the existing **Site** label (Operational Scale) — already a bounded, ownable area.
- **Crafting-tree depth as an alternate creativity outlet** — some industrial-automation games express creativity almost entirely through *what* you build and how you chain it, not spatial stacking. The compositional registry (18 elements) is already a natural backbone for this.
- **Real reference points worth remembering when this gets built for real:** some top-down farming/life-sim games have added genuine terraforming over time (cliff tiers/multiple elevation levels, rivers, ladders between levels) — not freeform (fixed-height tiers only), and construction itself stays menu/catalog-based rather than freeform placement, but a closer precedent than initially assumed. A classic top-down farming perspective is the target camera/perspective reference generally — its own verticality solution (single floor rendered at a time, ladder as a discrete instant-swap trigger, no visible transition) is real but explicitly *not* smooth enough for what's wanted here — see Floor/camera system below.

## The floor/camera occlusion system (real proposal, worth taking seriously later)
This is not speculative invention — it's a known, working technique.

**The core idea:** rather than a discrete instant floor-swap, or hardcoded pre-baked camera transitions (which can't work here since everything's procedural), use **dynamic occlusion/silhouette transparency** — hide or fade whatever's between the camera and the player based on the player's current position, snapping back once they're clear. This is the same technique classic isometric and top-down-angled RPGs across multiple eras use to handle multi-story buildings without true 3D. Combined with the 30–45° camera angle (which already gives a real height read, unlike strict top-down), this is a sound direction.

**Standardization — validated as the right simplification:** **one fixed Y-unit per floor, constant across all materials and all generated structures** (player-built or procedural) — turns "handle arbitrary verticality" into "handle a small integer floor-index," matching the same economy-of-systems instinct behind Individual/Operational/Universal Scale itself.

**Refinements surfaced during the conversation, for whenever this gets designed for real:**
- Floor transitions triggered by specific craftable items (stairs, ladders) — a natural category in the compositional-registry item system (a wooden ladder vs. a reinforced Titanium one).
- **The "load-bearing column / structural pillar" idea** — a specific craftable item that's the *only* way to extend a structure upward, giving verticality a real material cost and a clean mechanical anchor rather than "just build up somehow." Pair with material stats (Iron pillar supports more than wood) — same connective thread as the compositional registry driving everything else.
- A related but *not identical* precedent exists in some 2D sandbox games: a purely background-decorative wall layer with zero collision. What's being proposed here is player-built walls that *do* block movement but get occluded by the camera based on player position, which is closer to real building geometry than that kind of decorative-wall mechanic.
- **Confirmed this doesn't need to touch the Containment Collapse Model** — floors are a rendering/camera concern purely *within* an already-expanded, already-loaded Individual Scale environment (a Site or Ship currently rendering live), independent of the collapse/expand architecture.

## Combat difficulty
**Target feel:** the player should occasionally (rarely, not often) encounter something clearly beyond their current capability, in a way that feels fair-but-punishing rather than random, with the encounter itself communicating "this is possible, just not for you yet" — a style associated with a certain school of difficult, exploration-driven action games known for occasionally placing a clearly-overleveled threat in the open world.

**Mechanism — reuses existing systems rather than inventing a progression ladder:** harsher zones (per the Planetary Generation System's zone/band classification) naturally cluster both better materials *and* more dangerous residents — difficulty and reward scale together as a consequence of *where the player chooses to go*, not a fixed unlock gate (which would sit in real tension with the "no hardcoded progression" design pillar — some sandbox games' boss-gated progression tiers, while a great feel reference, are exactly that kind of hardcoded gate).

**"Recommended route" — internal only, never surfaced:** the generation system can track an internal "expected difficulty" value per region/planet to tune encounter and loot density, sketching an implicit route through a star system — but this is purely a backend tuning number. The player only ever learns about it through NPC dialogue or environmental clues, never a marker, meter, or UI element. Consistent with the discovery-over-instruction pillar.

**Real worldbuilding decision, not just musing:** the player's spawn point is placed deliberately far from the universal origin (Yin's location) — see the Master Design Document's Yin section — specifically so danger/reward scaling by distance-from-spawn and "discovering the origin late" can be the same underlying mechanism.

**Combat feel references:** classic top-down action-adventure directional melee combat (precise, hitbox-honest); twin-stick roguelike shooters for top-down ranged combat (tight dodge/aim loops); loadout-crafting philosophy built from parts rather than randomized loot-table drops (fits the emergent-systems philosophy better than pure RNG-stat itemization).

## Build variety — options menu
1. **Material-driven stat variance** — same item archetype, different compositional inputs, different stats. Passive variety, already free given the registry exists.
2. **Modular component/socket systems** — items with slots for attachments that change behavior, not just stats (a loadout-modification system, or item-socketing in the style of some action-RPGs).
3. **Society-specific gear lines** — the strongest option, since the Six Societies already imply six genuinely different mechanical flavors without inventing anything new: Industry gear reads heavy/durable/slow, Cult gear reads exotic/high-risk-high-reward, Bugs gear reads organic/self-repairing, etc. Just needs someone (later) to translate existing flavor into actual stat philosophy per society.
4. **Playstyle archetypes via approach, not class** — melee/ranged/environmental-trap/vehicle-centric as loose leanings (a class-ish-but-not-rigid model), avoiding a hard class system that would contradict the no-hardcoded-progression pillar.
5. **Yin/Yang rewards as genuinely unique, non-craftable anchor items** — since they're already narratively singular (one weapon, one tool, each from a one-time cosmic encounter), they could mechanically anchor entire builds around themselves, unlike anything obtainable from normal materials — matching their mythic narrative weight instead of sitting on the same power curve as everything else.

## Vehicle piloting feel
**Core principle:** the satisfaction comes from real momentum/inertia mastery, not snappy on-rails arcade movement — worth preserving genuine physics feel in both ground and air/space handling, in the tradition of orbital-mechanics simulation games.

**Construction ties back to the compositional registry, same as everything else:** modular vehicle-construction games build vehicles from parts with real physical properties — a rover built from cheap Aluminum vs. Titanium shouldn't just look different, it should *handle* different (weight, durability, top speed). Same underlying data driving buildings, weapons, and vehicles — this is the actual throughline connecting all of this session's topics back to the Unified Compositional Model.

**Air vehicles in a top-down/angled camera:** the standard technique for conveying altitude without true 3D is a **shadow sprite** beneath the vehicle that shrinks/moves independently as altitude changes (classic in top-down flight games) — cheap, well-understood, worth remembering once this gets built.

**Inspiration, split by context (functional references, not literal ports):**
- **Space/thruster-based piloting:** pure-inertia, zero-friction, rotate-and-thrust-only space flight (the foundational genre archetype); a deeper top-down gravity-well flying model with a tethered cargo pod for more advanced momentum feel; the classic thrust-against-gravity landing/takeoff mastery archetype.
- **Ground vehicles, top-down camera:** classic top-down car-driving games with realistic drift/momentum physics (the strongest, most historically relevant precedent for this camera style); more arcade-accessible top-down racers, if a snappier, less simulation-feeling handling model is wanted instead.

## Operational Scale — Ship-in-space scene construction (real, maintained implementation, added 2026-08-25)
Unlike the rest of this document, this section describes an actual scene structure that exists in the codebase and is meant to be maintained/extended over the course of development, not a parked idea. This is the concrete realization of the **Ship** situational label (Operational Scale — see Scale Terminology): the player-controlled ship, its camera, and everything visually surrounding it in open space.

**Structure:** a root scene instances the player's ship (with its attached camera) alongside a background starfield as siblings — deliberately siblings, not nested, since the camera follows the ship but the background must NOT move with it, or relative motion becomes invisible.

**Starfield — 3-layer parallax system, first pass.** The movement/tiling/layering *mechanics* below are real and durable; the visuals themselves (plain circles standing in for stars/planets, a soft blob standing in for a nebula) are explicitly unverified placeholders, expected to be replaced once real art exists.

- **Three depth layers — Near Space, Far Space, Deep Space** — each independently configured on three axes:
  - **Tile size** (how large an area of content repeats before wrapping): **Near > Far > Deep.** Deliberately different per layer, not just different speeds, so the same repeating pattern isn't visible at the same interval across all three layers simultaneously — travelling through space produces varied combinations rather than an obviously-looping backdrop.
  - **Apparent movement speed relative to the ship** (parallax depth illusion): **Deep moves slowest, Near moves fastest** — distant layers barely shift, close layers shift more, producing real depth perception as the ship travels.
  - **Object size** (stars/planets scattered within each layer): **Near largest, Far medium, Deep smallest** — reinforces the same near/far depth read visually, independent of movement.
  - Deep Space additionally includes an occasional very-low-opacity large placeholder shape standing in for a nebula/supernova — background visual interest, not yet real art.
- **Seamless tiling/wraparound** is the core mechanic requested — when the camera has moved far enough that a layer's content would run out, it loops back to repeat rather than showing empty space. Implemented via a built-in engine mechanism for exactly this (parallax-layer motion scale + mirroring), not hand-rolled wrap-around math.
- **Status:** movement/tiling mechanics implemented and functional. Visual content (shapes, colors, nebula placeholder) explicitly first-pass/unverified — revisit once real art exists.

## Status
Nothing here is locked except the Ship-in-space scene construction above (real, maintained code). Everything else in this document is still parked. Revisit once Individual/Operational Scale design work actually begins for real.
