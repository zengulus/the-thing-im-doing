# Project Rules

These are engineering and design guardrails for the Magic Glyph Roguelike.

The goal is to keep the game data-driven, mod-friendly, previewable, serializable, and readable as ritual grammar.

---

# 1. Core Architecture Rule

The gameplay source of truth is semantic data, not UI presentation.

A Working may be displayed as:

* graph nodes;
* spell circle geometry;
* ritual prose;
* debug JSON;
* omen trace playback.

But all of those are projections of the same underlying Working definition.

Do not bake spell meaning into visual layout.

---

# 2. Bottom-Up Mod Friendliness

* Build every gameplay content type as if the base game were the first installed mod.
* Base content, project mods, and user mods should load through the same pipeline wherever practical.
* Clauses, relics, enemies, local rules, terrain reactions, statuses, encounter templates, room templates, loot tables, and starting loadouts should be definitions registered by stable IDs, not scattered hardcoded branches.
* A feature is not truly implemented until new instances of that feature can be added without editing unrelated systems.
* Prefer data-driven definitions plus small reusable behavior primitives over bespoke classes for each piece of content.
* Treat behavior as content wherever possible: clauses, enemies, relic hooks, effects, and local rules should point at behavior graph IDs.
* Mod-authored behavior primitives should start as composite primitives: stable primitive IDs that expand into behavior graphs made from existing atoms.
* Leaf behavior atoms are trusted engine operations until a deliberate trusted-code mod API exists.
* Do not let arbitrary mod data execute arbitrary C#.
* If a system needs a registry, create the registry early and let base content register through it.
* If content must use C# behavior, bind that behavior through a stable behavior ID in data.
* If content must use a C# atom, bind it through a stable primitive ID in the behavior primitive registry.
* Do not use player-facing names as logic keys.
* Never branch on strings like `"Fire"`, `"Glass Hound"`, or `"Brittle Stone"`.

---

# 3. Content Definitions

Use stable IDs for content:

* `clause.spark_them`
* `enemy.glass_hound`
* `rule.brittle_stone`
* `relic.patient_bell`
* `effect.poison`
* `counter.stack`

Definitions should separate:

* identity: stable ID, tags, content pack;
* presentation: string keys, icons, glyphs, colors, sort order;
* numbers: costs, damage, ranges, durations;
* behavior hooks: behavior IDs, rule hooks, trigger names;
* requirements: unlocks, allowed pools, incompatible tags;
* layout hints: optional presentation metadata only.

Definitions should be additive by default.

Replacements, removals, and disables should be explicit override operations.

---

# 4. Working Semantics

A Working is a semantic spell definition.

It should contain:

* schema version;
* stable working ID or generated share ID;
* display name string or key;
* max step count;
* entry node;
* nodes;
* clause IDs;
* outputs;
* optional layout metadata.

A Working should remain valid if layout metadata is deleted.

A Working should be executable without the graph editor or spell-circle renderer.

A Working should be exportable to JSON once serialization exists.

---

# 5. Spell Circle Rule

The spell circle is presentation, not logic.

The circle renderer may decide:

* glyph placement;
* ring placement;
* arc shape;
* branch decoration;
* animation path;
* hover regions;
* visual emphasis.

It must not decide:

* execution order;
* clause effect;
* branch result;
* target identity;
* cost;
* trigger semantics;
* backlash rules.

If the circle and the semantic Working disagree, the semantic Working wins.

---

# 6. Preview and Trace Parity

The preview system must run through the same definitions, hooks, behavior machine, counters, effects, terrain rules, and relic rules as real casting.

A mechanic is incomplete until it is:

* previewable, or explicitly marked uncertain;
* traceable in the omen trace;
* inspectable in UI;
* explainable in plain ritual prose;
* safe to run on a cloned encounter.

Do not add hidden effects that only appear after real casting unless uncertainty is the explicit design point.

---

# 7. Gameplay Systems

* Clauses should be loaded from clause definitions.
* The Working editor should discover available clauses from the clause registry.
* Relics should be rule modifiers made from trigger/effect hooks, not one-off checks sprinkled through combat code.
* Enemies should be assembled from enemy definitions: stats, intent text keys, behavior profile IDs, tags, and rewards.
* Local rules should be registered rule definitions with hooks into movement, damage, terrain, marks, and spell execution.
* Terrain reactions should be definitions or rule hooks, not hardcoded tile-name checks outside the terrain/rule system.
* Enemy turns, spell clauses, rule reactions, effect triggers, and relic triggers should all run through the same behavior machine unless there is a strong reason to create a specialized executor.
* Loot and encounter generation should consume weighted pools from content definitions.
* The UI should ask registries what exists; it should not maintain its own parallel list of clauses, relics, enemies, or rules.
* Any code-only prototype shortcut should preserve the eventual data shape.

---

# 8. Registries and Loading

Create one registry per content family when that family becomes real:

* clauses;
* behaviors;
* behavior primitives;
* effects;
* enemies;
* relics;
* local rules;
* terrain;
* statuses;
* encounters;
* rooms;
* loot pools;
* mage backgrounds.

Registries should support deterministic load order:

1. base content;
2. project mods;
3. user mods.

Registries should expose read-only resolved definitions to gameplay code.

Runtime code should depend on registries and stable IDs, not file paths.

Behavior code should resolve primitive IDs through the primitive registry before declaring an atom unknown, so mods can add composite primitives without engine edits.

---

# 9. Schema and Validation

All JSON content files should include `schemaVersion`.

Registries and loaders should validate:

* duplicate IDs;
* missing string keys;
* missing behavior IDs;
* missing primitive IDs;
* malformed numbers;
* invalid enum values;
* invalid trigger names;
* incompatible schema versions;
* references to disabled content;
* circular or impossible definitions where detectable.

A broken mod should fail gracefully where possible.

The offending content should be disabled with a readable diagnostic.

Do not let one malformed optional content pack corrupt the base game.

---

# 10. Content and Strings

* External names always live in strings files only.
* Use stable IDs in code, saves, configs, mod data, and shared Working JSON.
* Player-facing names, sigils, labels, descriptions, tooltips, and lore text should be looked up by string key.
* Base strings live in `res://Content/Base/strings.json`.
* Mods may override strings with `res://Mods/<mod-id>/strings.json` or `user://mods/<mod-id>/strings.json`.
* Code may contain string keys and internal diagnostics, but not canonical player-facing content names.
* When adding a new content type, add an ID first, then put display text in the strings file.

---

# 11. Working Shareability

Shared Workings should be boring JSON first.

Future compact share codes may exist, but they should decode into the same canonical Working shape.

Imported Workings should be validated before use.

If an imported Working references missing content:

* explain what is missing;
* disable invalid nodes;
* preserve the original data if possible;
* allow the player to repair it.

Do not silently reinterpret missing clauses.

---

# 12. Build-Friendly Implication

Mod-friendly is build-friendly.

When base content is data-driven, designers can add and tune game pieces without waiting on new C# classes.

* Prefer making one more reusable primitive over making one more special case.
* Before hardcoding behavior, ask whether it is a reusable hook, trigger, condition, selector, targeter, effect, or modifier.
* New prototype content may start in code, but the code should already mirror the eventual data shape.
* When a code-only shortcut is necessary, leave it behind a stable ID boundary so it can move to data later.
* When a native behavior atom is necessary, add the smallest reusable atom and document it in `behavior_primitives.json` plus strings.

---

# 13. Player-Facing Language

Internally, the game may use graphs, nodes, booleans, IDs, registries, and JSON.

Player-facing language should prefer:

* Working;
* Clause;
* Glyph;
* Refrain;
* Remembered sign;
* Omen trace;
* Backlash;
* Circle;
* Seal;
* Anchor;
* Local grammar;
* Hostile dialect;
* Binding mark.

Avoid player-facing use of:

* program;
* script;
* variable;
* register;
* boolean;
* AST;
* function;
* graph, except in debug/editor contexts;
* node, except in debug/editor contexts.

The player should feel like they are constructing dangerous ritual grammar, not wiring business logic.

---

# 14. Feature Promotion Rule

A new feature may enter active implementation only if it satisfies at least one:

* makes Workings more expressive;
* makes preview or trace-reading clearer;
* pressures spell revision;
* improves content/mod authoring;
* improves serialization/shareability;
* improves spell-circle readability;
* removes a hardcoded special case;
* strengthens the magical-pseudocode fantasy.

If it only adds content, stats, lore, or complexity, defer it.
