using System;
using System.Collections.Generic;
using System.Linq;
using TheThingImDoing.Actors;
using TheThingImDoing.Core;
using TheThingImDoing.Spells;
using TheThingImDoing.World;

namespace TheThingImDoing.Behaviors;

public sealed class BehaviorMachine
{
    private const int MaxBehaviorSteps = 48;
    private const int MaxNestedBehaviorDepth = 8;

    public BehaviorExecutionResult Execute(string behaviorId, BehaviorExecutionContext context)
    {
        return ExecuteBehavior(behaviorId, context, depth: 0);
    }

    private static BehaviorExecutionResult ExecuteBehavior(string behaviorId, BehaviorExecutionContext context, int depth)
    {
        if (!BehaviorDefinitionCatalog.TryGet(behaviorId, out BehaviorDefinition? definition))
        {
            context.Trace.Add($"Behavior '{behaviorId}' was not found.");
            return new BehaviorExecutionResult(BehaviorFlow.Next, false);
        }

        return ExecuteBehavior(definition, context, depth);
    }

    private static BehaviorExecutionResult ExecuteBehavior(BehaviorDefinition definition, BehaviorExecutionContext context, int depth)
    {
        if (depth > MaxNestedBehaviorDepth)
        {
            context.Trace.Add($"Behavior '{definition.Id}' exceeded its nesting limit.");
            return new BehaviorExecutionResult(BehaviorFlow.Next, false);
        }

        Dictionary<int, BehaviorStepDefinition> stepsById = definition.Steps.ToDictionary(step => step.Id);
        int? currentStepId = definition.Steps.FirstOrDefault()?.Id;
        int steps = 0;
        bool changedWorld = false;

        while (currentStepId.HasValue)
        {
            if (++steps > MaxBehaviorSteps)
            {
                context.Trace.Add($"Behavior '{definition.Id}' exceeded its step limit.");
                return new BehaviorExecutionResult(BehaviorFlow.Next, changedWorld);
            }

            if (!stepsById.TryGetValue(currentStepId.Value, out BehaviorStepDefinition? step))
            {
                context.Trace.Add($"Behavior '{definition.Id}' referenced missing step {currentStepId.Value}.");
                return new BehaviorExecutionResult(BehaviorFlow.Next, changedWorld);
            }

            AtomResult atomResult = ExecuteAtom(step, context, depth);
            changedWorld |= atomResult.ChangedWorld;

            if (atomResult.Flow == BehaviorFlow.Stop)
            {
                return new BehaviorExecutionResult(BehaviorFlow.Next, changedWorld);
            }

            if (atomResult.Flow != BehaviorFlow.Next && step.True == null && step.False == null)
            {
                return new BehaviorExecutionResult(atomResult.Flow, changedWorld);
            }

            currentStepId = atomResult.Flow switch
            {
                BehaviorFlow.True => step.True ?? step.Next,
                BehaviorFlow.False => step.False ?? step.Next,
                _ => step.Next ?? GetImplicitNextStep(definition.Steps, step.Id)
            };
        }

        return new BehaviorExecutionResult(BehaviorFlow.Next, changedWorld);
    }

    private static int? GetImplicitNextStep(IReadOnlyList<BehaviorStepDefinition> steps, int currentStepId)
    {
        for (int i = 0; i < steps.Count - 1; i++)
        {
            if (steps[i].Id == currentStepId)
            {
                return steps[i + 1].Id;
            }
        }

        return null;
    }

    private static AtomResult ExecuteAtom(BehaviorStepDefinition step, BehaviorExecutionContext context, int depth)
    {
        return step.Op switch
        {
            "stop" => AtomResult.Stop(),
            "focus_selected_target" => FocusSelectedTarget(context),
            "focus_nearest_enemy" => FocusNearestEnemy(context),
            "branch_focus_marked" => Branch(IsFocusMarked(context), "if marked", context),
            "branch_focus_occupied" => Branch(IsFocusOccupied(context), "if occupied", context),
            "branch_focus_clear" => Branch(IsFocusClear(context), "if clear", context),
            "mark_focus" => MarkFocus(context),
            "damage_focus_actor" => DamageFocusActor(step.Amount ?? 1, context),
            "raise_stone_focus" => RaiseStoneFocus(context),
            "push_focus" => PushFocus(step.Amount ?? 1, context),
            "remember_focus" => RememberFocus(context),
            "return_to_remembered" => ReturnToRemembered(context),
            "branch_enemy_adjacent_player" => BranchEnemyAdjacentPlayer(context),
            "damage_player" => DamagePlayer(step.Amount ?? 1, context),
            "step_toward_player" => StepTowardPlayer(context),
            "increment_counter" => IncrementCounter(context),
            "branch_counter_at_least" => BranchCounterAtLeast(step.Amount ?? 1, context),
            "detonate_owned_tile_marks" => DetonateOwnedTileMarks(step.Amount ?? 1, context),
            "reset_counter" => ResetCounter(context),
            "mark_player_tile" => MarkPlayerTile(context),
            "heal_self_if_adjacent_blocking" => HealSelfIfAdjacentBlocking(step.Amount ?? 1, context),
            "raise_adjacent_stone" => RaiseAdjacentStone(context),
            "branch_event_tile_raised_stone" => BranchEventTileRaisedStone(context),
            "break_event_tile" => BreakEventTile(context),
            "damage_event_actor" => DamageEventActor(step.Amount ?? 1, context),
            _ => UnknownAtom(step.Op, context, depth)
        };
    }

    private static AtomResult FocusSelectedTarget(BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null)
        {
            return AtomResult.Next();
        }

        context.Working.FocusTile = context.Working.SelectedTarget;
        context.Working.FocusActorId = context.SpellWorld.GetActorAt(context.Working.SelectedTarget)?.Id;
        context.Trace.Add(context.Working.FocusActorId.HasValue
            ? $"Aimed at selected target: actor {context.Working.FocusActorId.Value}."
            : $"Aimed at selected tile {context.Working.SelectedTarget}.");
        return AtomResult.Next();
    }

    private static AtomResult FocusNearestEnemy(BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null || context.Caster == null)
        {
            return AtomResult.Next();
        }

        EncounterActor? foe = context.SpellWorld.GetNearestEnemy(context.Caster);

        if (foe == null)
        {
            context.Working.FocusActorId = null;
            context.Working.FocusTile = null;
            context.Trace.Add("Aimed at nearest foe, but there were no foes.");
            return AtomResult.Next();
        }

        context.Working.FocusActorId = foe.Id;
        context.Working.FocusTile = foe.Position;
        context.Trace.Add($"Aimed at nearest foe: enemy {foe.Id}.");
        return AtomResult.Next();
    }

    private static AtomResult Branch(bool passed, string label, BehaviorExecutionContext context)
    {
        context.Trace.Add($"Checked \"{label}\" on {DescribeFocus(context)}: {(passed ? "passed" : "failed")}.");
        return new AtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static AtomResult MarkFocus(BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null || context.Caster == null)
        {
            return AtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);

        if (target != null)
        {
            context.SpellWorld.AddMark(target, context.Caster.Id);
            context.Trace.Add($"Marked enemy {target.Id}.");
            return AtomResult.Next(changedWorld: true);
        }

        if (context.Working.FocusTile.HasValue && context.SpellWorld.IsInside(context.Working.FocusTile.Value))
        {
            context.SpellWorld.AddMark(context.Working.FocusTile.Value, context.Caster.Id);
            context.Trace.Add($"Marked tile {context.Working.FocusTile.Value}.");
            return AtomResult.Next(changedWorld: true);
        }

        context.Trace.Add("Tried to mark, but nothing was focused.");
        return AtomResult.Next();
    }

    private static AtomResult DamageFocusActor(int amount, BehaviorExecutionContext context)
    {
        if (context.SpellWorld == null)
        {
            return AtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);

        if (target == null)
        {
            context.Trace.Add("Damage found no focused actor.");
            return AtomResult.Next();
        }

        context.SpellWorld.ApplyDamage(target, amount, context.Caster);
        context.Trace.Add($"Damaged enemy {target.Id} for {amount}.");
        return AtomResult.Next(changedWorld: true);
    }

    private static AtomResult RaiseStoneFocus(BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null)
        {
            return AtomResult.Next();
        }

        if (!context.Working.FocusTile.HasValue)
        {
            context.Trace.Add("Raise stone failed because no tile was focused.");
            return AtomResult.Next();
        }

        if (!context.SpellWorld.CanRaiseStone(context.Working.FocusTile.Value))
        {
            context.Trace.Add($"Raise stone failed at {context.Working.FocusTile.Value}.");
            return AtomResult.Next();
        }

        context.SpellWorld.RaiseStone(context.Working.FocusTile.Value);
        context.Trace.Add($"Raised stone at {context.Working.FocusTile.Value}.");
        return AtomResult.Next(changedWorld: true);
    }

    private static AtomResult PushFocus(int distance, BehaviorExecutionContext context)
    {
        if (context.SpellWorld == null || context.Caster == null)
        {
            return AtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);

        if (target == null)
        {
            context.Trace.Add("Push found no focused actor.");
            return AtomResult.Next();
        }

        Direction direction = GetDirectionAwayFrom(context.Caster.Position, target.Position);
        bool pushed = context.SpellWorld.TryPushActor(target, direction, distance, context.Caster);
        context.Trace.Add(pushed
            ? $"Pushed enemy {target.Id} {direction.ToString().ToLowerInvariant()}."
            : $"Push against enemy {target.Id} failed.");
        return AtomResult.Next(pushed);
    }

    private static AtomResult RememberFocus(BehaviorExecutionContext context)
    {
        if (context.Working == null)
        {
            return AtomResult.Next();
        }

        context.Working.RememberedActorId = context.Working.FocusActorId;
        context.Working.RememberedTile = context.Working.FocusTile;
        context.Trace.Add(context.Working.RememberedActorId.HasValue
            ? $"Remembered actor {context.Working.RememberedActorId.Value}."
            : context.Working.RememberedTile.HasValue
                ? $"Remembered tile {context.Working.RememberedTile.Value}."
                : "Tried to remember, but nothing was focused.");
        return AtomResult.Next();
    }

    private static AtomResult ReturnToRemembered(BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null)
        {
            return AtomResult.Next();
        }

        if (context.Working.RememberedActorId.HasValue)
        {
            EncounterActor? actor = context.SpellWorld.GetActor(context.Working.RememberedActorId.Value);
            if (actor != null && actor.IsAlive)
            {
                context.Working.FocusActorId = actor.Id;
                context.Working.FocusTile = actor.Position;
                context.Trace.Add($"Returned focus to remembered actor {actor.Id}.");
                return AtomResult.Next();
            }
        }

        if (context.Working.RememberedTile.HasValue)
        {
            context.Working.FocusActorId = context.SpellWorld.GetActorAt(context.Working.RememberedTile.Value)?.Id;
            context.Working.FocusTile = context.Working.RememberedTile;
            context.Trace.Add($"Returned focus to remembered tile {context.Working.RememberedTile.Value}.");
            return AtomResult.Next();
        }

        context.Trace.Add("Return failed because no sign was remembered.");
        return AtomResult.Next();
    }

    private static AtomResult BranchEnemyAdjacentPlayer(BehaviorExecutionContext context)
    {
        bool adjacent = context.Enemy != null
            && context.Encounter != null
            && context.Enemy.Position.ManhattanDistanceTo(context.Encounter.Player.Position) == 1;
        return new AtomResult(adjacent ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static AtomResult DamagePlayer(int amount, BehaviorExecutionContext context)
    {
        context.Encounter?.Player.ApplyDamage(amount);
        return AtomResult.Next(changedWorld: amount > 0);
    }

    private static AtomResult StepTowardPlayer(BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return AtomResult.Next();
        }

        foreach (Direction direction in GetDirectionsToward(context.Enemy.Position, context.Encounter.Player.Position))
        {
            if (context.Encounter.TryMoveActor(context.Enemy, direction))
            {
                return AtomResult.Next(changedWorld: true);
            }
        }

        return AtomResult.Next();
    }

    private static AtomResult IncrementCounter(BehaviorExecutionContext context)
    {
        if (context.Enemy != null)
        {
            context.Enemy.BrainCounter++;
        }

        return AtomResult.Next();
    }

    private static AtomResult BranchCounterAtLeast(int amount, BehaviorExecutionContext context)
    {
        bool passed = context.Enemy != null && context.Enemy.BrainCounter >= amount;
        return new AtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static AtomResult DetonateOwnedTileMarks(int amount, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return AtomResult.Next();
        }

        bool changed = false;

        foreach (TileMark mark in context.Encounter.TileMarks.Where(mark => mark.OwnerActorId == context.Enemy.Id).ToArray())
        {
            EncounterActor? actor = context.Encounter.GetActorAt(mark.Position);

            if (actor != null && actor.Faction == Faction.Player)
            {
                actor.ApplyDamage(amount);
                changed = true;
            }

            context.Encounter.RemoveTileMark(mark.Position, context.Enemy.Id);
        }

        return AtomResult.Next(changed);
    }

    private static AtomResult ResetCounter(BehaviorExecutionContext context)
    {
        if (context.Enemy != null)
        {
            context.Enemy.BrainCounter = 0;
        }

        return AtomResult.Next();
    }

    private static AtomResult MarkPlayerTile(BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return AtomResult.Next();
        }

        context.Encounter.AddTileMark(context.Encounter.Player.Position, context.Enemy.Id);
        return AtomResult.Next(changedWorld: true);
    }

    private static AtomResult HealSelfIfAdjacentBlocking(int amount, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return AtomResult.Next();
        }

        if (!context.Encounter.IsAdjacentToBlockingTile(context.Enemy.Position))
        {
            return AtomResult.Next();
        }

        int before = context.Enemy.Health;
        context.Enemy.Heal(amount);
        return AtomResult.Next(context.Enemy.Health != before);
    }

    private static AtomResult RaiseAdjacentStone(BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return AtomResult.Next();
        }

        foreach (GridPos adjacent in TacticalEncounter.GetAdjacentPositions(context.Enemy.Position))
        {
            if (context.Encounter.TryRaiseStone(adjacent))
            {
                return AtomResult.Next(changedWorld: true);
            }
        }

        return AtomResult.Next();
    }

    private static AtomResult BranchEventTileRaisedStone(BehaviorExecutionContext context)
    {
        bool passed = context.Encounter != null
            && context.EventTile.HasValue
            && context.Encounter.Grid.IsInside(context.EventTile.Value)
            && context.Encounter.Grid.GetTile(context.EventTile.Value) == TileState.RaisedStone;
        return new AtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static AtomResult BreakEventTile(BehaviorExecutionContext context)
    {
        if (context.Encounter == null
            || !context.EventTile.HasValue
            || !context.Encounter.Grid.IsInside(context.EventTile.Value))
        {
            return AtomResult.Next();
        }

        context.Encounter.Grid.SetTile(context.EventTile.Value, TileState.Floor);
        return AtomResult.Next(changedWorld: true);
    }

    private static AtomResult DamageEventActor(int amount, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.EventActor == null)
        {
            return AtomResult.Next();
        }

        context.EventActor.ApplyDamage(amount);

        if (!context.EventActor.IsAlive)
        {
            context.Encounter.Grid.RemoveActor(context.EventActor.Id);
        }

        return AtomResult.Next(changedWorld: true);
    }

    private static AtomResult UnknownAtom(string op, BehaviorExecutionContext context, int depth)
    {
        if (BehaviorPrimitiveCatalog.TryGet(op, out BehaviorPrimitiveDefinition? primitive)
            && !string.IsNullOrWhiteSpace(primitive.BehaviorId))
        {
            BehaviorExecutionResult result = ExecuteBehavior(primitive.BehaviorId, context, depth + 1);
            return new AtomResult(result.Flow, result.ChangedWorld);
        }

        context.Trace.Add($"Unknown behavior atom '{op}'.");
        return AtomResult.Next();
    }

    private static bool IsFocusMarked(BehaviorExecutionContext context)
    {
        if (context.SpellWorld == null || context.Working == null || context.Caster == null)
        {
            return false;
        }

        EncounterActor? focusActor = GetFocusActor(context);

        if (focusActor != null)
        {
            return context.SpellWorld.HasMark(focusActor, context.Caster.Id);
        }

        return context.Working.FocusTile.HasValue && context.SpellWorld.HasMark(context.Working.FocusTile.Value, context.Caster.Id);
    }

    private static bool IsFocusOccupied(BehaviorExecutionContext context)
    {
        return GetFocusActor(context) != null
            || (context.Working?.FocusTile.HasValue == true && context.SpellWorld?.IsOccupied(context.Working.FocusTile.Value) == true);
    }

    private static bool IsFocusClear(BehaviorExecutionContext context)
    {
        return context.Working?.FocusTile.HasValue == true
            && context.SpellWorld?.IsClear(context.Working.FocusTile.Value) == true;
    }

    private static EncounterActor? GetFocusActor(BehaviorExecutionContext context)
    {
        if (context.SpellWorld == null || context.Working == null)
        {
            return null;
        }

        if (context.Working.FocusActorId.HasValue)
        {
            EncounterActor? actor = context.SpellWorld.GetActor(context.Working.FocusActorId.Value);

            if (actor != null && actor.IsAlive)
            {
                return actor;
            }
        }

        return context.Working.FocusTile.HasValue ? context.SpellWorld.GetActorAt(context.Working.FocusTile.Value) : null;
    }

    private static string DescribeFocus(BehaviorExecutionContext context)
    {
        EncounterActor? actor = GetFocusActor(context);

        if (actor != null)
        {
            return $"enemy {actor.Id}";
        }

        return context.Working?.FocusTile.HasValue == true ? $"tile {context.Working.FocusTile.Value}" : "nothing";
    }

    private static Direction GetDirectionAwayFrom(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return dx >= 0 ? Direction.East : Direction.West;
        }

        return dy >= 0 ? Direction.South : Direction.North;
    }

    private static IEnumerable<Direction> GetDirectionsToward(GridPos from, GridPos to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            if (dx > 0)
            {
                yield return Direction.East;
            }
            else if (dx < 0)
            {
                yield return Direction.West;
            }

            if (dy > 0)
            {
                yield return Direction.South;
            }
            else if (dy < 0)
            {
                yield return Direction.North;
            }
        }
        else
        {
            if (dy > 0)
            {
                yield return Direction.South;
            }
            else if (dy < 0)
            {
                yield return Direction.North;
            }

            if (dx > 0)
            {
                yield return Direction.East;
            }
            else if (dx < 0)
            {
                yield return Direction.West;
            }
        }
    }

    private readonly record struct AtomResult(BehaviorFlow Flow, bool ChangedWorld)
    {
        public static AtomResult Next(bool changedWorld = false)
        {
            return new AtomResult(BehaviorFlow.Next, changedWorld);
        }

        public static AtomResult Stop(bool changedWorld = false)
        {
            return new AtomResult(BehaviorFlow.Stop, changedWorld);
        }
    }
}
