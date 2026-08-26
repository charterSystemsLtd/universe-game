# Universe Game — System (Godot project / codebase)

Working-title project. A top-down 2D sandbox/exploration game built in Godot 4 + C#, spanning three nested camera scales (individual, vehicle, universe) around emergent systems, deterministic simulation, and procedural generation.

## Design documentation

Full public design documentation lives in [`docs/design/`](docs/design/), kept in sync with the project's design workspace via `scripts/sync-docs.sh`:

- [**Master Design Document**](docs/design/Master%20Design%20Document%20(Public).md) — design pillars, the three scales, the Unified Compositional Model, the Six Societies, the Two Fundamental Forces, and the Containment Collapse Model (the core technical architecture).
- [**Master Prompt**](docs/design/Master%20Prompt%20(Public).md) — condensed one-page brief, useful as a quick-start summary of the whole game.
- [**Planetary Generation System**](docs/design/Planetary%20Generation%20System%20(Public).md) — the actual formulas: element/compound registry, temperature/gravity/atmosphere derivation, region geometry, the Stellar Measure (sm) unit.
- [**Item & Crafting Systems**](docs/design/Item%20%26%20Crafting%20Systems%20(Public).md) — item catalog, the abstract `Item` data model, crafting-tree goals, and layered character/equipment rendering.
- [**Individual & Operational Scale — Gameplay Systems**](docs/design/Individual%20%26%20Operational%20Scale%20-%20Gameplay%20Systems%20(Public).md) — building/verticality, combat, vehicle piloting feel, and the real (maintained) Operational Scale ship-in-space scene construction.
- [**Societal Guide**](docs/design/Societal%20Guide%20(Public).md) — the Six Societies reference: profiles, habitat rules, and the full relationship wheel.
- [**Scale Terminology**](docs/design/Scale%20Terminology%20(Public).md) — the fixed internal scale names and their situational UI labels.

## Setup

1. Install the C#-capable Godot build:
   ```
   brew install --cask godot-mono
   ```
   This pulls in `dotnet-sdk` as a dependency, which needs an interactive sudo password — run this in a real Terminal window, not a scripted/automated shell.
2. Confirm install: `godot-mono --version`, or open `Godot_mono.app` from Applications.
3. Open this `System/` folder as a project in Godot (Import, point at `project.godot`).
4. If this is the first time opening the project, Godot may need you to generate the C# solution: **Project → Tools → C# → Create C# Solution**, then Build before pressing Play.

## Structure

- `project.godot` — engine config.
- `scenes/` — `.tscn` scene files (`Main.tscn` is the Operational Scale ship-in-space entry point; `IndividualScaleTest.tscn` is the current on-foot character test scene).
- `src/` — C# scripts.
- `assets/` — art/sprites (`assets/sprites/character/` holds the current character template, source `.aseprite` files under `source/`).
- `docs/` — public design documentation, see above.
- `scripts/` — repo tooling (`sync-docs.sh`).

## Status

Early active development. Placeholder ship movement and a parallax starfield background exist and work; a first on-foot character controller exists and is being wired up to real sprite art. See the design docs above for the full architecture and the project's own tracking note for exact current progress and next steps.
