# Mods

Project-local mods can live here during development.

Each mod may provide the same atomic content families used by `Content/Base`: `strings.json`,
`clauses.json`, `behaviors.json`, `behavior_primitives.json`, `effects.json`, `enemies.json`,
`rules.json`, `relics.json`, `rewards.json`, `environments.json`, `encounters.json`, and `runs.json`.
Definitions are loaded in folder-name order after base content. Use explicit `replace`, `override`,
`disable`, or `remove` operations when changing an existing stable ID.

Player-installed mods may mirror those files under `user://mods/<mod-id>/`.
