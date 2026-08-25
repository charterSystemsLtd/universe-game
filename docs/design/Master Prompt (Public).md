---
type: design-document
created: 2026-08-20
status: living-document
purpose: Condensed, refinable brief for the game — the "hand someone this and they'd get it" version.
sanitized: true
note: "Public/shareable version of this project's design documentation."
---

# Master Prompt (Public)

Distilled from [[03 Active Projects/Universe Game/Design/Public/Master Design Document (Public).md]]. This is the version to refine over time and reuse as a briefing document — for future sessions, collaborators, or as a grounding prompt for later procedural-generation systems (planet generation, NPC generation, music generation) that will need a compact description of the game's identity rather than the full design doc.

---

**Working title:** *Universe Game* (renamed 2026-08-25, deliberately plain/generic — not urgently flagged for replacement).

**Genre:** Top-down 2D sandbox/exploration game (a clean, classic top-down visual style — explicitly not 3D), spanning three nested scales of play: individual character, vehicle, and universe.

**Core philosophy:** Emergent systems, not authored ones. No hardcoded progression, mastery tracks, or storyline. The game generates its own complexity from simple interacting rules — discovery-driven, sandbox-first, nothing forced on the player. NPC societies must behave with real internal logic at their own scale (a settlement's resource output should actually make sense for its population — not a village of twenty residents sustained by a single tiny farm plot).

**The three scales:**
- **Individual Scale** (where the game begins; situational labels Ground/Deck/Drift): walk, gather, craft by hand, talk to NPCs face to face, terrain-gated by materials/armour. Must feel full of life and options at all times — this is the heart of the game, not a tutorial.
- **Operational Scale** (midgame; situational labels Site/Ship/Boat/Vehicle, Plane tentative): drive terrain-appropriate land/sea vehicles, fly atmospheres gated by planetary chemistry, pilot rockets between orbit and surface, explore settlements/wilderness/stations at speed.
- **Universal Scale** (unlocked late, via a crafted item/radar; no situational variants): a live view of the observable universe — ship trajectories, trade, automation, encroaching threats — the visible *result* of what's been built, not a separate system.

Full scale-naming rationale: [[03 Active Projects/Universe Game/Design/Public/Scale Terminology (Public).md]].

**Procedural universe:** Empty space + celestial bodies. Each planet's chemistry (a formula derived from a reference sample of planets) drives its geography, atmosphere, and what life can exist there. Individual lifeforms get genetics-style trait combination for organic-feeling diversity.

**Six societies** (faction-based, each with independent friend/enemy/neutral standing toward the other five — a 6-node relationship wheel): **Humanoids** (Yang-aligned, natural settlements), **The Industry** (Yin-leaning, robots/AI, harsh resource-rich worlds), **The Cult** (Yin worshippers, rare, magical, splintered from the Humanoids, mortal enemies), **The Bugs** (self-sustaining organic hive colonies, calm and pragmatic), **The Mushrooms** (tiny hive-mind, near-ubiquitous baseline lifeform), **The Network** (single mechanical hive-mind, opposed to *both* fundamental forces).

**Two fundamental forces** ("Yin"/"Yang," placeholder names — not good/evil, not Western dualism): **Yin** — a vast, edgeless, black-hole-born entity puppeting a jeweled celestial dummy, likely the final boss, grants a **weapon**. **Yang** — a wise, gently magical humanoid found only in untouched wilderness reached on foot and unarmoured, grants a **tool**.

**Technical spine (the hard constraint everything else depends on):** No per-entity simulation at the scale of a fully-rendered-everywhere-at-once world — too expensive for a universe this size. Instead, a **deterministic functional model**: each planet/region maintains a baked resource input/output calculation, modified by player/NPC action, that *collapses* to pure math when the player isn't physically present and *expands* into full live rendering (sprites, NPCs, machines) only on-site. Loading gets hidden progressively (e.g. a planet's atmosphere fades in as a flat color before full detail resolves), not blocked behind load screens. **This system's exact mechanics are not yet designed — it's the first real engineering problem to solve.**

**Future system, deferred:** procedural music engine — genre/instrument/rhythm rules keyed to in-game context (industrial, wild nature, deep space, etc.); Yin's theme specifically aiming for a sense of awe and smallness.
