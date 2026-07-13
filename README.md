# The Thing I'm Doing

Godot 4 .NET / C# tactics roguelike about writing short magical Workings.

## Playable Game

The main scene contains a finishable five-floor run through the Ashen Archive:

- deterministic 32×24 through 48×32 procedural maps with connected rooms, corridors, seeded terrain, fog of war, and camera-follow exploration;
- six data-driven enemy archetypes and the Obsidian Crown boss, distributed across the map;
- line-of-sight blocked by walls and raised stone, including player targeting and enemy perception;
- layered enemy awareness: distant enemies remain dormant, nearby enemies alert through sight, damage, or line of sight to alerted allies, and full AI runs only within a viewport-sized radius;
- two editable Workings with deterministic preview and omen traces;
- carried health, room rewards, clause unlocks, relics, victory, and defeat;
- environments, rules, enemies, behaviors, effects, rewards, generation templates, and encounters composed from reusable content atoms;
- intentionally simple placeholder geometry so playtesting stays focused on the systems.

Controls: click a tile to choose it, use WASD/arrows to move or strike, `F` to cast, `P` to preview, `E` to edit, `Tab` to change Working, `Space` to wait, and `R` to restart the run.

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

Runs generate a fresh seed by default. Reproduce a specific map sequence by setting the seed shown in the HUD:

```bash
TTID_RUN_SEED=4242 godot --path .
```
