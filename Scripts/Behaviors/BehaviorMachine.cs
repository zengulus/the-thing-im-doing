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
            ["stop"] = (_, _) => BehaviorAtomResult.Stop(),
            ["focus_selected_target"] = (_, context) => FocusSelectedTarget(context),
            ["focus_nearest_actor"] = (step, context) => FocusNearestActor(step.Relation, context),
            ["focus_self"] = (_, context) => FocusSelf(context),
            ["branch_focus_occupied"] = (_, context) => Branch(IsFocusOccupied(context), "if occupied", context),
            ["branch_focus_clear"] = (_, context) => Branch(IsFocusClear(context), "if clear", context),
            ["apply_focus_condition"] = (step, context) => ApplyFocusCondition(step.Counter, context),
            ["branch_focus_condition"] = (step, context) => BranchFocusCondition(step.Counter, context),
            ["damage_focus_actor"] = (step, context) => DamageFocusActor(step.Amount ?? 1, context),
            ["set_focus_tile_state"] = (step, context) => SetFocusTileState(step.State, context),
            ["push_focus_away_from_self"] = (step, context) => PushFocusAwayFromSelf(step.Amount ?? 1, context),
            ["pull_focus_toward_self"] = (step, context) => PullFocusTowardSelf(step.Amount ?? 1, context),
            ["store_focus_ref"] = (step, context) => StoreFocusRef(step.Ref, context),
            ["focus_ref"] = (step, context) => FocusRef(step.Ref, context),
            ["add_focus_actor_counter"] = (step, context) => AddFocusActorCounter(step.Counter, step.Amount ?? 1, context),
            ["add_focus_tile_counter"] = (step, context) => AddFocusTileCounter(step.Counter, step.Amount ?? 1, context),
            ["add_caster_counter"] = (step, context) => AddCasterCounter(step.Counter, step.Amount ?? 1, context),
            ["add_self_counter"] = (step, context) => AddSelfCounter(step.Counter, step.Amount ?? 1, context),
            ["attach_lingering_to_focus"] = (step, context) => AttachLingeringToFocus(step.Effect, step.Amount ?? 1, context),
            ["add_focus_lingering_counter"] = (step, context) => AddFocusLingeringCounter(step.Effect, step.Counter, step.Amount ?? 1, context),
            ["consume_focus_lingering_counter"] = (step, context) => ConsumeFocusLingeringCounter(step.Effect, step.Counter, step.Amount ?? 1, context),
            ["add_lingering_counter"] = (step, context) => AddLingeringCounter(step.Counter, step.Amount ?? 1, context),
            ["consume_lingering_counter"] = (step, context) => ConsumeLingeringCounter(step.Counter, step.Amount ?? 1, context),
            ["consume_focus_actor_counter"] = (step, context) => ConsumeFocusActorCounter(step.Counter, step.Amount ?? 1, context),
            ["consume_caster_counter"] = (step, context) => ConsumeCasterCounter(step.Counter, step.Amount ?? 1, context),
            ["consume_self_counter"] = (step, context) => ConsumeSelfCounter(step.Counter, step.Amount ?? 1, context),
            ["branch_focus_actor_counter_at_least"] = (step, context) => BranchFocusActorCounterAtLeast(step.Counter, step.Amount ?? 1, context),
            ["branch_focus_tile_counter_at_least"] = (step, context) => BranchFocusTileCounterAtLeast(step.Counter, step.Amount ?? 1, context),
            ["branch_self_counter_at_least"] = (step, context) => BranchSelfCounterAtLeast(step.Counter, step.Amount ?? 1, context),
            ["branch_lingering_counter_at_least"] = (step, context) => BranchLingeringCounterAtLeast(step.Counter, step.Amount ?? 1, context),
            ["branch_lingering_target_counter_at_least"] = (step, context) => BranchLingeringTargetCounterAtLeast(step.Counter, step.Amount ?? 1, context),
            ["branch_focus_adjacent_to_self"] = (_, context) => BranchFocusAdjacentToSelf(context),
            ["step_self_toward_focus"] = (_, context) => StepSelfTowardFocus(context),
            ["damage_actors_on_owned_tile_condition"] = (step, context) => DamageActorsOnOwnedTileCondition(step.Counter, step.Amount ?? 1, context),
            ["clear_owned_tile_condition"] = (step, context) => ClearOwnedTileCondition(step.Counter, context),
            ["apply_focus_tile_condition"] = (step, context) => ApplyFocusTileCondition(step.Counter, context),
            ["heal_self"] = (step, context) => HealSelf(step.Amount ?? 1, context),
            ["branch_self_adjacent_blocking"] = (_, context) => BranchSelfAdjacentBlocking(context),
            ["set_first_adjacent_clear_tile_state"] = (step, context) => SetFirstAdjacentClearTileState(step.State, context),
            ["branch_event_tile_state"] = (step, context) => BranchEventTileState(step.State, context),
            ["set_event_tile_state"] = (step, context) => SetEventTileState(step.State, context),
            ["branch_event_actor_hostile_to_lingering_target"] = (_, context) => BranchEventActorHostileToLingeringTarget(context),
            ["damage_event_actor"] = (step, context) => DamageEventActor(step.Amount ?? 1, context),
            ["damage_event_actor_by_lingering_counter"] = (step, context) => DamageEventActorByLingeringCounter(step.Counter, context),
            ["damage_event_actor_by_lingering_target_counter"] = (step, context) => DamageEventActorByLingeringTargetCounter(step.Counter, context),
            ["prevent_event_damage"] = (_, context) => PreventEventDamage(context),
            ["consume_lingering_target_counter"] = (step, context) => ConsumeLingeringTargetCounter(step.Counter, step.Amount ?? 1, context),
            ["detach_lingering"] = (_, context) => DetachLingering(context)
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

    private static BehaviorAtomResult ApplyFocusCondition(string conditionId, BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null || context.Caster == null)
        {
            return BehaviorAtomResult.Next();
        }

        string ownedConditionId = GetOwnedConditionId(conditionId, context.Caster.Id);

        EncounterActor? target = GetFocusActor(context);

        if (target != null)
        {
            context.SpellWorld.AddCounter(target, ownedConditionId, 1);
            context.Working.RecordCounterMutation(context.Caster, target, ownedConditionId, 1);
            context.Trace.Add($"Applied {conditionId} to actor {target.Id}.");
            return BehaviorAtomResult.Next(changedWorld: true);
        }

        if (context.Working.FocusTile.HasValue && context.SpellWorld.IsInside(context.Working.FocusTile.Value))
        {
            context.SpellWorld.AddCounter(context.Working.FocusTile.Value, ownedConditionId, 1);
            context.Trace.Add($"Applied {conditionId} to tile {context.Working.FocusTile.Value}.");
            return BehaviorAtomResult.Next(changedWorld: true);
        }

        context.Trace.Add($"Tried to apply {conditionId}, but nothing was focused.");
        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult BranchFocusCondition(string conditionId, BehaviorExecutionContext context)
    {
        return Branch(IsFocusConditionApplied(conditionId, context), $"if {conditionId}", context);
    }

    private static BehaviorAtomResult DamageFocusActor(int amount, BehaviorExecutionContext context)
    {
        if (context.SpellWorld == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);

        if (target == null)
        {
            context.Trace.Add("Damage found no focused actor.");
            return BehaviorAtomResult.Next();
        }

        context.SpellWorld.ApplyDamage(target, amount, context.Caster);
        context.Trace.Add($"Damaged enemy {target.Id} for {amount}.");
        return BehaviorAtomResult.Next(changedWorld: true);
    }

    private static BehaviorAtomResult SetFocusTileState(string stateName, BehaviorExecutionContext context)
    {
        if (context.Working == null || context.SpellWorld == null)
        {
            return BehaviorAtomResult.Next();
        }

        if (!context.Working.FocusTile.HasValue)
        {
            context.Trace.Add("Raise stone failed because no tile was focused.");
            return BehaviorAtomResult.Next();
        }

        if (!TryParseTileState(stateName, out TileState state))
        {
            context.Trace.Add($"Set tile state failed because '{stateName}' is not a tile state.");
            return BehaviorAtomResult.Next();
        }

        if (!context.SpellWorld.CanSetTileState(context.Working.FocusTile.Value, state))
        {
            context.Trace.Add($"Set tile state failed at {context.Working.FocusTile.Value}.");
            return BehaviorAtomResult.Next();
        }

        context.SpellWorld.SetTileState(context.Working.FocusTile.Value, state);
        context.Trace.Add($"Set tile {context.Working.FocusTile.Value} to {state}.");
        return BehaviorAtomResult.Next(changedWorld: true);
    }

    private static BehaviorAtomResult PushFocusAwayFromSelf(int distance, BehaviorExecutionContext context)
    {
        return MoveFocusRelativeToSelf(distance, awayFromSelf: true, context);
    }

    private static BehaviorAtomResult PullFocusTowardSelf(int distance, BehaviorExecutionContext context)
    {
        return MoveFocusRelativeToSelf(distance, awayFromSelf: false, context);
    }

    private static BehaviorAtomResult MoveFocusRelativeToSelf(int distance, bool awayFromSelf, BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);

        if (context.SpellWorld == null || self == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);

        if (target == null)
        {
            context.Trace.Add("Push found no focused actor.");
            return BehaviorAtomResult.Next();
        }

        Direction direction = awayFromSelf
            ? GetDirectionAwayFrom(self.Position, target.Position)
            : GetDirectionAwayFrom(target.Position, self.Position);
        bool pushed = context.SpellWorld.TryPushActor(target, direction, distance, self);
        context.Trace.Add(pushed
            ? $"Moved actor {target.Id} {direction.ToString().ToLowerInvariant()}."
            : $"Forced movement against actor {target.Id} failed.");
        return BehaviorAtomResult.Next(pushed);
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

    private static BehaviorAtomResult AddFocusActorCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId) || context.SpellWorld == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);

        if (target == null)
        {
            context.Trace.Add($"Tried to add {counterId}, but no actor was focused.");
            return BehaviorAtomResult.Next();
        }

        int next = context.SpellWorld.AddCounter(target, counterId, amount);
        context.Working?.RecordCounterMutation(context.Caster ?? target, target, counterId, amount);
        context.Trace.Add($"Set {counterId} on actor {target.Id} to {next}.");
        return BehaviorAtomResult.Next(changedWorld: amount != 0);
    }

    private static BehaviorAtomResult AddFocusTileCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId) || context.SpellWorld == null || context.Working?.FocusTile == null)
        {
            return BehaviorAtomResult.Next();
        }

        GridPos position = context.Working.FocusTile.Value;
        int next = context.SpellWorld.AddCounter(position, counterId, amount);
        context.Trace.Add($"Set {counterId} on tile {position} to {next}.");
        return BehaviorAtomResult.Next(changedWorld: amount != 0);
    }

    private static BehaviorAtomResult AddCasterCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId) || context.SpellWorld == null || context.Caster == null)
        {
            return BehaviorAtomResult.Next();
        }

        int next = context.SpellWorld.AddCounter(context.Caster, counterId, amount);
        context.Working?.RecordCounterMutation(context.Caster, context.Caster, counterId, amount);
        context.Trace.Add($"Set {counterId} on caster to {next}.");
        return BehaviorAtomResult.Next(changedWorld: amount != 0);
    }

    private static BehaviorAtomResult AddSelfCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);

        if (string.IsNullOrWhiteSpace(counterId) || self == null)
        {
            return BehaviorAtomResult.Next();
        }

        int next = self.Counters.Add(counterId, amount);
        context.Trace.Add($"Set {counterId} on self to {next}.");
        return BehaviorAtomResult.Next(changedWorld: amount != 0);
    }

    private static BehaviorAtomResult AttachLingeringToFocus(string effectId, int stacks, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(effectId) || context.SpellWorld == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? owner = GetSelf(context);
        EncounterActor? target = GetFocusActor(context);

        if (owner == null || target == null)
        {
            context.Trace.Add($"Tried to attach {effectId}, but no actor was focused.");
            return BehaviorAtomResult.Next();
        }

        LingeringEffectInstance? instance = context.SpellWorld.AttachLingeringEffect(target, effectId, owner, stacks);

        if (instance == null)
        {
            context.Trace.Add($"Tried to attach unknown lingering effect {effectId}.");
            return BehaviorAtomResult.Next();
        }

        context.Trace.Add($"Attached {effectId} to actor {target.Id} with {stacks} stack(s).");
        return BehaviorAtomResult.Next(changedWorld: true);
    }

    private static BehaviorAtomResult AddFocusLingeringCounter(
        string effectId,
        string counterId,
        int amount,
        BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(effectId)
            || string.IsNullOrWhiteSpace(counterId)
            || !LingeringEffectDefinitionCatalog.TryGet(effectId, out LingeringEffectDefinition? definition)
            || !definition.AllowsCounter(counterId))
        {
            context.Trace.Add($"Tried to add {counterId} to {effectId}, but that counter is not part of the effect type.");
            return BehaviorAtomResult.Next();
        }

        EncounterActor? target = GetFocusActor(context);
        LingeringEffectInstance? instance = target?.FindLingeringEffect(effectId);

        if (target == null || instance == null)
        {
            context.Trace.Add($"Tried to add {counterId} to {effectId}, but the focused actor did not have that effect.");
            return BehaviorAtomResult.Next();
        }

        int next = instance.Counters.Add(counterId, amount);
        context.Trace.Add($"Set {counterId} on {effectId} attached to actor {target.Id} to {next}.");
        return BehaviorAtomResult.Next(changedWorld: amount != 0);
    }

    private static BehaviorAtomResult ConsumeFocusLingeringCounter(
        string effectId,
        string counterId,
        int amount,
        BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(effectId)
            || string.IsNullOrWhiteSpace(counterId)
            || amount <= 0
            || !LingeringEffectDefinitionCatalog.TryGet(effectId, out LingeringEffectDefinition? definition)
            || !definition.AllowsCounter(counterId))
        {
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        EncounterActor? target = GetFocusActor(context);
        LingeringEffectInstance? instance = target?.FindLingeringEffect(effectId);

        if (instance == null || instance.Counters.Get(counterId) < amount)
        {
            context.Trace.Add($"Tried to consume {counterId} from {effectId}, but the focused actor did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        int next = instance.Counters.Add(counterId, -amount);
        context.Trace.Add($"Consumed {amount} {counterId} from {effectId}; now {next}.");
        return new BehaviorAtomResult(BehaviorFlow.True, true);
    }

    private static BehaviorAtomResult AddLingeringCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId)
            || context.LingeringEffect == null
            || !DoesLingeringEffectAllowCounter(context.LingeringEffect, counterId))
        {
            return BehaviorAtomResult.Next();
        }

        int next = context.LingeringEffect.Counters.Add(counterId, amount);
        context.Trace.Add($"Set {counterId} on lingering effect to {next}.");
        return BehaviorAtomResult.Next(changedWorld: amount != 0);
    }

    private static BehaviorAtomResult ConsumeLingeringCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId)
            || context.LingeringEffect == null
            || !DoesLingeringEffectAllowCounter(context.LingeringEffect, counterId)
            || amount <= 0
            || context.LingeringEffect.Counters.Get(counterId) < amount)
        {
            context.Trace.Add($"Tried to consume {counterId}, but the lingering effect did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        int next = context.LingeringEffect.Counters.Add(counterId, -amount);
        context.Trace.Add($"Consumed {amount} {counterId} from lingering effect; now {next}.");
        return new BehaviorAtomResult(BehaviorFlow.True, true);
    }

    private static BehaviorAtomResult ConsumeFocusActorCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId) || context.SpellWorld == null)
        {
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        EncounterActor? target = GetFocusActor(context);

        if (target == null || context.SpellWorld.GetCounter(target, counterId) < amount)
        {
            context.Trace.Add($"Tried to consume {counterId}, but the focused actor did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        int next = context.SpellWorld.AddCounter(target, counterId, -amount);
        context.Working?.RecordCounterMutation(context.Caster ?? target, target, counterId, -amount);
        context.Trace.Add($"Consumed {amount} {counterId} from actor {target.Id}; now {next}.");
        return new BehaviorAtomResult(BehaviorFlow.True, true);
    }

    private static BehaviorAtomResult ConsumeCasterCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(counterId)
            || context.SpellWorld == null
            || context.Caster == null
            || context.SpellWorld.GetCounter(context.Caster, counterId) < amount)
        {
            context.Trace.Add($"Tried to consume {counterId}, but the caster did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        int next = context.SpellWorld.AddCounter(context.Caster, counterId, -amount);
        context.Working?.RecordCounterMutation(context.Caster, context.Caster, counterId, -amount);
        context.Trace.Add($"Consumed {amount} {counterId} from caster; now {next}.");
        return new BehaviorAtomResult(BehaviorFlow.True, true);
    }

    private static BehaviorAtomResult ConsumeSelfCounter(string counterId, int amount, BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);

        if (string.IsNullOrWhiteSpace(counterId) || self == null || self.Counters.Get(counterId) < amount)
        {
            context.Trace.Add($"Tried to consume {counterId}, but self did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        int next = self.Counters.Add(counterId, -amount);
        context.Trace.Add($"Consumed {amount} {counterId} from self; now {next}.");
        return new BehaviorAtomResult(BehaviorFlow.True, true);
    }

    private static BehaviorAtomResult BranchFocusActorCounterAtLeast(string counterId, int amount, BehaviorExecutionContext context)
    {
        bool passed = false;

        if (!string.IsNullOrWhiteSpace(counterId) && context.SpellWorld != null)
        {
            EncounterActor? target = GetFocusActor(context);
            passed = target != null && context.SpellWorld.GetCounter(target, counterId) >= amount;
        }

        return Branch(passed, $"{counterId} at least {amount}", context);
    }

    private static BehaviorAtomResult BranchFocusTileCounterAtLeast(string counterId, int amount, BehaviorExecutionContext context)
    {
        bool passed = !string.IsNullOrWhiteSpace(counterId)
            && context.SpellWorld != null
            && context.Working?.FocusTile != null
            && context.SpellWorld.GetCounter(context.Working.FocusTile.Value, counterId) >= amount;

        return Branch(passed, $"{counterId} at least {amount}", context);
    }

    private static BehaviorAtomResult BranchSelfCounterAtLeast(string counterId, int amount, BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);
        bool passed = !string.IsNullOrWhiteSpace(counterId) && self != null && self.Counters.Get(counterId) >= amount;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult BranchLingeringCounterAtLeast(string counterId, int amount, BehaviorExecutionContext context)
    {
        bool passed = !string.IsNullOrWhiteSpace(counterId)
            && context.LingeringEffect != null
            && DoesLingeringEffectAllowCounter(context.LingeringEffect, counterId)
            && context.LingeringEffect.Counters.Get(counterId) >= amount;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult BranchLingeringTargetCounterAtLeast(
        string counterId,
        int amount,
        BehaviorExecutionContext context)
    {
        bool passed = !string.IsNullOrWhiteSpace(counterId)
            && context.LingeringTarget != null
            && context.LingeringTarget.Counters.Get(counterId) >= amount;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult BranchFocusAdjacentToSelf(BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);
        EncounterActor? focus = GetFocusActor(context);
        bool adjacent = self != null && focus != null && self.Position.ManhattanDistanceTo(focus.Position) == 1;
        return new BehaviorAtomResult(adjacent ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult StepSelfTowardFocus(BehaviorExecutionContext context)
    {
        if (context.Encounter == null)
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? self = GetSelf(context);
        GridPos? destination = context.Working?.FocusTile;

        if (self == null || !destination.HasValue)
        {
            return BehaviorAtomResult.Next();
        }

        foreach (Direction direction in GetDirectionsToward(self.Position, destination.Value))
        {
            if (context.Encounter.TryMoveActor(self, direction))
            {
                return BehaviorAtomResult.Next(changedWorld: true);
            }
        }

        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult DamageActorsOnOwnedTileCondition(string conditionId, int amount, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return BehaviorAtomResult.Next();
        }

        bool changed = false;

        foreach (TileCondition condition in context.Encounter.TileConditions
                     .Where(condition => condition.ConditionId == conditionId && condition.OwnerActorId == context.Enemy.Id)
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

    private static BehaviorAtomResult ClearOwnedTileCondition(string conditionId, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null)
        {
            return BehaviorAtomResult.Next();
        }

        bool changed = false;

        foreach (TileCondition condition in context.Encounter.TileConditions
                     .Where(condition => condition.ConditionId == conditionId && condition.OwnerActorId == context.Enemy.Id)
                     .ToArray())
        {
            context.Encounter.RemoveTileCondition(condition.Position, condition.ConditionId, context.Enemy.Id);
            changed = true;
        }

        return BehaviorAtomResult.Next(changed);
    }

    private static BehaviorAtomResult ApplyFocusTileCondition(string conditionId, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.Enemy == null || context.Working?.FocusTile == null)
        {
            return BehaviorAtomResult.Next();
        }

        context.Encounter.AddTileCondition(context.Working.FocusTile.Value, conditionId, context.Enemy.Id);
        return BehaviorAtomResult.Next(changedWorld: true);
    }

    private static BehaviorAtomResult HealSelf(int amount, BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);

        if (self == null)
        {
            return BehaviorAtomResult.Next();
        }

        int before = self.Health;
        self.Heal(amount);
        return BehaviorAtomResult.Next(self.Health != before);
    }

    private static BehaviorAtomResult BranchSelfAdjacentBlocking(BehaviorExecutionContext context)
    {
        EncounterActor? self = GetSelf(context);
        bool passed = self != null && context.Encounter?.IsAdjacentToBlockingTile(self.Position) == true;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult SetFirstAdjacentClearTileState(string stateName, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || !TryParseTileState(stateName, out TileState state))
        {
            return BehaviorAtomResult.Next();
        }

        EncounterActor? self = GetSelf(context);

        if (self == null)
        {
            return BehaviorAtomResult.Next();
        }

        foreach (GridPos adjacent in TacticalEncounter.GetAdjacentPositions(self.Position))
        {
            if (context.Encounter.Grid.IsEmpty(adjacent))
            {
                context.Encounter.Grid.SetTile(adjacent, state);
                return BehaviorAtomResult.Next(changedWorld: true);
            }
        }

        return BehaviorAtomResult.Next();
    }

    private static BehaviorAtomResult BranchEventTileState(string stateName, BehaviorExecutionContext context)
    {
        TryParseTileState(stateName, out TileState state);
        bool passed = context.Encounter != null
            && context.EventTile.HasValue
            && context.Encounter.Grid.IsInside(context.EventTile.Value)
            && context.Encounter.Grid.GetTile(context.EventTile.Value) == state;
        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult SetEventTileState(string stateName, BehaviorExecutionContext context)
    {
        if (context.Encounter == null
            || !context.EventTile.HasValue
            || !context.Encounter.Grid.IsInside(context.EventTile.Value)
            || !TryParseTileState(stateName, out TileState state))
        {
            return BehaviorAtomResult.Next();
        }

        context.Encounter.Grid.SetTile(context.EventTile.Value, state);
        return BehaviorAtomResult.Next(changedWorld: true);
    }

    private static BehaviorAtomResult DamageEventActor(int amount, BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.EventActor == null)
        {
            return BehaviorAtomResult.Next();
        }

        return BehaviorAtomResult.Next(context.Encounter.TryDamageActor(context.EventActor.Id, amount));
    }

    private static BehaviorAtomResult BranchEventActorHostileToLingeringTarget(BehaviorExecutionContext context)
    {
        bool passed = context.Encounter != null
            && context.EventActor != null
            && context.LingeringTarget != null
            && context.Encounter.IsHostile(context.LingeringTarget, context.EventActor);

        return new BehaviorAtomResult(passed ? BehaviorFlow.True : BehaviorFlow.False, false);
    }

    private static BehaviorAtomResult DamageEventActorByLingeringCounter(string counterId, BehaviorExecutionContext context)
    {
        if (context.Encounter == null
            || context.EventActor == null
            || context.LingeringEffect == null
            || string.IsNullOrWhiteSpace(counterId)
            || !DoesLingeringEffectAllowCounter(context.LingeringEffect, counterId))
        {
            return BehaviorAtomResult.Next();
        }

        int amount = context.LingeringEffect.Counters.Get(counterId);

        if (amount <= 0)
        {
            return BehaviorAtomResult.Next();
        }

        return BehaviorAtomResult.Next(context.Encounter.TryDamageActor(context.EventActor.Id, amount));
    }

    private static BehaviorAtomResult DamageEventActorByLingeringTargetCounter(
        string counterId,
        BehaviorExecutionContext context)
    {
        if (context.Encounter == null
            || context.EventActor == null
            || context.LingeringTarget == null
            || string.IsNullOrWhiteSpace(counterId))
        {
            return BehaviorAtomResult.Next();
        }

        int amount = context.LingeringTarget.Counters.Get(counterId);

        if (amount <= 0)
        {
            return BehaviorAtomResult.Next();
        }

        return BehaviorAtomResult.Next(context.Encounter.TryDamageActor(context.EventActor.Id, amount));
    }

    private static BehaviorAtomResult PreventEventDamage(BehaviorExecutionContext context)
    {
        if (context.EventDamage <= 0)
        {
            return BehaviorAtomResult.Next();
        }

        context.EventDamage = 0;
        context.Trace.Add("Prevented the current damage event.");
        return BehaviorAtomResult.Next(changedWorld: true);
    }

    private static BehaviorAtomResult ConsumeLingeringTargetCounter(
        string counterId,
        int amount,
        BehaviorExecutionContext context)
    {
        if (context.LingeringTarget == null
            || string.IsNullOrWhiteSpace(counterId)
            || amount <= 0
            || context.LingeringTarget.Counters.Get(counterId) < amount)
        {
            context.Trace.Add($"Tried to consume {counterId}, but the lingering target did not have enough.");
            return new BehaviorAtomResult(BehaviorFlow.False, false);
        }

        int next = context.LingeringTarget.Counters.Add(counterId, -amount);
        context.Trace.Add($"Consumed {amount} {counterId} from lingering target; now {next}.");
        return new BehaviorAtomResult(BehaviorFlow.True, true);
    }

    private static BehaviorAtomResult DetachLingering(BehaviorExecutionContext context)
    {
        if (context.Encounter == null || context.LingeringTarget == null || context.LingeringEffect == null)
        {
            return BehaviorAtomResult.Next();
        }

        return BehaviorAtomResult.Next(context.Encounter.DetachLingeringEffect(
            context.LingeringTarget.Id,
            context.LingeringEffect.InstanceId));
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

    private static bool IsFocusConditionApplied(string conditionId, BehaviorExecutionContext context)
    {
        if (context.SpellWorld == null || context.Working == null || context.Caster == null)
        {
            return false;
        }

        string ownedConditionId = GetOwnedConditionId(conditionId, context.Caster.Id);

        EncounterActor? focusActor = GetFocusActor(context);

        if (focusActor != null)
        {
            return context.SpellWorld.GetCounter(focusActor, ownedConditionId) > 0;
        }

        return context.Working.FocusTile.HasValue
            && context.SpellWorld.GetCounter(context.Working.FocusTile.Value, ownedConditionId) > 0;
    }

    private static string GetOwnedConditionId(string conditionId, int ownerActorId)
    {
        return conditionId.Contains(".owner.")
            ? conditionId
            : $"{conditionId}.owner.{ownerActorId}";
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

    private static EncounterActor? GetSelf(BehaviorExecutionContext context)
    {
        return context.Caster ?? context.Enemy ?? context.EventActor;
    }

    private static bool DoesLingeringEffectAllowCounter(LingeringEffectInstance instance, string counterId)
    {
        return LingeringEffectDefinitionCatalog.TryGet(instance.EffectId, out LingeringEffectDefinition? definition)
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
