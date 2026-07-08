# The Thing I'm Doing

Godot 4 .NET / C# project.

## Design

- [Magic Glyph Combinatoric Roguelike RPG](docs/design.md)
- [Project Rules](docs/rules.md)

## Local Setup

- Open the project folder from Windows Godot using the `project.godot` file in this WSL repo.
- Edit code in VS Code through Remote WSL.
- Keep project resources under `res://` and user data under `user://`.

If `dotnet` is installed under `~/.dotnet`, make sure this is on your shell path:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```
