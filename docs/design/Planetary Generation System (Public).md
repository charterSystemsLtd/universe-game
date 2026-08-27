---
type: design-document
created: 2026-08-20
status: living-document
sanitized: true
note: "Public/shareable version of this project's design documentation."
---

# Planetary Generation System (Public)

Detailed technical design for universe/planet generation — derived from the Master Design Document's "Unified Compositional Model" pillar. This document holds the actual formulas, registries, and rules; the Master Design Document stays at the conceptual level and links here for detail.

## The Stellar Measure (sm) — the game's proprietary distance unit
All in-game distances are expressed in **Stellar Measures (sm)** — an in-universe unit name. The underlying scale reference — roughly the length of a computer screen, with the player character occupying a small portion of it and surrounding objects roughly character-sized, matching a dense, readable tile-based visual style — sets the practical density of the world.

**Conversion basis:** real-world planetary radii (in km) get compressed by roughly two orders of magnitude (**1 sm ≈ 100 km**) — derived directly from the real-world largest-typical-solid-planet radius (~15,000 km), which becomes the in-game maximum planet size of **150 sm**.

**Derivation order, clarified 2026-08-25 — this is the actual load-bearing point, worth being precise about:** the "~100 km" figure is a **narrative/flavor label applied after the fact**, not the actual source of the sm's size. The sm's real size is derived **gameplay-first, from on-screen character scale** — matching the unit's own original naming logic (roughly one screen-width, with the player character occupying a small, legible portion of it). Concretely: character sprite size determines the game's internal resolution, which determines how many pixels/tiles make up one sm. Only *after* that chain is fixed does "~100 km" get attached as the in-fiction label for what that unit represents.

**Why this ordering matters:** it deliberately decouples two things that would otherwise be forced together — **how large one sm looks on screen** (a gameplay-feel decision) and **how many sm apart two planets actually are** (a pacing decision). Treating real-world astrophysical distances as authoritative would make interplanetary space enormous by default, which isn't automatically the intended feel — Xander's own framing: *"planets would be extremely far apart, and I don't necessarily want that to be the case (though I may later go back and change this)."* So: **sm's on-screen size is fixed now, from character scale. Interplanetary distances (how many sm apart bodies get placed) stay an independently tunable knob**, adjustable later purely for pacing/vastness feel without needing to touch the sm's underlying screen-relative definition at all. This resolved a real, live bug in early implementation (the first-pass starfield background was generating decorative "planets" at a density that flatly contradicted the design's own stated vastness — see the Individual & Operational Scale doc's Ship-in-space section for the fix) — the correction there was about object *density*, this section is about *why* density and unit-size are two separate decisions to begin with.

**The actual conversion, decided 2026-08-27 — this is the real completion of the derivation order above, not just a placeholder anymore:** tested directly on real hardware, fullscreen, comparing how the character felt at each resolution preset. **480×270 is the confirmed standard resolution** — character centered, roughly 15–16 tiles of context visible on each side. From that: 480px ÷ 16px-per-tile = 30 tiles exactly, rounded up to **32 tiles as the actual defined width of 1 sm** (a clean power of two, chosen deliberately for binary-friendly distance math elsewhere, not just because it was close). In code: `StellarMeasure.TilesPerStellarMeasure` (32) and `StellarMeasure.PixelsPerStellarMeasure` (512) — real, load-bearing constants now, not a future-work placeholder. Height-in-tiles wasn't pinned to a clean number the same way (270÷16 isn't a round value) — sm stays defined by width, same as its own "roughly one screen-width" naming logic already implied.

**Important reframing:** planets in this game are **flat 2D areas, not spheres** ("not even surface area... as planets are not going to be round, but flat, akin to a flat earth"). So "size" is a flat map dimension (width × height, generally treated as square for now), not a sphere's radius. The 100 km-per-sm conversion is applied directly to real radius figures as a practical numeric shortcut (not a rigorous sphere-to-flat-map geometric conversion) — consistent with the game's existing willingness to depart from strict realism for gameplay reasons (see Water/volatiles below).

## Size bounds (in sm)
| Body type | Size | Real-world reference |
|---|---|---|
| Minimum generatable planet | **5×5 sm** | ≈500 km — matches the prior minimum bound |
| Maximum solid/disembarkable planet | **150×150 sm** | ≈15,000 km |
| Maximum gas giant / star | **400×400 sm** | Stylized, not literal (real gas giants are much larger — deliberately compressed) |
| Coordinate (opportunistic spawn area — see Regions below) | **500×500 sm** | — |
| Region (full 9×9 grid of coordinates) | **4500×4500 sm** | 9 × 500 sm per side |
| Minimum buffer, coordinate border to body edge | **10 sm** | — |

## Regions of space — geometry resolved
- **A region is 4500×4500 sm**, made up of a real 9×9 physical grid — not an abstract slot count. **Each of the 81 coordinates is its own dedicated 500×500 sm territory.**
- **Each coordinate is an "opportunistic area"** — 75% chance to spawn a celestial body somewhere within its own 500×500 sm territory (85% planet / 15% star if it spawns). Because every coordinate has exclusive, non-overlapping territory, there's no packing/rejection-sampling problem: even the largest body (400×400 sm star/gas giant) fits inside its own 500×500 cell with real margin (50 sm per side, well past the 10 sm minimum buffer) — no risk of colliding with a neighboring coordinate's body.
- **Every region guarantees at least 1 star** — one coordinate is reserved for a guaranteed star placement first; the remaining 80 coordinates roll normally (which can add more stars via the normal 15% chance). A planet's *nearest* star doesn't have to be in its own region, but every region having its own star sets a floor on star density.
- **The hard load/unload boundary is the outer region edge (the full 4500×4500 sm boundary)**, not each individual coordinate — a region is the atomic unit for the Containment Collapse Model and the observation/chain mechanic below. The 10 sm buffer at each coordinate's own boundary is purely a spacing/anti-overlap rule between neighboring bodies, not a separate loading boundary.
- **Deliberately not called a "chunk"** — kept as its own distinct term to avoid overloaded terminology from elsewhere in the genre.

## The universal origin — Yin's spawn point
There is a single, fixed **origin coordinate** somewhere in the universe (not per-region — one specific location, universe-wide) that is **guaranteed empty of any procedurally-generated body.** This is where **Yin** spawns — hand-placed, not procedurally generated. The origin coordinate's **8 surrounding neighbors (a 3×3 block centered on it) are also guaranteed void**, giving Yin a reserved, empty buffer.

**Deliberately cheap to check:** this only ever needs a single comparison — "does this region contain the origin coordinate?" — performed *once per region*, not once per coordinate. The vast majority of regions in the universe will never contain the origin, fail that one check immediately, and proceed straight into normal generation with zero added per-coordinate overhead. Only the one specific region that actually contains the origin does any extra work (reserving its inner 3×3 block as void before generating the rest of itself normally).

## Ring-distance temperature bands — replaces the raw inverse-square formula for generation purposes
A star's `StarLuminosity` shifts which ring maps to which band, rather than needing the continuous formula at all:

| Star luminosity | Ring 1 (adjacent) | Ring 2 | Ring 3 | Ring 4+ |
|---|---|---|---|---|
| Low | Temperate | Cold | Frozen | Frozen |
| Average | Hot | Temperate | Cold | Frozen |
| High | Scorching | Hot | Temperate | Cold |

- "Ring distance" = grid distance (in coordinates) from a planet's coordinate to its nearest star's coordinate — cheap integer math, no real sm-distance calculation needed for band purposes.
- Luminosity tiers (placeholder thresholds, tunable like everything else in this doc): Low <0.8, Average 0.8–1.3, High >1.3.
- **The actual displayed "temperature" value for a planet is a random roll within its assigned band's stated range** (e.g. a Ring-2/Average-luminosity planet lands in Temperate, then rolls a specific value between 5°C and 35°C) — satisfies wanting a real per-planet number without needing the inverse-square formula at generation time.
- **Note:** a star's real-physics *size* doesn't independently drive temperature — luminosity already captures that. Kept `StarLuminosity` as the only driving stellar stat for this system rather than adding a second correlated one.
- **The original inverse-square formula isn't deleted** — it's still a reasonable fallback/cross-check, but ring-distance + luminosity tier is now the primary generation-time mechanism, chosen for predictability/tunability (easy to reason about and hand-balance) over the formula's harder-to-intuit distribution, not for raw computational cost (the formula itself was already cheap).

## Single-region generation algorithm (in order — neighboring-region logic deferred)
```
GenerateRegion(region):
    1. isOriginRegion = (region contains the universal origin coordinate)   // one check, once per region
       if isOriginRegion:
           reserve origin coordinate + its 8 neighbors as permanently void
           mark origin coordinate as Yin's spawn point (hand-placed)

    2. eligibleCoords = all 81 coordinates in region, minus any reserved-void coordinates

    3. Guaranteed star:
           pick one random coordinate from eligibleCoords
           place a Star there; roll its StarLuminosity
           remove it from eligibleCoords

    4. Remaining coordinates — occupancy + type roll (no positions finalized yet):
           for each coord in eligibleCoords:
               roll 75% occupancy
               if occupied: roll 85%/15% -> Planet or Star
               if Star: place Star here now, roll its StarLuminosity
               if Planet: mark as "pending" — don't finalize yet (needs to know every star
                          in the region first, so "nearest star" is computed correctly)

    5. Finalize each pending planet, now that every star in the region is known:
           a. find nearest Star by ring distance
           b. look up Band from (ring distance, that star's luminosity tier)
           c. roll actual Temperature = random value within that Band's range
           d. classify Zone from Band (Rocky / Transitional / Volatile)
           e. roll Size within the Zone's size range
           f. roll Composition weighted by Zone, plus an independent Volatile-Delivery bonus roll
           g. derive Gravity, Atmosphere, Water/volatiles from the resulting Size + Composition
              (existing formulas above)
           h. assign the planet's permanent 7-character identity code
```
**Explicitly out of scope for this pass:** how neighboring regions relate to each other (e.g. whether a planet's "nearest star" search should ever cross a region boundary, how the origin-region's void interacts with its neighbors) — planned as the next layer once this single-region algorithm is solid.

## Observation and live updates
The player does **not** need to be physically near instruments. The actual mechanic is about *capacity to observe*, and it's a connectivity/chain system:
1. **The player automatically observes their current region.**
2. **A region becomes observable if it neighbors an already-observable region *and* has an "observer" placed in it** (an in-game placeable item — exact form undecided: a radio system, an "internet link," something else — not designed yet).
3. This creates a **chain of observable regions** spreading outward from the player through adjacent observer-equipped regions.
4. **A region with an observer that is *not* chain-connected to the player is disconnected** — not actually observable. Its state still changes (resource extraction, construction, etc.), calculated as elapsed time/ticks, but that calculation only *resolves* when the player physically enters it, or when a neighboring region becomes observable and extends the chain to reach it.
5. **This check needs to be cheap** — effectively a graph-connectivity walk (flood-fill/BFS from the player's current region through observer-linked neighbors), recomputed only when an observer is placed/removed or the player changes region, not every tick.
6. **Deferred, future feature:** "export stations" will need their own call-list system to connect otherwise-disconnected regions (e.g. automated trade routes spanning unobserved space) — not designed now, flagged for later.
7. **Explicit design non-goal:** the player should never have to think about "region loading" the way players in some sandbox games have to think about world-streaming boundaries (e.g. mechanisms breaking at loading borders). This is meant to be invisible, handled entirely by the engineering above.

**Star Chart live feedback:** the *only* live per-planet feedback shown on the Star Chart is a small icon appearing above a planet's node once per second, reflecting the volume of goods extracted/manufactured there — and only for planets in currently-observable regions (per the chain logic above). Everything else about a collapsed region's state stays invisible until the player actually looks.

## Rocket takeoff/landing — scales with body size/gravity
- **Minimum completed-takeoff distance: 1 sm** from the surface (low-gravity bodies).
- **Maximum completed-takeoff distance: 5 sm** (high-gravity bodies) — the threshold scales with the body's gravity between these bounds.

## The Element/Compound Registry
Per the Unified Compositional Model: one registry, used for both planetary chemistry generation *and* physical item/material properties. Real-element-inspired, simplified for gameplay rather than modeling full real chemistry. **Water is not a registry row** — water is a compound, not an element, and the composition descriptor should only track actual elements. Water's presence is handled entirely as a derived property (see Water/volatiles below) — wherever an element roll produces both H and O, water is assumed to form in some spawnable form.

| Element | Directly holdable? | Example held form | Contributes to (generated items/features) | Real density (g/cm³, standard conditions) |
|---|---|---|---|---|
| **Hydrogen (H)** | No (gas) | — | Water (w/ O), ice, fuel/propellant, most organic/hydrocarbon compounds | ~0.00009 |
| **Helium (He)** | No (gas) | — | Marks gas-giant-type bodies; He-3 variant as exotic fuel | ~0.00018 |
| **Carbon (C)** | Yes | Pure carbon lump (ashy black substance) | Wood, plant matter, coal, hydrocarbon fuels, Cult exotic-material items | ~2.27 (graphite form) |
| **Nitrogen (N)** | No (gas) | — | Breathable atmosphere component, fertile soil/organic growth, industrial/explosive compounds | ~0.00125 |
| **Oxygen (O)** | No (reactive gas) | — | Water (w/ H), breathable atmosphere, oxidized minerals/rust, combustion items | ~0.00143 |
| **Phosphorus (P)** | Yes | Waxy solid chunk | Organic/biological compounds, industrial items | ~1.82 |
| **Sulfur (S)** | Yes | Yellow crystal | Toxic/corrosive atmosphere compounds, volcanic terrain, Industry chemical items | ~2.07 |
| **Silicon (Si)** | Yes | Raw silicate/quartz-like mineral | Rock/terrain generation, glass items, Cult crystal items, electronics | ~2.33 |
| **Aluminum (Al)** | Yes | Raw ore/ingot | Lightweight structural items, common crust material | ~2.70 |
| **Magnesium (Mg)** | Yes | Raw ore/ingot | Structural alloys; real-world tie-in: chlorophyll uses Mg | ~1.74 |
| **Calcium (Ca)** | Yes | Raw mineral | Bone/shell-analogue organic items, rock-terrain features | ~1.55 |
| **Iron (Fe)** | Yes | Raw ore/ingot | Core rock-world material *and* primary Industry metal; tools, structural items, machinery | ~7.87 |
| **Nickel (Ni)** | Yes | Raw ore/ingot | Co-occurs with Iron (real planetary-core chemistry); alloys, Industry items | ~8.91 |
| **Copper (Cu)** | Yes | Raw ore/ingot | Conductive/electronic items, early-tier Humanoid tools | ~8.96 |
| **Titanium (Ti)** | Yes | Raw ore/ingot | High-tier structural/armour items | ~4.51 |
| **Gold (Au) / Silver (Ag)** | Yes | Raw ore/ingot | Currency/trade, decorative/Cult ritual items, high-conductivity Industry items | Au ~19.3, Ag ~10.49 |
| **Uranium (U)** | Yes (hazard-flagged) | Raw ore, handled carefully | Fuel/reactor items, weapon-adjacent items, radioactive-hazard terrain | ~19.1 |
| **Chlorine (Cl)** | No (toxic gas) | — | Corrosive atmosphere compounds, industrial chemical items | ~0.00321 |
| **Exotic/Rare (placeholder category)** | Yes (as compounds) | Unspecified | Cult/exotic-world flavor materials — deliberately unspecified, revisit once needed rather than inventing pseudo-science now | — |

**On density and unit conversion — a genuine simplification:** these are standard real-world reference values, and **they don't need any sm-conversion.** Density is mass/volume — an intensive, scale-independent property. Only *length/distance* gets compressed for gameplay (the 100 km-per-sm conversion above); a planet's bulk density stays physically accurate regardless of how small we draw it on screen. Handy side effect: since real gas-phase elements are naturally very low density and metals are naturally high density, a composition-weighted average density will *automatically* produce gas-giant-like bodies as fluffy/low-density and rock/metal worlds as dense — without any special-casing needed.

## Derived properties (no longer independent random rolls)

### Temperature — formula
Distance-to-nearest-star-driven, inverse-square (physically grounded, game-simplified):

```
Temperature (°C) = 15 × (StarLuminosity / Distance²)
```

- `Distance` in sm.
- `StarLuminosity` — a per-star stat rolled at star generation. **Baseline range (0.5–2.0) is a placeholder, not tuned.** The actual goal is a roughly *even* distribution across all 5 temperature bands (Frozen/Cold/Temperate/Hot/Scorching) in real generated output, but tuning StarLuminosity's real bounds to hit that needs an actual running generator to test against, not guesswork now. **Deferred to an empirical tuning pass** once we can generate sample regions and look at the real resulting distribution.
- Calibrated so Distance=1.0 sm-equivalent baseline, Luminosity=1.0 → 15°C (Earth's rough average).
- Clamp to a reasonable game range (e.g. −250°C to +1000°C) to avoid absurd values as Distance→0.

### Temperature bands (gate societal construction style)
| Band | Range | Organic-society construction |
|---|---|---|
| Frozen | below −50°C | Fully indoor/domed; heavy protective gear mandatory outdoors |
| Cold | −50°C to 5°C | Mostly indoor/insulated; protective gear for extended outdoor exposure |
| **Temperate** | 5°C to 35°C | Full outdoor construction viable — the Earth-like norm |
| Hot | 35°C to 80°C | Mostly indoor/shaded, cooling-focused; protective gear for extended exposure |
| Scorching | above 80°C | Fully indoor/domed; heavy protective gear mandatory outdoors |

## Composition generation logic — causal, not independent rolls
Grounded in how our own solar system actually formed, so planet generation follows a real causal *order* rather than rolling size/composition/atmosphere as unrelated independent stats. Core real-astronomy concept: the **frost line** — the distance from a star beyond which it's cold enough for volatiles (water, ammonia, methane, CO₂) to condense into solid ice rather than remaining gas. Inside it, only high-condensation-temperature material (rock/metal) can accumulate into solids; beyond it, rock/metal *and* ice are both available, giving far more raw material to build from — which is the real reason our own outer planets are enormous gas/ice giants and our inner planets are small and rocky. Mars vs. Earth is the same zone but a smaller available-material budget at Mars's specific position, and a real, still-cascading consequence chain from that (smaller planet → faster interior cooling → earlier magnetic-field shutdown → billions of years of solar wind stripping its atmosphere).

**Generation order (replaces independent rolling):**
1. **Place the star(s) first** — gives `StarLuminosity` and each coordinate's `Distance`.
2. **Compute Temperature** (existing formula, distance-driven — unchanged, this doesn't need to know the planet's own properties yet, matching how the real disk's temperature gradient existed before any planets had finished forming).
3. **Classify the coordinate into a zone, reusing the existing temperature bands rather than adding a new stat:**
   - **Temperate / Hot / Scorching → Rocky Zone** (equivalent to "inside the frost line")
   - **Cold → Transitional Zone**
   - **Frozen → Volatile Zone** (equivalent to "beyond the frost line" — gas/ice giants become eligible here)
4. **Zone gates the size-roll range** — Rocky Zone rolls from a smaller effective range (terrestrial-planet-like); Volatile Zone rolls from a larger range and becomes eligible for gas/ice-giant-scale results (up to the 400×400 sm ceiling). This is a new dependency — previously size and zone/temperature were unrelated rolls.
5. **Zone gates composition weighting** — Rocky Zone weights heavily toward Fe/Ni/Si/Al/Mg/Ca/Ti, with H/He/ice-forming elements staying rare/trace; Volatile Zone weights include everything, with H/He/ices weighted up.
6. **A separate, zone-independent "volatile delivery" bonus roll** — represents comet/asteroid impact delivery, mirroring Earth's own genuinely-still-debated water history (leading real hypotheses: water present in Earth's original rocky material, and/or delivered later by icy impactors — likely both, exact proportions unresolved even in real astronomy). **This is the actual mechanism behind the earlier Goldilocks-abundance-boost design goal** — rather than an ad-hoc multiplier bolted onto Temperate-band planets, it's now a real, explainable roll that can occasionally hand a Rocky Zone planet a genuine water/ice bonus it wouldn't otherwise have, independent of its zone.
7. **Gravity, Atmosphere, and Water/volatiles are computed after this**, from the resulting size + composition — unchanged from the formulas below, just now correctly downstream of a real causal chain instead of being independent rolls sitting alongside it.

**Worth noting explicitly:** real atmospheric stripping (Mars's story) happens over billions of years — we're not simulating deep time, we're using the existing instantaneous atmosphere-derivation rule (gravity + temperature + composition → atmosphere) as a stand-in for wherever that long process would have already landed by the time a player finds the planet. The shortcut we'd already built turns out to be the right one, not something this pass needed to change.

### Gravity — formula
Function of size (in sm) and composition-derived density:

```
Gravity (g) = K × Density × Size
```

- `Density` = composition-weighted average of the element density table above (rock/metal-heavy compositions → high; gas/ice-heavy → low).
- `Size` = the planet's flat-map dimension in sm (replaces the earlier "Radius" framing).
- `K` calibrated so Earth-equivalent inputs (Density ≈ 5.51 g/cm³, Size ≈ 64 sm [Earth's real ~6,371 km radius ÷ 100]) yield ~1g — i.e. `K ≈ 1 / (5.51 × 64) ≈ 0.00284`, though **this exact constant should be treated as a starting point for the same empirical tuning pass as StarLuminosity**, not a final locked value.

### Atmosphere — derived, not an independent roll
Unchanged in logic, restated with sm-based gravity thresholds:
1. **Retention** from gravity: Low (<0.3g) → minimal; Medium (0.3–1.5g) → moderate; High (>1.5g) → strong.
2. **Volatile availability** from composition: how much H/O/N/C/S/Cl the rolled composition contains.
3. **Density** of atmosphere = f(Retention, Volatile availability, Temperature) — high temperature reduces retention even at decent gravity.
4. **Type** of atmosphere = determined by which volatiles dominate: H/O/N-dominant → breathable-potential; S/Cl-dominant → toxic/corrosive; low volatile abundance regardless of gravity → thin/none.

### Water/volatiles
Requires both H and O present in the rolled composition to form water; abundance scales with how much H+O the roll produced. **Temperate-band (Goldilocks) planets get a deliberate abundance multiplier on H/O co-occurrence odds** — an explicit, acknowledged departure from real-world statistical rarity, prioritizing gameplay variety over realism.

## Non-solid / no-disembark bodies
Gas giants and similar bodies with no solid surface are legitimate generatable celestial bodies (max 400×400 sm) — no disembarking, but they can host **orbital-only presence**: stations, mining rigs, or society activity centered on extracting a resource from the body without ever landing on "it."

## Finite-planet surface topology — real implementation, real open problems (added 2026-08-27)

First real Individual Scale prototype work surfaced a cluster of genuinely important, connected problems around what it means for a planet to have a **finite, wrapping surface** rather than an infinite or unbounded one. Some of this is built; most of it is captured here specifically because it doesn't have a clean single code location to attach a `// TODO` comment to yet — it's cross-cutting architecture, not a one-line fix.

**What's actually built:** `Planet` (data/rules — size, wrap-around math, wrapped-distance) and `Ground` (the visual drawing, sized from `Planet`) as a deliberate data-half/rendering-half split, same pattern as item design elsewhere in this doc set. A 30×30-tile test planet wraps at the edges (Asteroids-style — walking off one edge brings you out the opposite edge), grounded in this doc's own "flat, not spherical" planet framing above. `Planet.WrappedDistance()` exists now — any future proximity check (event triggers, NPC awareness, the item/machine case below) needs to measure distance *through* the wrap, not around raw coordinates, or two points genuinely adjacent across the seam would read as almost the planet's full width apart.

**Real, open problem #1 — the current wrap is a hard "pop," not smooth.** Crossing the boundary teleports the character (and the camera, which is a child of it) instantly to the opposite side — a visible cut, not a continuous scroll. Genuinely seamless wrapping (walking off the right edge while already seeing yourself approaching from the left, no visible cut) needs a real additional technique: duplicating a **thin border strip** near each edge — mirroring a copy of the start content onto the far edge of the end content, purely visually. This does not violate "one instance per physical location" as long as it stays scoped to a narrow seam — gameplay logic (collision, position, item state) still only exists in exactly one real place; only a thin visual strip gets mirrored for continuity. **Not built. Real feature, deferred.**

**Real, open problem #2 — item/machine functionality that physically straddles the boundary.** A machine with a physical input/output near or over the planet's edge needs to route its functional connections to the "stitched" opposite side, so society/economy/crafting logic isn't broken by what's fundamentally a rendering/coordinate technicality. Concretely: this is an extension of the `IConnectable` capability already defined in [Item & Crafting Systems (Public)](Item%20&%20Crafting%20Systems%20(Public).md) — a connectable item needs to be able to identify "my neighbor is across the seam" and resolve the connection through `Planet.WrapPosition`/`WrappedDistance` rather than failing to find a neighbor just because the raw coordinates are far apart. **Not built. No concrete code location yet — logged here specifically because of that.**

**Real, open problem #3 — planets smaller than the visible render area.** If the camera can see more world at once than the planet actually contains, the same physical location (and, worse, the character's own sprite) could appear more than once simultaneously on screen — a genuine physics-breaking duplicate, not just a visual quirk (the "Portal problem," per the conversation that surfaced this). This only becomes a real risk once problem #1's border-duplication technique exists — the current hard-pop wrap can't actually show two copies at once, since it only ever draws the ground once and just cuts the camera's position within that single drawing.

**Recommended direction (a suggestion, not a decision):** rather than falling back to a hard wall (which the border-duplication work would otherwise make necessary on small worlds, and which undercuts the "one continuous plane" feel this whole system is for), **cap the effective visible render area to never exceed the planet's actual size** — reusing the *already-established* `InGameResolution` system (see the Individual & Operational Scale doc's Ship-in-space section) as the lever, rather than camera zoom (which stays parked, per Xander's own call). Concretely: if a planet is smaller than the currently-selected resolution preset would show, reduce the effective rendered area for *that specific planet* to fit within one true instance of it. This is consistent with `InGameResolution` already being the system's designated "how much world is visible" control, and doubles as a genuine compute optimization (a small planet legitimately needs fewer pixels rendered) rather than being a special-case hack bolted on separately. **Not built. Real design question, flagged for whenever problem #1 gets built** — until then, there's no duplication risk to solve.

### Extension to problem #1 — render only what's actually visible, not the whole planet (added 2026-08-27)

A real, substantive extension to the border-duplication idea above, from a follow-up conversation: **don't render the whole planet's surface at all — render only the region actually visible on screen, based on character position + `InGameResolution`, and separately render a matching-size region from the wrapped opposite side when near an edge.** This isn't just a cheaper way to do the same thing — it's a real compute-saving reframe. The full planet will eventually be populated with real items/creatures/structures (per Item & Crafting Systems); rendering the *entire* thing every frame regardless of what's actually on screen wastes real work on content the player can't see. The underlying planet-wide item/creature *logic* still runs in full on the backend (position, state, AI, whatever eventually exists) — only the *visual* draw gets scoped to what's actually visible. **Not built. Real feature, same status as problem #1 above — this is the fuller version of it, not a separate item.**

**This gives a precise, concrete threshold for when wrap-safe rendering is even possible, worth stating exactly:** as long as the planet's actual size is **greater than or equal to** the visible render area, wrapping can render safely with zero duplicated pixels — the visible region plus its wrapped counterpart can coexist on screen without overlapping. **The instant the planet becomes even one pixel smaller than the visible area, the system must fall back to the edge-collision (hard wall) approach instead** — this is the real, exact condition under which problem #3's "Portal problem" becomes possible, and confirms planetary surface *handling itself* is legitimately tied to `InGameResolution`, not just planetary *rendering*. Accepted as fine for now (Xander's own call) — not a design gap, a real consequence of the resolution-driven rendering approach.

**Sanity check, confirmed:** rendering the same underlying content at two screen positions near a seam does **not** create two real game objects — `Planet` still holds exactly one instance of any item/creature/structure's actual state; drawing it twice (once for the "near edge" screen region, once for its wrapped counterpart) is purely a rendering-time query against that same single source of truth, the same way a split-screen security feed shows one hallway from two camera angles, not two hallways. The real engineering discipline this requires: any actual *gameplay* logic (collision, interaction, damage, whatever) must always resolve through `Planet.WrapPosition` back to the one real canonical position — never accidentally operate on "the visual copy" as if it were a second, independent object. Worth stating as a real implementation rule once this gets built, not just an assumption to hope holds.

### Structural fix for procedural generation — Planet/Ground pairing (added 2026-08-27)

The original implementation had `Planet` and `Ground` as two independently-created nodes, manually cross-wired via drag-and-drop in one hand-built test scene — fine for one hand-placed planet, but it can't scale to procedural generation, where nothing would force a newly generated planet to remember to also create and correctly wire a matching ground.

**Real fix, implemented:** `Ground` is now a **child of `Planet` inside `Planet.tscn` itself**, not a sibling wired externally — the pairing is structural, not configurable. `Ground.SourcePlanet` is wired to its own parent (`NodePath("..")`) baked directly into the scene file. This means instantiating a planet procedurally is now just standard Godot scene instancing (`PackedScene.Instantiate()`, the same mechanism already used everywhere else in this codebase) — the correctly-wired ground comes along automatically, every time, with no per-instance configuration needed. `Character.CurrentPlanet` still needs a runtime assignment (which planet a character is standing on genuinely changes, unlike Ground's fixed relationship to its own Planet) — but that's a simple one-line code assignment once a planet's actually generated, not editor wiring.

**Real gap surfaced, not yet solved:** `Planet` currently has no reference to the planet's actual identity/position/icon within the Universal Scale (the Star Chart, per the Master Design Document) — worth solving before real generation connects Universal Scale to individual planets, but may be able to live as data directly on `Planet` itself rather than needing a separate system. **Logged, not designed yet.**

### Speculative future ideas, explicitly parked (added 2026-08-27)

Two genuinely interesting ideas surfaced directly from thinking through the wrap/duplication bugs above — both explicitly **far future / endgame scope**, not designed, just worth not losing:

- **A deliberate "broken physics" liminal environment** — a rare, late-game location (a black hole or similarly exotic stellar environment was floated) that intentionally produces a repeating, tessellating view where the player can see multiple copies of their own character — the exact "Portal problem" above, but as an intentional feature rather than a bug, in a location explicitly meant to feel wrong/otherworldly. A late-game cultist NPC conjuring such a space was floated as a possible narrative hook, fitting the Cult's existing Yin-worshipper/exotic-materials profile (Societal Guide).
- **Planets whose surface re-generates on every visit** — the planet renders consistently from Universal/Operational Scale (same icon, same orbital position), but landing produces a newly-generated surface each time, with near-zero travel time between visits producing entirely different environments. Would require a real mechanism for swapping which `Planet`+`Ground` pairing a location's Universal Scale identity currently points to. Endgame-scale extension, not core design.
- **A craftable "surface map" item**, parallel to the Star Chart but for an individual planet's surface — floated with two possible display modes: true coordinate mapping (the whole planet as a flat square, no boundary handling shown) versus wrap-aware mapping (showing the boundary-handling/seam behavior). Parked alongside the above.

## Subsurface environments — deferred, logged as a TODO
Planets generate as **single-surface-level only for now.** Caves/craters/subsurface layers are a real future feature, not designed yet.

**Scope expansion — sm needs a real rethink across several distinct uses before any of this is tuned for real:**
- **Launch height / atmosphere-boundary trigger** — a hard Y-distance-from-surface boundary (in sm) where a planet's atmosphere stops affecting a ship, which is also the trigger point for switching that ship's rendering from Operational Scale up to Universal Scale (and, symmetrically, into Drift at Individual Scale for an unprotected player). This refines the earlier "1–5 sm takeoff completion, scaling with gravity" rule into a proper scale-transition boundary, not just a gameplay-feel number.
- **Maximum build height for structures** — likely tied directly to the same launch-height boundary (a structure probably shouldn't be legally/physically buildable past the point where atmosphere stops mattering).
- **Atmosphere calculations relative to planetary scale** — the existing atmosphere-derivation rules (gravity/composition/temperature → density/type) need to be reconciled against real sm distances now that planet size is a concrete flat-map dimension, not an abstract stat.
- **Cave generation height, in sm** — tied to the floor/camera occlusion system in [Individual & Operational Scale - Gameplay Systems (Public)](Individual%20&%20Operational%20Scale%20-%20Gameplay%20Systems%20(Public).md). **Proposal:** carry the same standardized per-floor sm height used for buildings down into cave generation — giving "standard" building-story-sized underground levels — while *also* allowing larger, irregular, naturally-occurring open caverns, with blending elements between the two types (regular carved-feeling spaces vs. wild natural ones). Not designed yet, but the standardized-floor-height concept from the building system is confirmed as the right base unit to reuse rather than inventing a separate cave-specific unit.
- **None of these get real numbers yet** — per the movement/physics build-sequence, actual tuning happens empirically once there's a working reference planet, not from first principles now.

## Society habitat requirements (first pass)

| Society | Composition preference | Temperature preference | Atmosphere preference | Gravity preference | Placement logic |
|---|---|---|---|---|---|
| **Humanoids** | Organic-forming (C/H/O/N present) | See detailed survivability rules below | Breathable strongly preferred; toxic survivable only with life support | ~1g preferred, tolerate 0.5–1.5g | Habitability-driven, with rare artificial-habitat outliers |
| **The Industry** | Metal/silicate-rich, especially high-value ores | Indifferent — mechanical, unaffected by extremes | Indifferent — no biological need | Indifferent | **Resource-value-driven, not habitability-driven** — see motivation note below |
| **The Cult** | Exotic/rare minerals a draw; otherwise flexible | Favors extreme bands (Frozen/Scorching) — isolation value, congruent with their Yin alignment | Flexible, tech/magic-assisted survival | Flexible | Isolation- and rarity-seeking over comfort |
| **The Bugs** | Organic-forming preferred, tolerant of toxic-organic | Warm-to-temperate preferred, tolerant of Hot | Toxic-tolerant, doesn't need breathable | Flexible, low-moderate preferred | Scales with organic abundance |
| **The Mushrooms** | Minimal requirements — near-universal | Wide tolerance, avoids only Scorching extremes | Wide tolerance | Wide tolerance | Baseline lifeform — present in small numbers almost everywhere |
| **The Network** | **Deferred** — this needs to be designed separately once the Network's construction and visual identity actually exist; its conditions are different enough from the other five that folding it into this table now would be premature. Not locked — see note below. | | | | |

### Humanoid survivability rules
Humanoids can technically survive anywhere, but required technological complexity — and therefore settlement scale — scales directly with environmental hostility, which in turn shapes spawn probability:
- **Frozen / Scorching bands:** require *high* technological complexity and correspondingly *large-scale* settlements to survive at all. This naturally implies a *low* spawn probability — and gives a concrete mechanism for the earlier "large abandoned settlement needs a destruction explanation" rule: a large dead settlement in a Frozen/Scorching band is exactly what a failed high-tech colonization attempt would look like.
- **Cold / Hot bands:** can be either small-and-surviving or large-and-thriving, but **can never be Individual-Scale presence** — no lone wandering Humanoid NPCs or unprotected single buildings; minimum viable presence is an Operational-Scale Site with real life-support infrastructure.
- **Temperate band (or an artificially-simulated-temperate environment, e.g. inside a dome):** the *only* condition where Individual-Scale Humanoid presence — lone NPCs, tiny unprotected settlements — is possible at all.

### The Industry's motivation — proposed, not locked
Worth preserving verbatim: The Industry's cold, mechanical indifference is not meant to read as a generalization about real-world AI — it's a deliberate niche within the game's own fiction (and sets up rivalry with the Network, a separate hive-mind society). **Proposed in-universe justification (open for adjustment):** The Industry isn't indifferent by nature — it's still executing an ancient founding directive (a "Prime Quota" or similar) issued by whoever originally built it, with no revision mechanism ever added. Millennia after whoever gave the original order stopped issuing new ones, The Industry is still blindly optimizing for it — indifference as *stuck automation following outdated orders*, not as an inherent trait of machine minds. This also sets up a clean contrast with the Network: Industry = rigid, ancient-directive-bound automatons with no real unifying awareness; Network = an actively adaptive, unified hive-mind. Two different models of "machine society," not one generic trope.

## Distribution targets — tuning goals, not hardcoded constants
References for the best player experience, to be **validated against what the actual generation rules produce**, not baked in as literal fixed probabilities:
- **~50% of all generated planets: completely uninhabited.**
- **Among inhabited planets: single-society-per-planet is the default norm.**
- **~10% of inhabited planets: mixed-society** (cohabiting, warring, symbiotic, or simply coexisting separately).
- **Abandoned settlements should generally be small.** A large settlement found completely empty needs an explicit destruction explanation.
- **Rare hostile-environment outliers** — frequency not yet set, needs playtesting.

## Worked examples — societies in hostile/foreign environments

### Example 1: Humanoids in an artificial habitat on an airless silicate world
1. Define a **constructed habitat** as its own object type, distinct from a natural settlement — an explicit subsystem dependency graph (life support, power, structural integrity).
2. Each subsystem has an input/output function (life support: consumes power + water, outputs breathable air within a bounded pocket/dome).
3. The habitat's existence state (alive/failing/dead) is a direct function of whether critical subsystems are currently satisfied — plugs directly into the Containment Collapse Model's aggregate input/output function.
4. Player interaction: damaging a critical subsystem changes that term's value; cascading failure is a deterministic, time-based function once the critical-failure flag is set — not a live per-NPC simulation.
5. Failure *appearance* comes from a small lookup table keyed by which subsystem broke (suffocation-flavored for life support, freeze-flavored for power) — cheap, not simulated per-NPC.

### Example 2: A society alone on a wild, Earth-like world — deferred
Originally worked through using the Network as the example, but its placement/construction logic is explicitly deferred — its conditions differ enough from the other five societies that designing it now, before its visual identity even exists, would be premature. Not deleted, just parked.

**What's worth keeping regardless of which society the example used** — a generalizable pattern that surfaced while working through it: every planet should probably track at least two meta-stats — **extraction/depletion level** and **ecological/informational health** — that *any* society's presence rules can react to threshold-crossings on, not one-off logic built per-society. Worth carrying forward as a shared mechanism once society placement logic gets built for real.

## Open / not yet solved
- Real-world distance unit for a region/coordinate is now resolved (sm, 1 sm ≈ 100 km) — but the exact `Distance` baseline used in the temperature formula still needs to be reconciled with actual region-scale distances once regions are being generated for real.
- StarLuminosity range and the gravity formula's `K` constant — both explicitly deferred to an empirical tuning pass once sample regions can actually be generated and inspected.
- The actual spawn-probability formula tying the habitat requirements table to a real number per planet, per society.
- **Cadence for recomputing slower universe-shaping changes — partial answer given, not fully formalized.** Recomputation should generally align with **Operational Scale occurrences** — triggered by real events happening at that scale (a ship departing, a rocket launching, resources crossing a threshold) rather than a fixed real-time or tick interval — and the actual recompute will likely be a **"down-and-then-back-up" cascading pass**: a change propagates *down* through the containment hierarchy to wherever it actually originates, then the resulting effects propagate back *up* through the aggregate functions at each containing level (item → machine → vehicle → site/planet → region → universe). This matches the Containment Collapse Model's existing recursive structure directly — it's the same hierarchy, just now with a stated direction for how a change ripples through it. Still needs an actual implementation, but the shape is real now, not guesswork.
- Cave/subsurface generation — deferred TODO.
- **Item-to-element pointer mappings — the relationship direction clarified, actual table still deferred.** Not "element → single item," but **per-element, a list of items (craftable or naturally-occurring) that are plausible wherever that element is present.** This list is what determines whether an item can be legitimately placed on the ground or in *wild/natural* storage on a given planet — i.e., a real plausibility filter for generated loot/scenery, not just flavor.
  - **Real exception, and an important one: societal (settlement) storage does not follow this rule directly.** Interplanetary societies plausibly stock items their own operations genuinely need, regardless of whether that item's source materials occur locally — logistics/import, not local sourcing. Example: **The Industry** wouldn't stock food (they don't need it, being mechanical) even on an organic-rich world, but *would* plausibly stock battery cells even on a planet whose composition can't naturally produce battery materials, because Industry settlements structurally require batteries to function. So the real rule is two-tiered: **wild/natural generation is composition-constrained** (element-plausible items only); **societal generation is need-constrained** (whatever that society's own operations require, regardless of local composition) — two different filters, not one.
  - Table itself still deferred until Individual/Operational Scale design work begins in earnest.
- ~~Real-world grounding pass — how our own solar system actually formed~~ — **Done.** See "Composition generation logic" above — frost-line concept, zone classification reusing the existing temperature bands, zone-gated size/composition rolls, and a volatile-delivery bonus roll that gives the earlier Goldilocks-abundance goal a real causal mechanism instead of an ad-hoc multiplier.
- **New from this pass:** the exact size-roll ranges per zone (Rocky vs. Volatile) and the exact composition-weighting values per zone aren't numerically specified yet — the *logic/order* is real now, the actual weight numbers are the next layer down, likely another candidate for the empirical tuning pass alongside StarLuminosity and gravity's `K`.
