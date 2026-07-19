# The Thing I'm Doing

Godot 4 .NET / C# tactics sandbox about writing short magical Workings inside a learnable alien environment.

## Playable Game

The main scene is a complete, replayable sandbox expedition through the Living Archive:

- one deterministic 40×28 procedural map per seed, with connected rooms, corridors, varied terrain, fog of war, and camera-follow exploration;
- a sandbox-first structure where terrain, local law, owned statuses, enemy support roles, and spell clauses combine without encounter scripts;
- exactly one remote Obsidian Crown boss; defeating it wins immediately, while every ordinary threat is optional;
- all six ordinary enemy archetypes distributed across the sandbox, dormant until the player enters their local awareness;
- line-of-sight blocked by walls and raised stone, including player targeting and enemy perception;
- a Crown echo that stays dormant during early investigation, then offers only a coarse direction so the boss remains an eventual finish line rather than the opening waypoint;
- two editable Workings with deterministic preview and omen traces;
- a complete Clause Codex from the start: Generators create setup, Operators route or reshape it, and Consumers spend setup for payoff;
- mark-consuming direct damage, terrain collisions, status interactions, counters, memory, wards, a relic hook, victory, defeat, and seeded restart;
- environments, rules, enemies, behaviors, effects, rewards, generation templates, and encounters composed from reusable content atoms;
- clear placeholder geometry that keeps playtesting focused on the systems.

Controls: click a tile or use `Shift`+WASD/arrows to target; use WASD/arrows to move or strike, `F` to cast, `P` to preview, `E` to edit, `Tab` to change Working, `Space` to wait, and `R` to open the restart menu.

## Design

- [Magic Glyph Combinatoric Roguelike RPG](docs/design.md)
- [Systemic Depth and Content Roadmap](docs/roadmap.md)
- [Project Rules](docs/rules.md)

## Local Setup

The repository is configured for Godot 4.7 .NET. A local headless/editor install can be exposed as `godot` or `godot4`; project resources belong under `res://` and saves/mods under `user://`.

If `dotnet` is installed under `~/.dotnet`, make sure this is on your shell path:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

Run the pure C# game/system tests with:

```bash
dotnet test Tests/TheThingImDoing.Tests/TheThingImDoing.Tests.csproj
```

Import, compile, and smoke-test the main scene without a display:

```bash
godot --headless --editor --path . --quit
godot --headless --path . --audio-driver Dummy --quit-after 10
```

Create a Linux build after installing the matching Godot 4.7 Mono export templates:

```bash
godot --headless --path . --export-release "Linux" Builds/Linux/TheThingImDoing.x86_64
```

Ship the complete `Builds/Linux` directory; the executable uses its generated `data_TheThingImDoing_linuxbsd_x86_64` companion directory.

Sandboxes generate a fresh seed by default. Reproduce a specific Archive by setting the seed shown in the HUD:

```bash
TTID_RUN_SEED=4242 godot --path .
```
