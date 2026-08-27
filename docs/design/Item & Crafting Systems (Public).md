---
type: design-document
created: 2026-08-25
status: living-document — early capture, expect heavy iteration
---

# Item & Crafting Systems (Public)

Captured from a single large braindump session (2026-08-25) — the actual "meat and potatoes" of the game, per Xander's own framing: everything built so far (Unified Compositional Model, Modular Scene Assembly, the sm scale) was scaffolding *for* this. This document is intentionally a first-pass catalog, not a finished spec — the point right now is making sure nothing gets lost, not locking anything in.

## Item wishlist — raw catalog, unsorted by priority

**Raw materials / natural resources:** ice, water, wood (six-ish types, environment-dependent), mushrooms (many types — deferred until mushroom-colony work), iron, copper, gold, uranium, rare earths (lithium specifically called out, for batteries).

**Structures (wood-primary to start, extensible to other materials):** walls, platforms (flooring/extra floors), stairs, inclines (roofing, ramps).

**Electrical:** wiring and whatever it powers — see "Electrical systems" below, this has a real architectural decision attached, not just a content list.

**Furniture:** not yet itemized, flagged as a category to fill in.

**Weapons & armor:** swords, bows/arrows, guns, armor.

**Food:** wheat, carrots, potatoes, etc. — explicitly a placeholder list, needs real expansion for biodiversity across different environment types once the composition/temperature-band system is actually feeding into what grows where.

**Crafting stations:** a "relatively large set" — not itemized yet, but treated as its own real category (not just an implicit side-effect of recipes).

## Drops vs. placement — a real distinction, not the same mechanic

**Physical/natural entities and the items they yield are two different things, not the same object wearing two hats.** Example given directly: cutting down an oak tree yields oak wood (planks); placing oak wood back down does **not** recreate a tree — it places wood. This means a harvestable natural entity (the tree) should **reference** what item(s) it drops, rather than a tree literally *being* an item with alternate placement behavior. See the abstract Item model below — this maps to a `IHarvestSource`-style capability, separate from `IPlaceable`.

**Open, undecided:** whether some materials need a refinement chain (tree → logs → planks) rather than a direct single drop. Flagged as likely relevant specifically for fuel, not decided as a general rule.

## Walls — adjacency/connection system needed

Walls (and likely wiring, and possibly other tile-adjacent structures later) need a real connection system: sprites that visually connect to neighbors up/down/left/right (or any combination), both for rendering *and* for barrier/collision function. This is a well-established pattern in top-down/tile games generally (often called "autotiling" or "blob tiles") — not something to invent from scratch, worth researching the standard tile-bitmask approach once this gets built for real.

## Electrical systems — real architectural decision, not just content

**Explicit design call: NOT a Minecraft-redstone-style local block-to-block procedural signal propagation system.** Instead: each electrical system gets identified and referenced as its own distinct object in code, with its behavior handled **programmatically**, not by scanning/propagating through individual tile components. Xander's own reasoning, worth preserving exactly: this matters for **maintaining the Containment Collapse Model** and **quasi-infinite expandability** within the universe — a system that requires per-tile procedural signal propagation doesn't collapse/expand cleanly the way the rest of the architecture is designed to.

Practical implication once this gets built: a "circuit" or electrical network should be its own real object (something like a graph of connected components), collapsible/expandable as a unit the same way a vehicle or site collapses — not a property scattered across individual world tiles that has to be rescanned every time.

## Crafting tree — the real target

**Goal: build a complete, connected crafting tree starting from uncraftable (base/raw) items, so every item in the game is reachable and connected to something** — nothing floating disconnected from the rest of the system. This is as much a data-integrity goal as a content goal: worth having a real way to validate "is everything actually connected" once there's enough content to check.

## Fluid dynamics — parked, later system

A basic fluid type, parametrized by:
- **Viscosity** — influences movement/flow behavior.
- **Chemical interaction** — a defined, finite set of interaction rules keyed to the game's actual element/compound registry (not arbitrary special-casing). Examples floated: clean water + dyed water → partially-dyed water (usefulness still uncertain, pending further item design); water + lava → a temperature-based reaction producing stone.

Multiple liquid types get built on top of whatever base system results. **Explicitly deferred** — interesting functionality, but real edge-case complexity; not a near-term build target.

## Excavation/extraction system — open problem, real ideas wanted

**The problem, stated precisely:** the character can move with floor-index verticality (not true 3D — see the engine-architecture decision elsewhere: this is a 2D engine with a small-integer floor concept, not a real 3D Y-axis). Given that, if an extractor removes material from the ground over time, how does the Containment Collapse Model cleanly recompute "what's been removed" when a region collapses and later re-expands? A rejected idea: tracking depletion as a literal physical sub-volume of ground being consumed — judged too messy to implement cleanly. Floated but undecided: defined "workable zones" or quarries; a tile/sm-based pathfinding mechanic for equipment driven by some kind of control interface (a spiritual analog to redstone-driven automation) — but this is explicitly parked pending the electrical/logic-gate system above, since it depends on that existing first. Real, acknowledged connections to other systems: the Star Chart, the Containment Collapse Model directly, and sorting/storage/inventory systems.

**A concrete starting idea, offered directly (not a final answer):** rather than modeling depletion as a continuous volume, treat modified ground the same way persistent-but-procedural worlds conventionally do it — **store only the tiles that differ from their procedurally-generated default, as a sparse per-region override list, keyed by tile coordinate.** On region re-expansion, generate the default terrain as normal, then apply the override list on top. This avoids needing to track "how much has been removed" as its own continuous quantity — a tile is either untouched (falls back to generation) or explicitly overridden (stored). This is a well-proven pattern for exactly this class of problem, worth treating as a real starting point once excavation gets built for real, not a final design.

## The abstract Item data model — the real technical question

**Recommended shape: composition over deep inheritance** — a lean core `Item` type holding only what's universally true of every item, with optional **capability interfaces** attached per item for everything else. Avoids one bloated class trying to hold every possible field for every item type (a sword doesn't need `FuelValue`, a log doesn't need `WeaponDamage`), and keeps the system open to new item categories without touching the base type.

**Core `Item` fields (every item has these):**
- Identity — internal ID, display name, description.
- **Composition reference** — which element(s)/compound(s) from the Unified Compositional Model registry this item derives from, where applicable. This is what lets density, flammability, hardness, etc. come from the *same* registry driving planet chemistry, rather than being hand-typed per item.
- Stack behavior — stackable, max stack size.
- Inventory icon reference.

**Capability interfaces (attach only what applies):**
- `IPlaceable` — can exist as a physical object in the world. Points to that object's placed-in-world representation. **This is the same data/rendering-half split established in Modular Scene Assembly** — an item's inventory data is its data half, its placed-in-world appearance is its rendering half. Not a coincidence; same underlying pattern.
- `IHarvestSource` — for natural entities (a tree) that yield item(s) when harvested/destroyed. Deliberately separate from `IPlaceable` — see "Drops vs. placement" above. References what item(s), what quantity/probability.
- `ICraftable` — has a recipe: required inputs, quantities, required crafting station type.
- `IConnectable` — for walls/wiring/anything needing adjacency-aware sprites and behavior (see "Walls" above). **Real open extension, not yet built:** on a finite, wrapping planet, a connectable item physically near the edge needs to resolve its neighbors *through* the wrap (the "stitched" opposite side), not just by raw adjacent coordinates — see [Planetary Generation System (Public)](Planetary%20Generation%20System%20(Public).md) → "Finite-planet surface topology" for the full problem and the `Planet.WrapPosition`/`WrappedDistance` tools this will need to use.
- `IElectricalComponent` — participates in an electrical network, per the programmatic-not-procedural design call above.
- `IFuel` — burn value, for whatever the refinement/fuel system ends up needing.
- `IEquippable` — weapons/armor: stats, equip slot.
- `IConsumable` — food: nutrition/effects.

### Layered character rendering & equipment compositing (added 2026-08-25)
Real, durable decisions on how `IEquippable` visuals actually get rendered — not just data, an engine-level approach.

- **Per-direction sprites, not necessarily per-frame.** Equipment needs a sprite per facing direction (matching the character's 8-directional set) to visually align with the body, but rigid items (most armor, helmets) can reuse a single sprite across all animation frames of a given direction — only genuinely flowing items (capes, free hair) need per-frame variants. Keeps art cost proportional to how dynamic a piece actually looks, not a flat 8-directions-times-every-frame tax on everything.
- **Hair/headwear clipping — solved via a `HidesHair` flag on headwear items, not bounding-box discipline.** When a `HidesHair` item is equipped, the hair layer simply isn't drawn (or swaps to a reduced "tucked under" variant) — clipping becomes structurally impossible rather than something to avoid by careful sizing. A shared silhouette-guide reference is still worth keeping for proportional art consistency across headwear, but the flag is what actually prevents the bug.
- **Composition is baked on equip-change, never live-overlaid during play.** Whenever equipped items change (a comparatively rare event), walk through each needed animation frame once and composite that frame's active layers (body, hair-or-hidden, each equipped piece in slot order) into a single combined image, which is what actually gets displayed during gameplay. Real mechanism: composite source frame images together (CPU-side pixel buffer operations) into a combined result, converted to a real displayable texture. Zero per-frame overlay cost while actually playing — the cost is paid once, at the moment gear changes, not every rendered frame. Given sprite sizes here are tiny (16×16–24px), this baking pass is computationally trivial. Caching baked results by exact equipment-combination signature is a real future optimization (useful if multiple characters end up wearing the same combination), not needed for a first version.
- **The chosen reference character sprite becomes the canonical body template** once the 16×16-vs-16×24 visual-style decision locks in — all future armor/clothing art gets drawn against it directly, the same way 3D art keeps a T-pose reference.

**Proposed process for adding a new item** (a first pass, refine as real items get built):
1. Define core identity + composition reference (which registry element/compound it derives from, if any).
2. Decide which capability interfaces apply — this determines everything else needed.
3. For each chosen capability, there's a matching content checklist: `IPlaceable` needs a placed-in-world sprite/scene fragment; `IConnectable` needs the adjacency sprite variant set; `ICraftable` needs a defined recipe and station; etc. — each capability interface should carry its own "what you owe the game" checklist once built for real.
4. Register into the crafting tree — verify it connects to *something*, either as an input consumed by another recipe or an output producible from one, per the crafting-tree connectivity goal above.

## Status
Everything in this document is a first-pass capture, not a locked spec — expect heavy revision once real items start getting built and the abstract Item model gets tested against actual content. Priority ordering across these systems isn't decided; this document exists so nothing gets lost before that ordering conversation happens.
