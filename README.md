# Xander's Game — System (Godot project / codebase)

Working-title project. See [[03 Active Projects/Xander's Game/Xander's Game.md]] for full project tracking, scope, and decisions.

## Setup

1. Finish installing the C#-capable Godot build (blocked on a manual sudo step as of 2026-08-19):
   ```
   brew install --cask godot-mono
   ```
   This pulls in `dotnet-sdk` as a dependency, which needs an interactive sudo password — run this yourself in a real Terminal window, not via an automated/scripted shell.
2. Confirm install: `godot-mono --version`, or open `Godot.app` from Applications.
3. Open this `System/` folder as a project in Godot (File → Import, point at `project.godot` in this folder).
4. `project.godot` here is a **hand-scaffolded starting point**, not yet verified against a real running editor — Godot may rewrite/regenerate parts of it on first open (normal). Let the editor's own Project Settings be the source of truth going forward once it's opened for real.
5. The first time you add a C# script, Godot will auto-generate a `.csproj` file (`XandersGame.csproj`) in this folder — that's expected and normal, not something to hand-write.

## Structure (to be built out)
Nothing beyond `project.godot` exists yet. Standard Godot structure once real work starts: `scenes/`, `scripts/`, `assets/` (art/audio), matching Godot's own conventions rather than inventing a custom layout.

## Status
Scaffolded 2026-08-19. No actual game code yet — tech stack was just decided this session. Real architecture/design work (game loop, resource-tracking data model, scene structure) starts next session.
