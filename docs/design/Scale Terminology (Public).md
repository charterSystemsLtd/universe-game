---
type: design-document
created: 2026-08-20
status: final
sanitized: true
---

# Scale Terminology — final reference (Public)

The fixed vocabulary for the game's three nested gameplay/camera scales. **The internal term is what code, docs, and design conversations use** (e.g. a `GameScale` enum value) — stable, never changes, never shown raw to the player. **Situational labels are UI-facing only**, swapped based on the player's actual context; the list can grow later without touching the underlying system. See [Master Design Document (Public)](Master%20Design%20Document%20(Public).md) for the full description of what each scale actually does.

| Scale (fixed internal term) | Situational label | Context |
|---|---|---|
| **Individual Scale** | Ground | Standing on a planet/moon surface |
| | Deck | Walking the interior of a ship/station while in transit |
| | Drift | Floating freely in open space, no vessel |
| **Operational Scale** | Site | Fixed settlement, building, or building cluster |
| | Ship | Spacecraft / rocket |
| | Boat | Liquid-based transport |
| | Vehicle | Car / rover / land transport |
| | *Plane (tentative, deferred)* | Atmosphere-only flight — cheaper, narrower alternative to Ship, not committed to yet |
| **Universal Scale** | *(none — single context)* | Observable-universe overview: trajectories, background automation |

## Naming rationale (for future reference, in case this ever needs revisiting)
All three fixed terms deliberately share the "-al" suffix (Individual / Operational / Universal) — not a coincidence, this rhyme is what made the set feel right once spotted. Situational labels were deliberately kept as **concrete nouns naming what the player is in/on** (Ground, Deck, Drift, Site, Ship, Boat, Vehicle) rather than adverbs describing state (earlier rejected candidates: Aboard, Adrift) — concrete and swappable in a UI string table, no ambiguity about part of speech.
