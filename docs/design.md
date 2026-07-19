# The Living Archive — Shipped Game Design

## Product contract

The Thing I'm Doing is a compact turn-based tactics sandbox about writing short magical Workings inside an alien, internally coherent environment.

Each session generates one connected Archive. The player may explore it freely, avoid or engage ordinary threats, revise two prepared Workings, and experiment with the complete Clause Codex. Exactly one Obsidian Crown waits deeper in the map. Breaking that Crown wins the session; clearing every other enemy is never required.

The core loop is:

> Explore → observe a local problem → generate setup → reshape it → preview the payoff → cast → read the omen → revise.

The game is intentionally one strong systemic sandbox, not a linear campaign, content treadmill, or metaprogression game. The Archive is the subject: its local law, mutable terrain, owned marks, dormant ecology, and interacting inhabitants should create situations the player can investigate and exploit. The Crown is only the eventual terminating objective that gives this experimentation a finish line.

## Session structure

- A fresh seed creates one 40×28 connected room-and-corridor map.
- The player starts with 8 health, both sample Workings, every registered clause, and every registered relic.
- Six ordinary enemy archetypes are distributed across the Archive.
- Distant enemies are dormant. Sight, damage, or an alerted nearby ally can wake them, and full behavior remains bounded to the local viewport-sized radius.
- Fog of war hides unexplored terrain. Walls and raised stone block movement and sight.
- The Crown echo stays dormant through early exploration. After roughly 30% of the map is observed it offers only a coarse cardinal direction, preventing late-session blind wandering without becoming the opening waypoint.
- The sole required objective is the unique Obsidian Crown. Its death wins even if ordinary enemies remain alive.
- Player death loses. A new sandbox always creates a fresh seed unless `TTID_RUN_SEED` is configured.

## Workings

A Working is a deterministic graph of at most seven unique clauses. Semantic data is authoritative; layout is presentation only.

The player has two prepared slots and can:

- add unlocked clauses from the Codex;
- connect ordinary `NEXT` outputs and conditional `TRUE`/`FALSE` outputs;
- preview the exact cloned-world result without spending a turn;
- step through the omen trace;
- cast only when the current preview succeeds and changes the world.

A failed Working resolves transactionally: partial mutations are rolled back. A successful no-effect omen is shown plainly and cannot be committed from the game UI.

### Clause roles

Every clause has exactly one role:

| Role | Purpose | Representative clauses |
|---|---|---|
| Generator | Establish focus, state, terrain, counters, or memory | Aim Target, Mark, Poison, Raise Stone, Add Charge, Store Memory |
| Operator | Test, route, recall, move, or modestly reshape setup | If Marked, If Clear, Push, Recall Memory |
| Consumer | Spend or commit setup for payoff | Damage, Spend Poison, Spend Memory, Lightning Shield |

Roles are shown in both the Codex and graph cards. They are behavioral promises, not merely colors.

The baseline setup/payoff example is `Mark or Damage`:

```text
aim at nearest foe
if they bear your mark:
    consume the mark to deal 2 damage
else:
    mark them
```

This alternates setup and payoff. A standalone Damage clause cannot harm an unmarked target. The damage Consumer removes only the caster's mark, preserving marks owned by other actors.

Operators are allowed to be “middling”: Push can reposition a target without setup, for example, but its stronger payoff comes from driving that target into raised stone under Brittle Stone.

## Tactical rules

- Turns alternate strictly between the player and locally engaged enemies.
- Moving into an adjacent enemy performs a basic strike.
- Preview and live casting use the same Working machine, behavior graphs, rule hooks, effects, counters, relic hooks, and cloned encounter state.
- Perception and active AI use a seven-tile Chebyshev radius so anything described as in sight remains on the playable viewport.
- Raised stone is temporary when created by a Working. Under Brittle Stone, pushing an actor into it breaks the stone and damages the actor.
- Enemy intent, awareness, health, counters, effects, local rules, target state, and omen traces remain inspectable.

## Enemy sandbox

The ordinary roster supplies distinct local pressures and combinations. Scribes can leave hazardous signs for other actors, Saints reshape routes, Chirurgeons sustain nearby threats, and movement, stone, marks, poison, wards, and collision rules can compound without a bespoke encounter script:

- Glass Hound — direct pursuit and collision pressure.
- Ash Scribe — marks ground, then detonates it.
- Root Saint — raises terrain and heals near stone.
- Spore Cantor — ranged poison and close pressure.
- Iron Pilgrim — durable pursuit with periodic ward.
- Moss Chirurgeon — regroups and heals active allies.

The Obsidian Crown is the only boss and only mandatory kill. It brands ground, advances, detonates its marks, wards itself, and strikes at close range. Generation excludes the objective explicitly, as well as boss/capstone enemies, from roster repetition, so each sandbox contains exactly one Crown. Its delayed, coarse echo prevents late-session aimless wandering, but the boss remains embedded in the same ecology and obeys the same inspectable systems as everything else.

## Presentation and input

- The world uses deliberately simple, high-contrast geometry.
- Walls render as inset crossed slabs; raised stone renders as a diamond. Shape and text reinforce color.
- The fixed HUD shows health, exploration, visible threats, enemy intents, local rules, selected tile, seed, objective, and Crown echo.
- Mouse and keyboard are both valid for targeting. `Shift`+movement keys move the target cursor through explored tiles.
- The opening guide explains the objective, roles, preview, terrain, and controls in a scrollable panel.

## Content and architecture

Base content and mods load through the same deterministic JSON registry pipeline. Stable IDs, string keys, behavior graphs, reusable trusted atoms, schema validation, and source-aware overrides remain mandatory.

Gameplay systems remain pure C# where practical and are tested without scene dependencies. The Godot controller projects encounter state into visuals and input; it does not redefine spell meaning.

## Deliberate limits

The shipped scope does not include multiple floors, save/load, metaprogression, narrative campaigns, procedural ontologies, online features, controller remapping, or production art/audio. Those may be explored later, but none is required to complete the sandbox-to-Crown experience.
