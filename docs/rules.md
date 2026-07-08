# Project Rules

## Bottom-Up Mod Friendliness

- Build every gameplay content type as if the base game were the first installed mod.
- Clauses, relics, enemies, local rules, terrain reactions, statuses, encounter templates, room templates, loot tables, and starting loadouts should be definitions registered by stable IDs, not scattered hardcoded branches.
- A feature is not truly implemented until new instances of that feature can be added without editing unrelated systems.
- Prefer data-driven definitions plus small reusable behavior primitives over bespoke classes for each piece of content.
- Treat behavior as content wherever possible: clauses, enemies, relic hooks, and local rules should point at behavior graph IDs.
- Mod-authored behavior primitives should start as composite primitives: a stable primitive ID that expands into a behavior graph made from existing atoms.
- Leaf behavior atoms are trusted engine operations until a deliberate trusted-code mod API exists. Do not let arbitrary mod data execute arbitrary C#.
- The game should load base content and mod content through the same pipeline wherever practical.
- If a system needs a registry, create the registry early and let base content register through it.
- If content must use C# behavior, bind that behavior through a stable behavior ID in data instead of matching on display names.
- If content must use a C# atom, bind it through a stable primitive ID in the behavior primitive registry.
- Do not use player-facing names as logic keys. Never branch on strings like `"Fire"`, `"Glass Hound"`, or `"Brittle Stone"`.
- Data schemas should be boring, explicit, and versionable. Add a `schemaVersion` field once definitions move to JSON/resources.

## Content Definitions

- Use stable IDs for content: `clause.spark_them`, `enemy.glass_hound`, `rule.brittle_stone`, `relic.patient_bell`.
- Keep definitions small and composable.
- Definitions should separate:
  - identity: stable ID, tags, content pack;
  - presentation: string keys, icons, colors, sort order;
  - numbers: costs, damage, ranges, durations;
  - behavior hooks: behavior IDs, rule hooks, trigger names;
  - requirements: unlocks, allowed pools, incompatible tags.
- Definitions should be additive by default. Replacements and removals should be explicit override operations.
- Content packs should be able to add definitions, override fields, and disable definitions by stable ID.

## Gameplay Systems

- Clauses should be loaded from clause definitions. The working editor should discover available clauses from the clause registry.
- Relics should be rule modifiers made from trigger/effect hooks, not one-off checks sprinkled through combat code.
- Enemies should be assembled from enemy definitions: stats, intent text keys, behavior profile IDs, tags, and rewards.
- Local rules should be registered rule definitions with hooks into movement, damage, terrain, marks, and spell execution.
- Terrain reactions should be definitions or rule hooks, not hardcoded tile-name checks outside the terrain/rule system.
- Enemy turns, spell clauses, rule reactions, and relic triggers should all run through the same behavior machine unless there is a strong reason to create a specialized executor.
- Loot and encounter generation should consume weighted pools from content definitions.
- The UI should ask registries what exists; it should not maintain its own parallel list of clauses, relics, enemies, or rules.
- The preview system must run through the same definitions and hooks as real casting.

## Registries And Loading

- Create one registry per content family when that family becomes real: clauses, relics, enemies, local rules, terrain, statuses, encounters.
- Registries should support deterministic load order: base content first, then project mods, then user mods.
- Registries should validate duplicate IDs, missing string keys, missing behavior IDs, malformed numbers, and incompatible schema versions.
- Registries should expose read-only resolved definitions to gameplay code.
- Runtime code should depend on registries and stable IDs, not file paths.
- Behavior code should resolve primitive IDs through the primitive registry before declaring an atom unknown, so mods can add composite primitives without engine edits.
- Content loaders should report clear diagnostics for mod authors.
- A broken mod should fail gracefully where possible, with the offending content disabled and a readable error.

## Build-Friendly Implication

- Mod-friendly is build-friendly: when base content is data-driven, designers can add and tune game pieces without waiting on new C# classes.
- Prefer making one more reusable primitive over making one more special case.
- Before hardcoding behavior, ask whether it is a reusable hook, trigger, condition, selector, targeter, effect, or modifier.
- New prototype content may start in code, but the code should already mirror the eventual data shape.
- When a code-only shortcut is necessary, leave it behind a stable ID boundary so it can move to data later.
- When a native behavior atom is necessary, add the smallest reusable atom and document it in `behavior_primitives.json` plus strings.

## Content And Strings

- External names always live in strings files only.
- Use stable IDs in code, saves, configs, and mod data.
- Player-facing names, sigils, labels, descriptions, tooltips, and lore text should be looked up by string key.
- Base strings live in `res://Content/Base/strings.json`.
- Mods may override strings with `res://Mods/<mod-id>/strings.json` or `user://mods/<mod-id>/strings.json`.
- Code may contain string keys and internal diagnostics, but not canonical player-facing content names.
- When adding a new content type, add an ID first, then put its display text in the strings file.

## Mod-Friendly Data

- Prefer additive content with stable IDs over hardcoded name matching.
- Keep behavior code separate from display strings.
- Let base game content use the same loading path that mods use.
- Mod overrides should be deterministic; load folder names in sorted order.
- Base content lives in JSON under `res://Content/Base`; project mods live under `res://Mods/<mod-id>`; player mods live under `user://mods/<mod-id>`.
