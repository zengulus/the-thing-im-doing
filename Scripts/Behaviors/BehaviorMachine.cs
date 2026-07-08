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
    private static readonly Lazy<BehaviorAtomRegistry> DefaultAtomRegistry = new(CreateDefaultAtomRegistry);

    private readonly BehaviorAtomRegistry _atomRegistry;

    public BehaviorMachine()
        : this(DefaultAtomRegistry.Value)
    {
    }

    public BehaviorMachine(BehaviorAtomRegistry atomRegistry)
    {
        _atomRegistry = atomRegistry;
    }

    public BehaviorExecutionResult Execute(string behaviorId, BehaviorExecutionContext context)
    {
        return ExecuteBehavior(behaviorId, context, depth: 0);
    }

    public BehaviorExecutionResult Execute(BehaviorDefinition definition, BehaviorExecutionContext context)
    {
        return ExecuteBehavior(definition, context, depth: 0);
    }

    private BehaviorExecutionResult ExecuteBehavior(string behaviorId, BehaviorExecutionContext context, int depth)
    {
        if (!BehaviorDefinitionCatalog.TryGet(behaviorId, out BehaviorDefinition? definition))
        {
            context.Trace.Add($"Behavior '{behaviorId}' was not found.");
            return new BehaviorExecutionResult(BehaviorFlow.Next, false);
        }

        return ExecuteBehavior(definition, context, depth);
    }

    private BehaviorExecutionResult ExecuteBehavior(BehaviorDefinition definition, BehaviorExecutionContext context, int depth)
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

            BehaviorAtomResult atomResult = ExecuteAtom(step, context, depth);
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

    private static BehaviorAtomRegistry CreateDefaultAtomRegistry()
    {
        return new BehaviorAtomRegistry(new Dictionary<string, BehaviorAtomExecutor>(StringComparer.Ordinal)
        {
            ["flow.stop"] = (_, _) => BehaviorAtomResult.Stop(),
            ["focus.selected_target"] = (_, context) => FocusSelectedTarget(context),
            ["focus.nearest_actor"] = (step, context) => FocusNearestActor(step.Relation, context),
            ["focus.self"] = (_, context) => FocusSelf(context),
            ["focus.store_ref"] = (step, context) => StoreFocusRef(step.Ref, context),
            ["focus.ref"] = (step, context) => FocusRef(step.Ref, context),
            ["branch.occupied"] = (step, context) => Branch(IsOccupied(step.Target, context), "if occupied", context),
            ["branch.clear"] = (step, context) => Branch(IsClear(step.Target, context), "if clear", context),
            ["branch.adjacent"] = (step, context) => BranchAdjacent(step, context),
            ["branch.relation"] = (step, context) => BranchRelation(step, context),
            ["branch.tile_state"] = (step, context) => BranchTileState(step, context),
            ["branch.adjacent_blocking"] = (step, context) => BranchAdjacentBlocking(step, context),
            ["counter.add"] = (step, context) => AddCounter(step, context),
            ["counter.consume"] = (step, context) => ConsumeCounter(step, context),
            ["counter.at_least"] = (step, context) => BranchCounterAtLeast(step, context),
            ["damage.apply"] = (step, context) => Damage(step, context),
            ["damage.prevent_event"] = (_, context) => PreventEventDamage(context),
            ["effect.attach"] = (step, context) => AttachEffect(step, context),
            ["effect.detach"] = (step, context) => DetachEffect(step, context),
            ["effect.has"] = (step, context) => BranchEffectAttached(step, context),
            ["effect.damage_actors_on_owned_tiles"] = (step, context) => DamageActorsOnOwnedTiles(step.Effect, step.Amount ?? 1, context),
            ["effect.clear_owned_tiles"] = (step, context) => ClearOwnedTileEffects(step.Effect, context),
            ["move.relative"] = (step, context) => MoveRelative(step, context),
            ["move.step_toward"] = (step, context) => StepToward(step, context),
            ["tile.set_state"] = (step, context) => SetTileState(step, context),
            ["tile.set_first_adjacent_clear_state"] = (step, context) => SetFirstAdjacentClearTileState(step, context),
            ["heal.apply"] = (step, context) => Heal(step, context)
        });
    }

    private BehaviorAtomResult ExecuteAtom(BehaviorStepDefinition step, BehaviorExecutionContext context, int depth)
    {
        return _atomRegistry.TryExecute(step, context, out BehaviorAtomResult result)
            ? result
            : UnknownAtom(step.Op, context, depth);
    }

    private static BehaviorAtomResult FocusSelectedTarget(BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null)
        {
            return BehaviorAtomResult.Next();
        }

        context.Working.FocusTile = context.Working.SelectedTarget;
        context.Working.FocusActorId = context.SpellWorld.GetActorAt(context.Working.SelectedTarget)?.Id;
        context.Trace.Add(context.Working.FocusActorId.HasValue
            ? $"Aimed at selected target: actor {context.Working.FocusActorId.Value}."
            : $"Aimed at selected tile {context.Working.SelectedTarget}.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult FocusNearestActor(string relation, BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null || context.Caster == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? actor = context.SpellWorld.GetNearestActor(context.Caster, string.IsNullOrWhiteSpace(relation) ? "hostile" : relation);

        if (actor == null)
        {
            context.Working.FocusActorId = null;
            context.Working.FocusTile = null;
            context.Trace.Add($"Aimed at nearest {relation}, but none was found.");
            return BehaviorAtomResult.Next();
        }

        context.Working.FocusActorId = actor.Id;
        context.Working.FocusTile = actor.Position;
        context.Trace.Add($"Aimed at nearest {relation}: actor {actor.Id}.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult FocusSelf(BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);

        if (context.Working == null || self == null)
        {
            return BehaviorAtomResult.Next();
        }

        context.Working.FocusActorId = self.Id;
        context.Working.FocusTile = self.Position;
        context.Trace.Add($"Focused self: actor {self.Id}.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult Branch(bool passed, string label, BehaviorExecutionContext context)
    {
        context.Trace.Add($"Checked \"{label}\" on {DescribeFocus(context)}: {(passed ? "passed" : "failed")}.");
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult AddCounter(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        return ModifyCounter(step, context, step.Amount ?? 1, requireAvailable: false);
    }

    private static BehaviorAtomResult ConsumeCounter(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        return ModifyCounter(step, context, -(step.Amount ?? 1), requireAvailable: true);
    }

    private static BehaviorAtomResult ModifyCounter(
        BehaviorStepDefinition step,
        BehaviorExecutionContext context,
        int amount,
        bool requireAvailable)
    {
        if (string.IsNullOrWhiteSpace(step.Counter) || !TrySelectCounterTarget(step, context, out CounterTarget target))
        {
            return requireAvailable ? new BehaviorAtomResult(BehaviorFlow.False, false) : BehaviorAtomResult.Next();
        }

        int before = target.Get(step.Counter, context);

        if (requireAvailable && before < -amount)
        {
            context.Trace.Add($"Tried to consume {step.Counter}, but {step.Target} did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        EffectCommandResult result = target.Modify(step.Counter, amount, context);

        if (target.Actor != null && context.Working != null)
        {
            EncounterActor caster = context.Caster ?? target.Actor;
            context.Working.RecordCounterMutation(caster, target.Actor, step.Counter, amount);
        }

        context.Trace.Add($"Set {step.Counter} on {target.Label} to {result.CounterValue}.");
        return new BehaviorAtomResult(requireAvailable ? BehaviorFlow.True : BehaviorFlow.Next, amount != 0);
    }

    private static BehaviorAtomResult BranchCounterAtLeast(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        bool passed = !string.IsNullOrWhiteSpace(step.Counter)
            && TrySelectCounterTarget(step, context, out CounterTarget target)
            && target.Get(step.Counter, context) >= (step.Amount ?? 1);

        return Branch(passed, $"{step.Counter} at least {step.Amount ?? 1}", context);
    }

    private static BehaviorAtomResult Damage(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? target = SelectActor(step.Target, context);

        if (target == null)
        {
            context.Trace.Add("Damage found no target actor.");
            return BehaviorAtomResult.Next();
        }

        int amount = ResolveAmount(step, context);

        if (amount <= 0)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? source = SelectActor(step.Source, context);
        EffectCommandResult result = context.Resolve(new DamageActorCommand(target.Id, amount, source?.Id));
        context.Trace.Add($"Damaged actor {target.Id} for {amount}.");
        return BehaviorAtomResult.Next(result.ChangedWorld);
    }

    private static BehaviorAtomResult AttachEffect(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(step.Effect))
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? owner = SelectActor(DefaultIfBlank(step.Source, "self"), context);

        if (owner == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? actor = SelectActor(step.Target, context);

        if (actor != null)
        {
            EffectCommandResult result = context.Resolve(new AttachActorEffectCommand(
                actor.Id,
                step.Effect,
                owner.Id,
                step.Amount ?? 0));
            context.Trace.Add($"Attached {step.Effect} to actor {actor.Id}.");
            return BehaviorAtomResult.Next(result.ChangedWorld);
        }

        GridPos? tile = SelectTile(step.Target, context);

        if (tile.HasValue)
        {
            EffectCommandResult result = context.Resolve(new AttachTileEffectCommand(
                tile.Value,
                step.Effect,
                owner.Id,
                step.Amount ?? 0));
            context.Trace.Add($"Attached {step.Effect} to tile {tile.Value}.");
            return BehaviorAtomResult.Next(result.ChangedWorld);
        }

        context.Trace.Add($"Tried to attach {step.Effect}, but no target was selected.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult DetachEffect(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EffectInstance? effect = SelectEffect(step.Target, step.Effect, context);

        if (effect == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? actor = SelectActor(DefaultIfBlank(step.Source, "effect.target"), context);

        if (actor != null)
        {
            return BehaviorAtomResult.Next(context.Resolve(new DetachActorEffectCommand(actor.Id, effect.InstanceId)).ChangedWorld);
        }

        GridPos? tile = SelectTile(step.Source, context);
        return tile.HasValue
            ? BehaviorAtomResult.Next(context.Resolve(new RemoveTileEffectCommand(tile.Value, effect.InstanceId)).ChangedWorld)
            : BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult BranchEffectAttached(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? owner = SelectActor(DefaultIfBlank(step.Source, "self"), context);

        if (owner == null || string.IsNullOrWhiteSpace(step.Effect))
        {
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        EncounterActor? actor = SelectActor(step.Target, context);

        if (actor != null)
        {
            bool passed = context.SpellWorld?.HasEffect(actor, step.Effect, owner)
                ?? actor.FindEffect(step.Effect, owner.Id) != null;
            return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
        }

        GridPos? tile = SelectTile(step.Target, context);
        bool tilePassed = tile.HasValue && context.SpellWorld?.HasEffect(tile.Value, step.Effect, owner) == true;
        return new BehaviorAtomResult(tilePassed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult SetTileState(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        GridPos? tile = SelectTile(step.Target, context);

        if (!tile.HasValue || !TryParseTileState(step.State, out TileState state))
        {
            return BehaviorAtomResult.Next();
        }

        EffectCommandResult result = context.Resolve(new SetTileStateCommand(tile.Value, state));
        context.Trace.Add($"Set tile {tile.Value} to {state}.");
        return BehaviorAtomResult.Next(result.ChangedWorld);
    }

    private static BehaviorAtomResult MoveRelative(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? target = SelectActor(step.Target, context);
        EncounterActor? source = SelectActor(DefaultIfBlank(step.Source, "self"), context);

        if (target == null || source == null)
        {
            return BehaviorAtomResult.Next();
        }

        bool toward = step.Mode == "toward";
        Direction direction = toward
            ? GetDirectionAwayFrom(target.Position, source.Position)
            : GetDirectionAwayFrom(source.Position, target.Position);
        EffectCommandResult result = context.Resolve(new PushActorCommand(target.Id, direction, step.Amount ?? 1, source.Id));
        context.Trace.Add(result.ChangedWorld
            ? $"Moved actor {target.Id} {direction.ToString().ToLowerInvariant()}."
            : $"Forced movement against actor {target.Id} failed.");
        return BehaviorAtomResult.Next(result.ChangedWorld);
    }

    private static BehaviorAtomResult StepToward(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        if (context.Encounter == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? actor = SelectActor(DefaultIfBlank(step.Target, "self"), context);
        GridPos? destination = SelectTile(DefaultIfBlank(step.Source, "focus.tile"), context);

        if (actor == null || !destination.HasValue)
        {
            return BehaviorAtomResult.Next();
        }

        foreach (Direction direction in GetDirectionsToward(actor.Position, destination.Value))
        {
            if (context.Encounter.TryMoveActor(actor, direction))
            {
                return BehaviorAtomResult.Next(changedWorld: true);
            }
        }

        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult BranchAdjacent(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? source = SelectActor(DefaultIfBlank(step.Source, "self"), context);
        EncounterActor? target = SelectActor(step.Target, context);
        bool adjacent = source != null && target != null && source.Position.ManhattanDistanceTo(target.Position) == 1;
        return new BehaviorAtomResult(adjacent ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult BranchRelation(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? source = SelectActor(DefaultIfBlank(step.Source, "self"), context);
        EncounterActor? target = SelectActor(step.Target, context);
        bool passed = source != null
            && target != null
            && (step.Relation switch
            {
                "hostile" or "enemy" => context.Encounter?.IsHostile(source, target) == true,
                "ally" => context.Encounter?.IsAlly(source, target) == true,
                "self" => source.Id == target.Id,
                "any" or "" => source.Id != target.Id,
                _ => false
            });
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult BranchTileState(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        GridPos? tile = SelectTile(step.Target, context);
        TryParseTileState(step.State, out TileState state);
        bool passed = tile.HasValue
            && context.Encounter != null
            && context.Encounter.Grid.IsInside(tile.Value)
            && context.Encounter.Grid.GetTile(tile.Value) == state;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult BranchAdjacentBlocking(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? actor = SelectActor(DefaultIfBlank(step.Target, "self"), context);
        bool passed = actor != null && context.Encounter?.IsAdjacentToBlockingTile(actor.Position) == true;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult Heal(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        EncounterActor? actor = SelectActor(DefaultIfBlank(step.Target, "self"), context);

        if (actor == null)
        {
            return BehaviorAtomResult.Next();
        }

        int before = actor.Health;
        actor.Heal(step.Amount ?? 1);
        return BehaviorAtomResult.Next(actor.Health != before);
    }

    private static BehaviorAtomResult SetFirstAdjacentClearTileState(
        BehaviorStepDefinition step,
        BehaviorExecutionContext context)
    {
        if (context.Encounter == null || !TryParseTileState(step.State, out TileState state))
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? actor = SelectActor(DefaultIfBlank(step.Target, "self"), context);

        if (actor == null)
        {
            return BehaviorAtomResult.Next();
        }

        foreach (GridPos adjacent in TacticalEncounter.GetAdjacentPositions(actor.Position))
        {
            if (context.Encounter.Grid.IsEmpty(adjacent))
            {
                context.Encounter.Grid.SetTile(adjacent, state);
                return BehaviorAtomResult.Next(changedWorld: true);
            }
        }

        return BehaviorAtomResult.Next();
    }

    private static bool IsOccupied(string targetId, BehaviorExecutionContext context)
    {
        EncounterActor? actor = SelectActor(targetId, context);

        if (actor != null)
        {
            return true;
        }

        GridPos? tile = SelectTile(targetId, context);
        return tile.HasValue && context.SpellWorld?.IsOccupied(tile.Value) == true;
    }

    private static bool IsClear(string targetId, BehaviorExecutionContext context)
    {
        GridPos? tile = SelectTile(targetId, context);
        return tile.HasValue && context.SpellWorld?.IsClear(tile.Value) == true;
    }

    private static EncounterActor? SelectActor(string targetId, BehaviorExecutionContext context)
    {
        return DefaultIfBlank(targetId, "focus.actor") switch
        {
            "focus" or "focus.actor" => GetFocusActor(context),
            "caster" => context.Caster,
            "self" => GetSelf(context),
            "enemy" => context.Enemy,
            "event.actor" => context.EventActor,
            "effect.target" => context.EffectTarget,
            _ => null
        };
    }

    private static GridPos? SelectTile(string targetId, BehaviorExecutionContext context)
    {
        return DefaultIfBlank(targetId, "focus.tile") switch
        {
            "focus" or "focus.tile" => context.Working?.FocusTile,
            "selected.target" => context.Working?.SelectedTarget,
            "event.tile" => context.EventTile,
            "self.tile" => SelectActor("self", context)?.Position,
            "caster.tile" => context.Caster?.Position,
            "effect.target.tile" => context.EffectTarget?.Position,
            _ => null
        };
    }

    private static EffectInstance? SelectEffect(
        string targetId,
        string effectId,
        BehaviorExecutionContext context)
    {
        return DefaultIfBlank(targetId, "effect") switch
        {
            "effect" or "current.effect" => context.Effect,
            "focus.effect" => SelectActor("focus.actor", context)?.FindEffect(effectId),
            _ => null
        };
    }

    private static bool TrySelectCounterTarget(
        BehaviorStepDefinition step,
        BehaviorExecutionContext context,
        out CounterTarget target)
    {
        string targetId = DefaultIfBlank(step.Target, "focus.actor");

        if (targetId is "effect" or "current.effect" or "focus.effect")
        {
            EffectInstance? effect = SelectEffect(targetId, step.Effect, context);

            if (effect != null && DoesEffectAllowCounter(effect, step.Counter))
            {
                target = CounterTarget.ForEffect(effect);
                return true;
            }
        }

        GridPos? tile = SelectTile(targetId, context);

        if (tile.HasValue)
        {
            target = CounterTarget.ForTile(tile.Value);
            return true;
        }

        EncounterActor? actor = SelectActor(targetId, context);

        if (actor != null)
        {
            target = CounterTarget.ForActor(actor);
            return true;
        }

        target = default;
        return false;
    }

    private static int ResolveAmount(BehaviorStepDefinition step, BehaviorExecutionContext context)
    {
        if (!string.IsNullOrWhiteSpace(step.Counter))
        {
            var counterStep = step with { Target = DefaultIfBlank(step.Source, "effect") };

            if (TrySelectCounterTarget(counterStep, context, out CounterTarget target))
            {
                return target.Get(step.Counter, context);
            }
        }

        return step.Amount ?? 1;
    }

    private static string DefaultIfBlank(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private readonly record struct CounterTarget(EncounterActor? Actor, GridPos? Tile, EffectInstance? Effect, string Label)
    {
        public static CounterTarget ForActor(EncounterActor actor)
        {
            return new CounterTarget(actor, null, null, $"actor {actor.Id}");
        }

        public static CounterTarget ForTile(GridPos tile)
        {
            return new CounterTarget(null, tile, null, $"tile {tile}");
        }

        public static CounterTarget ForEffect(EffectInstance effect)
        {
            return new CounterTarget(null, null, effect, $"effect {effect.EffectId}");
        }

        public int Get(string counterId, BehaviorExecutionContext context)
        {
            if (Actor != null)
            {
                return context.SpellWorld?.GetCounter(Actor, counterId) ?? Actor.Counters.Get(counterId);
            }

            if (Tile.HasValue)
            {
                return context.SpellWorld?.GetCounter(Tile.Value, counterId)
                    ?? context.Encounter?.GetTileCounter(Tile.Value, counterId)
                    ?? 0;
            }

            return Effect?.Counters.Get(counterId) ?? 0;
        }

        public EffectCommandResult Modify(string counterId, int amount, BehaviorExecutionContext context)
        {
            if (Actor != null)
            {
                return context.Resolve(new ModifyActorCounterCommand(Actor.Id, counterId, amount, context.Caster?.Id));
            }

            if (Tile.HasValue)
            {
                return context.Resolve(new ModifyTileCounterCommand(Tile.Value, counterId, amount));
            }

            return Effect != null
                ? context.Resolve(new ModifyEffectCounterCommand(Effect, counterId, amount))
                : EffectCommandResult.NoChange;
        }
    }

    private static BehaviorAtomResult StoreFocusRef(string refId, BehaviorExecutionContext context)
    {
        if (context.Working == null)
        {
            return BehaviorAtomResult.Next();
        }

        context.Working.StoreReference(refId);
        context.Trace.Add($"Stored focus in {refId}.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult FocusRef(string refId, BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null)
        {
            return BehaviorAtomResult.Next();
        }

        if (!context.Working.TryGetReference(refId, out WorkingReference reference))
        {
            context.Trace.Add($"Focus ref failed because {refId} was empty.");
            return BehaviorAtomResult.Next();
        }

        if (reference.ActorId.HasValue)
        {
            EncounterActor? actor = context.SpellWorld.GetActor(reference.ActorId.Value);
            if (actor != null && actor.IsAlive)
            {
                context.Working.FocusActorId = actor.Id;
                context.Working.FocusTile = actor.Position;
                context.Trace.Add($"Focused {refId}: actor {actor.Id}.");
                return BehaviorAtomResult.Next();
            }
        }

        if (reference.Tile.HasValue)
        {
            context.Working.FocusActorId = context.SpellWorld.GetActorAt(reference.Tile.Value)?.Id;
            context.Working.FocusTile = reference.Tile;
            context.Trace.Add($"Focused {refId}: tile {reference.Tile.Value}.");
            return BehaviorAtomResult.Next();
        }

        context.Trace.Add($"Focus ref failed because {refId} held nothing.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult DamageActorsOnOwnedTiles(string effectId, int amount, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return BehaviorAtomResult.Next();
        }

        bool changed = false;

        foreach (TileCondition condition in context.Encounter.TileConditions
                     .Where(condition => condition.ConditionId == effectId && condition.OwnerActorId == context.Enemy.Id)
                     .ToArray())
        {
            EncounterActor? actor = context.Encounter.GetActorAt(condition.Position);

            if (actor != null && context.Encounter.IsHostile(context.Enemy, actor))
            {
                changed |= context.Encounter.TryDamageActor(actor.Id, amount);
            }

        }

        return BehaviorAtomResult.Next(changed);
    }

    private static BehaviorAtomResult ClearOwnedTileEffects(string effectId, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return BehaviorAtomResult.Next();
        }

        bool changed = false;

        foreach (TileCondition condition in context.Encounter.TileConditions
                     .Where(condition => condition.ConditionId == effectId && condition.OwnerActorId == context.Enemy.Id)
                     .ToArray())
        {
            context.Encounter.RemoveTileCondition(condition.Position, condition.ConditionId, context.Enemy.Id);
            changed = true;
        }

        return BehaviorAtomResult.Next(changed);
    }

    private static BehaviorAtomResult PreventEventDamage(BehaviorExecutionContext context)
    {
        EffectCommandResult result = context.Resolve(new PreventEventDamageCommand());
        context.Trace.Add("Prevented the current damage event.");
        return BehaviorAtomResult.Next(result.ChangedWorld);
    }

    private BehaviorAtomResult UnknownAtom(string op, BehaviorExecutionContext context, int depth)
    {
        if (BehaviorPrimitiveCatalog.TryGet(op, out BehaviorPrimitiveDefinition? primitive)
            && !string.IsNullOrWhiteSpace(primitive.BehaviorId))
        {
            BehaviorExecutionResult result = ExecuteBehavior(primitive.BehaviorId, context, depth + 1);
            return new BehaviorAtomResult(result.Flow, result.ChangedWorld);
        }

        context.Trace.Add($"Unknown behavior atom '{op}'.");
        return BehaviorAtomResult.Next();
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

    private static EncounterActor? GetSelf(BehaviorExecutionContext context)
    {
        return context.Caster ?? context.Enemy ?? context.EventActor;
    }

    private static bool DoesEffectAllowCounter(EffectInstance instance, string counterId)
    {
        return EffectDefinitionCatalog.TryGet(instance.EffectId, out EffectDefinition? definition)
            && definition.AllowsCounter(counterId);
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

    private static bool TryParseTileState(string stateName, out TileState state)
    {
        return Enum.TryParse(stateName, ignoreCase: true, out state);
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

}
