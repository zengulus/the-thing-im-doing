# Codex Build Brief — Magic Glyph Roguelike Prototype

## Implemented Game Scope

The original milestone brief below began as a one-room prototype plan. The current build supersedes that scope with a finishable procedural tactics roguelike while retaining the same readable-Working constraints. The long-term direction is expeditionary xenology: each campaign enters a generated alien reality with its own learnable ontology, ecology, history, substances, and procedurally assembled entities. See [roadmap.md](roadmap.md).

The run is assembled atomically from JSON definitions: behavior primitives form behavior graphs; graphs power clauses, enemies, effects, relics, and local rules; encounter templates seed large connected room-and-corridor maps; encounters pair with themed environments; and five increasingly expansive floors form the complete Ashen Archive run. Player health and chosen rewards carry between floors, advanced clauses unlock during the run, and the final Obsidian Crown encounter ends in an explicit victory state. The current floor-clear reward screen is a baseline pacing mechanism; the long-term reward economy is driven by exploration, observation, recovered evidence, optional risk, and confirmed understanding rather than kills. The current small `TileState` model is also only a baseline: the roadmap replaces it with layered terrain, composable substances, and deterministic turn-based physics shared by preview and live resolution. Graphics remain placeholders by design.

Exploration uses fog of war and a following camera. Walls and raised stone block sight. Enemies remain dormant at long distance, become alerted through player sight, damage, or sight of nearby alerted allies, and only execute full behavior inside a viewport-sized engagement radius. This keeps simulation cost bounded without making distant actors omniscient.

The historical prototype brief below is retained as design lineage, not as the active scope restriction.

## Project Goal

Build a Godot C# prototype for a turn-based tactics roguelike where spells are written as compact magical pseudocode.

The prototype should prove one core loop:

> read the room → adjust a short spell → preview the result → cast → inspect the trace → fix one clause → feel clever.

Do not build the full roguelike yet.
Do not build long-term progression yet.
Do not build multiple regions yet.

This project is currently a **core-fun prototype**, not a full game.

---

# Core Design Target

The player is not writing low-level code.
The player is shaping readable magical workings.

The target complexity is closer to simple Python/pseudocode than assembly.

Example player-facing spell:

```text
aim at nearest foe
if they are marked:
    spark them
else:
    mark them
```

Do not expose low-level programming terms in the UI unless required internally.

Prefer in-universe terms:

| Internal concept   | Player-facing term |
| ------------------ | ------------------ |
| spell program      | working            |
| instruction        | clause             |
| execution trace    | omen trace         |
| variable/register  | remembered sign    |
| branch             | condition          |
| loop               | refrain            |
| runtime error      | backlash           |
| cursor             | focus              |
| compiler/test room | proving circle     |

Internal code may use precise engineering names where useful.

---

# Hard Scope

## Build Only This First

* One tactical test room.
* One player-controlled mage.
* Strict turn-based combat.
* Three enemy types.
* Two prepared spell slots.
* A small spell editor.
* A step-through preview / trace system.
* A compact magical pseudocode spell representation.
* One local floor rule active at a time.
* Win condition: defeat all enemies.
* Loss condition: player health reaches zero.

## Do Not Build Yet

* Full procedural campaign.
* Multiple regions.
* Meta-progression.
* Mage backgrounds.
* Narrative/lore system.
* Bosses.
* Complex relic economy.
* Multiple registers.
* Unbounded loops.
* Hidden rule discovery.
* Large content pools.
* Real-time-with-pause combat.
* Energy-based initiative.

The first prototype should validate spell construction and debugging before expanding.

---

# Godot / C# Conventions

Use Godot 4.x with C#.

Follow these conventions:

* Use small C# components over large manager classes.
* Use `[Export]` for tunable scene values.
* Use signals/events for cross-object communication.
* Use `CharacterBody2D` for the player and controlled actors.
* Use `Area2D` for pickups, triggers, hitboxes, and hurtboxes.
* Keep turn/combat logic deterministic.
* Keep spell execution mostly pure C# and testable outside scenes.
* Avoid direct `.tscn` editing unless necessary.
* Prefer scene composition over giant inheritance trees.

---

# Suggested Repository Structure

```text
res://
  Scenes/
    Main.tscn
    TestRoom.tscn
    Player/
      Player.tscn
    Enemies/
      AshScribe.tscn
      GlassHound.tscn
      RootSaint.tscn
    UI/
      SpellBar.tscn
      SpellEditor.tscn
      SpellPreview.tscn
      OmenTracePanel.tscn

  Scripts/
    Core/
      GridPos.cs
      Direction.cs
      TurnSystem.cs
      GameResult.cs

    Actors/
      ActorComponent.cs
      HealthComponent.cs
      FactionComponent.cs
      MovementComponent.cs
      PlayerController.cs
      EnemyController.cs

    Combat/
      CombatAction.cs
      MoveAction.cs
      CastSpellAction.cs
      WaitAction.cs
      DamageSystem.cs
      PushSystem.cs

    Spells/
      Working.cs
      ClauseDefinition.cs
      ClauseFamily.cs
      WorkingMachine.cs
      WorkingContext.cs
      OmenTrace.cs
      OmenTraceEvent.cs
      WorkingResult.cs
      ISpellWorld.cs

    Behaviors/
      BehaviorDefinition.cs
      BehaviorDefinitionCatalog.cs
      BehaviorPrimitiveDefinition.cs
      BehaviorPrimitiveCatalog.cs
      BehaviorMachine.cs

    World/
      TacticalGrid.cs
      TileState.cs
      TileOccupancy.cs
      FloorRuleSet.cs
      FloorRuleDefinition.cs

    Relics/
      RelicDefinition.cs
      RelicDefinitionCatalog.cs

    UI/
      SpellEditorController.cs
      SpellPreviewController.cs
      OmenTracePanelController.cs
      SpellBarController.cs

  Content/Base/
    clauses.json
    behaviors.json
    behavior_primitives.json
    enemies.json
    rules.json
    relics.json
    strings.json
```

---

# Core Data Model

## `Working`

A spell/working composed from readable clauses.

Responsibilities:

* Store ordered list of clauses.
* Store display name.
* Store max clause count.
* Expose estimated focus cost.
* Validate basic structure.
* Provide player-facing text representation.

Do not make `Working` responsible for executing itself.

---

## `ClauseDefinition`

Defines one available magical clause.

Fields:

* `string Id`
* `string DisplayNameKey`
* `string PlayerTextKey`
* `ClauseFamily Family`
* `int BaseFocusCost`
* `string TooltipKey`
* `string BehaviorId`
* tags and condition flag

Clauses should be mechanically small but player-facing text should be readable.

Example:

```text
DisplayName: "If Marked"
PlayerText: "if they are marked:"
Tooltip: "Continue through this condition only if the current focus target has your mark."
```

---

## `WorkingMachine`

Pure C# spell interpreter.

Responsibilities:

* Execute a `Working`.
* Maintain working state.
* Query/mutate the world via `ISpellWorld`.
* Produce an `OmenTrace`.
* Return a `WorkingResult`.

Must not depend directly on scene nodes.

Clause execution delegates to behavior graph IDs. The machine should not switch on clause IDs.

---

## `WorkingContext`

Temporary state for one casting.

Fields:

* caster actor ID;
* selected target tile;
* current focus tile;
* current remembered sign, optional;
* temporary marks;
* focus available;
* active floor rule;
* current condition state;
* execution step limit.

Keep this simple.

Only support one remembered sign in the first prototype.

---

## `OmenTrace`

A step-by-step explanation of what the working did.

Used for:

* preview;
* post-cast explanation;
* debugging;
* tests;
* animation timing.

Trace event examples:

```text
1. Aimed at nearest foe: Glass Hound.
2. Checked condition: target was not marked.
3. Marked Glass Hound.
4. Working ended.
```

The trace is a core feature, not debug-only output.

---

## `ISpellWorld`

Interface between the spell machine and the tactical grid.

Example methods:

```csharp
public interface ISpellWorld
{
    bool IsInside(GridPos pos);
    bool IsOccupied(GridPos pos);
    bool IsEmpty(GridPos pos);
    ActorComponent? GetActorAt(GridPos pos);

    IEnumerable<ActorComponent> GetEnemiesOf(ActorComponent caster);
    ActorComponent? GetNearestEnemy(ActorComponent caster);

    bool HasMark(GridPos pos, ActorComponent owner);
    void AddMark(GridPos pos, ActorComponent owner, int durationTurns = 1);
    void RemoveMark(GridPos pos, ActorComponent owner);

    void ApplyDamage(ActorComponent target, int amount, ActorComponent? source);
    bool TryPushActor(ActorComponent actor, Direction direction, int distance, ActorComponent? source);

    bool CanRaiseStone(GridPos pos);
    void RaiseStone(GridPos pos, int durationTurns = 1);

    FloorRuleSet GetActiveFloorRules();
}
```

Keep this interface narrow. Add methods only when required by a milestone.

---

# First Working Grammar

Use player-facing magical pseudocode.

## Required Clauses

### Targeting

* `aim at target`
* `aim at nearest foe`

### Conditions

* `if occupied:`
* `if marked:`
* `if clear:`
* `else:`

### Memory

* `remember them`
* `return to remembered`

### Marks

* `mark them`

### Effects

* `spark them`
* `push them`
* `raise stone`

### Optional after core works

* `for each marked tile:`
* `repeat twice:`
* `when they enter:`

Do not implement loops until basic conditionals and preview are solid.

---

# First Example Workings

These should be buildable in the spell editor.

## Mark-or-Spark

```text
aim at nearest foe
if marked:
    spark them
else:
    mark them
```

Expected behavior:

* If nearest foe is marked, deal damage.
* Otherwise, apply a mark.

---

## Emergency Wall

```text
aim at target
if clear:
    raise stone
```

Expected behavior:

* If the selected tile is empty, create a temporary blocking tile.
* If occupied or blocked, fail cleanly and explain why.

---

## Push Trap

```text
aim at target
if occupied:
    push them
```

Expected behavior:

* Push an actor away from the caster.
* If pushed into a wall, apply collision damage if the active rule supports it.

---

## Remembered Strike

```text
aim at nearest foe
remember them
aim at target
if clear:
    raise stone
return to remembered
spark them
```

Expected behavior:

* Remember an enemy.
* Raise a wall at the selected tile if possible.
* Return focus to remembered enemy.
* Damage them.

This is near the upper complexity limit for the first prototype.

---

# Turn-Based Combat

Use strict player-turn/enemy-turn structure.

## Player Actions

* Move one tile.
* Cast one prepared working.
* Wait to recover focus.
* Edit prepared working, if test mode allows.
* End turn.

## Enemy Turn

Enemies act in deterministic order.

Each enemy should:

* expose intent before acting;
* have readable behavior;
* pressure spell construction;
* avoid complex hidden logic.

---

# First Enemy Set

## Ash Scribe

Purpose: teaches marks and marked-tile danger.

Behavior:

* Each turn, marks a nearby tile.
* Every third turn, heats or damages marked tiles.
* Telegraphs which marked tiles will trigger.

Implementation:

* Track internal counter.
* On counter 1–2: place marks.
* On counter 3: trigger marked tiles, reset counter.

---

## Glass Hound

Purpose: teaches push and terrain interaction.

Behavior:

* Moves toward player.
* Attacks adjacent player.
* If pushed into wall, takes damage or becomes stunned.
* Repeated direct `spark` should be less effective only after the core loop works.

Implementation:

* Simple chase pathing first.
* Add stun-on-wall-collision after push system exists.

---

## Root Saint

Purpose: teaches terrain manipulation.

Behavior:

* Raises temporary blocking terrain near itself.
* Heals if adjacent to raised stone or wall.
* Can accidentally help or hinder the player depending on positioning.

Implementation:

* Prefer deterministic placement.
* Avoid complex growth simulation initially.

---

# Local Floor Rules

Only one active rule at a time.

Implement as modular rule objects.

## Rule 1: Brittle Stone

When an actor is pushed into raised stone, the stone breaks and the actor takes 1 damage.

## Rule 2: Hot Marks

Marked actors take +1 damage from `spark`.

## Rule 3: Fading Marks

Marks expire at the start of the owner’s next turn.

Start with one rule only. Add others after preview and trace support them.

---

# Rules Create Situations, Not Tax

Local rules should create new tactical situations, not merely make the game arbitrarily harder.

A local rule is good when the player can exploit it.

The player should usually respond to a rule by thinking:

> How can I use this?

not merely:

> How do I compensate for this?

Rules may increase danger, but they must also create opportunity. They are local magical physics, not difficulty modifiers.

## Good Rule Pattern

A good local rule:

* changes how existing clauses behave;
* creates new board states;
* gives enemies new tactical meaning;
* gives the player new tactical leverage;
* is visible or previewable;
* can be explained in the omen trace;
* can be exploited by clever spell construction.

Example:

> Brittle Stone: when an actor is pushed into raised stone, the stone breaks and the actor takes 1 damage.

This is good because it creates both hazard and weapon. The player can be punished by it, but can also build around it with `raise stone` and `push them`.

## Bad Rule Pattern

A bad local rule only taxes the player.

Avoid rules like:

* enemies deal +1 damage;
* spells cost +1 focus;
* player movement is slower;
* all conditions are less reliable;
* random backlash chance increases;
* enemies get more health.

These can be difficulty modifiers, but they do not create interesting magical grammar by themselves.

## Design Test

Before adding a local rule, ask:

1. What new situation does this create?
2. How can the player exploit it?
3. How can enemies exploit it?
4. Which clauses become more interesting because this rule exists?
5. Can preview show the consequence?
6. Can the omen trace explain it?

If the answer is mostly “it makes things harder,” reject or redesign the rule.

---

# UI Requirements

## Spell Bar

* Show two prepared workings.
* Show focus cost.
* Show whether each working is currently castable.
* Hover/select to preview.

## Spell Editor

* Opens as a full-screen codex/workbench page, not as a floating panel over combat.
* Left side explains the available clauses and lets the player add them.
* Right side contains the node board, prepared slots, cast/preview actions, and full trace.
* Node-based board inspired by ComfyUI.
* Clauses are nodes with input and output ports.
* Ordinary clauses have a single flow output.
* Condition clauses expose true/false flow outputs.
* Add/remove/drag/connect clause nodes.
* Maximum practical graph size should remain small initially.
* Later increase to 6 or 7.
* Use readable clause text, not opcode names.
* Show tooltip for each clause.
* Do not require programming terminology.

## Spell Preview

Mandatory.

Preview feedback must remain visible even when the full editor page is closed.

Show:

* current target;
* affected actor/tile;
* mark placement;
* raised stone;
* damage;
* push direction;
* failed conditions;
* final result.

Preview must be produced by the same `WorkingMachine` used for real casting, but with world mutations simulated or rolled back.

## Omen Trace Panel

Show step-by-step textual explanation.

Example:

```text
1. Aimed at nearest foe: Glass Hound.
2. Checked "if marked": failed.
3. Followed "else".
4. Marked Glass Hound.
```

---

# Implementation Milestones

## Milestone 1 — Tactical Grid

Goal: move player and enemies on a grid.

Tasks:

* [x] Create `GridPos` and `Direction`.
* [x] Create `TacticalGrid`.
* [x] Add tile occupancy.
* [x] Add player scene.
* [x] Add basic movement.
* [x] Add one dummy enemy.
* [x] Add turn alternation.
* [x] Add win/loss checks.

Acceptance:

* Player moves one tile per turn.
* Enemy takes a simple turn.
* Occupancy prevents overlapping.
* Combat ends when player dies or all enemies die.

---

## Milestone 2 — Working Machine Skeleton

Goal: execute a simple working without UI polish.

Tasks:

* [x] Create `Working`.
* [x] Create `ClauseDefinition`.
* [x] Create `WorkingMachine`.
* [x] Create `WorkingContext`.
* [x] Create `OmenTrace`.
* [x] Create `ISpellWorld`.
* [x] Implement `aim at target`.
* [x] Implement `spark them`.

Acceptance:

* A hardcoded working can target and damage an enemy.
* The result includes an omen trace.
* The same machine can run in preview mode and cast mode.

---

## Milestone 3 — Conditions and Marks

Goal: support the first real spell grammar.

Tasks:

* [x] Implement marks.
* [x] Implement `aim at nearest foe`.
* [x] Implement `if marked`.
* [x] Replace `else` clause with condition false-output ports.
* [x] Implement `mark them`.
* [x] Implement trace output for passed/failed conditions.

Acceptance:

* `Mark-or-Spark` works.
* First cast marks the target.
* Second cast sparks the marked target.
* Trace clearly explains both casts.

---

## Milestone 4 — Spell Editor

Goal: allow player to assemble workings.

Tasks:

* [x] Create basic node-board spell editor UI.
* [x] Display available clauses.
* [x] Add clause to working.
* [x] Remove clause from working.
* [x] Drag clause nodes.
* [x] Connect clause nodes.
* [x] Enforce max graph size.
* [x] Save edits directly to one of two prepared slots.

Acceptance:

* Player can build `Mark-or-Spark`.
* Player can assign it to a spell slot.
* Player can cast it in combat.
* Editing takes only a few clicks.

---

## Milestone 5 — Preview and Omen Trace UI

Goal: make spell outcomes understandable before casting.

Tasks:

* [x] Add spell preview overlay.
* [x] Add omen trace panel.
* [x] Run `WorkingMachine` in preview mode.
* [x] Show damage, marks, raised stone, and failed clauses.
* [x] Add step-through trace if feasible.

Implementation note:

* Preview now runs against a cloned encounter and returns both the omen trace and forecast board state.
* The test room overlays predicted tile changes, marks, actor movement, damage, and failed previews on the board.
* Trace controls can reveal the omen trace one step at a time or jump back to the full trace.

Acceptance:

* Player can preview `Mark-or-Spark`.
* Player can see why a condition passed or failed.
* Preview matches actual cast result.
* Failed spells explain themselves.

---

## Milestone 6 — Push and Stone

Goal: support terrain manipulation.

Tasks:

* [x] Implement `raise stone`.
* [x] Implement `push them`.
* [x] Implement wall/stone collision.
* [x] Add `Brittle Stone` local rule.
* [x] Add trace events for push and collision.

Acceptance:

* `Emergency Wall` works.
* `Push Trap` works.
* Pushed enemies collide with stone.
* Brittle Stone rule modifies the result.
* Trace explains collision damage.

---

## Milestone 7 — First Enemy Set

Goal: make enemies pressure spell-building.

Tasks:

* [x] Implement Ash Scribe.
* [x] Implement Glass Hound.
* [x] Implement Root Saint.
* [x] Add enemy intent display.
* [x] Add simple enemy configs.

Acceptance:

* Ash Scribe makes marks matter.
* Glass Hound rewards push/terrain use.
* Root Saint makes stone placement tactically ambiguous.
* Player has reason to change or choose different workings.

---

## Milestone 8 — Playtest Pass

Goal: determine whether the core loop is fun.

Tasks:

* [x] Create one hand-authored test room.
* [x] Include all three enemy types.
* [x] Include two prepared slots.
* [x] Include one active local rule.
* [x] Add restart button.
* [x] Add simple debug logging.
* [x] Run repeated self-tests.

Implementation note:

* Self-tests cover `Mark-or-Spark`, `Emergency Wall`, `Push Trap`, Brittle Stone collision tracing, and preview/cast consistency.

Acceptance:

* At least five useful workings can be created.
* Player can understand failed workings from trace.
* Player modifies spells in response to enemies.
* No single spell dominates every encounter.
* Editing does not feel slower than combat.

---

# Testing Guidance

Where possible, write pure C# tests for the spell machine.

Prioritise tests for:

* targeting;
* mark application;
* condition pass/fail;
* else behavior;
* damage application;
* push collision;
* raised stone;
* local rule modification;
* preview/cast consistency.

Example test names:

```text
MarkOrSpark_FirstCast_MarksNearestEnemy
MarkOrSpark_SecondCast_DamagesMarkedEnemy
EmergencyWall_ClearTile_RaisesStone
EmergencyWall_OccupiedTile_FailsWithTrace
PushTrap_IntoWall_AppliesCollisionDamage
PreviewAndCast_ProduceSameResult
```

Testing the spell machine matters more than testing scene/UI code.

---

# Agentic Programming Rules

When implementing, work milestone by milestone.

For each milestone:

1. Inspect existing project structure.
2. Identify the smallest coherent change.
3. Add or update code.
4. Add tests if practical.
5. Run available checks.
6. Summarise what changed.
7. Stop before expanding scope.

Do not skip ahead.

Do not add content systems before the core loop works.

Do not introduce abstractions that are not needed by the current or next milestone.

Do not build a generalized RPG framework.

Keep graph-based spells compact and readable.

Do not add procedural generation until the hand-authored test room is fun.

Prefer boring, testable code over clever architecture.

---

# Current Priority

Start at Milestone 1 unless the repository already has equivalent grid movement and turn structure.

The first useful deliverable is:

> a player and dummy enemy taking turns on a tactical grid.

The second useful deliverable is:

> a hardcoded working that damages a target and produces an omen trace.

The third useful deliverable is:

> `Mark-or-Spark`, where the first cast marks and the second cast damages.

Everything else depends on those.
