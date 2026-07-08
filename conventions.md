# Project conventions

This is a Godot 4 .NET / C# project.

Development environment:
- The repo lives inside WSL.
- Godot editor runs on Windows.
- VSCode edits the project through Remote WSL.
- Avoid absolute OS-specific paths.
- Use `res://` for project resources and `user://` for user data.

Code style:
- Prefer small C# components over large manager classes.
- Use `[Export]` for tunable values.
- Use signals for cross-object events.
- Use `CharacterBody2D` for controlled 2D actors.
- Use `Area2D` for triggers, pickups, hitboxes, and hurtboxes.
- Keep physics movement in `_PhysicsProcess`.
- Avoid editing `.tscn` files directly unless explicitly requested.
- Do not move files without checking Godot resource paths.